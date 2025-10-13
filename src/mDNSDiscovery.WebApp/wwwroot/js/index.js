// Filter functionality
function updateFilters() {
    const vendors = new Set();
    const types = new Set();

    document.querySelectorAll('.device-card').forEach(card => {
        const vendor = card.getAttribute('data-vendor');
        const type = card.getAttribute('data-type');
        if (vendor) vendors.add(vendor);
        if (type) types.add(type);
    });

    const vendorSelect = document.getElementById('filterVendor');
    const typeSelect = document.getElementById('filterType');

    const currentVendor = vendorSelect.value;
    const currentType = typeSelect.value;

    vendorSelect.innerHTML = '<option value="">All Vendors</option>';
    Array.from(vendors).sort().forEach(vendor => {
        const option = document.createElement('option');
        option.value = vendor;
        option.textContent = vendor;
        if (vendor === currentVendor) option.selected = true;
        vendorSelect.appendChild(option);
    });

    typeSelect.innerHTML = '<option value="">All Types</option>';
    Array.from(types).sort().forEach(type => {
        const option = document.createElement('option');
        option.value = type;
        option.textContent = type;
        if (type === currentType) option.selected = true;
        typeSelect.appendChild(option);
    });
}

function sortDevices() {
    const sortBy = document.getElementById('sortBy').value;
    const container = document.querySelector('#devices-container .row');
    const cards = Array.from(document.querySelectorAll('.device-card'));

    cards.sort((a, b) => {
        let aVal, bVal;

        switch (sortBy) {
            case 'name':
                aVal = (a.getAttribute('data-name') || '').toLowerCase();
                bVal = (b.getAttribute('data-name') || '').toLowerCase();
                return aVal.localeCompare(bVal);
            case 'name-desc':
                aVal = (a.getAttribute('data-name') || '').toLowerCase();
                bVal = (b.getAttribute('data-name') || '').toLowerCase();
                return bVal.localeCompare(aVal);
            case 'ip':
                aVal = a.getAttribute('data-ip') || '';
                bVal = b.getAttribute('data-ip') || '';
                // Sort IPs numerically
                const aNum = aVal.split('.').map(n => parseInt(n).toString().padStart(3, '0')).join('');
                const bNum = bVal.split('.').map(n => parseInt(n).toString().padStart(3, '0')).join('');
                return aNum.localeCompare(bNum);
            case 'lastseen':
                aVal = a.querySelector('.card').getAttribute('data-last-seen') || '';
                bVal = b.querySelector('.card').getAttribute('data-last-seen') || '';
                return bVal.localeCompare(aVal); // Newest first
            case 'lastseen-asc':
                aVal = a.querySelector('.card').getAttribute('data-last-seen') || '';
                bVal = b.querySelector('.card').getAttribute('data-last-seen') || '';
                return aVal.localeCompare(bVal); // Oldest first
            case 'vendor':
                aVal = (a.getAttribute('data-vendor') || '').toLowerCase();
                bVal = (b.getAttribute('data-vendor') || '').toLowerCase();
                return aVal.localeCompare(bVal);
            case 'type':
                aVal = (a.getAttribute('data-type') || '').toLowerCase();
                bVal = (b.getAttribute('data-type') || '').toLowerCase();
                return aVal.localeCompare(bVal);
            default:
                return 0;
        }
    });

    // Re-append cards in sorted order
    cards.forEach(card => container.appendChild(card));
}

function applyFilters() {
    const vendorFilter = document.getElementById('filterVendor').value.toLowerCase();
    const typeFilter = document.getElementById('filterType').value.toLowerCase();
    const searchFilter = document.getElementById('searchDevice').value.toLowerCase();

    let visibleCount = 0;
    const totalCount = document.querySelectorAll('.device-card').length;

    document.querySelectorAll('.device-card').forEach(card => {
        const vendor = (card.getAttribute('data-vendor') || '').toLowerCase();
        const type = (card.getAttribute('data-type') || '').toLowerCase();
        const searchData = (card.getAttribute('data-search') || '').toLowerCase();

        const matchesVendor = !vendorFilter || vendor === vendorFilter;
        const matchesType = !typeFilter || type === typeFilter;
        const matchesSearch = !searchFilter || searchData.includes(searchFilter);

        if (matchesVendor && matchesType && matchesSearch) {
            card.style.display = '';
            visibleCount++;
        } else {
            card.style.display = 'none';
        }
    });

    const filteredCountText = visibleCount === totalCount
        ? ''
        : `Showing ${visibleCount} of ${totalCount} devices`;
    document.getElementById('filteredCount').textContent = filteredCountText;

    // Apply sorting after filtering
    sortDevices();
}

// Initialize Bootstrap tooltips
function initTooltips() {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
}

// Auto-refresh state
let autoRefreshEnabled = true;
let refreshInterval = null;

// Refresh function
function performRefresh() {
    // Save filter collapse state before reload
    const filtersCollapse = document.getElementById('filtersCollapse');
    if (filtersCollapse) {
        const isOpen = filtersCollapse.classList.contains('show');
        localStorage.setItem('filtersCollapseState', isOpen ? 'open' : 'closed');
    }

    // Save filter values before reload
    saveFilterValues();

    // Trigger Blazor component refresh via SignalR circuit
    if (window.Blazor && window.Blazor.reconnect) {
        location.reload();
    }
}

