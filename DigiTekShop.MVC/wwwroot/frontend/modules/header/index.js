/**
 * Header Module - Main Entry Point
 * Composes all header-related functionality
 */

import { mountHeaderSearch } from './search.js';
import { mountMegaSearch } from './mega-search.js';
import { mountVerticalMenu } from './vertical-menu.js';
import { mountNavigation } from './navigation.js';

export function mountHeader(context = {}) {
    console.log('Initializing header modules...');
    
    try {
        // Initialize all header modules
        mountHeaderSearch();
        mountMegaSearch();
        mountVerticalMenu();
        mountNavigation();
        
        console.log('Header modules initialized successfully');
    } catch (error) {
        console.error('Error initializing header modules:', error);
    }
}
