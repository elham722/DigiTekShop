/**
 * Vertical Menu Module
 * مدیریت منوی عمودی
 */
(function ($) {
  "use strict";
  
  let VerticalMenu = {
    /**
     * مقداردهی اولیه Vertical Menu
     */
    init: function () {
      this.bindEvents();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      $(".vertical-menu-items>ul>li").on("mouseenter", this.handleMenuItemHover.bind(this));
    },

    /**
     * مدیریت hover روی آیتم‌های منو
     */
    handleMenuItemHover: function () {
      $(this).addClass("show").siblings().removeClass("show");
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.VerticalMenu = VerticalMenu;

})(jQuery);