// Save filter values to localStorage
function saveFilterValues() {
    const filterVendor = document.getElementById('filterVendor');
    const filterType = document.getElementById('filterType');
    const searchDevice = document.getElementById('searchDevice');
    const sortBy = document.getElementById('sortBy');

    if (filterVendor) localStorage.setItem('filterVendor', filterVendor.value);
    if (filterType) localStorage.setItem('filterType', filterType.value);
    if (searchDevice) localStorage.setItem('searchDevice', searchDevice.value);
    if (sortBy) localStorage.setItem('sortBy', sortBy.value);
}

// Restore filter values from localStorage
function restoreFilterValues() {
    const filterVendor = document.getElementById('filterVendor');
    const filterType = document.getElementById('filterType');
    const searchDevice = document.getElementById('searchDevice');
    const sortBy = document.getElementById('sortBy');

    if (filterVendor && localStorage.getItem('filterVendor')) {
        filterVendor.value = localStorage.getItem('filterVendor');
    }
    if (filterType && localStorage.getItem('filterType')) {
        filterType.value = localStorage.getItem('filterType');
    }
    if (searchDevice && localStorage.getItem('searchDevice')) {
        searchDevice.value = localStorage.getItem('searchDevice');
    }
    if (sortBy && localStorage.getItem('sortBy')) {
        sortBy.value = localStorage.getItem('sortBy');
    }
}

// Manual refresh button
function setupRefreshButton() {
    const refreshBtn = document.getElementById('refreshNow');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', function () {
            const btn = this;
            const icon = btn.querySelector('i');

            // Add spinning animation
            icon.classList.add('spin');
            btn.disabled = true;

            // Save filter collapse state before reload
            const filtersCollapse = document.getElementById('filtersCollapse');
            if (filtersCollapse) {
                const isOpen = filtersCollapse.classList.contains('show');
                localStorage.setItem('filtersCollapseState', isOpen ? 'open' : 'closed');
            }

            // Save filter values before reload
            saveFilterValues();

            location.reload();
        });
    }
}

// Toggle auto-refresh button
function setupAutoRefreshToggle() {
    const toggleBtn = document.getElementById('toggleAutoRefresh');
    if (toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            autoRefreshEnabled = !autoRefreshEnabled;
            const btn = this;
            const icon = btn.querySelector('i');
            const text = document.getElementById('autoRefreshText');

            if (autoRefreshEnabled) {
                // Resume auto-refresh
                btn.classList.remove('btn-outline-danger');
                btn.classList.add('btn-outline-success');
                icon.classList.remove('bi-play-circle');
                icon.classList.add('bi-pause-circle');
                text.textContent = 'Auto';
                btn.title = 'Pause auto-refresh';

                // Start interval
                if (refreshInterval === null) {
                    refreshInterval = setInterval(() => {
                        if (autoRefreshEnabled) {
                            performRefresh();
                        }
                    }, 5000);
                }
            } else {
                // Pause auto-refresh
                btn.classList.remove('btn-outline-success');
                btn.classList.add('btn-outline-danger');
                icon.classList.remove('bi-pause-circle');
                icon.classList.add('bi-play-circle');
                text.textContent = 'Paused';
                btn.title = 'Resume auto-refresh';
            }
        });
    }
}

// Initialize the page
export function initializeIndexPage() {
    // Restore filter collapse state from localStorage WITHOUT animation to prevent flicker
    const filtersCollapse = document.getElementById('filtersCollapse');
    const savedState = localStorage.getItem('filtersCollapseState');
    if (filtersCollapse && savedState === 'open') {
        // Add 'show' class directly without animation to prevent flicker
        filtersCollapse.classList.add('show');
    }

    // Restore filter values from localStorage
    restoreFilterValues();

    // Set up event listeners
    const filterVendor = document.getElementById('filterVendor');
    const filterType = document.getElementById('filterType');
    const searchDevice = document.getElementById('searchDevice');
    const sortBy = document.getElementById('sortBy');
    const clearFilters = document.getElementById('clearFilters');

    if (filterVendor) filterVendor.addEventListener('change', applyFilters);
    if (filterType) filterType.addEventListener('change', applyFilters);
    if (searchDevice) searchDevice.addEventListener('input', applyFilters);
    if (sortBy) sortBy.addEventListener('change', applyFilters);
    if (clearFilters) {
        clearFilters.addEventListener('click', function () {
            document.getElementById('filterVendor').value = '';
            document.getElementById('filterType').value = '';
            document.getElementById('searchDevice').value = '';
            document.getElementById('sortBy').value = 'lastseen';

            // Clear localStorage
            localStorage.removeItem('filterVendor');
            localStorage.removeItem('filterType');
            localStorage.removeItem('searchDevice');
            localStorage.setItem('sortBy', 'lastseen');

            applyFilters();
        });
    }

    // Initialize filters
    updateFilters();

    // Apply filters after restoration to maintain filtered view
    applyFilters();

    // Initialize tooltips
    initTooltips();

    // Setup refresh buttons
    setupRefreshButton();
    setupAutoRefreshToggle();

    // Start auto-refresh interval
    refreshInterval = setInterval(() => {
        if (autoRefreshEnabled) {
            performRefresh();
        }
    }, 5000);
}

// Clean up when leaving the page
export function disposeIndexPage() {
    if (refreshInterval) {
        clearInterval(refreshInterval);
        refreshInterval = null;
    }
}
