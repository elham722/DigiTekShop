/**
 * Sticky Module
 * مدیریت چسبندگی عناصر
 */
(function ($) {
  "use strict";
  
  let Sticky = {
    /**
     * مقداردهی اولیه Sticky
     */
    init: function () {
      this.initCompareSticky();
    },

    /**
     * مقداردهی چسبندگی مقایسه
     */
    initCompareSticky: function () {
      if ($(".compare-container .compare-list").length) {
        let productsList = $(".compare-container .compare-list"),
          top = productsList.offset().top;

        $(window).scroll(function () {
          if ($(this).scrollTop() >= top - 100) {
            productsList.addClass("is-sticky");
          } else {
            productsList.removeClass("is-sticky");
          }
        });
      }
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Sticky = Sticky;

})(jQuery);
