/**
 * ReadMore Module
 * مدیریت متن قابل گسترش
 */
(function ($) {
  "use strict";
  
  let ReadMore = {
    /**
     * مقداردهی اولیه ReadMore
     */
    init: function () {
      this.bindEvents();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      $(".expandable-text-expand-btn").on("click", this.handleExpandClick.bind(this));
    },

    /**
     * مدیریت کلیک روی دکمه گسترش
     */
    handleExpandClick: function () {
      let contentFixedHeight = $(this).parents(".expandable-text");
      contentFixedHeight.toggleClass("active");
      $(this).find(".show-more").toggleClass("d-none");
      $(this).find(".show-less").toggleClass("d-none");
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.ReadMore = ReadMore;

})(jQuery);
