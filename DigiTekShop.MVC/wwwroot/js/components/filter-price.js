/**
 * Filter Price Module
 * فیلتر قیمت
 */
(function ($) {
  "use strict";
  
  let FilterPrice = {
    /**
     * مقداردهی اولیه Filter Price
     */
    init: function () {
      this.initPriceSlider();
    },

    /**
     * مقداردهی اسلایدر قیمت
     */
    initPriceSlider: function () {
      if ($(".filter-price").length) {
        var skipSlider = document.getElementById("slider-non-linear-step");
        var $sliderFrom = document.querySelector(".js-slider-range-from");
        var $sliderTo = document.querySelector(".js-slider-range-to");
        var min = parseInt($sliderFrom.dataset.range),
          max = parseInt($sliderTo.dataset.range);
          
        noUiSlider.create(skipSlider, {
          start: [$sliderFrom.value, $sliderTo.value],
          connect: true,
          direction: "rtl",
          format: wNumb({
            thousand: ",",
            decimals: 0,
          }),
          step: 1,
          range: {
            min: min,
            max: max,
          },
        });
        
        var skipValues = [
          document.getElementById("skip-value-lower"),
          document.getElementById("skip-value-upper"),
        ];
        
        skipSlider.noUiSlider.on("update", function (values, handle) {
          skipValues[handle].value = values[handle];
        });
      }
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.FilterPrice = FilterPrice;

})(jQuery);
