import { pickMusicFolders } 
    from './folderpicker/folderPickerLoader.js';

document.addEventListener('DOMContentLoaded', () => {

    const btn = document.getElementById('pickMusicBtn');

    if (btn) {

        btn.addEventListener('click', () => {
            pickMusicFolders();
        });
    }
});