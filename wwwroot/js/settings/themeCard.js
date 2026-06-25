document.addEventListener("DOMContentLoaded", () => {
    // Grab all wrappers rendered by the partial loop
    const wrappers = document.querySelectorAll('.canvas-wrapper');

    // Loop through each one to set up its own isolated canvas
    wrappers.forEach(wrapper => {
        const c = wrapper.querySelector('canvas.fc');
        if (!c) return;
        const ctx = c.getContext('2d');

        let W, H, mid;

        function syncSize() {
            W = wrapper.offsetWidth;
            H = wrapper.offsetHeight;
            mid = H * 0.5;
            c.width = W;
            c.height = H;
        }

        syncSize();
        const ro = new ResizeObserver(syncSize);
        ro.observe(wrapper);

        // Colors from data-* attributes
        function hexToRgb(hex) {
            const m = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
            return m
                ? { r: parseInt(m[1], 16), g: parseInt(m[2], 16), b: parseInt(m[3], 16) }
                : { r: 30, g: 30, b: 40 };
        }

        const topColor = hexToRgb(wrapper.dataset.background || '#08122a');
        const bottomColor = hexToRgb(wrapper.dataset.accent || '#a83c18');
        
        // Determine glow based on modes
        function updateGlow(activeSystemMode) {
            const sysMode = activeSystemMode?.toLowerCase();
            const cardMode = wrapper.dataset.mode?.toLowerCase();
            
            if (sysMode === cardMode) {
                wrapper.style.setProperty('--glow-color', `rgba(${bottomColor.r},${bottomColor.g},${bottomColor.b},1)`);
            } else {
                wrapper.style.setProperty('--glow-color', `rgba(${topColor.r},${topColor.g},${topColor.b},1)`);
            }
        }

        // Run once on page load
        updateGlow(wrapper.dataset.currentThemeMode);

        // Listen for previews or resets from themeSwitcher.js
        window.addEventListener('themePreviewChanged', (e) => {
            updateGlow(e.detail.mode);
        });

        const seed = Math.random() * 1000;
        let t = 0;

        function waveY(x, phase) {
            return mid
                + Math.sin(x * 0.017 + phase + seed) * H * 0.07
                + Math.sin(x * 0.031 + phase * 1.37 + 1.05 + seed * 0.5) * H * 0.038
                + Math.sin(x * 0.008 - phase * 0.82 + 2.3 + seed * 0.7) * H * 0.054
                + Math.sin(x * 0.052 + phase * 0.63 + 0.85 + seed * 0.3) * H * 0.023
                + Math.sin(x * 0.074 - phase * 1.1 + 3.7 + seed * 0.9) * H * 0.013;
        }

        function rgb(c) { return `rgb(${c.r},${c.g},${c.b})`; }
        function rgba(c, a) { return `rgba(${c.r},${c.g},${c.b},${a})`; }

        function draw() {
            ctx.clearRect(0, 0, W, H);

            // Background
            ctx.fillStyle = rgb(bottomColor);
            ctx.fillRect(0, 0, W, H);

            // Top fill
            ctx.beginPath();
            ctx.moveTo(-1, 0);
            ctx.lineTo(W + 1, 0);
            ctx.lineTo(W + 1, waveY(W, t));
            for (let x = W - 1; x >= 0; x--) ctx.lineTo(x, waveY(x, t));
            ctx.lineTo(-1, waveY(0, t));
            ctx.closePath();
            ctx.fillStyle = rgb(topColor);
            ctx.fill();

            // Secondary wave
            const t2 = t + 0.6;
            ctx.beginPath();
            ctx.moveTo(-1, waveY(-1, t2));
            for (let x = 0; x <= W + 1; x++) ctx.lineTo(x, waveY(x, t2));
            for (let x = W + 1; x >= -1; x--) ctx.lineTo(x, waveY(x, t2) + H * 0.038);
            ctx.closePath();

            const grad = ctx.createLinearGradient(0, mid - H * 0.1, 0, mid + H * 0.1);
            grad.addColorStop(0, rgba(topColor, 0.55));
            grad.addColorStop(1, rgba(bottomColor, 0));
            ctx.fillStyle = grad;
            ctx.fill();

            // Wave outline
            ctx.beginPath();
            ctx.moveTo(0, waveY(0, t));
            for (let x = 1; x <= W; x++) ctx.lineTo(x, waveY(x, t));
            ctx.strokeStyle = 'rgba(255,255,255,0.12)';
            ctx.lineWidth = 1.5;
            ctx.stroke();

            // Glow band
            const glow = ctx.createLinearGradient(0, mid - 4, 0, mid + 4);
            glow.addColorStop(0, 'rgba(255,255,255,0)');
            glow.addColorStop(0.5, 'rgba(255,255,255,0.06)');
            glow.addColorStop(1, 'rgba(255,255,255,0)');
            const gH = H * 0.025;
            ctx.beginPath();
            ctx.moveTo(0, waveY(0, t) - gH);
            for (let x = 1; x <= W; x++) ctx.lineTo(x, waveY(x, t) - gH);
            for (let x = W; x >= 0; x--) ctx.lineTo(x, waveY(x, t) + gH);
            ctx.closePath();
            ctx.fillStyle = glow;
            ctx.fill();

            t += 0.009;
            requestAnimationFrame(draw);
        }

        draw(); // Start the loop for this specific card
    });
});