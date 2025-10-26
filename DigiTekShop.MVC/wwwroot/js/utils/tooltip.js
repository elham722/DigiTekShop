/**
 * Tooltip Module
 * مدیریت راهنماها
 */
(function ($) {
  "use strict";
  
  let Tooltip = {
    /**
     * مقداردهی اولیه Tooltip
     */
    init: function () {
      this.initTooltips();
    },

    /**
     * مقداردهی تمام tooltip ها
     */
    initTooltips: function () {
      $('[data-bs-toggle="tooltip"]').tooltip();
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Tooltip = Tooltip;

})(jQuery);
