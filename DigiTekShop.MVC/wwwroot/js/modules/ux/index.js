/**
 * UX Module - Main Entry Point
 * Composes all UX-related functionality
 */

import { mountTooltip } from './tooltip.js';
import { mountReadMore } from './readmore.js';
import { mountSmoothScroll } from './smooth-scroll.js';
import { mountCopyClipboard } from './copy-clipboard.js';
import { mountBox } from './box.js';
import { mountSticky } from './sticky.js';
import { mountZoom } from './zoom.js';

export function mountUX(root = document) {
    console.log('Initializing UX modules...');
    
    try {
        // Initialize all UX modules
        mountTooltip(root);
        mountReadMore(root);
        mountSmoothScroll(root);
        mountCopyClipboard(root);
        mountBox(root);
        mountSticky(root);
        mountZoom(root);
        
        console.log('UX modules initialized successfully');
    } catch (error) {
        console.error('Error initializing UX modules:', error);
    }
}
