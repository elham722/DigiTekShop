/**
 * Filters Module - Main Entry Point
 * Composes all filter-related functionality
 */

import { mountPriceFilter } from './price.js';

export function mountFilters(context = {}) {
    console.log('Initializing filter modules...');
    
    try {
        // Initialize all filter modules
        mountPriceFilter(context);
        
        console.log('Filter modules initialized successfully');
    } catch (error) {
        console.error('Error initializing filter modules:', error);
    }
}
