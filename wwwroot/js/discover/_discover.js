import { addDeviceSongs, removeDeviceSongs } from './songs.js';

(function(){
    const SWEEP_DURATION_MS = 6000;   
    const BLIP_HOLD_MS = 1400;        

    const radarCircle = document.getElementById('radarCircle');
    const ticksLayer = document.getElementById('ticksLayer');
    const dotsLayer = document.getElementById('dotsLayer');
    const deviceList = document.getElementById('deviceList');
    const tooltip = document.getElementById('tooltip');
    const ttName = document.getElementById('ttName');
    const ttIp = document.getElementById('ttIp');

    const knownDevices = new Map();
    const selected = new Set();
    const blipTimers = new Map();
    let discoveredCount = 0;

    function buildTicks(){
        for(let i = 0; i < 12; i++){
            const tick = document.createElement('div');
            tick.className = 'tick';
            tick.style.transform = 'translate(-50%,-50%) rotate(' + (i * 30) + 'deg) translateY(-124px)';
            ticksLayer.appendChild(tick);
        }
    }

    function polarToPercent(angleDeg, radiusPct){
        const rad = angleDeg * Math.PI / 180;
        return {
            x: 50 + radiusPct * Math.cos(rad),
            y: 50 + radiusPct * Math.sin(rad)
        };
    }

    function setActive(device, isActive){
        if(isActive) selected.add(device.id); else selected.delete(device.id);

        const dot = dotsLayer.querySelector('[data-id="' + device.id + '"]');
        const item = deviceList.querySelector('[data-id="' + device.id + '"]');

        if(dot){ dot.classList.toggle('active', isActive); dot.setAttribute('aria-pressed', String(isActive)); }
        if(item){
            item.classList.toggle('active', isActive);
            item.querySelector('input[type="checkbox"]').checked = isActive;
        }

        // Send commands to songs.js to merge or remove the queue
        if (isActive){
            addDeviceSongs(device.ip);
        } else {
            removeDeviceSongs(device.ip);
        }
    }

    function toggle(device){ setActive(device, !selected.has(device.id)); }

    // checks if device is discovered before applying visual hovers
    function setHover(device, isHovered){
        if (!device.discovered) return; 

        const dot = dotsLayer.querySelector('[data-id="' + device.id + '"]');
        const item = deviceList.querySelector('[data-id="' + device.id + '"]');

        if(dot) dot.classList.toggle('hovered', isHovered);
        if(item) item.classList.toggle('hovered', isHovered);
    }

    function showTooltip(device){
        ttName.textContent = device.name;
        ttIp.textContent = device.ip;

        const pos = polarToPercent(device.angle, device.radius);

        tooltip.style.left = pos.x + '%';
        tooltip.style.top = pos.y + '%';
        tooltip.classList.add('show');
    }

    function hideTooltip(){ tooltip.classList.remove('show'); }

    function buildDot(device){
        const pos = polarToPercent(device.angle, device.radius);
        const dot = document.createElement('div');

        dot.className = 'device-dot';
        dot.dataset.id = device.id;
        dot.style.left = pos.x + '%';
        dot.style.top = pos.y + '%';

        dot.setAttribute('role', 'button');
        dot.setAttribute('tabindex', '-1');
        dot.setAttribute('aria-pressed', 'false');
        dot.setAttribute('aria-label', device.name + ', ' + device.ip);

        dot.addEventListener('click', () => toggle(device));
        dot.addEventListener('keydown', (e) => {
            if(e.key === 'Enter' || e.key === ' '){ e.preventDefault(); toggle(device); }
        });
        
        dot.addEventListener('mouseenter', () => { setHover(device, true); showTooltip(device); });
        dot.addEventListener('mouseleave', () => { setHover(device, false); hideTooltip(); });

        dotsLayer.appendChild(dot);
        return dot;
    }

    function buildListItem(device){
        const li = document.createElement('li');
        li.className = 'device-item';
        li.dataset.id = device.id;

        const inputId = 'chk-' + device.id;
        li.innerHTML = [
            '<input type="checkbox" id="', inputId, '" />',
            '<label class="radio-indicator" for="', inputId, '"></label>',
            '<div class="device-meta">',
            '<span class="device-name">', device.name, '</span>',
            '<span class="device-sub"><span>', device.ip, '</span></span>',
            '</div>'
        ].join('');

        li.addEventListener('click', (e) => {
            if(e.target.closest('input, .radio-indicator')) return;
            toggle(device);
        });
        li.querySelector('input[type="checkbox"]').addEventListener('change', (e) => {
            setActive(device, e.target.checked);
        });
        
        li.addEventListener('mouseenter', () => setHover(device, true));
        li.addEventListener('mouseleave', () => setHover(device, false));

        deviceList.appendChild(li);
    }

    function discover(device){
        if(device.discovered) return;
        device.discovered = true;
        discoveredCount++;

        const dot = dotsLayer.querySelector('[data-id="' + device.id + '"]');
        const item = deviceList.querySelector('[data-id="' + device.id + '"]');

        if(dot){ dot.classList.add('discovered'); dot.setAttribute('tabindex', '0'); }
        if(item) item.classList.add('show');
    }

    function blip(device){
        discover(device);
        if(selected.has(device.id)) return; 

        const dot = dotsLayer.querySelector('[data-id="' + device.id + '"]');
        if(!dot) return;

        if(blipTimers.has(device.id)) clearTimeout(blipTimers.get(device.id));
        dot.classList.remove('blip');

        void dot.offsetWidth; 
        dot.classList.add('blip');

        const timer = setTimeout(() => {
            dot.classList.remove('blip');
            blipTimers.delete(device.id);
        }, BLIP_HOLD_MS);

        blipTimers.set(device.id, timer);
    }

    function startSweepWatcher(){
        let prevAngle = 0;
        let startTime = null;

        function frame(timestamp){
            if(startTime === null) startTime = timestamp;
            const elapsed = (timestamp - startTime) % SWEEP_DURATION_MS;
            const currentAngle = (elapsed / SWEEP_DURATION_MS) * 360;

            knownDevices.forEach((device) => {
                const passed = currentAngle >= prevAngle
                    ? (device.angle >= prevAngle && device.angle < currentAngle)
                    : (device.angle >= prevAngle || device.angle < currentAngle); 
                if(passed) blip(device);
            });

            prevAngle = currentAngle;
            requestAnimationFrame(frame);
        }
        requestAnimationFrame(frame);
    }

    function handleDeviceUpdate(event){
        const payloadList = event.detail;
        if (!payloadList) return;

        // Map incoming IDs for quick lookup
        const incomingIds = new Set(payloadList.map(d => d.hostId));

        // Clean up devices that disappeared
        knownDevices.forEach((device, id) => {
            if (!incomingIds.has(id)) {
                const dot = dotsLayer.querySelector('[data-id="' + id + '"]');
                const item = deviceList.querySelector('[data-id="' + id + '"]');
                if (dot) dot.remove();
                if (item) item.remove();

                // Clear active blip timers if any
                if (blipTimers.has(id)) {
                    clearTimeout(blipTimers.get(id));
                    blipTimers.delete(id);
                }

                // Remove songs from queue list automatically
                removeDeviceSongs(device.ip);
                
                // Remove from state trackers
                selected.delete(id);
                knownDevices.delete(id);
                if (device.discovered) discoveredCount--;
            }
        });

        // Process any new additions or modifications
        payloadList.forEach((d) => {
            if (!knownDevices.has(d.hostId)) {
                const newDevice = {
                    id: d.hostId,
                    name: d.deviceName,
                    ip: d.ip,
                    angle: Math.random() * 360,      
                    radius: 12 + Math.random() * 30, 
                    discovered: false
                };
                knownDevices.set(newDevice.id, newDevice);
                buildDot(newDevice);
                buildListItem(newDevice);
            } else {
                // Update existing device properties if IP changed dynamically
                const existing = knownDevices.get(d.hostId);
                if (existing.ip !== d.ip) {
                    existing.ip = d.ip;
                    //TODO: Update UI text
                }
            }
        });
    }

    buildTicks();
    startSweepWatcher();
    window.addEventListener('LanDeviceUpdated', handleDeviceUpdate);
})();