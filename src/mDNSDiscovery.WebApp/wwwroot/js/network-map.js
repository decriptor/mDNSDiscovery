// Network Map Visualization using Canvas
let canvas, ctx, tooltip, container;
let nodes = [];
let links = [];
let nodeIndex = {};
let camera = { x: 0, y: 0, zoom: 1 };
let isDragging = false;
let dragStart = { x: 0, y: 0 };
let hoveredNode = null;
let currentGroupBySubnet = true;
let resizeObserver = null;

export function renderNetworkMap(networkData, groupBySubnet) {
    canvas = document.getElementById('network-canvas');
    tooltip = document.getElementById('network-tooltip');
    container = document.getElementById('network-map-container');
    currentGroupBySubnet = groupBySubnet;

    if (!canvas) {
        console.error('Canvas element not found');
        return;
    }

    ctx = canvas.getContext('2d');

    // Set canvas size to match the (responsive) container
    canvas.width = container.clientWidth;
    canvas.height = container.clientHeight;

    // Process network data
    nodes = networkData.nodes.map((node) => ({
        ...node,
        x: 0,
        y: 0,
        vx: 0,
        vy: 0
    }));
    links = networkData.links || [];

    nodeIndex = {};
    nodes.forEach(node => {
        nodeIndex[node.id] = node;
    });

    // Layout nodes
    layoutNodes();

    // Setup event listeners (idempotent-ish: guarded against double-binding via disposeNetworkMap)
    setupEventListeners();

    // Start animation loop
    animate();
}

function layoutNodes() {
    if (currentGroupBySubnet) {
        layoutBySubnet();
    } else {
        layoutCircular();
    }
}

function layoutBySubnet() {
    const centerX = canvas.width / 2;
    const centerY = canvas.height / 2;
    const mainRadius = Math.min(canvas.width, canvas.height) / 3;

    const rootHub = nodes.find(n => n.id === 'hub:root');
    const subnetHubs = nodes.filter(n => n.isHub && n.id !== 'hub:root');
    const deviceNodes = nodes.filter(n => !n.isHub);

    if (rootHub) {
        rootHub.x = centerX;
        rootHub.y = centerY;
    }

    // Group device nodes by subnet
    const subnets = {};
    deviceNodes.forEach(node => {
        if (!subnets[node.subnet]) {
            subnets[node.subnet] = [];
        }
        subnets[node.subnet].push(node);
    });

    const subnetKeys = Object.keys(subnets);
    const subnetCount = Math.max(subnetKeys.length, 1);

    subnetKeys.forEach((subnet, subnetIndex) => {
        const angle = (subnetIndex / subnetCount) * Math.PI * 2;
        const subnetX = centerX + Math.cos(angle) * mainRadius;
        const subnetY = centerY + Math.sin(angle) * mainRadius;

        const hub = subnetHubs.find(h => h.subnet === subnet);
        if (hub) {
            hub.x = subnetX;
            hub.y = subnetY;
        }

        const subnetNodes = subnets[subnet];
        const nodeCount = subnetNodes.length;
        // Extra spacing between nodes so labels don't overlap at default zoom
        const subnetRadius = 90 + nodeCount * 16;

        subnetNodes.forEach((node, nodeIndex) => {
            const nodeAngle = (nodeIndex / nodeCount) * Math.PI * 2;
            node.x = subnetX + Math.cos(nodeAngle) * subnetRadius;
            node.y = subnetY + Math.sin(nodeAngle) * subnetRadius;
        });
    });
}

function layoutCircular() {
    const centerX = canvas.width / 2;
    const centerY = canvas.height / 2;
    const radius = Math.min(canvas.width, canvas.height) / 2.4;

    const rootHub = nodes.find(n => n.id === 'hub:root');
    const others = nodes.filter(n => n.id !== 'hub:root');

    if (rootHub) {
        rootHub.x = centerX;
        rootHub.y = centerY;
    }

    others.forEach((node, index) => {
        const angle = (index / Math.max(others.length, 1)) * Math.PI * 2;
        node.x = centerX + Math.cos(angle) * radius;
        node.y = centerY + Math.sin(angle) * radius;
    });
}

