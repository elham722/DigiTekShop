/**
 * Zoom Module
 * Vanilla JS replacement for zoomsl functionality
 * Uses medium-zoom as a modern alternative
 */

export function mountZoom(root = document) {
    const zoomImages = root.querySelectorAll('.zoom-img');
    
    if (zoomImages.length === 0) {
        console.warn('Zoom: No zoom images found');
        return;
    }

    // Check if medium-zoom is available
    if (typeof mediumZoom === 'undefined') {
        console.warn('Zoom: medium-zoom not available, using native zoom');
        return;
    }

    // Only enable zoom on desktop
    if (window.innerWidth > 768) {
        try {
            mediumZoom(zoomImages, {
                margin: 24,
                background: 'rgba(0, 0, 0, 0.8)',
                scrollOffset: 0,
                container: {
                    top: 0,
                    right: 0,
                    bottom: 0,
                    left: 0
                },
                template: `
                    <div class="zoom-overlay">
                        <div class="zoom-container">
                            <img class="zoom-image" />
                            <div class="zoom-close">×</div>
                        </div>
                    </div>
                `
            });
        } catch (error) {
            console.error('Error initializing zoom:', error);
        }
    }

    console.log('Zoom module initialized');
}
