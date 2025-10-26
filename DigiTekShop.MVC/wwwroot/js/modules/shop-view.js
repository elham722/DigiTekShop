/**
 * Shop View Module
 * مدیریت نمایش فروشگاه (Grid/List)
 */
(function ($) {
  "use strict";
  
  let ShopView = {
    /**
     * مقداردهی اولیه Shop View
     */
    init: function () {
      this.bindEvents();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      $(".btn-list-view").on("click", this.handleListViewClick.bind(this));
      $(".btn-grid-view").on("click", this.handleGridViewClick.bind(this));
    },

    /**
     * مدیریت کلیک روی نمایش لیستی
     */
    handleListViewClick: function () {
      $(".listing-products-content .product-card-container").removeClass(
        "col-xl-3 col-lg-4 col-md-6 col-sm-6"
      );
      $(".listing-products-content .product-card-container").addClass(
        "col-lg-6 col-md-12 col-sm-6"
      );
      $(
        ".listing-products-content .product-card-container .product-card"
      ).addClass("product-card-horizontal");
    },

    /**
     * مدیریت کلیک روی نمایش شبکه‌ای
     */
    handleGridViewClick: function () {
      $(".listing-products-content .product-card-container").removeClass(
        "col-lg-6 col-md-12 col-sm-6"
      );
      $(".listing-products-content .product-card-container").addClass(
        "col-xl-3 col-lg-4 col-md-6 col-sm-6"
      );
      $(
        ".listing-products-content .product-card-container .product-card"
      ).removeClass("product-card-horizontal");
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.ShopView = ShopView;

})(jQuery);
