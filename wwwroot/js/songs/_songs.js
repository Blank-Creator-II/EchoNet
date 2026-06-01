(async function () {
    'use strict';

    // ── Get the data injected by the Razor view ──
    var initialSongs = window.__songsInitialData || [];
    var songClickHandler = window.__songClickHandler || null;
    var currentView = window.__songPageState[0]; // 'list' or 'grid'
    var sortState = window.__songPageState[1]; // 'newest', 'oldest', 'az', 'za'

    var songs = initialSongs.slice();

    var tbody = document.getElementById('songsTbody');
    var songsGrid = document.getElementById('songsGrid');
    var songsTable = document.getElementById('songsTable');
    var columnHeaders = document.getElementById('columnHeaders');
    var countEl = document.getElementById('songCountDisplay');
    var sortGroup = document.getElementById('sortGroup');
    var emptyState = document.getElementById('emptyState');
    var header = document.getElementById('header');
    var tableContainer = document.getElementById('tableContainer');

    if (!tbody || !tableContainer || !songsGrid) return;

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
                songs.sort(function (a, b) { return b.CreatedAt.localeCompare(a.CreatedAt); });
                break;
            case 'oldest':
                songs.sort(function (a, b) { return a.CreatedAt.localeCompare(b.CreatedAt); });
                break;
            case 'az':
                songs.sort(function (a, b) { return a.Title.localeCompare(b.Title, 'en', { sensitivity: 'base' }); });
                break;
            case 'za':
                songs.sort(function (a, b) { return b.Title.localeCompare(a.Title, 'en', { sensitivity: 'base' }); });
                break;
            default:
                songs.sort(function (a, b) { return b.CreatedAt.localeCompare(a.CreatedAt); });
        }
        render();
        syncPlayingRow();
    }

    // -- Render the table --
    function render() {
        if (songs.length === 0) {
            tbody.innerHTML = '';
            songsGrid.innerHTML = '';
            emptyState.style.display = 'block';
            countEl.textContent = '0 songs';
            return;
        }

        emptyState.style.display = 'none';
        countEl.textContent = songs.length + ' song' + (songs.length !== 1 ? 's' : '');

        if (currentView === 'list') {
            // Render Table Rows
            var rowsHTML = songs.map(function (song, i) {
                var artworkHTML = song.CoverArtDataUri
                    ? '<img class="artwork-thumb" src="' + escapeHTML(song.CoverArtDataUri) + '" alt="" loading="lazy" />'
                    : '<div class="artwork-placeholder"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" fill="var(--accent)"><path d="M480-254 330-104q-23 23-56 23t-56-23L104-218q-23-23-23-56t23-56l150-150-150-150q-23-23-23-56t23-56l114-114q23-23 56-23t56 23l150 150 150-150q23-23 56-23t56 23l114 114q23 23 23 56t-23 56L706-480l150 150q23 23 23 56t-23 56L742-104q-23 23-56 23t-56-23L480-254Zm28.5-277.5Q520-543 520-560t-11.5-28.5Q497-600 480-600t-28.5 11.5Q440-577 440-560t11.5 28.5Q463-520 480-520t28.5-11.5ZM310-536l114-114-150-150-114 114 150 150Zm90 96q17 0 28.5-11.5T440-480q0-17-11.5-28.5T400-520q-17 0-28.5 11.5T360-480q0 17 11.5 28.5T400-440Zm108.5 68.5Q520-383 520-400t-11.5-28.5Q497-440 480-440t-28.5 11.5Q440-417 440-400t11.5 28.5Q463-360 480-360t28.5-11.5ZM560-440q17 0 28.5-11.5T600-480q0-17-11.5-28.5T560-520q-17 0-28.5 11.5T520-480q0 17 11.5 28.5T560-440Zm-24 130 150 150 114-114-150-150-114 114ZM339-621Zm282 282Z"/></svg></div>';

                return '<tr class="song-row" data-id="' + song.Id + '">' +
                    '<td>' +
                        '<div class="row-index-wrap">' +
                            '<span class="row-num">' + (i + 1) + '</span>' +
                            '<span class="row-play-icon">▶</span>' +
                            '<span class="eq-bars" aria-hidden="true"><span></span><span></span><span></span><span></span></span>' +
                        '</div>' +
                    '</td>' +
                    '<td><div class="artwork-area">' + artworkHTML + '</div></td>' +
                    '<td><div class="title-cell"><span class="title-text">' + escapeHTML(song.Title) + '</span></div></td>' +
                    '<td><span class="artist-cell">' + escapeHTML(song.Artist) + '</span></td>' +
                    '<td><span class="album-cell">' + escapeHTML(song.Album) + '</span></td>' +
                    '<td><span class="duration-cell">' + escapeHTML(song.FormattedDuration) + '</span></td>' +
                '</tr>';
            }).join('');
            tbody.innerHTML = rowsHTML;
        } else {
            // Render Grid Cards
            var cardsHTML = songs.map(function (song) {
                var artworkHTML = song.CoverArtDataUri
                    ? '<img class="artwork-thumb" src="' + escapeHTML(song.CoverArtDataUri) + '" alt="" loading="lazy" />'
                    : '<div class="artwork-placeholder"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" fill="var(--accent)"><path d="M480-254 330-104q-23 23-56 23t-56-23L104-218q-23-23-23-56t23-56l150-150-150-150q-23-23-23-56t23-56l114-114q23-23 56-23t56 23l150 150 150-150q23-23 56-23t56 23l114 114q23 23 23 56t-23 56L706-480l150 150q23 23 23 56t-23 56L742-104q-23 23-56 23t-56-23L480-254Zm28.5-277.5Q520-543 520-560t-11.5-28.5Q497-600 480-600t-28.5 11.5Q440-577 440-560t11.5 28.5Q463-520 480-520t28.5-11.5ZM310-536l114-114-150-150-114 114 150 150Zm90 96q17 0 28.5-11.5T440-480q0-17-11.5-28.5T400-520q-17 0-28.5 11.5T360-480q0 17 11.5 28.5T400-440Zm108.5 68.5Q520-383 520-400t-11.5-28.5Q497-440 480-440t-28.5 11.5Q440-417 440-400t11.5 28.5Q463-360 480-360t28.5-11.5ZM560-440q17 0 28.5-11.5T600-480q0-17-11.5-28.5T560-520q-17 0-28.5 11.5T520-480q0 17 11.5 28.5T560-440Zm-24 130 150 150 114-114-150-150-114 114ZM339-621Zm282 282Z"/></svg></div>';

                return '<div class="song-card song-row" data-id="' + song.Id + '">' +
                            '<div class="card-artwork-area">' +
                                artworkHTML +
                                '<div class="card-play-overlay">▶</div>' +
                                '<div class="card-eq-overlay"><span class="eq-bars" aria-hidden="true"><span></span><span></span><span></span><span></span></span></div>' +
                            '</div>' +
                            '<div class="card-info">' +
                                '<div class="card-title" title="' + escapeHTML(song.Title) + '">' + escapeHTML(song.Title) + '</div>' +
                                '<div class="card-artist" title="' + escapeHTML(song.Artist) + '">' + escapeHTML(song.Artist) + '</div>' +
                            '</div>' +
                        '</div>';
            }).join('');
            songsGrid.innerHTML = cardsHTML;
        }

        // Attach click handlers to the active container
        var activeContainer = currentView === 'list' ? tbody : songsGrid;
        var elements = activeContainer.querySelectorAll('.song-row');
        for (var r = 0; r < elements.length; r++) {
            elements[r].addEventListener('click', function () {
                activeContainer.querySelectorAll('.song-row.focused').forEach(r => r.classList.remove('focused'));
                var el = this;
                var songId = el.getAttribute('data-id');
                var song = songs.filter(function (s) { return String(s.Id) === songId; })[0];

                // Notify external handler
                if (song && typeof songClickHandler === 'function') {
                    songClickHandler(song);
                    //syncPlayingRow(song);
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
        var activeContainer = currentView === 'list' ? tbody : songsGrid;
        var rows = Array.prototype.slice.call(activeContainer.querySelectorAll('.song-row'));
        if (rows.length === 0) return;

        // Find which row currently has keyboard focus
        var focused = activeContainer.querySelector('.song-row.focused');
        
        // If nothing is focused yet, look for the playing song, or start at index -1
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
            // Trigger play only if a row is actively selected via keyboard
            if (focused) {
                var songId = focused.getAttribute('data-id');
                var song = songs.filter(function (s) { return String(s.Id) === songId; })[0];
                if (song && typeof songClickHandler === 'function') {
                    songClickHandler(song);
                    //syncPlayingRow(song);
                }
            }
        }
    });

    async function syncPlayingRow(data) {
        // Fallback to global state if no data argument was passed at all
        var targetData = data || window.__playerStatus;
        if (!targetData) return; // Exit early if there's no data to extract an ID from

        // Supports: songMetadata.Id, fallback/global.id, or event.detail.id
        var ID = targetData.Id || targetData.id || targetData.detail?.id;
        if (!ID) return;

        // Query over the parent container to clear/set regardless of active view
        const rows = tableContainer.querySelectorAll('.song-row');
        rows.forEach(r => r.classList.remove('playing'));

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

    // -- Kick off --
    updateLayoutVisibility();
    sortSongs(sortState);   // renders with the saved AppData order from the raw data
})();