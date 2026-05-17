const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
    pickFolders: () => ipcRenderer.invoke('pick-folders')
});