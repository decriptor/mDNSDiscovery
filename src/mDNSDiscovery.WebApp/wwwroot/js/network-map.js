// Network Map Visualization using Canvas
let canvas, ctx, tooltip;
let nodes = [];
let camera = { x: 0, y: 0, zoom: 1 };
let isDragging = false;
let dragStart = { x: 0, y: 0 };
let selectedNode = null;
let hoveredNode = null;

export function renderNetworkMap(networkData, groupBySubnet) {
    canvas = document.getElementById('network-canvas');
    tooltip = document.getElementById('network-tooltip');

    if (!canvas) {
        console.error('Canvas element not found');
        return;
    }

    ctx = canvas.getContext('2d');

    // Set canvas size
    const container = document.getElementById('network-map-container');
    canvas.width = container.clientWidth;
    canvas.height = container.clientHeight;

    // Process network data
    nodes = networkData.nodes.map((node, index) => ({
        ...node,
        x: 0,
        y: 0,
        vx: 0,
        vy: 0
    }));

    // Layout nodes
    if (groupBySubnet) {
        layoutBySubnet();
    } else {
        layoutCircular();
    }

    // Setup event listeners
    setupEventListeners();

    // Start animation loop
    animate();
}

function layoutBySubnet() {
    // Group nodes by subnet
    const subnets = {};
    nodes.forEach(node => {
        if (!subnets[node.subnet]) {
            subnets[node.subnet] = [];
        }
        subnets[node.subnet].push(node);
    });

    const subnetKeys = Object.keys(subnets);
    const subnetCount = subnetKeys.length;

    const centerX = canvas.width / 2;
    const centerY = canvas.height / 2;
    const mainRadius = Math.min(canvas.width, canvas.height) / 3;

    subnetKeys.forEach((subnet, subnetIndex) => {
        const angle = (subnetIndex / subnetCount) * Math.PI * 2;
        const subnetX = centerX + Math.cos(angle) * mainRadius;
        const subnetY = centerY + Math.sin(angle) * mainRadius;

        const subnetNodes = subnets[subnet];
        const nodeCount = subnetNodes.length;
        const subnetRadius = 80 + nodeCount * 10;

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
    const radius = Math.min(canvas.width, canvas.height) / 3;

    nodes.forEach((node, index) => {
        const angle = (index / nodes.length) * Math.PI * 2;
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

    if (node) {
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
    tooltip.innerHTML = `
        <div style="font-weight: bold; margin-bottom: 4px;">${node.label}</div>
        <div style="font-size: 0.85rem; color: #666;">
            <div>IP: ${node.ip}</div>
            <div>Vendor: ${node.vendor}</div>
            <div>Services: ${node.serviceCount}</div>
            <div style="margin-top: 4px; font-size: 0.8rem; color: #999;">${node.services}</div>
        </div>
    `;
    tooltip.style.display = 'block';
    tooltip.style.left = (x + 10) + 'px';
    tooltip.style.top = (y + 10) + 'px';
}

function hideTooltip() {
    tooltip.style.display = 'none';
}

function animate() {
    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Set up transform
    ctx.save();

    // Draw grid
    drawGrid();

    // Draw nodes
    nodes.forEach(node => {
        drawNode(node);
    });

    ctx.restore();

    // Continue animation
    requestAnimationFrame(animate);
}

function drawGrid() {
    const gridSize = 50 * camera.zoom;
    const offsetX = (camera.x * camera.zoom) % gridSize;
    const offsetY = (camera.y * camera.zoom) % gridSize;

    ctx.strokeStyle = '#e9ecef';
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

function drawNode(node) {
    const x = (node.x + camera.x) * camera.zoom;
    const y = (node.y + camera.y) * camera.zoom;
    const radius = (node.size / 2) * camera.zoom;

    // Skip if off-screen
    if (x < -radius || x > canvas.width + radius || y < -radius || y > canvas.height + radius) {
        return;
    }

    // Draw shadow if hovered
    if (hoveredNode === node) {
        ctx.shadowColor = 'rgba(0, 0, 0, 0.3)';
        ctx.shadowBlur = 10;
        ctx.shadowOffsetX = 2;
        ctx.shadowOffsetY = 2;
    }

    // Draw node circle
    ctx.fillStyle = node.color;
    ctx.beginPath();
    ctx.arc(x, y, radius, 0, Math.PI * 2);
    ctx.fill();

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
    const fontSize = Math.max(10, 12 * camera.zoom);
    ctx.font = `${fontSize}px sans-serif`;
    ctx.fillStyle = '#212529';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';

    // Truncate long labels
    let label = node.label;
    const maxWidth = radius * 2;
    const textWidth = ctx.measureText(label).width;

    if (textWidth > maxWidth && label.length > 8) {
        label = label.substring(0, 8) + '...';
    }

    ctx.fillText(label, x, y + radius + fontSize + 5);

    // Draw service count badge
    if (node.serviceCount > 1) {
        const badgeRadius = Math.max(8, 10 * camera.zoom);
        const badgeX = x + radius - badgeRadius;
        const badgeY = y - radius + badgeRadius;

        ctx.fillStyle = '#dc3545';
        ctx.beginPath();
        ctx.arc(badgeX, badgeY, badgeRadius, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = '#fff';
        ctx.font = `bold ${Math.max(8, 10 * camera.zoom)}px sans-serif`;
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
}
