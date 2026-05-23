export function initFolderPicker() {

    const folderList = document.getElementById('folderList');
    const noFolder = document.getElementById('noFolder');
    const folderCount = document.getElementById('folderCount');
    const scanBtn = document.getElementById('scanBtn');
    const overlay = document.getElementById('folderPickerOverlay');

    let selectedFolders = [];

    function renderFolders() {

        const existingRows = document.querySelectorAll('.folder-row');
        existingRows.forEach(r => r.remove());

        if (selectedFolders.length === 0) {
            noFolder.style.display = 'flex';
            scanBtn.disabled = true;
        }
        else {
            noFolder.style.display = 'none';
            scanBtn.disabled = false;
        }

        folderCount.textContent = selectedFolders.length;

        selectedFolders.forEach(path => {

            const row = document.createElement('div');
            row.className = 'folder-row';

            row.innerHTML = `
                <button class="remove-folder-btn" aria-label="Remove folder">
                    <svg xmlns="http://www.w3.org/2000/svg"
                        height="18px"
                        viewBox="0 -960 960 960" 
                        width="18px">
                        <path d="m256-200-56-56 224-224-224-224 56-56 224 224 224-224 56 56-224 224 224 224-56 56-224-224-224 224Z"/>
                    </svg>
                </button>

                <div class="folder-path">${path}</div>
            `;

            const removeBtn = row.querySelector('.remove-folder-btn');

            removeBtn.addEventListener('click', () => {

                selectedFolders =
                    selectedFolders.filter(p => p !== path);

                row.classList.add('removing');

                row.addEventListener('animationend', () => {
                    renderFolders();
                }, { once: true });
            });

            folderList.appendChild(row);
        });
    }

    async function addFolders() {

        const paths = await window.electronAPI.pickFolders();

        if (!paths) return;

        for (const path of paths) {

            if (!selectedFolders.includes(path)) {
                selectedFolders.push(path);
            }
        }

        renderFolders();
    }

    async function scanFolders() {

        try {

            Toast.show('Starting library scan...');

            const response = await fetch('/Player/FolderPicker', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(selectedFolders)
            });

            const data = await response.json();

            if (!response.ok) {

                Toast.show(data.message || 'Something went wrong');

                return;
            }

            await Toast.show(data.message);

            closePicker();
        }
        catch (err) {

            console.error(err);

            Toast.show('Server connection failed');
        }
    }

    function closePicker() {

        const modal = document.getElementById('folderPickerModal');

        overlay.classList.add('closing');
        modal.classList.add('closing');

        modal.addEventListener('animationend', () => {

            overlay.remove();

            // cleanup
            document.getElementById('modalHost').innerHTML = '';

        }, { once: true });
    }

    // click backdrop to close
    overlay.addEventListener('click', (e) => {

        if (e.target === overlay) {
            closePicker();
        }
    });

    document.getElementById('addBtn')
        .addEventListener('click', addFolders);

    document.getElementById('scanBtn')
        .addEventListener('click', scanFolders);

    document.getElementById('cancelBtn')
        .addEventListener('click', closePicker);
}