function setupEventListeners() {
    // Mouse events
    canvas.addEventListener('mousedown', handleMouseDown);
    canvas.addEventListener('mousemove', handleMouseMove);
    canvas.addEventListener('mouseup', handleMouseUp);
    canvas.addEventListener('wheel', handleWheel, { passive: false });
    canvas.addEventListener('mouseleave', handleMouseLeave);
    canvas.addEventListener('click', handleClick);

    // Touch events for mobile
    canvas.addEventListener('touchstart', handleTouchStart, { passive: false });
    canvas.addEventListener('touchmove', handleTouchMove, { passive: false });
    canvas.addEventListener('touchend', handleTouchEnd);

    // Keep the canvas backing store in sync with its responsive container
    if (typeof ResizeObserver !== 'undefined' && container) {
        resizeObserver = new ResizeObserver(() => handleResize());
        resizeObserver.observe(container);
    } else {
        window.addEventListener('resize', handleResize);
    }
}

function handleResize() {
    if (!canvas || !container) return;

    const newWidth = container.clientWidth;
    const newHeight = container.clientHeight;

    if (newWidth === canvas.width && newHeight === canvas.height) {
        return;
    }

    canvas.width = newWidth;
    canvas.height = newHeight;
    layoutNodes();
}

function handleMouseDown(e) {
    isDragging = true;
    dragStart = getMousePos(e);
}

function handleMouseMove(e) {
    const pos = getMousePos(e);

    if (isDragging) {
        camera.x += (pos.x - dragStart.x) / camera.zoom;
        camera.y += (pos.y - dragStart.y) / camera.zoom;
        dragStart = pos;
    } else {
        // Check for hover
        hoveredNode = getNodeAtPosition(pos.x, pos.y);

        if (hoveredNode) {
            showTooltip(hoveredNode, pos.x, pos.y);
            canvas.style.cursor = 'pointer';
        } else {
            hideTooltip();
            canvas.style.cursor = isDragging ? 'grabbing' : 'grab';
        }
    }
}

function handleMouseUp() {
    isDragging = false;
    canvas.style.cursor = 'grab';
}

function handleWheel(e) {
    e.preventDefault();
    const delta = e.deltaY > 0 ? 0.9 : 1.1;
    camera.zoom *= delta;
    camera.zoom = Math.max(0.5, Math.min(camera.zoom, 3));
}

function handleMouseLeave() {
    isDragging = false;
    hideTooltip();
    hoveredNode = null;
}

function handleClick(e) {
    const pos = getMousePos(e);
    const node = getNodeAtPosition(pos.x, pos.y);

    if (node && !node.isHub) {
        // Navigate to device details page
        const deviceId = encodeURIComponent(`${node.label}_${node.ip}`);
        window.location.href = `/device/${deviceId}`;
    }
}

function handleTouchStart(e) {
    e.preventDefault();
    if (e.touches.length === 1) {
        const touch = e.touches[0];
        dragStart = { x: touch.clientX - canvas.offsetLeft, y: touch.clientY - canvas.offsetTop };
        isDragging = true;
    }
}

function handleTouchMove(e) {
    e.preventDefault();
    if (isDragging && e.touches.length === 1) {
        const touch = e.touches[0];
        const pos = { x: touch.clientX - canvas.offsetLeft, y: touch.clientY - canvas.offsetTop };
        camera.x += (pos.x - dragStart.x) / camera.zoom;
        camera.y += (pos.y - dragStart.y) / camera.zoom;
        dragStart = pos;
    }
}

function handleTouchEnd() {
    isDragging = false;
}

function getMousePos(e) {
    const rect = canvas.getBoundingClientRect();
    return {
        x: e.clientX - rect.left,
        y: e.clientY - rect.top
    };
}

