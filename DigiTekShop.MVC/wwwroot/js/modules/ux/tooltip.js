/**
 * Tooltip Module
 * Bootstrap-based tooltip functionality
 */

export function mountTooltip(root = document) {
    const tooltipElements = root.querySelectorAll('[data-bs-toggle="tooltip"]');
    
    if (tooltipElements.length === 0) {
        console.warn('Tooltip: No tooltip elements found');
        return;
    }

    // Check if Bootstrap is available
    if (typeof bootstrap === 'undefined') {
        console.warn('Tooltip: Bootstrap not available');
        return;
    }

    tooltipElements.forEach(element => {
        try {
            new bootstrap.Tooltip(element, {
                placement: element.dataset.bsPlacement || 'top',
                trigger: element.dataset.bsTrigger || 'hover',
                delay: {
                    show: 200,
                    hide: 100
                }
            });
        } catch (error) {
            console.error('Error initializing tooltip:', error);
        }
    });

    console.log('Tooltip module initialized');
}
