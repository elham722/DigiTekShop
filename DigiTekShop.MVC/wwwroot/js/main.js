/**
 * Main JavaScript File
 * فایل اصلی JavaScript
 */

// ==========================================================================
// Core Files
// ==========================================================================
import './core/config.js';
import './utils/utils.js';
import './core/init.js';

// ==========================================================================
// Component Files
// ==========================================================================
import './components/header.js';
import './components/search.js';
import './components/navigation.js';
import './components/footer.js';

// ==========================================================================
// Page Files
// ==========================================================================
import './pages/home.js';

// ==========================================================================
// Additional Components (can be added later)
// ==========================================================================
// import './components/minicart.js';
// import './components/slider.js';
// import './components/modal.js';

// ==========================================================================
// Additional Pages (can be added later)
// ==========================================================================
// import './pages/product.js';
// import './pages/cart.js';
// import './pages/checkout.js';
// import './pages/profile.js';

// ==========================================================================
// Legacy Support (for non-module environments)
// ==========================================================================
if (typeof window !== 'undefined') {
  // راه‌اندازی خودکار در محیط‌های غیر-module
  document.addEventListener('DOMContentLoaded', () => {
    console.log('🚀 JavaScript modules loaded');
  });
}
