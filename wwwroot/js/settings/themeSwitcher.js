window.selectTheme = async function (card) {
    // Apply theme to frontend
    const root = document.documentElement;

    root.style.setProperty('--name', card.dataset.theme);
    root.style.setProperty('--mode', card.dataset.mode);

    root.style.setProperty('--bg', card.dataset.background);
    root.style.setProperty('--surface', card.dataset.surface);
    root.style.setProperty('--surface-2', card.dataset.surfaceAlt);

    root.style.setProperty('--text', card.dataset.text);
    root.style.setProperty('--muted', card.dataset.mutedText);

    root.style.setProperty('--accent', card.dataset.accent);
    root.style.setProperty('--accent-hover', card.dataset.accentHover);
    root.style.setProperty('--accent-active', card.dataset.accentActive);

    root.style.setProperty('--border', card.dataset.border);
    root.style.setProperty('--border-hover', card.dataset.borderHover);
    root.style.setProperty('--border-focus', card.dataset.borderFocus);

    root.style.setProperty('--focus-ring', card.dataset.focusRing);
    root.style.setProperty('--text-on-accent', card.dataset.textOnAccent);

    root.style.setProperty('--button-secondary-bg', card.dataset.buttonSecondaryBg);
    root.style.setProperty('--button-secondary-hover-bg', card.dataset.buttonSecondaryHoverBg);
    root.style.setProperty('--button-secondary-text', card.dataset.buttonSecondaryText);

    root.style.setProperty('--input-bg', card.dataset.inputBg);
    root.style.setProperty('--input-placeholder', card.dataset.inputPlaceholder);

    root.style.setProperty('--disabled-bg', card.dataset.disabledBg);
    root.style.setProperty('--disabled-text', card.dataset.disabledText);

    // Change the preview bar name
    document.getElementById("active-name").textContent = card.dataset.theme;

    // Send new theme data to bakend
    const themeName = card.dataset.theme;
    await fetch('/Settings/ChangeTheme', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: new URLSearchParams({
            themeName: themeName
        })
    });
};