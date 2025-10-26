/**
 * Zoom Module
 * مدیریت زوم تصاویر
 */
(function ($) {
  "use strict";
  
  let Zoom = {
    /**
     * مقداردهی اولیه Zoom
     */
    init: function () {
      this.initImageZoom();
    },

    /**
     * مقداردهی زوم تصاویر
     */
    initImageZoom: function () {
      if ($(window).width() > 768) {
        $(".zoom-img").imagezoomsl({
          zoomrange: [2.12, 12],
          magnifiersize: [530, 340],
          scrollspeedanimate: 10,
          loopspeedanimate: 5,
          cursorshadeborder: "10px solid black",
        });
      }
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Zoom = Zoom;

})(jQuery);
