/**
 * Form Module
 * مدیریت فرم‌ها
 */
(function ($) {
  "use strict";
  
  let Form = {
    /**
     * مقداردهی اولیه Form
     */
    init: function () {
      this.initSelect2();
      this.bindEvents();
    },

    /**
     * مقداردهی Select2
     */
    initSelect2: function () {
      if ($(".select2").length) {
        $(".select2").select2({
          dir: "rtl",
        });
      }
    },

    /**
     * اتصال رویدادها
     */
    bindEvents: function () {
      $(".form-input-code-container .input-code").keyup(this.handleInputCodeKeyup.bind(this));
      $(".custom-radio-circle-label").on("click", this.handleRadioLabelClick.bind(this));
    },

    /**
     * مدیریت keyup در فیلدهای کد
     */
    handleInputCodeKeyup: function (e) {
      if (this.value.length === this.maxLength) {
        let next = $(this).data("next");
        $("#input-code-" + next).focus();
      }
    },

    /**
     * مدیریت کلیک روی برچسب رادیو
     */
    handleRadioLabelClick: function () {
      let label = $(this).data("variant-label");
      $(".product-variant-selected").text(label);
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Form = Form;

})(jQuery);
