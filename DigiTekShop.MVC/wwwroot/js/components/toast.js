/**
 * Toast Module
 * مدیریت اعلان‌ها
 */
(function ($) {
  "use strict";
  
  let Toast = {
    /**
     * مقداردهی اولیه Toast
     */
    init: function () {
      this.bindEvents();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      $("[data-toast]").on("click", this.handleToastClick.bind(this));
    },

    /**
     * مدیریت کلیک روی دکمه toast
     */
    handleToastClick: function (event) {
      event.preventDefault();
      var t = $(this),
        a = t.data("toast-type"),
        e = t.data("toast-icon"),
        i = t.data("toast-position"),
        n = t.data("toast-title"),
        o = t.data("toast-message"),
        p = t.data("toast-color"),
        s = "";

      s = this.getToastConfig(a, e, i, n, o, p);
      iziToast.show(s);
    },

    /**
     * دریافت تنظیمات toast بر اساس موقعیت
     */
    getToastConfig: function (type, icon, position, title, message, color) {
      var baseConfig = {
        rtl: true,
        class: "iziToast-" + type || "",
        title: title || "Title",
        message: message || "toast message",
        animateInside: false,
        progressBar: false,
        icon: icon,
        timeout: 3200,
        transitionInMobile: "fadeIn",
        transitionOutMobile: "fadeOut",
        color: color || "blue",
      };

      switch (position) {
        case "topRight":
          return {
            ...baseConfig,
            position: "topRight",
            transitionIn: "fadeInLeft",
            transitionOut: "fadeOut",
          };
        case "bottomRight":
          return {
            ...baseConfig,
            position: "bottomRight",
            transitionIn: "fadeInLeft",
            transitionOut: "fadeOut",
          };
        case "topLeft":
          return {
            ...baseConfig,
            position: "topLeft",
            transitionIn: "fadeInRight",
            transitionOut: "fadeOut",
          };
        case "bottomLeft":
          return {
            ...baseConfig,
            position: "bottomLeft",
            transitionIn: "fadeInRight",
            transitionOut: "fadeOut",
          };
        case "topCenter":
          return {
            ...baseConfig,
            position: "topCenter",
            transitionIn: "fadeInDown",
            transitionOut: "fadeOut",
          };
        case "bottomCenter":
          return {
            ...baseConfig,
            position: "bottomCenter",
            transitionIn: "fadeInUp",
            transitionOut: "fadeOut",
          };
        default:
          return {
            ...baseConfig,
            position: "topRight",
            transitionIn: "fadeInLeft",
            transitionOut: "fadeOut",
          };
      }
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Toast = Toast;

})(jQuery);
