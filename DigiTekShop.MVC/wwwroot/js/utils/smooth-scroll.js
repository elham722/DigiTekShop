/**
 * Smooth Scroll Module
 * مدیریت اسکرول نرم
 */
(function ($) {
  "use strict";
  
  let SmoothScroll = {
    /**
     * مقداردهی اولیه Smooth Scroll
     */
    init: function () {
      this.bindEvents();
      this.scrNav();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      var link = $(".product-tabs a.nav-link");
      
      // Move to specific section when click on menu link
      link.on("click", this.handleLinkClick.bind(this));
      
      // Run the scrNav when scroll
      $(window).on("scroll", this.scrNav.bind(this));
    },

    /**
     * مدیریت کلیک روی لینک
     */
    handleLinkClick: function (e) {
      var target = $($(this).attr("href"));
      $("html, body").animate(
        {
          scrollTop: target.offset().top,
        },
        600
      );
      $(this).addClass("active");
      e.preventDefault();
    },

    /**
     * scrNav function
     * Change active dot according to the active section in the window
     */
    scrNav: function () {
      var sTop = $(window).scrollTop();
      $(".tab-content").each(function () {
        var id = $(this).attr("id"),
          offset = $(this).offset().top - 1,
          height = $(this).height();
          
        if (sTop >= offset && sTop < offset + height) {
          $(".product-tabs a.nav-link").removeClass("active");
          $(".product-tabs")
            .find('[data-scroll="' + id + '"]')
            .addClass("active");
        }
      });
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.SmoothScroll = SmoothScroll;

})(jQuery);
