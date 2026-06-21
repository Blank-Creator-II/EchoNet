let totalDuration = 0;
let currentProgress = 0;
let isDragging = false;
let isVolumeDragging = false;

const progressSong = document.getElementById('progressSong');
const progressFill = document.getElementById('progressFill');
const progressThumb = document.getElementById('progressThumb');
const currentTimeEl = document.getElementById('currentTime');
const durationTimeEL = document.getElementById('durationTime');

function formatTime(seconds) {

    if (!Number.isFinite(seconds) || seconds < 0) {
        return '0:00';
    }

    const mins = Math.floor(seconds / 60);

    const secs = Math.floor(seconds % 60);

    return `${mins}:${String(secs).padStart(2, '0')}`;
}

function setProgress(fraction) {

    currentProgress = Math.max(0, Math.min(1, fraction));

    progressFill.style.width =
        `${currentProgress * 100}%`;

    progressThumb.style.left =
        `${currentProgress * 100}%`;

    const currentSeconds =
        totalDuration > 0
            ? currentProgress * totalDuration
            : 0;

    currentTimeEl.textContent =
        formatTime(currentSeconds);

    durationTimeEL.textContent = 
        formatTime(totalDuration);
}

// -- signalR arch --
const audioConnection = new signalR.HubConnectionBuilder()
    .withUrl("/audioHub")
    .withAutomaticReconnect() // Automatically handles dropouts
    .build();

// Handle stream updates sent directly from VLC's TimeChanged event
audioConnection.on("ReceiveStatus", (data) => {
    window.__playerStatus = data;

    // Sync state to visual elements 
    window.dispatchEvent(new CustomEvent('playerStatusUpdated', { detail: data }));

    // Do not fight the user's cursor positions while they are dragging the progress slider
    if (isDragging) return;

    totalDuration = data.duration > 0 ? data.duration : 0;
    const currentTime = data.currentTime > 0 ? data.currentTime : 0;
    const fraction = totalDuration > 0 ? currentTime / totalDuration : 0;

    setProgress(fraction);
});

// Handle sudden state events (Play, Pause, Stop)
audioConnection.on("ReceiveStateChange", (data) => {
    window.__playerStatus = window.__playerStatus || {};
    window.__playerStatus.isPlaying = data.isPlaying;
    window.__playerStatus.isSeekable = data.isSeekable;
    window.__playerStatus.id = data.id;

    updatePlayBtnUI(data);

    // Sync tracklist row instantly when playback state registers
    if (data.id) {
        window.dispatchEvent(new CustomEvent('playerStatusUpdated', { detail: data }));
    }
});

// Handle song change
audioConnection.on("ReceiveMediaChange", async (data) => {
    await updateSongInfo(data.song);

    // Force an immediate UI highlight refresh on automated track change
    if (data.song) {
        window.dispatchEvent(new CustomEvent('playerStatusUpdated', { 
            detail: { id: data.song.id || data.song.Id } 
        }));
    }
});

// Start the real-time websocket connection loop
audioConnection.start()
    .then(() => console.log("Real-time Audio Sync Active via SignalR"))
    .catch(err => console.error("SignalR Init Failure: ", err));

function getProgressFraction(event) {

    const rect =
        progressSong.getBoundingClientRect();

    const clientX =
        event.touches
            ? event.touches[0].clientX
            : event.clientX;

    const x =
        clientX - rect.left;

    return Math.max(0,
        Math.min(1, x / rect.width));
}

function onProgressStart(event) {
    if(!window.__playerStatus.isSeekable)
    {
        return;
    }

    event.preventDefault();
    isDragging = true;
    setProgress(getProgressFraction(event));

    function onMove(e) {
        e.preventDefault();
        setProgress(getProgressFraction(e));
    }

    async function onEnd(e) {
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('touchmove', onMove);
        document.removeEventListener('mouseup', onEnd);
        document.removeEventListener('touchend', onEnd);

        const finalFraction = getProgressFraction(e);
        const seekSeconds = finalFraction * totalDuration;

        // Set UI matching user's drop spot instantly
        setProgress(finalFraction);

        // Send to server
        await sendSeekPosition(seekSeconds);
        
        // Allow polling to resume safely AFTER the server processing is done
        isDragging = false; 
    }

    document.addEventListener('mousemove', onMove);
    document.addEventListener('touchmove', onMove, { passive: false });
    document.addEventListener('mouseup', onEnd);
    document.addEventListener('touchend', onEnd);
}

