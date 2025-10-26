/**
 * Preloader Module
 * مدیریت نمایش صفحه بارگذاری
 */
(function ($) {
  "use strict";
  
  let Preloader = {
    /**
     * مقداردهی اولیه Preloader
     */
    init: function () {
      this.bindEvents();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      $(window).on("load", this.handleWindowLoad.bind(this));
    },

    /**
     * مدیریت رویداد بارگذاری کامل صفحه
     */
    handleWindowLoad: function () {
      let preloaderFadeOutTime = 500;

      setTimeout(() => {
        $("body").addClass("loaded");
      }, preloaderFadeOutTime);
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Preloader = Preloader;

})(jQuery);
