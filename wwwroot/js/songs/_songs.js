(async function () {
    'use strict';

    // Get the data injected by the Razor view
    var initialSongs = window.__songsInitialData || [];
    var songClickHandler = window.__songClickHandler || null;

    var songs = initialSongs.slice();

    var tbody = document.getElementById('songsTbody');
    var countEl = document.getElementById('songCountDisplay');
    var sortGroup = document.getElementById('sortGroup');
    var emptyState = document.getElementById('emptyState');
    var header = document.getElementById('header');
    var tableContainer = document.getElementById('tableContainer');

    if (!tbody || !tableContainer) return;

    tableContainer.addEventListener('scroll', function () {
        header.classList.toggle('scrolled', tableContainer.scrollTop > 0);
    });

    function escapeHTML(str) {
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    async function sortSongs(type) {
        switch (type) {
            case 'newest':
                songs.sort(function (a, b) { return new Date(b.CreatedAt) - new Date(a.CreatedAt); });
                break;
            case 'oldest':
                songs.sort(function (a, b) { return new Date(a.CreatedAt) - new Date(b.CreatedAt); });
                break;
            case 'az':
                songs.sort(function (a, b) { return a.Title.localeCompare(b.Title, 'en', { sensitivity: 'base' }); });
                break;
            case 'za':
                songs.sort(function (a, b) { return b.Title.localeCompare(a.Title, 'en', { sensitivity: 'base' }); });
                break;
            // TODO: 'most-listened' / 'least-listened' when we have a `listens` field
            default:
                // newest by default
                songs.sort(function (a, b) { return new Date(b.CreatedAt) - new Date(a.CreatedAt); });
        }
        render();
        syncPlayingRow();
    }

    // -- Render the table --
    function render() {
        if (songs.length === 0) {
            tbody.innerHTML = '';
            emptyState.style.display = 'block';
            countEl.textContent = '0 songs';
            return;
        }

        emptyState.style.display = 'none';
        countEl.textContent = songs.length + ' song' + (songs.length !== 1 ? 's' : '');

        var rowsHTML = songs.map(function (song, i) {
            var artworkHTML = song.CoverArtDataUri
                ? '<img class="artwork-thumb" src="' + escapeHTML(song.CoverArtDataUri) + '" alt="" loading="lazy" onerror="this.style.display=\'none\';this.insertAdjacentHTML(\'afterend\',\'<div class=artwork-placeholder>♪</div>\')">'
                : '<div class="artwork-placeholder">♪</div>';

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

        // Attach click handlers
        var rows = tbody.querySelectorAll('.song-row');
        for (var r = 0; r < rows.length; r++) {
            rows[r].addEventListener('click', function () {
                var row = this;
                var songId = row.getAttribute('data-id');
                var song = songs.filter(function (s) { return String(s.Id) === songId; })[0];

                /* Update playing highlight
                var allRows = tbody.querySelectorAll('.song-row');
                for (var k = 0; k < allRows.length; k++) {
                    allRows[k].classList.remove('playing');
                }
                row.classList.add('playing');
                */

                // Notify external handler
                if (song && typeof songClickHandler === 'function') {
                    songClickHandler(song);
                }
            });
        }
    }

    if (sortGroup) {
        // Initial active button
        var activeBtn = sortGroup.querySelector('.sort-btn.active');
        if (!activeBtn) {
            activeBtn = sortGroup.querySelector('[data-sort="newest"]');
            if (activeBtn) activeBtn.classList.add('active');
        }

        sortGroup.addEventListener('click', function (e) {
            var btn = e.target.closest('.sort-btn');
            if (!btn) return;
            var sortType = btn.getAttribute('data-sort');
            if (!sortType) return;

            // Update active style
            var allBtns = sortGroup.querySelectorAll('.sort-btn');
            for (var j = 0; j < allBtns.length; j++) {
                allBtns[j].classList.remove('active');
            }
            btn.classList.add('active');

            sortSongs(sortType);
        });
    }

    // -- View toggle buttons (not done yet) --
    var viewToggleBtns = document.querySelectorAll('.view-toggle-btn');
    for (var i = 0; i < viewToggleBtns.length; i++) {
        viewToggleBtns[i].addEventListener('click', function () {
            var allBtns = document.querySelectorAll('.view-toggle-btn');
            for (var j = 0; j < allBtns.length; j++) {
                allBtns[j].classList.remove('active-view');
            }
            this.classList.add('active-view');
        });
    }

    // -- Keyboard navigation --
    document.addEventListener('keydown', function (e) {
        var playing = tbody.querySelector('.song-row.playing');
        if (!playing) return;
        var rows = Array.prototype.slice.call(tbody.querySelectorAll('.song-row'));
        var idx = rows.indexOf(playing);

        if (e.key === 'ArrowDown' && idx < rows.length - 1) {
            e.preventDefault();
            rows.forEach(function (r) { r.classList.remove('playing'); });
            rows[idx + 1].classList.add('playing');
            rows[idx + 1].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        } else if (e.key === 'ArrowUp' && idx > 0) {
            e.preventDefault();
            rows.forEach(function (r) { r.classList.remove('playing'); });
            rows[idx - 1].classList.add('playing');
            rows[idx - 1].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        } else if (e.key === 'Enter') {
            e.preventDefault();
            var row = tbody.querySelector('.song-row.playing');
            if (row) {
                var songId = row.getAttribute('data-id');
                var song = songs.filter(function (s) { return String(s.Id) === songId; })[0];
                if (song && typeof songClickHandler === 'function') {
                    songClickHandler(song);
                }
            }
        }
    });

    async function syncPlayingRow(event) {

        let data = event?.detail || window.__playerStatus;

        if (!data) {
            const response = await fetch('/Player/Status');
            data = await response.json();
        }

        // If player is not initialized yet,
        // do not wipe existing UI state.
        if (!data || !data.currentSongId) {
            return;
        }

        const rows = tbody.querySelectorAll('.song-row');

        rows.forEach(r =>
            r.classList.remove('playing'));

        const activeRow =
            tbody.querySelector(
                '.song-row[data-id="' +
                data.currentSongId +
                '"]'
            );

        if (activeRow) {
            activeRow.classList.add('playing');
        }
    }

    window.addEventListener('playerStatusUpdated', syncPlayingRow);

    // -- Kick off --
    render();   // renders with default (newest) order from the raw data order
    await syncPlayingRow();
})();