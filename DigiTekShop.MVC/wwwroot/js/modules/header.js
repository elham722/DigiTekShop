/**
 * Header Module
 * مدیریت هدر و جستجو
 */
(function ($) {
  "use strict";
  
  let Header = {
    /**
     * مقداردهی اولیه Header
     */
    init: function () {
      this.bindEvents();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      // جستجو
      $(".search-container .search-form .search-field").on("click", this.handleSearchFieldClick.bind(this));
      $(".search-container .search-form .btn-close-search-result").on("click", this.handleCloseSearchResult.bind(this));
      
      // ناوبری
      $(".toggle-navigation").on("click", this.handleToggleNavigation.bind(this));
      $(".navigation .toggle-submenu").on("click", this.handleToggleSubmenu.bind(this));
      $(".navigation .close-submenu").on("click", this.handleCloseSubmenu.bind(this));
      $(".navigation-overlay, .close-navigation").on("click", this.handleCloseNavigation.bind(this));
    },

    /**
     * مدیریت کلیک روی فیلد جستجو
     */
    handleSearchFieldClick: function () {
      let parents = $(this).parents(".search-container");
      parents.find(".btn-search").addClass("d-none");
      parents.find(".btn-close-search-result").removeClass("d-none");
      parents.find(".search-result-container").addClass("show");
    },

    /**
     * مدیریت بستن نتایج جستجو
     */
    handleCloseSearchResult: function (e) {
      e.preventDefault();
      let parents = $(this).parents(".search-container");
      $(this).addClass("d-none");
      parents.find(".btn-search").removeClass("d-none");
      parents.find(".search-result-container").removeClass("show");
    },

    /**
     * مدیریت باز کردن ناوبری
     */
    handleToggleNavigation: function () {
      $(".navigation").addClass("toggle");
      $(".navigation-overlay").fadeIn(100);
    },

    /**
     * مدیریت باز کردن زیرمنو
     */
    handleToggleSubmenu: function (event) {
      event.preventDefault();
      $(this).siblings(".submenu").addClass("toggle");
    },

    /**
     * مدیریت بستن زیرمنو
     */
    handleCloseSubmenu: function (event) {
      event.preventDefault();
      $(this).parent(".submenu").removeClass("toggle");
    },

    /**
     * مدیریت بستن ناوبری
     */
    handleCloseNavigation: function (event) {
      event.preventDefault();
      $(".navigation").removeClass("toggle");
      $(".navigation .submenu").removeClass("toggle");
      $(".navigation-overlay").fadeOut(100);
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Header = Header;

})(jQuery);
