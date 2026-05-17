const { app, BrowserWindow, dialog, ipcMain } = require('electron');
const treeKill = require('tree-kill');
const { spawn } = require('child_process');
const path = require('path');

let mainWindow;
let backendProcess;

function startBackend() {
    backendProcess = spawn('dotnet', ['run'], {
        cwd: path.join(__dirname, '..'),
        stdio: 'pipe'
    });

    backendProcess.stdout.on('data', (data) => {
        console.log(`[.NET]: ${data}`);
    });

    backendProcess.stderr.on('data', (data) => {
        console.error(`[.NET ERROR]: ${data}`);
    });

	backendProcess.on('exit', (code, signal) => {
	    console.log(`[.NET]: exited with code ${code}, signal ${signal}`);
	});
}

async function createWindow() {
    mainWindow = new BrowserWindow({
        title: "EchoNet",
        width: 1200,
        height: 800,
        autoHideMenuBar: true,
        webPreferences: {
            preload: path.join(__dirname, 'bridge.js')
        }
    });

    // Give backend time to start (polling)
    async function waitForServer() {
        for (let i = 0; i < 40; i++) {
            try {
                console.log(`[Electron]: Checking server... attempt ${i}`);

                const res = await fetch('http://127.0.0.1:9292/ready');
                console.log("[Electron]: STATUS:", res.status);

                const text = await res.text();
                const parsed = JSON.parse(text);
                console.log("[Electron]: BODY:", parsed);

                if (res.status === 200 && parsed === "READY") {
                    console.log("[Electron]: Server READY");
                    return true;
                }
            } catch (err) {
                console.log("[Electron]: Fetch error:", err.message);
            }

            await new Promise(r => setTimeout(r, 500));
        }

        console.log("[Electron]: Server never became ready");
        return false;
    }

    mainWindow.webContents.on('did-fail-load', (event, errorCode, errorDescription) => {
        console.error('[Electron]: Load failed:', errorCode, errorDescription);
    });

    mainWindow.webContents.on('did-finish-load', () => {
        console.log('[Electron]: Page loaded');
    });

    mainWindow.webContents.on('crashed', () => {
        console.error('[Electron]: Renderer crashed');
    });

    const isUp = await waitForServer();
    if (!isUp) {
        console.error("[Electron]: Backend failed to start.");
        app.quit();
        return;
    }

    console.log("[Electron]: Loading UI");
    await mainWindow.loadURL("http://127.0.0.1:9292");

    //mainWindow.webContents.openDevTools({ mode: 'detach' });
}

app.whenReady().then(async () => {
    await startBackend();
    await createWindow();
});

// Clean up when closing using treeKill for safty in cross platforms
let isQuitting = false;

app.on('before-quit', async (event) => {
    if (isQuitting) return;

    event.preventDefault();
    isQuitting = true;

    try {
        console.log("[Electron]: Sending shutdown request");

        await fetch('http://127.0.0.1:9292/shutdown', {
            method: 'POST'
        });
    }
    catch (err) {
        console.error("[Electron]: Shutdown failed", err);
        console.log("[Electron]: Forcing a Shutdown");
        if (backendProcess?.pid) {
            treeKill(backendProcess.pid);
        }
    }

    app.quit();
});

// Folder picker
ipcMain.handle('pick-folders', async () => {
    const result = await dialog.showOpenDialog({
        properties: ['openDirectory', 'multiSelections']
    });

    return result.filePaths;
});
