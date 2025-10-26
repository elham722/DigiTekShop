/**
 * Dependencies Loader
 * بارگذاری کتابخانه‌های خارجی
 */

// لیست کتابخانه‌های مورد نیاز
const dependencies = [
  // Core Libraries
  'js/dependencies/jquery-3.6.0.min.js',
  'js/dependencies/bootstrap.bundle.min.js',
  
  // UI Components
  'js/dependencies/swiper-bundle.min.js',
  'js/dependencies/select2.min.js',
  'js/dependencies/fancybox.umd.js',
  'js/dependencies/remodal.min.js',
  'js/dependencies/iziToast.min.js',
  
  // Form Controls
  'js/dependencies/bootstrap-slider.min.js',
  'js/dependencies/nouislider.min.js',
  'js/dependencies/wNumb.js',
  
  // Utilities
  'js/dependencies/simplebar.min.js',
  'js/dependencies/jquery.countdown.min.js',
  'js/dependencies/jquery.simple.timer.min.js',
  'js/dependencies/zoomsl.min.js'
];

/**
 * بارگذاری وابستگی‌ها
 */
function loadDependencies() {
  dependencies.forEach(function(src) {
    const script = document.createElement('script');
    script.src = src;
    script.async = false; // بارگذاری همزمان برای حفظ ترتیب
    document.head.appendChild(script);
  });
}

// اجرای بارگذاری وابستگی‌ها
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', loadDependencies);
} else {
  loadDependencies();
}
