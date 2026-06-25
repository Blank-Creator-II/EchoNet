var remoteSongClickHandler = window.__remoteSongClickHandler || null;
var currentView = window.__remoteSongPageState[0]; // 'list' or 'grid'
var sortState = window.__remoteSongPageState[1]; // 'newest', 'oldest', 'az', 'za'

// Track songs by device so they can be merged together
var songsByIp = new Map();
var songs = []; 

var tbody = document.getElementById('songsTbody');
var songsGrid = document.getElementById('songsGrid');
var songsTable = document.getElementById('songsTable');
var columnHeaders = document.getElementById('columnHeaders');
var countEl = document.getElementById('songCountDisplay');
var sortGroup = document.getElementById('sortGroup');
var emptyState = document.getElementById('emptyState');
var header = document.getElementById('header');
var tableContainer = document.getElementById('tableContainer');

tableContainer.addEventListener('scroll', function () {
    header.classList.toggle('scrolled', tableContainer.scrollTop > 0);
});

function escapeHTML(str) {
    if (!str) return '';
    var div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

async function sortSongs(type) {
    switch (type) {
        case 'newest':
            songs.sort(function (a, b) { return b.formattedCreatedAt.localeCompare(a.formattedCreatedAt); });
            break;
        case 'oldest':
            songs.sort(function (a, b) { return a.formattedCreatedAt.localeCompare(b.formattedCreatedAt); });
            break;
        case 'az':
            songs.sort(function (a, b) { return a.title.localeCompare(b.title, 'en', { sensitivity: 'base' }); });
            break;
        case 'za':
            songs.sort(function (a, b) { return b.title.localeCompare(a.title, 'en', { sensitivity: 'base' }); });
            break;
        default:
            songs.sort(function (a, b) { return b.formattedCreatedAt.localeCompare(a.formattedCreatedAt); });
    }
    render();
    syncPlayingRow();
}

function render() {
    if (!songs || songs.length === 0) {
        tbody.innerHTML = '';
        songsGrid.innerHTML = '';
        emptyState.style.display = 'block';
        countEl.textContent = '0 songs';
        return;
    }

    emptyState.style.display = 'none';
    countEl.textContent = songs.length + ' song' + (songs.length !== 1 ? 's' : '');
    
    if (currentView === 'list') {
        var rowsHTML = songs.map(function (song, i) {
            if (song.hasCoverArt) {
                var coverUrl = song.hostUrl + '/coverArt/' + song.id + '/64';   
                var artworkHTML = '<img class="artwork-thumb" src="' + escapeHTML(coverUrl) + '" alt="" loading="lazy" />'
            }
            else {
                var artworkHTML = '<div class="artwork-placeholder"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" fill="var(--accent)"><path d="M480-254 330-104q-23 23-56 23t-56-23L104-218q-23-23-23-56t23-56l150-150-150-150q-23-23-23-56t23-56l114-114q23-23 56-23t56 23l150 150 150-150q23-23 56-23t56 23l114 114q23 23 23 56t-23 56L706-480l150 150q23 23 23 56t-23 56L742-104q-23 23-56 23t-56-23L480-254Zm28.5-277.5Q520-543 520-560t-11.5-28.5Q497-600 480-600t-28.5 11.5Q440-577 440-560t11.5 28.5Q463-520 480-520t28.5-11.5ZM310-536l114-114-150-150-114 114 150 150Zm90 96q17 0 28.5-11.5T440-480q0-17-11.5-28.5T400-520q-17 0-28.5 11.5T360-480q0 17 11.5 28.5T400-440Zm108.5 68.5Q520-383 520-400t-11.5-28.5Q497-440 480-440t-28.5 11.5Q440-417 440-400t11.5 28.5Q463-360 480-360t28.5-11.5ZM560-440q17 0 28.5-11.5T600-480q0-17-11.5-28.5T560-520q-17 0-28.5 11.5T520-480q0 17 11.5 28.5T560-440Zm-24 130 150 150 114-114-150-150-114 114ZM339-621Zm282 282Z"/></svg></div>';
            }

            return '<tr class="song-row" data-id="' + song.id + '">' +
                '<td>' +
                    '<div class="row-index-wrap">' +
                        '<span class="row-num">' + (i + 1) + '</span>' +
                        '<span class="row-play-icon">▶</span>' +
                        '<span class="eq-bars" aria-hidden="true"><span></span><span></span><span></span><span></span></span>' +
                    '</div>' +
                '</td>' +
                '<td><div class="artwork-area">' + artworkHTML + '</div></td>' +
                '<td><div class="title-cell"><span class="title-text">' + escapeHTML(song.title) + '</span></div></td>' +
                '<td><span class="artist-cell">' + escapeHTML(song.artist) + '</span></td>' +
                '<td><span class="album-cell">' + escapeHTML(song.album) + '</span></td>' +
                '<td><span class="duration-cell">' + escapeHTML(song.formattedDuration) + '</span></td>' +
            '</tr>';
        }).join('');
        tbody.innerHTML = rowsHTML;
    } else {
        var cardsHTML = songs.map(function (song, i) {
            if (song.hasCoverArt) {
                var coverUrl = song.hostUrl + '/coverArt/' + song.id + '/512';   
                var artworkHTML = '<img class="artwork-thumb" src="' + escapeHTML(coverUrl) + '" alt="" loading="lazy" />'
            }
            else {
                var artworkHTML = '<div class="artwork-placeholder"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" fill="var(--accent)"><path d="M480-254 330-104q-23 23-56 23t-56-23L104-218q-23-23-23-56t23-56l150-150-150-150q-23-23-23-56t23-56l114-114q23-23 56-23t56 23l150 150 150-150q23-23 56-23t56 23l114 114q23 23 23 56t-23 56L706-480l150 150q23 23 23 56t-23 56L742-104q-23 23-56 23t-56-23L480-254Zm28.5-277.5Q520-543 520-560t-11.5-28.5Q497-600 480-600t-28.5 11.5Q440-577 440-560t11.5 28.5Q463-520 480-520t28.5-11.5ZM310-536l114-114-150-150-114 114 150 150Zm90 96q17 0 28.5-11.5T440-480q0-17-11.5-28.5T400-520q-17 0-28.5 11.5T360-480q0 17 11.5 28.5T400-440Zm108.5 68.5Q520-383 520-400t-11.5-28.5Q497-440 480-440t-28.5 11.5Q440-417 440-400t11.5 28.5Q463-360 480-360t28.5-11.5ZM560-440q17 0 28.5-11.5T600-480q0-17-11.5-28.5T560-520q-17 0-28.5 11.5T520-480q0 17 11.5 28.5T560-440Zm-24 130 150 150 114-114-150-150-114 114ZM339-621Zm282 282Z"/></svg></div>';
            }

            return '<div class="song-card song-row" data-id="' + song.id + '">' +
                        '<div class="card-artwork-area">' +
                            artworkHTML +
                            '<div class="card-play-overlay">▶</div>' +
                            '<div class="card-eq-overlay"><span class="eq-bars" aria-hidden="true"><span></span><span></span><span></span><span></span></span></div>' +
                        '</div>' +
                        '<div class="card-info">' +
                            '<div class="card-title" title="' + escapeHTML(song.title) + '">' + escapeHTML(song.title) + '</div>' +
                            '<div class="card-artist" title="' + escapeHTML(song.artist) + '">' + escapeHTML(song.artist) + '</div>' +
                        '</div>' +
                    '</div>';
        }).join('');
        songsGrid.innerHTML = cardsHTML;
    }

    var activeContainer = currentView === 'list' ? tbody : songsGrid;
    var elements = activeContainer.querySelectorAll('.song-row');
    for (var r = 0; r < elements.length; r++) {
        elements[r].addEventListener('click', function () {
            activeContainer.querySelectorAll('.song-row.focused').forEach(row => row.classList.remove('focused'));
            var el = this;
            var songId = el.getAttribute('data-id');
            var song = songs.filter(function (s) { return String(s.id) === songId; })[0];

            if (song && typeof remoteSongClickHandler === 'function') {
                remoteSongClickHandler(song,songs);
                syncPlayingRow(song);
            }
        });
    }
}

if (sortGroup) {
    var activeBtn = sortGroup.querySelector('.sort-btn.active') || sortGroup.querySelector(`[data-sort="${sortState}"]`);
    if (activeBtn) activeBtn.classList.add('active');

    sortGroup.addEventListener('click', async function (e) {
        var btn = e.target.closest('.sort-btn');
        if (!btn) return;
        var sortType = btn.getAttribute('data-sort');
        if (!sortType) return;

        var allBtns = sortGroup.querySelectorAll('.sort-btn');
        for (var j = 0; j < allBtns.length; j++) {
            allBtns[j].classList.remove('active');
        }
        btn.classList.add('active');
        sortSongs(sortType);
        sortState = sortType;
        await savePageState(currentView,sortType);
    });
}

function updateLayoutVisibility() {
    if (currentView === 'list') {
        songsTable.style.display = '';
        columnHeaders.style.display = '';
        songsGrid.style.display = 'none';
    } else {
        songsTable.style.display = 'none';
        columnHeaders.style.display = 'none';
        songsGrid.style.display = 'grid';
    }
}

// -- View toggle buttons --
var viewToggleBtns = document.querySelectorAll('.view-toggle-btn');
for (var i = 0; i < viewToggleBtns.length; i++) {
    viewToggleBtns[i].addEventListener('click', async function () {
        var selectedView = this.getAttribute('data-view');
        if (currentView === selectedView) return;

        for (var j = 0; j < viewToggleBtns.length; j++) {
            viewToggleBtns[j].classList.remove('active-view');
        }
        this.classList.add('active-view');
        
        currentView = selectedView;
        await savePageState(selectedView,sortState);
        
        // Toggle Display Elements
        updateLayoutVisibility();

        render();
        syncPlayingRow();
    });
}

// -- Keyboard navigation --
document.addEventListener('keydown', function (e) {
    // FIXED: Prevent keyboard from crashing if data hasn't loaded yet
    if (!songs || songs.length === 0) return; 

    var activeContainer = currentView === 'list' ? tbody : songsGrid;
    var rows = Array.prototype.slice.call(activeContainer.querySelectorAll('.song-row'));
    if (rows.length === 0) return;

    var focused = activeContainer.querySelector('.song-row.focused');
    var idx = focused ? rows.indexOf(focused) : (activeContainer.querySelector('.song-row.playing') ? rows.indexOf(activeContainer.querySelector('.song-row.playing')) : -1);

    if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
        if (idx < rows.length - 1) {
            e.preventDefault();
            rows.forEach(function (r) { r.classList.remove('focused'); });
            rows[idx + 1].classList.add('focused');
            rows[idx + 1].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    } else if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
        if (idx > 0) {
            e.preventDefault();
            rows.forEach(function (r) { r.classList.remove('focused'); });
            rows[idx - 1].classList.add('focused');
            rows[idx - 1].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    } else if (e.key === 'Enter') {
        e.preventDefault();
        if (focused) {
            var songId = focused.getAttribute('data-id');
            var song = songs.filter(function (s) { return String(s.id) === songId; })[0];
            if (song && typeof remoteSongClickHandler === 'function') {
                remoteSongClickHandler(song);
                syncPlayingRow(song);
            }
        }
    }
});

async function syncPlayingRow(data) {
    var targetData = data || window.__playerStatus;
    if (!targetData) return; 

    var ID = targetData.Id || targetData.id || targetData.detail?.id;
    if (!ID) return;

    tableContainer.querySelectorAll('.song-row.playing').forEach(r => r.classList.remove('playing'));

    const activeRows = tableContainer.querySelectorAll('.song-row[data-id="' + ID + '"]');
    activeRows.forEach(r => r.classList.add('playing'));
}

window.addEventListener('playerStatusUpdated', syncPlayingRow);

async function savePageState(_viewType,_sortState) {
    try {

        const response = await fetch('/Song/SaveState', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                viewType: _viewType,
                sortType: _sortState
            })
        });

        if (!response.ok) {
            console.error('ViewState saving failed');
            return;
        }

    } catch (err) {
        console.error(err);
    }
}

// export to append devices rather than replace
export async function addDeviceSongs(ip) {
    if (songsByIp.has(ip)) return;
    try {
        const response = await fetch(`http://${ip}:9292/api/music/songs`);
        if (!response.ok) return;
        
        const data = await response.json();
        songsByIp.set(ip, data);
        
        flattenAndRender();
    } catch (err) {
        console.error(err);
    }
}

// export to remove a specific device
export function removeDeviceSongs(ip) {
    if (songsByIp.has(ip)) {
        songsByIp.delete(ip);
        flattenAndRender();
    }
}

// Dynamically merge all selected devices
function flattenAndRender() {
    songs = [];
    songsByIp.forEach(deviceSongs => {
        songs = songs.concat(deviceSongs);
    });
    sortSongs(sortState); // renders with sorted set of new songs

    // Keep remote queue in perfect alignment with device state changes NOTE: at the current time only works when the page is loaded
    syncRemoteQueue();
}

async function syncRemoteQueue() {
    try {
        await fetch('/Player/UpdateRemoteQueue', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(songs)
        });
    } catch (err) {
        console.error('Failed to sync remote queue with server:', err);
    }
}

// -- Kick off --
updateLayoutVisibility();
sortSongs(sortState);   // renders with the saved AppData order from the raw data
