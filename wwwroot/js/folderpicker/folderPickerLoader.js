import { initFolderPicker } from './folderPicker.js';

export async function pickMusicFolders() {

    try {

        const response = await fetch('/Home/FolderPicker');

        if (!response.ok) {
            console.error('Failed to load folder picker');
            return;
        }

        const html = await response.text();

        const host = document.getElementById('modalHost');

        host.innerHTML = html;

        initFolderPicker();
    }
    catch (err) {
        console.error(err);
    }
}