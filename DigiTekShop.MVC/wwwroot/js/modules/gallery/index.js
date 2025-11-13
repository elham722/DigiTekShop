/**
 * Gallery Module - Main Entry Point
 * Composes all gallery-related functionality
 */

export function mountGallery(context = {}) {
    const { Fancybox } = context;
    
    console.log('Initializing gallery modules...');
    
    try {
        // Configure Fancybox if available
        if (Fancybox) {
            Fancybox.defaults.Hash = false;
            Fancybox.defaults.toolbar = {
                display: {
                    left: ["infobar"],
                    middle: [],
                    right: ["slideshow", "thumbs", "close"]
                }
            };
            Fancybox.defaults.Thumbs = {
                autoStart: false,
                hideOnClose: true
            };
            Fancybox.defaults.Slideshow = {
                autoStart: false,
                speed: 3000
            };
        }
        
        console.log('Gallery modules initialized successfully');
    } catch (error) {
        console.error('Error initializing gallery modules:', error);
    }
}