progressSong.addEventListener('mousedown', onProgressStart);

progressSong.addEventListener('touchstart', onProgressStart,{ passive: false });

async function sendSeekPosition(seconds) {

    try {

        const response = await fetch('/Player/Seek', {

            method: 'POST',

            headers: {
                'Content-Type': 'application/json'
            },

            body: JSON.stringify({
                position: seconds
            })
        });

        if (!response.ok) {
            console.error('Seek failed');
        }

    } catch (err) {

        console.error(err);
    }
}


const volumeSong = document.getElementById('volumeSong');
const volumeFill = document.getElementById('volumeFill');

let volume = parseFloat(document.getElementById('muteBtn').dataset.volume) || 0;

function setVolumeUI(value) {

    volume = Math.max(0, Math.min(100, value));

    volumeFill.style.width = `${volume}%`;

    updateMuteBtnUIFromVolume(volume);
}

function getVolumeFraction(event) {

    const rect = volumeSong.getBoundingClientRect();

    const clientX =
        event.touches
            ? event.touches[0].clientX
            : event.clientX;

    const x = clientX - rect.left;

    return Math.max(0, Math.min(1, x / rect.width));
}

function onVolumeStart(event) {

    event.preventDefault();

    isVolumeDragging = true;

    const fraction = getVolumeFraction(event);

    setVolumeUI(fraction * 100);

    function onMove(e) {
        setVolumeUI(getVolumeFraction(e) * 100);
    }

    async function onEnd(e) {

        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('touchmove', onMove);

        document.removeEventListener('mouseup', onEnd);
        document.removeEventListener('touchend', onEnd);

        isVolumeDragging = false;

        const finalVolume = Math.round(getVolumeFraction(e) * 100);

        await sendVolume(finalVolume);
    }

    document.addEventListener('mousemove', onMove);
    document.addEventListener('touchmove', onMove, { passive: false });

    document.addEventListener('mouseup', onEnd);
    document.addEventListener('touchend', onEnd);
}

volumeSong.addEventListener('mousedown', onVolumeStart);
volumeSong.addEventListener('touchstart', onVolumeStart, { passive: false });

async function sendVolume(value) {

    try {

        await fetch('/Player/Volume', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ volume: value })
        });

    } catch (err) {
        console.error(err);
    }
}

function playIconSVG() {
    return `
        <svg xmlns="http://www.w3.org/2000/svg" height="34px" viewBox="0 -960 960 960" width="34px" fill="var(--bg)">
            <path d="M320-200v-560l440 280-440 280Z"/>
        </svg>
    `;
}

function pauseIconSVG() {
    return `
        <svg xmlns="http://www.w3.org/2000/svg" height="34px" viewBox="0 -960 960 960" width="34px" fill="var(--bg)">
            <path d="M240-200v-560h160v560H240Zm320 0v-560h160v560H560Z"/>
        </svg>
    `;
}

function mutedIconSVG() {
    return `
        <svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 -960 960 960" width="24px" fill="var(--button-secondary-text)">
            <path d="M792-56 671-177q-25 16-53 27.5T560-131v-82q14-5 27.5-10t25.5-12L480-368v208L280-360H120v-240h128L56-792l56-56 736 736-56 56Zm-8-232-58-58q17-31 25.5-65t8.5-70q0-94-55-168T560-749v-82q124 28 202 125.5T840-481q0 53-14.5 102T784-288ZM650-422l-90-90v-130q47 22 73.5 66t26.5 96q0 15-2.5 29.5T650-422ZM480-592 376-696l104-104v208Zm-80 238v-94l-72-72H200v80h114l86 86Zm-36-130Z"/>
        </svg>
    `;
}

function unmutedIconSVG() {
    return `
        <svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 -960 960 960" width="24px" fill="var(--button-secondary-text)">
            <path d="M560-131v-82q90-26 145-100t55-168q0-94-55-168T560-749v-82q124 28 202 125.5T840-481q0 127-78 224.5T560-131ZM120-360v-240h160l200-200v640L280-360H120Zm440 40v-322q47 22 73.5 66t26.5 96q0 51-26.5 94.5T560-320ZM400-606l-86 86H200v80h114l86 86v-252ZM300-480Z"/>
        </svg>
    `;
}

