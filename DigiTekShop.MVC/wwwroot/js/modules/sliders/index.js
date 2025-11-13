/**
 * Sliders Module - Main Entry Point
 * Composes all slider-related functionality
 */

import { mountHeroSlider } from './hero.js';
import { mountProductSlider } from './product.js';
import { mountGallerySlider } from './gallery.js';
import { mountCheckoutSlider } from './checkout.js';

export function mountSliders(context = {}) {
    console.log('Initializing slider modules...');
    
    try {
        // Initialize all slider modules
        mountHeroSlider(context);
        mountProductSlider(context);
        mountGallerySlider(context);
        mountCheckoutSlider(context);
        
        console.log('Slider modules initialized successfully');
    } catch (error) {
        console.error('Error initializing slider modules:', error);
    }
}
