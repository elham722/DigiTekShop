/**
 * SimpleBar Module
 * مدیریت اسکرول بار سفارشی
 */
(function ($) {
  "use strict";
  
  let SimpleBar = {
    /**
     * مقداردهی اولیه SimpleBar
     */
    init: function () {
      this.initSimpleBars();
    },

    /**
     * مقداردهی تمام SimpleBar ها
     */
    initSimpleBars: function () {
      if ($(".do-simplebar").length) {
        $(".do-simplebar").each(function (index, el) {
          new SimpleBar(el, {
            autoHide: false,
            direction: "rtl",
          });
        });
      }
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.SimpleBar = SimpleBar;

})(jQuery);
