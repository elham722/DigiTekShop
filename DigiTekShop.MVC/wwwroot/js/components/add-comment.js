/**
 * Add Comment Module
 * مدیریت اضافه کردن نظر
 */
(function ($) {
  "use strict";
  
  let AddComment = {
    /**
     * مقداردهی اولیه Add Comment
     */
    init: function () {
      this.initCommentForm();
    },

    /**
     * مقداردهی فرم نظر
     */
    initCommentForm: function () {
      if ($(".add-comment-product").length) {
        var inputs = $("#advantage-input, #disadvantage-input");
        var inputChangeCallback = this.handleInputChange.bind(this);
        
        inputs.each(function () {
          inputChangeCallback.bind(this)();
          $(this).on("change keyup", inputChangeCallback.bind(this));
        });

        this.bindAdvantagesEvents();
        this.bindDisadvantagesEvents();
      }
    },

    /**
     * مدیریت تغییر در فیلدهای ورودی
     */
    handleInputChange: function () {
      var self = $(this);
      if (self.val().trim().length > 0) {
        self.siblings(".js-icon-form-add").show();
      } else {
        self.siblings(".js-icon-form-add").hide();
      }
    },

    /**
     * اتصال رویدادهای مزایا
     */
    bindAdvantagesEvents: function () {
      $("#advantages")
        .delegate(".js-icon-form-add", "click", this.handleAddAdvantage.bind(this))
        .delegate(".js-icon-form-remove", "click", this.handleRemoveAdvantage.bind(this));
    },

    /**
     * اتصال رویدادهای معایب
     */
    bindDisadvantagesEvents: function () {
      $("#disadvantages")
        .delegate(".js-icon-form-add", "click", this.handleAddDisadvantage.bind(this))
        .delegate(".js-icon-form-remove", "click", this.handleRemoveDisadvantage.bind(this));
    },

    /**
     * مدیریت اضافه کردن مزیت
     */
    handleAddAdvantage: function (e) {
      var parent = $(".js-advantages-list");
      if (parent.find(".js-advantage-item").length >= 5) {
        return;
      }
      
      var advantageInput = $("#advantage-input");
      if (advantageInput.val().trim().length > 0) {
        parent.append(
          '<div class="ui-dynamic-label ui-dynamic-label--positive js-advantage-item">\n' +
            advantageInput.val() +
            '<button type="button" class="ui-dynamic-label-remove js-icon-form-remove"></button>\n' +
            '<input type="hidden" name="comment[advantages][]" value="' +
            advantageInput.val() +
            '">\n' +
            "</div>"
        );
        advantageInput.val("").change();
        advantageInput.focus();
      }
    },

    /**
     * مدیریت حذف مزیت
     */
    handleRemoveAdvantage: function (e) {
      $(this).parent(".js-advantage-item").remove();
    },

    /**
     * مدیریت اضافه کردن عیب
     */
    handleAddDisadvantage: function (e) {
      var parent = $(".js-disadvantages-list");
      if (parent.find(".js-disadvantage-item").length >= 5) {
        return;
      }
      
      var disadvantageInput = $("#disadvantage-input");
      if (disadvantageInput.val().trim().length > 0) {
        parent.append(
          '<div class="ui-dynamic-label ui-dynamic-label--negative js-disadvantage-item">\n' +
            disadvantageInput.val() +
            '<button type="button" class="ui-dynamic-label-remove js-icon-form-remove"></button>\n' +
            '<input type="hidden" name="comment[disadvantages][]" value="' +
            disadvantageInput.val() +
            '">\n' +
            "</div>"
        );
        disadvantageInput.val("").change();
        disadvantageInput.focus();
      }
    },

    /**
     * مدیریت حذف عیب
     */
    handleRemoveDisadvantage: function (e) {
      $(this).parent(".js-disadvantage-item").remove();
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.AddComment = AddComment;

})(jQuery);
