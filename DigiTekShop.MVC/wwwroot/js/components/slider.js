/**
 * Slider Module
 * مدیریت انواع اسلایدرها
 */
(function ($) {
  "use strict";
  
  let Slider = {
    /**
     * مقداردهی اولیه تمام اسلایدرها
     */
    init: function () {
      this.initNotificationSlider();
      this.initMiniSlider();
      this.initMainSlider();
      this.initProductSlider();
      this.initProductSpecialsSlider();
      this.initCategorySlider();
      this.initCompareSlider();
      this.initCheckoutSliders();
      this.initGallerySlider();
    },

    /**
     * اسلایدر اعلان‌ها
     */
    initNotificationSlider: function () {
      const notificationSwiperSlider = new Swiper(".notification-swiper-slider", {
        slidesPerView: 1,
        spaceBetween: 10,
        direction: "vertical",
        autoplay: {
          delay: 5000,
        },
      });
    },

    /**
     * اسلایدر کوچک
     */
    initMiniSlider: function () {
      const miniSwiperSlider = new Swiper(".mini-single-swiper-slider", {
        slidesPerView: 1,
        spaceBetween: 10,
        autoplay: {
          delay: 5000,
        },
        pagination: {
          el: ".swiper-pagination",
          clickable: true,
        },
        navigation: {
          nextEl: ".swiper-button-next",
          prevEl: ".swiper-button-prev",
        },
      });
    },

    /**
     * اسلایدر اصلی
     */
    initMainSlider: function () {
      const mainSwiperSlider = new Swiper(".main-swiper-slider", {
        slidesPerView: 1,
        spaceBetween: 10,
        loop: true,
        effect: "fade",
        fadeEffect: {
          crossFade: true,
        },
        autoplay: {
          delay: 2500,
        },
        pagination: {
          el: ".swiper-pagination",
          clickable: true,
          dynamicBullets: true,
        },
        navigation: {
          nextEl: ".swiper-button-next",
          prevEl: ".swiper-button-prev",
        },
      });
    },

    /**
     * اسلایدر محصولات
     */
    initProductSlider: function () {
      const productSwiperSlider = new Swiper(".product-swiper-slider", {
        spaceBetween: 10,
        pagination: {
          el: ".swiper-pagination",
          clickable: true,
          dynamicBullets: true,
        },
        navigation: {
          nextEl: ".swiper-button-next",
          prevEl: ".swiper-button-prev",
        },
        breakpoints: {
          1200: {
            slidesPerView: 5,
          },
          1090: {
            slidesPerView: 4,
          },
          768: {
            slidesPerView: 3,
            spaceBetween: 10,
          },
          576: {
            slidesPerView: 2,
            spaceBetween: 10,
          },
          480: {
            slidesPerView: 1,
            spaceBetween: 8,
          },
        },
      });
    },

    /**
     * اسلایدر محصولات ویژه
     */
    initProductSpecialsSlider: function () {
      const productSpecialsSwiperSlider = new Swiper(".product-specials-swiper-slider", {
        spaceBetween: 10,
        pagination: {
          el: ".swiper-pagination",
          clickable: true,
          dynamicBullets: true,
        },
        navigation: {
          nextEl: ".swiper-button-next",
          prevEl: ".swiper-button-prev",
        },
        breakpoints: {
          1200: {
            slidesPerView: 4,
          },
          992: {
            slidesPerView: 3,
            spaceBetween: 10,
          },
          576: {
            slidesPerView: 3,
            spaceBetween: 10,
          },
          480: {
            slidesPerView: 2,
            spaceBetween: 8,
          },
        },
      });
    },

    /**
     * اسلایدر دسته‌بندی‌ها
     */
    initCategorySlider: function () {
      const categorySwiperSlider = new Swiper(".category-swiper-slider", {
        spaceBetween: 10,
        pagination: {
          el: ".swiper-pagination",
          clickable: true,
          dynamicBullets: true,
        },
        navigation: {
          nextEl: ".swiper-button-next",
          prevEl: ".swiper-button-prev",
        },
        breakpoints: {
          1200: {
            slidesPerView: 7,
          },
          1090: {
            slidesPerView: 6,
          },
          768: {
            slidesPerView: 5,
            spaceBetween: 10,
          },
          576: {
            slidesPerView: 4,
            spaceBetween: 10,
          },
          480: {
            slidesPerView: 3,
            spaceBetween: 8,
          },
          0: {
            slidesPerView: 2,
            spaceBetween: 8,
          },
        },
      });
    },

    /**
     * اسلایدر مقایسه
     */
    initCompareSlider: function () {
      const compareSwiperSlider = new Swiper(".compare-swiper-slider", {
        spaceBetween: 10,
        slidesPerView: "auto",
        navigation: {
          nextEl: ".swiper-button-next",
          prevEl: ".swiper-button-prev",
        },
      });
    },

    /**
     * اسلایدرهای تسویه حساب
     */
    initCheckoutSliders: function () {
      // اسلایدر بسته‌بندی
      const checkoutPackSwiperSlider = new Swiper(".checkout-pack-swiper-slider", {
        spaceBetween: 10,
        navigation: {
          nextEl: ".swiper-button-next",
          prevEl: ".swiper-button-prev",
        },
        breakpoints: {
          1200: {
            slidesPerView: 6,
          },
          1090: {
            slidesPerView: 5,
          },
          768: {
            slidesPerView: 4,
            spaceBetween: 10,
          },
          576: {
            slidesPerView: 3,
            spaceBetween: 10,
          },
          480: {
            slidesPerView: 2,
            spaceBetween: 8,
          },
        },
      });

      // اسلایدر زمان
      const checkoutTimeSwiperSlider = new Swiper(".checkout-time-swiper-slider", {
        slidesPerView: "auto",
        spaceBetween: 10,
        freeMode: true,
        navigation: {
          nextEl: ".swiper-button-next",
          prevEl: ".swiper-button-prev",
        },
      });
    },

    /**
     * اسلایدر گالری
     */
    initGallerySlider: function () {
      if ($(".gallery-swiper-slider").length) {
        const gallerySwiperSlider = new Swiper(".gallery-swiper-slider", {
          centeredSlides: true,
        });
        
        const galleryThumbsSwiperSlider = new Swiper(".gallery-thumbs-swiper-slider", {
          slidesPerView: 4,
          slideToClickedSlide: true,
          centeredSlides: true,
          spaceBetween: 15,
          navigation: {
            nextEl: ".swiper-button-next",
            prevEl: ".swiper-button-prev",
          },
        });
        
        gallerySwiperSlider.controller.control = galleryThumbsSwiperSlider;
        galleryThumbsSwiperSlider.controller.control = gallerySwiperSlider;
      }
    }
  };

  // اتصال به namespace اصلی
  if (typeof window.SCRIPT === 'undefined') {
    window.SCRIPT = {};
  }
  
  window.SCRIPT.Slider = Slider;

})(jQuery);