function getNodeAtPosition(x, y) {
    for (let i = nodes.length - 1; i >= 0; i--) {
        const node = nodes[i];
        const screenX = (node.x + camera.x) * camera.zoom;
        const screenY = (node.y + camera.y) * camera.zoom;
        const radius = (node.size / 2) * camera.zoom;

        const dx = x - screenX;
        const dy = y - screenY;
        const distance = Math.sqrt(dx * dx + dy * dy);

        if (distance <= radius) {
            return node;
        }
    }
    return null;
}

function showTooltip(node, x, y) {
    if (node.isHub) {
        tooltip.innerHTML = `<div style="font-weight: bold;">${node.label}</div>`;
    } else {
        tooltip.innerHTML = `
            <div style="font-weight: bold; margin-bottom: 4px;">${node.label}</div>
            <div style="font-size: 0.85rem;">
                <div>IP: ${node.ip}</div>
                <div>Vendor: ${node.vendor}</div>
                <div>Services: ${node.serviceCount}</div>
                <div style="margin-top: 4px; font-size: 0.8rem; opacity: 0.75;">${node.services}</div>
            </div>
        `;
    }
    tooltip.style.display = 'block';
    tooltip.style.left = (x + 10) + 'px';
    tooltip.style.top = (y + 10) + 'px';
}

function hideTooltip() {
    tooltip.style.display = 'none';
}

function isDarkTheme() {
    return document.documentElement.getAttribute('data-bs-theme') === 'dark';
}

function animate() {
    if (!canvas || !ctx) {
        return;
    }

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    ctx.save();

    // Draw grid
    drawGrid();

    // Draw edges first so nodes render on top of them
    drawEdges();

    // Draw nodes
    nodes.forEach(node => {
        drawNode(node);
    });

    ctx.restore();

    // Continue animation
    requestAnimationFrame(animate);
}

function drawGrid() {
    const dark = isDarkTheme();
    const gridSize = 50 * camera.zoom;
    const offsetX = (camera.x * camera.zoom) % gridSize;
    const offsetY = (camera.y * camera.zoom) % gridSize;

    ctx.strokeStyle = dark ? 'rgba(255, 255, 255, 0.08)' : '#e9ecef';
    ctx.lineWidth = 1;
    ctx.beginPath();

    // Vertical lines
    for (let x = offsetX; x < canvas.width; x += gridSize) {
        ctx.moveTo(x, 0);
        ctx.lineTo(x, canvas.height);
    }

    // Horizontal lines
    for (let y = offsetY; y < canvas.height; y += gridSize) {
        ctx.moveTo(0, y);
        ctx.lineTo(canvas.width, y);
    }

    ctx.stroke();
}

function drawEdges() {
    if (!links.length) return;

    const dark = isDarkTheme();
    ctx.strokeStyle = dark ? 'rgba(173, 181, 189, 0.35)' : 'rgba(108, 117, 125, 0.4)';
    ctx.lineWidth = Math.max(1, 1.5 * camera.zoom);

    links.forEach(link => {
        const source = nodeIndex[link.source];
        const target = nodeIndex[link.target];
        if (!source || !target) return;

        const sx = (source.x + camera.x) * camera.zoom;
        const sy = (source.y + camera.y) * camera.zoom;
        const tx = (target.x + camera.x) * camera.zoom;
        const ty = (target.y + camera.y) * camera.zoom;

        ctx.beginPath();
        ctx.moveTo(sx, sy);
        ctx.lineTo(tx, ty);
        ctx.stroke();
    });
}

function drawRoundedSquare(cx, cy, half) {
    const r = Math.min(6, half * 0.5);
    const x = cx - half;
    const y = cy - half;
    const size = half * 2;

    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + size - r, y);
    ctx.arcTo(x + size, y, x + size, y + r, r);
    ctx.lineTo(x + size, y + size - r);
    ctx.arcTo(x + size, y + size, x + size - r, y + size, r);
    ctx.lineTo(x + r, y + size);
    ctx.arcTo(x, y + size, x, y + size - r, r);
    ctx.lineTo(x, y + r);
    ctx.arcTo(x, y, x + r, y, r);
    ctx.closePath();
    ctx.fill();
}

