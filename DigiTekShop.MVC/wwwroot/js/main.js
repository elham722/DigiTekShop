/**
 * DigiTekShop - Modern Frontend Entry Point
 * ESM-based architecture without jQuery dependency
 */

// ========================================
// Vendor Imports (Vanilla JS Libraries)
// ========================================

// Bootstrap (if needed for JS components)
// import 'bootstrap/dist/js/bootstrap.bundle.min.js';

// Swiper
import Swiper from 'swiper';

// SimpleBar
import SimpleBar from 'simplebar';

// Fancybox
import { Fancybox } from '@fancyapps/ui';

// noUiSlider
import noUiSlider from 'nouislider';

// iziToast (if keeping)
// import iziToast from 'izitoast';
// import 'izitoast/dist/css/iziToast.min.css';

// ========================================
// App Styles
// ========================================
// CSS will be loaded via layout

// ========================================
// App Modules
// ========================================
import { mountHeader, refreshAuthStatus, handleLogout } from './modules/header/index.js';
import { mountSliders } from './modules/sliders/index.js';
import { mountForms } from './modules/forms/index.js';
import { mountShopView } from './modules/shop-view.js';
import { mountFilters } from './modules/filters/index.js';
import { mountUX } from './modules/ux/index.js';
import { mountGallery } from './modules/gallery/index.js';
import { Api } from './modules/net/api.js';
import { DigiTekNotification } from './modules/notifications/index.js';
import { AuthManager } from './modules/auth/index.js';

// ========================================
// Global Configuration
// ========================================

// Make libraries available globally for compatibility
window.Swiper = Swiper;
window.SimpleBar = SimpleBar;
window.Fancybox = Fancybox;
window.noUiSlider = noUiSlider;

// Make app modules available globally
window.DigiTekApi = Api;
window.DigiTekNotification = DigiTekNotification;
window.AuthManager = AuthManager;
window.refreshAuthStatus = refreshAuthStatus; // For refreshing auth status after login
window.handleLogout = handleLogout; // For handling logout

// ========================================
// Application Bootstrap
// ========================================

document.addEventListener('DOMContentLoaded', () => {
    console.log('DigiTekShop Frontend: Initializing...');
    
    try {
        // Initialize core modules
        mountHeader({ Swiper });
        mountSliders({ Swiper });
        mountForms();
        mountShopView();
        mountFilters({ noUiSlider });
        mountUX();
        mountGallery({ Fancybox });
        
        // Initialize auth if on auth pages
        if (document.querySelector('#phoneForm')) {
            window.authManager = new AuthManager();
        }
        
        console.log('DigiTekShop Frontend: Initialized successfully');
    } catch (error) {
        console.error('DigiTekShop Frontend: Initialization failed', error);
    }
});

// Handle page load completion
window.addEventListener('load', () => {
    // Add loaded class for preloader
    document.body.classList.add('loaded');
    
    // Initialize any components that need full page load
    console.log('DigiTekShop Frontend: Page fully loaded');
});

// ========================================
// Global Error Handling
// ========================================

window.addEventListener('error', (event) => {
    console.error('Global JavaScript Error:', event.error);
    // Could send to error tracking service here
});

window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled Promise Rejection:', event.reason);
    // Could send to error tracking service here
});

// ========================================
// Development Helpers
// ========================================

if (process.env.NODE_ENV === 'development') {
    // Add development-only features
    window.DigiTekDebug = {
        version: '1.0.0',
        modules: {
            Api,
            DigiTekNotification,
            AuthManager
        }
    };
}