function shuffleOnIconSVG() {
    return `
        <svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 -960 960 960" width="24px" fill="var(--accent)">
            <path d="M120-40q-33 0-56.5-23.5T40-120v-720q0-33 23.5-56.5T120-920h720q33 0 56.5 23.5T920-840v720q0 33-23.5 56.5T840-40H120Zm440-120h240v-240h-80v102L594-424l-57 57 127 127H560v80Zm-344 0 504-504v104h80v-240H560v80h104L160-216l56 56Zm151-377 56-56-207-207-56 56 207 207Z"/>
        </svg>
    `;
}

function shuffleOffIconSVG() {
    return `
        <svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 -960 960 960" width="24px" fill="var(--button-secondary-text)">
            <path d="M560-160v-80h104L537-367l57-57 126 126v-102h80v240H560Zm-344 0-56-56 504-504H560v-80h240v240h-80v-104L216-160Zm151-377L160-744l56-56 207 207-56 56Z"/>
        </svg>
    `;
}

function NoLoopIconSVG() {
    return `
        <svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 -960 960 960" width="24px" fill="var(--button-secondary-text)">
            <path d="M280-80 120-240l160-160 56 58-62 62h406v-160h80v240H274l62 62-56 58Zm-80-440v-240h486l-62-62 56-58 160 160-160 160-56-58 62-62H280v160h-80Z"/>
        </svg>
    `;
}

function LoopIconSVG() {
    return `
        <svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 -960 960 960" width="24px" fill="var(--accent)">
            <path d="M120-40q-33 0-56.5-23.5T40-120v-720q0-33 23.5-56.5T120-920h720q33 0 56.5 23.5T920-840v720q0 33-23.5 56.5T840-40H120Zm160-40 56-58-62-62h486v-240h-80v160H274l62-62-56-58-160 160L280-80Zm-80-440h80v-160h406l-62 62 56 58 160-160-160-160-56 58 62 62H200v240Z"/>
        </svg>
    `;
}

function LoopOnceIconSVG() {
    return `
        <svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 -960 960 960" width="24px" fill="var(--accent)">
            <path d="M120-40q-33 0-56.5-23.5T40-120v-720q0-33 23.5-56.5T120-920h720q33 0 56.5 23.5T920-840v720q0 33-23.5 56.5T840-40H120Zm160-40 56-58-62-62h486v-240h-80v160H274l62-62-56-58-160 160L280-80Zm-80-440h80v-160h406l-62 62 56 58 160-160-160-160-56 58 62 62H200v240Zm260 160h60v-240H400v60h60v180Z"/>
        </svg>
    `;
}


// Play / Pause
document.getElementById('playPauseBtn').addEventListener('click', async () => {
    try {
        const response = await fetch('/Player/TogglePlay', {
            method: 'POST'
        });

        if (!response.ok) {
            console.error('Play/Pause failed');
        }

        const data = await response.json();

        if (data.isSeekable)
            updatePlayBtnUI(data);

    } catch (err) {
        console.error(err);
    }
});

function updatePlayBtnUI(data) {
    const playBtn = document.getElementById('playPauseBtn');

    if (playBtn) { playBtn.innerHTML = data.isPlaying ? pauseIconSVG() : playIconSVG(); }
}

// Shuffle
document.getElementById('shuffleBtn').addEventListener('click', async () => {
    try {
        const response = await fetch('/Player/ToggleShuffle', {
            method: 'POST'
        });

        const data = await response.json();

        updateShuffleBtnUI(data);

    } catch (err) {
        console.error(err);
    }
})

function updateShuffleBtnUI(data) {
    const shuffleBtn = document.getElementById('shuffleBtn');

    if (shuffleBtn) { shuffleBtn.innerHTML = data.isShuffled ? shuffleOnIconSVG() : shuffleOffIconSVG(); }
}

// Previous
document.getElementById('previousSongBtn').addEventListener('click', async () => {
    try {
        const response = await fetch('/Player/Previous', {
            method: 'POST'
        });

    } catch (err) {
        console.error(err);
    }
})

// Next
document.getElementById('nextSongBtn').addEventListener('click', async () => {
    try {
        const response = await fetch('/Player/Next', {
            method: 'POST'
        });

    } catch (err) {
        console.error(err);
    }
})

// Loop
document.getElementById('loopBtn').addEventListener('click', async () => {
    try {
        const response = await fetch('/Player/Loop', {
            method: 'POST'
        });

        const data = await response.json();

        updateLoopBtnUI(data);

    } catch (err) {
        console.error(err);
    }
})


