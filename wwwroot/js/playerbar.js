let totalDuration = 0;
let currentProgress = 0;
let isDragging = false;

const progressTrack = document.getElementById('progressTrack');
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

setInterval(async () => {

    if (isDragging)
        return;

    try {

        const response =
            await fetch('/Player/Status');

        const data =
            await response.json();

        totalDuration =
            data.duration > 0
                ? data.duration
                : 0;

        const currentTime =
            data.currentTime > 0
                ? data.currentTime
                : 0;

        const fraction =
            totalDuration > 0
                ? currentTime / totalDuration
                : 0;

        setProgress(fraction);

        if (!isVolumeDragging) {
            updateMuteBtnUIFromVolume(data.volume);
        }

        if (!isVolumeDragging && data.volume !== undefined) {
            setVolumeUI(data.volume);
            updateMuteBtnUIFromVolume(data.volume);
        }

    } catch (err) {

        console.error(err);
    }

}, 250);

function getProgressFraction(event) {

    const rect =
        progressTrack.getBoundingClientRect();

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

    event.preventDefault();

    isDragging = true;

    const fraction =
        getProgressFraction(event);

    setProgress(fraction);

    function onMove(e) {

        e.preventDefault();

        setProgress(
            getProgressFraction(e));
    }

    async function onEnd(e) {

        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('touchmove', onMove);

        document.removeEventListener('mouseup', onEnd);
        document.removeEventListener('touchend', onEnd);

        isDragging = false;

        const seekSeconds =
            getProgressFraction(e) * totalDuration;

        await sendSeekPosition(seekSeconds);
        setProgress(getProgressFraction(e), true);
    }

    document.addEventListener('mousemove', onMove);

    document.addEventListener(
        'touchmove',
        onMove,
        { passive: false });

    document.addEventListener('mouseup', onEnd);

    document.addEventListener('touchend', onEnd);
}

progressTrack.addEventListener('mousedown', onProgressStart);

progressTrack.addEventListener(
    'touchstart',
    onProgressStart,
    { passive: false }
);

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


const volumeTrack = document.getElementById('volumeTrack');
const volumeFill = document.getElementById('volumeFill');

let volume = 100;
let isVolumeDragging = false;

function setVolumeUI(value) {

    volume = Math.max(0, Math.min(100, value));

    volumeFill.style.width = `${volume}%`;

    updateMuteBtnUIFromVolume(volume);
}

function getVolumeFraction(event) {

    const rect = volumeTrack.getBoundingClientRect();

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

volumeTrack.addEventListener('mousedown', onVolumeStart);
volumeTrack.addEventListener('touchstart', onVolumeStart, { passive: false });

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

        await updatePlayBtnUI(data);

    } catch (err) {
        console.error(err);
    }
});

async function updatePlayBtnUI(data) {
    const playBtn = document.getElementById('playPauseBtn');

    if (playBtn) {
        playBtn.innerHTML = data.isPlaying
            ? pauseIconSVG()
            : playIconSVG();
    }
}

// Mute / Unmute
document.getElementById('muteBtn').addEventListener('click', async () => {

    const newVolume = volume > 0 ? 0 : 100;

    setVolumeUI(newVolume);
    await sendVolume(newVolume);
});

function updateMuteBtnUIFromVolume(vol) {
    const muteBtn = document.getElementById('muteBtn');
    if (!muteBtn) return;

    const shouldBeMuted = vol === 0;

    const currentIsMuted =
        muteBtn.dataset.muted === "1";

    if (currentIsMuted === shouldBeMuted) return;

    muteBtn.dataset.muted = shouldBeMuted ? "1" : "0";

    muteBtn.innerHTML = shouldBeMuted
        ? mutedIconSVG()
        : unmutedIconSVG();
}