function drawNode(node) {
    const x = (node.x + camera.x) * camera.zoom;
    const y = (node.y + camera.y) * camera.zoom;
    const radius = (node.size / 2) * camera.zoom;

    // Skip if off-screen
    if (x < -radius - 60 || x > canvas.width + radius + 60 || y < -radius - 60 || y > canvas.height + radius + 60) {
        return;
    }

    // Draw shadow if hovered
    if (hoveredNode === node) {
        ctx.shadowColor = 'rgba(0, 0, 0, 0.3)';
        ctx.shadowBlur = 10;
        ctx.shadowOffsetX = 2;
        ctx.shadowOffsetY = 2;
    }

    // Draw node shape: hubs are small rounded squares, devices are circles
    ctx.fillStyle = node.color;
    if (node.isHub) {
        drawRoundedSquare(x, y, radius * 1.1);
    } else {
        ctx.beginPath();
        ctx.arc(x, y, radius, 0, Math.PI * 2);
        ctx.fill();
    }

    // Draw border
    ctx.strokeStyle = hoveredNode === node ? '#000' : '#fff';
    ctx.lineWidth = hoveredNode === node ? 3 : 2;
    ctx.stroke();

    // Reset shadow
    ctx.shadowColor = 'transparent';
    ctx.shadowBlur = 0;
    ctx.shadowOffsetX = 0;
    ctx.shadowOffsetY = 0;

    // Draw label
    const dark = isDarkTheme();
    const fontSize = Math.max(9, (node.isHub ? 11 : 10) * camera.zoom);
    ctx.font = `${node.isHub ? 'bold ' : ''}${fontSize}px sans-serif`;
    ctx.fillStyle = dark ? '#e9ecef' : '#212529';
    ctx.textAlign = 'center';

    // Truncate long labels so neighboring labels don't overlap
    let label = node.label;
    const maxChars = node.isHub ? 16 : 10;
    if (label.length > maxChars) {
        label = label.substring(0, maxChars) + '…';
    }

    if (node.isHub) {
        // Hub labels sit above the node; device labels cluster below it,
        // so putting the hub label on the opposite side avoids collisions.
        ctx.textBaseline = 'bottom';
        ctx.fillText(label, x, y - radius - 6);
    } else {
        ctx.textBaseline = 'top';
        ctx.fillText(label, x, y + radius + 4);
    }

    // Draw service count badge
    if (!node.isHub && node.serviceCount > 1) {
        const badgeRadius = Math.max(7, 9 * camera.zoom);
        const badgeX = x + radius - badgeRadius * 0.5;
        const badgeY = y - radius + badgeRadius * 0.5;

        ctx.fillStyle = '#dc3545';
        ctx.beginPath();
        ctx.arc(badgeX, badgeY, badgeRadius, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = '#fff';
        ctx.textBaseline = 'middle';
        ctx.font = `bold ${Math.max(7, 9 * camera.zoom)}px sans-serif`;
        ctx.fillText(node.serviceCount, badgeX, badgeY);
    }
}

// Cleanup function
export function disposeNetworkMap() {
    if (canvas) {
        canvas.removeEventListener('mousedown', handleMouseDown);
        canvas.removeEventListener('mousemove', handleMouseMove);
        canvas.removeEventListener('mouseup', handleMouseUp);
        canvas.removeEventListener('wheel', handleWheel);
        canvas.removeEventListener('mouseleave', handleMouseLeave);
        canvas.removeEventListener('click', handleClick);
        canvas.removeEventListener('touchstart', handleTouchStart);
        canvas.removeEventListener('touchmove', handleTouchMove);
        canvas.removeEventListener('touchend', handleTouchEnd);
    }

    if (resizeObserver) {
        resizeObserver.disconnect();
        resizeObserver = null;
    } else {
        window.removeEventListener('resize', handleResize);
    }

    canvas = null;
    ctx = null;
}
