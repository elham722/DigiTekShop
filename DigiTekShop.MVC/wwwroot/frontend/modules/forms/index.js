/**
 * Forms Module - Main Entry Point
 * Composes all form-related functionality
 */

import { mountQuantity } from './quantity.js';
import { mountSelect } from './select.js';
import { mountInputCode } from './input-code.js';
import { mountRadio } from './radio.js';

export function mountForms(root = document) {
    console.log('Initializing form modules...');
    
    try {
        // Initialize all form modules
        mountQuantity(root);
        mountSelect(root);
        mountInputCode(root);
        mountRadio(root);
        
        console.log('Form modules initialized successfully');
    } catch (error) {
        console.error('Error initializing form modules:', error);
    }
}
