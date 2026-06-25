let selectedTheme = null;
let originalTheme = null;

// Store all original values on page load
document.addEventListener("DOMContentLoaded", () => {

    const root = document.documentElement;

    // We grab the initial system mode from the first card so we know what to reset to
    const firstCard = document.querySelector('.canvas-wrapper');
    const initialMode = firstCard ? firstCard.dataset.currentThemeMode : 'light';

    originalTheme = {
        name: document.getElementById("active-name").textContent.trim(),
        mode: initialMode,

        background: getComputedStyle(root).getPropertyValue('--bg'),
        surface: getComputedStyle(root).getPropertyValue('--surface'),
        surfaceAlt: getComputedStyle(root).getPropertyValue('--surface-2'),

        text: getComputedStyle(root).getPropertyValue('--text'),
        mutedText: getComputedStyle(root).getPropertyValue('--muted'),

        accent: getComputedStyle(root).getPropertyValue('--accent'),
        accentHover: getComputedStyle(root).getPropertyValue('--accent-hover'),
        accentActive: getComputedStyle(root).getPropertyValue('--accent-active'),

        border: getComputedStyle(root).getPropertyValue('--border'),
        borderHover: getComputedStyle(root).getPropertyValue('--border-hover'),
        borderFocus: getComputedStyle(root).getPropertyValue('--border-focus'),

        focusRing: getComputedStyle(root).getPropertyValue('--focus-ring'),
        textOnAccent: getComputedStyle(root).getPropertyValue('--text-on-accent'),

        buttonSecondaryBg: getComputedStyle(root).getPropertyValue('--button-secondary-bg'),
        buttonSecondaryHoverBg: getComputedStyle(root).getPropertyValue('--button-secondary-hover-bg'),
        buttonSecondaryText: getComputedStyle(root).getPropertyValue('--button-secondary-text'),

        inputBg: getComputedStyle(root).getPropertyValue('--input-bg'),
        inputPlaceholder: getComputedStyle(root).getPropertyValue('--input-placeholder'),

        disabledBg: getComputedStyle(root).getPropertyValue('--disabled-bg'),
        disabledText: getComputedStyle(root).getPropertyValue('--disabled-text')
    };
});

function applyTheme(theme) {

    const root = document.documentElement;

    root.style.setProperty('--bg', theme.background);
    root.style.setProperty('--surface', theme.surface);
    root.style.setProperty('--surface-2', theme.surfaceAlt);

    root.style.setProperty('--text', theme.text);
    root.style.setProperty('--muted', theme.mutedText);

    root.style.setProperty('--accent', theme.accent);
    root.style.setProperty('--accent-hover', theme.accentHover);
    root.style.setProperty('--accent-active', theme.accentActive);

    root.style.setProperty('--border', theme.border);
    root.style.setProperty('--border-hover', theme.borderHover);
    root.style.setProperty('--border-focus', theme.borderFocus);

    root.style.setProperty('--focus-ring', theme.focusRing);
    root.style.setProperty('--text-on-accent', theme.textOnAccent);

    root.style.setProperty('--button-secondary-bg', theme.buttonSecondaryBg);
    root.style.setProperty('--button-secondary-hover-bg', theme.buttonSecondaryHoverBg);
    root.style.setProperty('--button-secondary-text', theme.buttonSecondaryText);

    root.style.setProperty('--input-bg', theme.inputBg);
    root.style.setProperty('--input-placeholder', theme.inputPlaceholder);

    root.style.setProperty('--disabled-bg', theme.disabledBg);
    root.style.setProperty('--disabled-text', theme.disabledText);

    document.getElementById("active-name").textContent = theme.name;

    // Broadcast the change to the cards
    window.dispatchEvent(new CustomEvent('themePreviewChanged', {
        detail: { mode: theme.mode }
    }));
}

window.selectTheme = function (card) {

    selectedTheme = {
        name: card.dataset.theme,
        mode: card.dataset.mode,

        background: card.dataset.background,
        surface: card.dataset.surface,
        surfaceAlt: card.dataset.surfaceAlt,

        text: card.dataset.text,
        mutedText: card.dataset.mutedText,

        accent: card.dataset.accent,
        accentHover: card.dataset.accentHover,
        accentActive: card.dataset.accentActive,

        border: card.dataset.border,
        borderHover: card.dataset.borderHover,
        borderFocus: card.dataset.borderFocus,

        focusRing: card.dataset.focusRing,
        textOnAccent: card.dataset.textOnAccent,

        buttonSecondaryBg: card.dataset.buttonSecondaryBg,
        buttonSecondaryHoverBg: card.dataset.buttonSecondaryHoverBg,
        buttonSecondaryText: card.dataset.buttonSecondaryText,

        inputBg: card.dataset.inputBg,
        inputPlaceholder: card.dataset.inputPlaceholder,

        disabledBg: card.dataset.disabledBg,
        disabledText: card.dataset.disabledText
    };

    // Preview only
    applyTheme(selectedTheme);
};

document.getElementById('btn-resetTheme').addEventListener('click', () => {

    applyTheme(originalTheme);
    
    selectedTheme = null;
    Toast.show('Theme reset')
});

async function saveTheme() {

    if (!selectedTheme)
        return;

    try {

        await fetch('/Settings/ChangeTheme', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: new URLSearchParams({
                themeName: selectedTheme.name
            })
        });

        // New saved state becomes original
        originalTheme = { ...selectedTheme };
        Toast.show('Theme saved')

    } catch (err) {
        console.error(err);
        Toast.show('Failed to save theme');
    }
}

document.getElementById('btn-saveTheme').addEventListener('click', saveTheme);