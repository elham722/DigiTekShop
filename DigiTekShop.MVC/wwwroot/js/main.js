/**
 * Main JavaScript File
 * فایل اصلی JavaScript که تمام ماژول‌ها را بارگذاری می‌کند
 */
(function ($) {
  "use strict";
  
  // ایجاد namespace اصلی
  let SCRIPT = {};

  /**
   * مقداردهی اولیه تمام ماژول‌ها
   */
  function initModules() {
    // Core Modules
    SCRIPT.Preloader.init();
    
    // Module Components
    SCRIPT.Header.init();
    SCRIPT.VerticalMenu.init();
    SCRIPT.MegaSearch.init();
    SCRIPT.ShopView.init();
    
    // UI Components
    SCRIPT.Countdown.init();
    SCRIPT.Slider.init();
    SCRIPT.ReadMore.init();
    SCRIPT.Form.init();
    SCRIPT.Quantity.init();
    SCRIPT.FilterPrice.init();
    SCRIPT.Box.init();
    SCRIPT.Toast.init();
    SCRIPT.AddComment.init();
    
    // Utility Components
    SCRIPT.SimpleBar.init();
    SCRIPT.Zoom.init();
    SCRIPT.Tooltip.init();
    SCRIPT.SmoothScroll.init();
    SCRIPT.Sticky.init();
    SCRIPT.CopyClipboard.init();
  }

  /**
   * اجرای کدها پس از بارگذاری کامل صفحه
   */
  $(window).on("load", function () {
    // کدهای مربوط به بارگذاری کامل صفحه
  });

  /**
   * اجرای کدها پس از آماده شدن DOM
   */
  $(document).ready(function () {
    initModules();
  });

  // اتصال به window برای دسترسی جهانی
  window.SCRIPT = SCRIPT;

})(jQuery);
