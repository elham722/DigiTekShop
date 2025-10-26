/**
 * Box Module
 * مدیریت باکس‌ها
 */
(function ($) {
  "use strict";
  
  let Box = {
    /**
     * مقداردهی اولیه Box
     */
    init: function () {
      this.bindEvents();
      this.initFancybox();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      $("[data-btn-box]").on("click", this.handleBoxButtonClick.bind(this));
      $("[data-btn-box-close]").on("click", this.handleBoxCloseClick.bind(this));
      $(".toggle-responsive-sidebar").on("click", this.handleToggleSidebarClick.bind(this));
      $(".responsive-sidebar-overlay").on("click", this.handleSidebarOverlayClick.bind(this));
    },

    /**
     * مدیریت کلیک روی دکمه باکس
     */
    handleBoxButtonClick: function (event) {
      event.preventDefault();
      let parent = $(this).data("parent");
      let target = $(this).data("target");
      $(parent).addClass("d-none");
      $(target).removeClass("d-none");
    },

    /**
     * مدیریت کلیک روی دکمه بستن باکس
     */
    handleBoxCloseClick: function (event) {
      event.preventDefault();
      let parent = $(this).data("parent");
      let show = $(this).data("show");
      $(parent).addClass("d-none");
      $(show).removeClass("d-none");
    },

    /**
     * مدیریت کلیک روی دکمه نمایش نوار کناری
     */
    handleToggleSidebarClick: function (e) {
      e.preventDefault();
      $(".responsive-sidebar").addClass("show");
      $(".responsive-sidebar-overlay").addClass("show");
    },

    /**
     * مدیریت کلیک روی overlay نوار کناری
     */
    handleSidebarOverlayClick: function (e) {
      $(".responsive-sidebar").removeClass("show");
      $(this).removeClass("show");
    },

    /**
     * مقداردهی Fancybox
     */
    initFancybox: function () {
      Fancybox.defaults.Hash = false;
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Box = Box;

})(jQuery);
