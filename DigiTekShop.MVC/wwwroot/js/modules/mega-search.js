/**
 * Mega Search Module
 * مدیریت جستجوی پیشرفته
 */
(function ($) {
  "use strict";
  
  let MegaSearch = {
    /**
     * مقداردهی اولیه Mega Search
     */
    init: function () {
      this.bindEvents();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      $(".user-option-btn--search").on("click", this.handleSearchButtonClick.bind(this));
      $(".mega-search-container .mega-search-box-close, .mega-search-container .mega-search-overlay").on("click", this.handleCloseSearch.bind(this));
    },

    /**
     * مدیریت کلیک روی دکمه جستجو
     */
    handleSearchButtonClick: function (e) {
      e.preventDefault();
      $(".mega-search-container").addClass("show");
    },

    /**
     * مدیریت بستن جستجو
     */
    handleCloseSearch: function () {
      $(".mega-search-container").removeClass("show");
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.MegaSearch = MegaSearch;

})(jQuery);
