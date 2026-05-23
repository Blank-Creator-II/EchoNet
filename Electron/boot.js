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

    const theme = loadTheme();

    mainWindow = new BrowserWindow({
        title: "EchoNet",
        width: 1200,
        height: 800,
        show: false,
        autoHideMenuBar: true,
        backgroundColor: theme.background, // avoids color flash
        webPreferences: {
            preload: path.join(__dirname, 'bridge.js'),
        }
    });

    mainWindow.webContents.on('before-input-event', (event, input) => {
        const key = input.key.toLowerCase();

        // Ctrl+R / Cmd+R
        const reloadShortcut =
            key === 'r' && (input.control || input.meta);

        // F5
        const f5 = key === 'f5';

        if (reloadShortcut || f5) {
            event.preventDefault();

            console.log('[Electron]: Reload blocked during loading screen');
        }
    });

    // Load startup screen immediately
    await mainWindow.loadFile(path.join(__dirname, 'startup.html'));

    await applyTheme(theme);

    mainWindow.show();

    // Utility sleep helper
    const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

    // Wait for backend readiness
    async function waitForServer() {

        // Begin polling
        for (let i = 0; i < 60; i++) {
            try {
                console.log(`[Electron]: Checking server... attempt ${i + 1}/60`); // 1 min check

                const res = await fetch('http://127.0.0.1:9292/ready');

                console.log("[Electron]: STATUS:", res.status);

                const text = await res.text();
                const parsed = JSON.parse(text);

                console.log("[Electron]: BODY:", parsed);

                if (res.status === 200 && parsed === "READY") {
                    console.log("[Electron]: Server READY");
                    return true;
                }
            }
            catch (err) {
                console.log("[Electron]: Fetch error:", err.message);
            }

            // 1sec between checks
            await sleep(1000);
        }

        await mainWindow.loadURL("about:blank");
        console.log("[Electron]: Server never became ready");
        await dialog.showMessageBox({
            type: "error",
            title: "Server Startup Failed",
            message: "The backend server never became ready.",
            buttons: ["OK"],
        });
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

    // Wait until backend is available
    const isUp = await waitForServer();

    if (!isUp) {
        console.error("[Electron]: Backend failed to start.");
        app.quit();
        return;
    }

    // Trigger startup animation completion
    try {
        await mainWindow.webContents.executeJavaScript(`
            if (window.loadingComplete) {
                window.loadingComplete();
            }
        `);
    }
    catch (err) {
        console.error("[Electron]: Failed to trigger loadingComplete()", err);
    }

    console.log("[Electron]: Loading UI");

    // Load actual app
    await mainWindow.loadURL("http://127.0.0.1:9292");

    console.log('[Electron]: Reload unblocked');
    // Re-enable reloads after startup screen is gone
    mainWindow.webContents.removeAllListeners('before-input-event');

    await mainWindow.webContents.executeJavaScript(`
        document.body.style.opacity = '0';
        document.body.style.transition = 'opacity 500ms ease';

        requestAnimationFrame(() => {
            document.body.style.opacity = '1';
        });
    `);

    // mainWindow.webContents.openDevTools({ mode: 'detach' });
    mainWindow.webContents.session.clearCache();
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

        const controller = new AbortController();

        const timeout = setTimeout(() => {
            controller.abort();
        }, 5000);

        await fetch("http://127.0.0.1:9292/shutdown", {
            method: "POST",
            signal: controller.signal,
        });

        clearTimeout(timeout);
    }
    catch (err) {
        console.error("[Electron]: Shutdown failed", err);
        console.log("[Electron]: Forcing a Shutdown");

        await dialog.showMessageBox({
            type: "error",
            title: "Shutdown Failed",
            message:
                "The backend server did not accept the shutdown signal.\n\n" +
                "Electron will try to forcefully terminate the backend process.",
            buttons: ["OK"],
        });

        if (backendProcess?.pid) {
            treeKill(backendProcess.pid, "SIGKILL");
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

// Theme loader
const fs = require('fs');

const settingsPath = path.join(
    __dirname,
    '..',
    'wwwroot',
    'data',
    'settings.json'
);

const themesDir = path.join(
    __dirname,
    '..',
    'wwwroot',
    'theme'
);

// Default fallback
const defaultThemeName = 'Crimson Shadow';

function loadThemeName() {
    try {
        if (!fs.existsSync(settingsPath)) {
            return defaultThemeName;
        }

        const settings = JSON.parse(
            fs.readFileSync(settingsPath, 'utf8')
        );

        return settings.Theme ?? defaultThemeName;
    }
    catch {
        return defaultThemeName;
    }
}

function loadTheme() {
    try {
        const themeName = loadThemeName();

        const themePath = path.join(
            themesDir,
            `${themeName}.json`
        );

        if (!fs.existsSync(themePath)) {
            throw new Error(`Theme not found: ${themeName}`);
        }

        return JSON.parse(
            fs.readFileSync(themePath, 'utf8')
        );
    }
    catch (err) {
        console.error('Failed to load theme:', err);

        // Hard fallback
        return {
            name: 'Fallback',
            background: '#151515',
            surface: '#1e1e1e',
            text: '#e0e0e0',
            accent: '#dc143c'
        };
    }
}

async function applyTheme(theme) {
    await mainWindow.webContents.executeJavaScript(`
        (() => {
            const theme = ${JSON.stringify(theme)};
            const root = document.documentElement;

            root.style.setProperty('--ls-bg', theme.background);
            root.style.setProperty('--ls-surface', theme.surface);
            root.style.setProperty('--ls-scanline', theme.surfaceAlt);

            root.style.setProperty('--ls-text', theme.text);
            root.style.setProperty('--ls-text-muted', theme.mutedText);

            root.style.setProperty('--ls-accent', theme.accent);
            root.style.setProperty('--ls-accent-dim', theme.accentHover);
            root.style.setProperty('--ls-accent-glow', theme.accentActive);

            root.style.setProperty('--ls-border', theme.border);
        })();
    `);
}