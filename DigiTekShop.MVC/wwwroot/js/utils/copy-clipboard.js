/**
 * Copy Clipboard Module
 * مدیریت کپی به کلیپ‌بورد
 */
(function ($) {
  "use strict";
  
  let CopyClipboard = {
    /**
     * مقداردهی اولیه Copy Clipboard
     */
    init: function () {
      this.bindEvents();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      $(".copy-url-btn").on("click", this.handleCopyClick.bind(this));
    },

    /**
     * مدیریت کلیک روی دکمه کپی
     */
    handleCopyClick: function () {
      var btn = $(this);
      this.copyClipboard($(this).data("copy"));
      $(this).addClass("copied");
      $(this).html("کپی شد");
      
      setTimeout(function () {
        btn.removeClass("copied");
        btn.html("کپی لینک");
      }, 2000);
    },

    /**
     * تابع کپی به کلیپ‌بورد
     */
    copyClipboard: function (text) {
      var field = document.createElement("input");
      field.setAttribute("value", text);
      field.setAttribute("contenteditable", true); // IOS compatibility
      document.body.appendChild(field);
      field.select();
      document.execCommand("copy");
      document.body.removeChild(field);
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.CopyClipboard = CopyClipboard;

})(jQuery);
