const { app, BrowserWindow, dialog, ipcMain } = require('electron');
const treeKill = require('tree-kill');
const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

// --- Unified Logging System removed that ugly log ---

const originalLog = console.log;
const originalError = console.error;

function getTimestamp() {
    const now = new Date();
    const pad = (num, size = 2) => String(num).padStart(size, '0');
    return `${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}:${pad(now.getMilliseconds(), 3)}`;
}

function formatLog(framework, defaultLevel, args) {
    let message = args.map(arg => {
        if (arg instanceof Error) return arg.stack || arg.message;
        if (typeof arg === 'object') return JSON.stringify(arg);
        return String(arg);
    }).join(' ');

    message = message.replace(/\x1b\[[0-9;]*m/g, '');

    if (message.includes('[ ASP.NET EXIT]')) {
        framework = ' ASP.NET';
        defaultLevel = 'WAR';
        message = message.replace(/^\[\ ASP.NET EXIT\]\s*/i, 'Process Exited: ');
    }

    // Clean out redundant framework prefixes from original string logs
    message = message.replace(/^\[Electron\]:?\s*/i, '');
    message = message.replace(/^\[\ ASP.NET\]:?\s*/i, '');

    // Resolve structural log levels dynamically based on text keywords
    let level = defaultLevel;
    const upper = message.toUpperCase();
    if (upper.includes('ERROR') || upper.includes('FAIL') || upper.includes('CRASHED')) {
        level = 'ERR';
    } else if (upper.includes('WARN') || upper.includes('WARNING')) {
        level = 'WAR';
    } else if (upper.includes('DEBUG') || upper.includes('DBUG')) {
        level = 'DBG';
    } else if (upper.includes('TRACE') || upper.includes('TRCE')) {
        level = 'TRC';
    } else if (upper.includes('STATUS: 200') || upper.includes('READY') || upper.includes('SUCCESS') || upper.includes('INFO')) {
        level = 'INF';
    }

    // Clean out redundant  ASP.NET core log levels prefix (e.g., "info: ", "fail: ", "dbug: ")
    message = message.replace(/^(info|warn|fail|dbug|crit|trce|error):\s*/i, '');

    // Set matching colors based strictly on log levels
    let levelColor = '\x1b[32m'; // Default: INF (Green)
    if (level === 'ERR') levelColor = '\x1b[31m'; // Red
    if (level === 'WAR') levelColor = '\x1b[33m'; // Yellow
    if (level === 'DBG') levelColor = '\x1b[36m'; // Cyan
    if (level === 'TRC') levelColor = '\x1b[90m'; // Gray

    // Fixed Framework-specific Identifier Colors (Distinct from log levels)
    // Electron = Orange (\x1b[38;5;208m) |  ASP.NET = Bright Magenta (\x1b[95m) 
    const FW_COLOR = framework === 'Electron' ? '\x1b[38;5;208m' : '\x1b[95m';
    const RESET = '\x1b[0m';

    // Output target design: [Framework]-[Timestamp]-[Level]: Message
    return `${FW_COLOR}[${framework}]${RESET}${levelColor}-[${getTimestamp()}]-[${level}]: ${message}${RESET}`;
}

// Global Electron Interceptors
console.log = (...args) => originalLog(formatLog('Electron', 'INF', args));
console.error = (...args) => originalError(formatLog('Electron', 'ERR', args));
console.warn = (...args) => originalLog(formatLog('Electron', 'WAR', args));
console.info = (...args) => originalLog(formatLog('Electron', 'INF', args));

// Explicit ASP.NET Stream Printer
function printDotnetLog(data, isError = false) {
    const lines = data.toString().split('\n');
    lines.forEach(line => {
        if (!line.trim()) return;
        originalLog(formatLog(' ASP.NET', isError ? 'ERR' : 'INF', [line]));
    });
}

let mainWindow;
let backendProcess;

function startBackend() {
    backendProcess = spawn('dotnet', ['run'], {
        cwd: path.join(__dirname, '..'),
        stdio: 'pipe'
    });

    backendProcess.stdout.on('data', (data) => {
        printDotnetLog(data);
    });

    backendProcess.stderr.on('data', (data) => {
        printDotnetLog(data, true);
    });

    backendProcess.on('exit', (code, signal) => {
        console.log(`[ ASP.NET EXIT] code=${code} signal=${signal}`);
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
        backgroundColor: theme.background, 
        webPreferences: {
            preload: path.join(__dirname, 'bridge.js'),
        }
    });

    mainWindow.webContents.on('before-input-event', (event, input) => {
        const key = input.key.toLowerCase();
        const reloadShortcut = key === 'r' && (input.control || input.meta);
        const f5 = key === 'f5';

        if (reloadShortcut || f5) {
            event.preventDefault();
            console.log('[Electron]: Reload blocked during loading screen');
        }
    });

    await mainWindow.loadFile(path.join(__dirname, 'startup.html'));
    await applyTheme(theme);
    mainWindow.show();

    const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

    async function waitForServer() {
        for (let i = 0; i < 60; i++) {
            try {
                console.log(`[Electron]: Checking server... attempt ${i + 1}/60`);
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

    const isUp = await waitForServer();
    if (!isUp) {
        console.error("[Electron]: Backend failed to start.");
        app.quit();
        return;
    }

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
    await mainWindow.loadURL("http://127.0.0.1:9292");

    console.log('[Electron]: Reload unblocked');
    mainWindow.webContents.removeAllListeners('before-input-event');

    await mainWindow.webContents.executeJavaScript(`
        document.body.style.opacity = '0';
        document.body.style.transition = 'opacity 500ms ease';
        requestAnimationFrame(() => {
            document.body.style.opacity = '1';
        });
    `);

    mainWindow.webContents.session.clearCache();
}

app.whenReady().then(async () => {
    await startBackend();
    await createWindow();
});

let isQuitting = false;
app.on('before-quit', async (event) => {
    if (isQuitting) return;

    event.preventDefault();
    isQuitting = true;

    try {
        console.log("[Electron]: Sending shutdown request");
        const controller = new AbortController();
        const timeout = setTimeout(() => { controller.abort(); }, 5000);

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
            message: "The backend server did not accept the shutdown signal.\n\nElectron will try to forcefully terminate the backend process.",
            buttons: ["OK"],
        });

        if (backendProcess?.pid) {
            treeKill(backendProcess.pid, "SIGKILL");
        }
    }
    app.quit();
});

ipcMain.handle('pick-folders', async () => {
    const result = await dialog.showOpenDialog({
        properties: ['openDirectory', 'multiSelections']
    });
    return result.filePaths;
});

const AppDataPath = path.join(__dirname, '..', 'wwwroot', 'data', 'AppData.json');
const themesDir = path.join(__dirname, '..', 'wwwroot', 'theme');
const defaultThemeName = 'Crimson Shadow';

function loadThemeName() {
    try {
        if (!fs.existsSync(AppDataPath)) return defaultThemeName;
        const AppData = JSON.parse(fs.readFileSync(AppDataPath, 'utf8'));
        return AppData.Theme ?? defaultThemeName;
    } catch {
        return defaultThemeName;
    }
}

function loadTheme() {
    try {
        const themeName = loadThemeName();
        const themePath = path.join(themesDir, `${themeName}.json`);
        if (!fs.existsSync(themePath)) throw new Error(`Theme not found: ${themeName}`);
        return JSON.parse(fs.readFileSync(themePath, 'utf8'));
    } catch (err) {
        console.error('Failed to load theme:', err);
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