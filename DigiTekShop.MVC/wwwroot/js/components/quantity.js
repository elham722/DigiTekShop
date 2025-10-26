/**
 * Quantity Module
 * کنترل تعداد محصول
 */
(function ($) {
  "use strict";
  
  let Quantity = {
    /**
     * مقداردهی اولیه Quantity
     */
    init: function () {
      this.bindEvents();
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      $(".num-in span").click(this.handleQuantityClick.bind(this));
    },

    /**
     * مدیریت کلیک روی دکمه‌های +/-
     */
    handleQuantityClick: function () {
      var $input = $(this).parents(".num-block").find("input.in-num");
      
      if ($(this).hasClass("minus")) {
        var count = parseFloat($input.val()) - 1;
        count = count < 1 ? 1 : count;
        
        if (count < 2) {
          $(this).addClass("dis");
        } else {
          $(this).removeClass("dis");
        }
        
        $input.val(count);
      } else {
        var count = parseFloat($input.val()) + 1;
        $input.val(count);
        
        if (count > 1) {
          $(this).parents(".num-block").find(".minus").removeClass("dis");
        }
      }

      $input.change();
      return false;
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Quantity = Quantity;

})(jQuery);
