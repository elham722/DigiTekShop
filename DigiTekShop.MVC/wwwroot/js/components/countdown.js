/**
 * Countdown Module
 * مدیریت شمارش معکوس
 */
(function ($) {
  "use strict";
  
  let Countdown = {
    /**
     * مقداردهی اولیه Countdown
     */
    init: function () {
      this.initVerifyCodeTimer();
      this.initProductCountdown();
    },

    /**
     * مقداردهی تایمر کد تایید
     */
    initVerifyCodeTimer: function () {
      $("#timer--verify-code").startTimer({
        onComplete: function (element) {
          $(".verify-code-wrapper .send-again").addClass("d-inline-block");
          onReset: $(".verify-code-wrapper .send-again");
        },
      });

      $(".verify-code-wrapper .send-again").on("click", this.handleSendAgainClick.bind(this));
    },

    /**
     * مدیریت کلیک روی "ارسال مجدد"
     */
    handleSendAgainClick: function (event) {
      event.preventDefault();
      $(this).removeClass("d-inline-block");
      $("#timer--verify-code").empty();
      $("#timer--verify-code").startTimer({});
    },

    /**
     * مقداردهی شمارش معکوس محصولات
     */
    initProductCountdown: function () {
      $("[data-countdown]").each(function () {
        var $this = $(this),
          finalDate = $(this).data("countdown");
        
        $this.countdown(finalDate, function (event) {
          $this.html(
            event.strftime(
              "<span>%D</span><span class='divider'>:</span><span>%H</span><span class='divider'>:</span><span>%M</span><span class='divider'>:</span><span>%S</span>"
            )
          );
        });
      });
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Countdown = Countdown;

})(jQuery);