function updateLoopBtnUI(data) {
    const loopBtn = document.getElementById('loopBtn');

    if (loopBtn) {
        if (data.newState === 0) {loopBtn.innerHTML = NoLoopIconSVG();}
        else if (data.newState === 1) {loopBtn.innerHTML = LoopIconSVG();}
        else {loopBtn.innerHTML = LoopOnceIconSVG();}
    }
}

// Mute / Unmute
document.getElementById('muteBtn').addEventListener('click', async () => {

    const isCurrentlyMuted = Number(volume) === 0;
    const newVolume = isCurrentlyMuted ? 100 : 0;

    setVolumeUI(newVolume);
    await sendVolume(newVolume);
});

function updateMuteBtnUIFromVolume(vol) {
    const muteBtn = document.getElementById('muteBtn');
    if (!muteBtn) return;

    const shouldBeMuted = vol === 0;
    const currentIsMuted = muteBtn.dataset.muted === "1";

    if (currentIsMuted === shouldBeMuted) return;

    muteBtn.dataset.muted = shouldBeMuted ? "1" : "0";
    muteBtn.innerHTML = shouldBeMuted ? mutedIconSVG() : unmutedIconSVG();
}

async function updateSongInfo(song) {

    const titleEl = document.querySelector('.song-title');
    const artistEl = document.querySelector('.song-artist');
    const coverArtContainer = document.getElementById('playerSongThumb');

    if (titleEl) {
        titleEl.textContent = song.title;
    }

    if (artistEl) {
        artistEl.textContent =
            song.artist || 'Unknown Artist';
    }

    if (coverArtContainer) {
        // If cover art exists and isn't just an empty string/null
        if (song.hasCoverArt) {
            coverArtContainer.innerHTML = `
                <img class="song-thumb" src="${song.coverArtDirectory}_256.jpg" alt="" loading="lazy" />
            `;
        } else {
            // Fallback placeholder directly if no string data is returned
            coverArtContainer.innerHTML = '<div class="playbar-artwork-placeholder"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" fill="var(--accent)"><path d="M480-254 330-104q-23 23-56 23t-56-23L104-218q-23-23-23-56t23-56l150-150-150-150q-23-23-23-56t23-56l114-114q23-23 56-23t56 23l150 150 150-150q23-23 56-23t56 23l114 114q23 23 23 56t-23 56L706-480l150 150q23 23 23 56t-23 56L742-104q-23 23-56 23t-56-23L480-254Zm28.5-277.5Q520-543 520-560t-11.5-28.5Q497-600 480-600t-28.5 11.5Q440-577 440-560t11.5 28.5Q463-520 480-520t28.5-11.5ZM310-536l114-114-150-150-114 114 150 150Zm90 96q17 0 28.5-11.5T440-480q0-17-11.5-28.5T400-520q-17 0-28.5 11.5T360-480q0 17 11.5 28.5T400-440Zm108.5 68.5Q520-383 520-400t-11.5-28.5Q497-440 480-440t-28.5 11.5Q440-417 440-400t11.5 28.5Q463-360 480-360t28.5-11.5ZM560-440q17 0 28.5-11.5T600-480q0-17-11.5-28.5T560-520q-17 0-28.5 11.5T520-480q0 17 11.5 28.5T560-440Zm-24 130 150 150 114-114-150-150-114 114ZM339-621Zm282 282Z"/></svg></div>';
        }
    }

    await savePlayerState(song);
}

async function savePlayerState(song) {
    try {

        const response = await fetch('/Player/SaveState', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(song)
        });

        if (!response.ok) {
            console.error('PlayerState saving failed');
            return;
        }

    } catch (err) {
        console.error(err);
    }
}

document.addEventListener('DOMContentLoaded', async () => {
    try {
        // Check the server status once on page boot
        const response = await fetch('/Player/Status');
        if (!response.ok) throw new Error('Initial status check failed');
        const data = await response.json();

        
        window.__playerStatus = data;

        // Sync the playbar timeline instantly
        totalDuration = data.duration > 0 ? data.duration : 0;
        const currentTime = data.currentTime > 0 ? data.currentTime : 0;
        const fraction = totalDuration > 0 ? currentTime / totalDuration : 0;
        
        setProgress(fraction);
        updatePlayBtnUI(data);

        // Broadcast this so rendering functions catches the layout initialization update
        window.dispatchEvent(new CustomEvent('playerStatusUpdated', { detail: data }));

    } catch (err) {
        console.error("Failed to auto-heal player session on page load:", err);
    }
});