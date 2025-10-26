/**
 * Home Page
 * صفحه اصلی
 */

class HomePage {
  constructor() {
    this.config = window.CONFIG || {};
    this.utils = window.Utils || {};
    this.isInitialized = false;
    
    this.init();
  }

  init() {
    if (this.isInitialized) return;
    
    console.log('🏠 راه‌اندازی Home Page...');
    
    this.bindEvents();
    this.initSliders();
    this.initProductCards();
    this.initBanners();
    this.initCountdowns();
    
    this.isInitialized = true;
    console.log('✅ Home Page راه‌اندازی شد');
  }

  /**
   * اتصال event listeners
   */
  bindEvents() {
    // Product card events
    this.bindProductCardEvents();
    
    // Banner events
    this.bindBannerEvents();
    
    // Slider events
    this.bindSliderEvents();
  }

  /**
   * اتصال product card events
   */
  bindProductCardEvents() {
    const productCards = this.utils.$$('.product-card');
    
    productCards.forEach(card => {
      this.setupProductCard(card);
    });
  }

  /**
   * اتصال banner events
   */
  bindBannerEvents() {
    const banners = this.utils.$$('.banner-img a');
    
    banners.forEach(banner => {
      this.utils.on(banner, 'click', (e) => {
        this.handleBannerClick(banner);
      });
    });
  }

  /**
   * اتصال slider events
   */
  bindSliderEvents() {
    // این بخش می‌تواند در آینده پیاده‌سازی شود
  }

  /**
   * راه‌اندازی sliders
   */
  initSliders() {
    this.initMainSlider();
    this.initProductSliders();
    this.initCategorySliders();
  }

  /**
   * راه‌اندازی main slider
   */
  initMainSlider() {
    const mainSlider = this.utils.$('.main-swiper-slider');
    if (!mainSlider) return;
    
    // راه‌اندازی Swiper
    if (window.Swiper) {
      this.mainSwiper = new Swiper('.main-swiper-slider', {
        loop: true,
        autoplay: {
          delay: 5000,
          disableOnInteraction: false,
        },
        pagination: {
          el: '.swiper-pagination',
          clickable: true,
        },
        navigation: {
          nextEl: '.swiper-button-next',
          prevEl: '.swiper-button-prev',
        },
        effect: 'fade',
        fadeEffect: {
          crossFade: true
        }
      });
    }
  }

  /**
   * راه‌اندازی product sliders
   */
  initProductSliders() {
    const productSliders = this.utils.$$('.product-swiper-slider');
    
    productSliders.forEach(slider => {
      this.initProductSlider(slider);
    });
  }

  /**
   * راه‌اندازی product slider
   */
  initProductSlider(slider) {
    if (window.Swiper) {
      new Swiper(slider, {
        slidesPerView: 1,
        spaceBetween: 20,
        loop: true,
        autoplay: {
          delay: 3000,
          disableOnInteraction: false,
        },
        pagination: {
          el: '.swiper-pagination',
          clickable: true,
        },
        navigation: {
          nextEl: '.swiper-button-next',
          prevEl: '.swiper-button-prev',
        },
        breakpoints: {
          576: {
            slidesPerView: 2,
          },
          768: {
            slidesPerView: 3,
          },
          992: {
            slidesPerView: 4,
          },
          1200: {
            slidesPerView: 5,
          }
        }
      });
    }
  }

  /**
   * راه‌اندازی category sliders
   */
  initCategorySliders() {
    const categorySliders = this.utils.$$('.category-swiper-slider');
    
    categorySliders.forEach(slider => {
      this.initCategorySlider(slider);
    });
  }

  /**
   * راه‌اندازی category slider
   */
  initCategorySlider(slider) {
    if (window.Swiper) {
      new Swiper(slider, {
        slidesPerView: 2,
        spaceBetween: 20,
        loop: true,
        pagination: {
          el: '.swiper-pagination',
          clickable: true,
        },
        navigation: {
          nextEl: '.swiper-button-next',
          prevEl: '.swiper-button-prev',
        },
        breakpoints: {
          576: {
            slidesPerView: 3,
          },
          768: {
            slidesPerView: 4,
          },
          992: {
            slidesPerView: 6,
          },
          1200: {
            slidesPerView: 8,
          }
        }
      });
    }
  }

  /**
   * راه‌اندازی product cards
   */
  initProductCards() {
    const productCards = this.utils.$$('.product-card');
    
    productCards.forEach(card => {
      this.setupProductCard(card);
    });
  }

  /**
   * راه‌اندازی product card
   */
  setupProductCard(card) {
    // راه‌اندازی product actions
    this.setupProductActions(card);
    
    // راه‌اندازی product rating
    this.setupProductRating(card);
    
    // راه‌اندازی product countdown
    this.setupProductCountdown(card);
  }

  /**
   * راه‌اندازی product actions
   */
  setupProductActions(card) {
    const actionButtons = card.querySelectorAll('.product-actions a');
    
    actionButtons.forEach(button => {
      this.utils.on(button, 'click', (e) => {
        e.preventDefault();
        this.handleProductAction(button, card);
      });
    });
  }

  /**
   * راه‌اندازی product rating
   */
  setupProductRating(card) {
    const rating = card.querySelector('.product-rating');
    if (!rating) return;
    
    // راه‌اندازی rating stars
    this.setupRatingStars(rating);
  }

  /**
   * راه‌اندازی rating stars
   */
  setupRatingStars(rating) {
    const stars = rating.querySelectorAll('.star');
    
    stars.forEach((star, index) => {
      this.utils.on(star, 'click', () => {
        this.handleStarClick(stars, index);
      });
    });
  }

  /**
   * راه‌اندازی product countdown
   */
  setupProductCountdown(card) {
    const countdown = card.querySelector('.countdown-timer');
    if (!countdown) return;
    
    const endTime = countdown.getAttribute('data-countdown');
    if (!endTime) return;
    
    this.initCountdown(countdown, endTime);
  }

  /**
   * راه‌اندازی banners
   */
  initBanners() {
    const banners = this.utils.$$('.banner-img');
    
    banners.forEach(banner => {
      this.setupBanner(banner);
    });
  }

  /**
   * راه‌اندازی banner
   */
  setupBanner(banner) {
    const link = banner.querySelector('a');
    if (!link) return;
    
    // راه‌اندازی hover effects
    this.setupBannerHover(banner);
  }

  /**
   * راه‌اندازی banner hover
   */
  setupBannerHover(banner) {
    this.utils.on(banner, 'mouseenter', () => {
      this.utils.addClass(banner, 'hover');
    });
    
    this.utils.on(banner, 'mouseleave', () => {
      this.utils.removeClass(banner, 'hover');
    });
  }

  /**
   * راه‌اندازی countdowns
   */
  initCountdowns() {
    const countdowns = this.utils.$$('.countdown-timer');
    
    countdowns.forEach(countdown => {
      const endTime = countdown.getAttribute('data-countdown');
      if (endTime) {
        this.initCountdown(countdown, endTime);
      }
    });
  }

  /**
   * راه‌اندازی countdown
   */
  initCountdown(element, endTime) {
    const endDate = new Date(endTime).getTime();
    
    const updateCountdown = () => {
      const now = new Date().getTime();
      const distance = endDate - now;
      
      if (distance < 0) {
        element.textContent = 'زمان به پایان رسید';
        return;
      }
      
      const days = Math.floor(distance / (1000 * 60 * 60 * 24));
      const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
      const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
      const seconds = Math.floor((distance % (1000 * 60)) / 1000);
      
      element.textContent = `${days} روز ${hours} ساعت ${minutes} دقیقه ${seconds} ثانیه`;
    };
    
    updateCountdown();
    setInterval(updateCountdown, 1000);
  }

  /**
   * مدیریت کلیک روی product action
   */
  handleProductAction(button, card) {
    const action = button.getAttribute('data-action') || button.className;
    
    switch (true) {
      case action.includes('shopping-cart'):
        this.handleAddToCart(card);
        break;
      case action.includes('search'):
        this.handleQuickView(card);
        break;
      case action.includes('heart'):
        this.handleAddToWishlist(card);
        break;
      default:
        console.log('عمل نامشخص:', action);
    }
  }

  /**
   * مدیریت اضافه کردن به سبد خرید
   */
  handleAddToCart(card) {
    const productTitle = card.querySelector('.product-title a').textContent;
    console.log(`🛒 اضافه کردن به سبد خرید: ${productTitle}`);
    
    // نمایش پیام موفقیت
    this.showMessage('محصول به سبد خرید اضافه شد', 'success');
  }

  /**
   * مدیریت quick view
   */
  handleQuickView(card) {
    const productTitle = card.querySelector('.product-title a').textContent;
    console.log(`👁️ مشاهده سریع: ${productTitle}`);
    
    // باز کردن modal
    this.openQuickViewModal(card);
  }

  /**
   * مدیریت اضافه کردن به علاقمندی
   */
  handleAddToWishlist(card) {
    const productTitle = card.querySelector('.product-title a').textContent;
    console.log(`❤️ اضافه کردن به علاقمندی: ${productTitle}`);
    
    // نمایش پیام موفقیت
    this.showMessage('محصول به علاقمندی اضافه شد', 'success');
  }

  /**
   * مدیریت کلیک روی banner
   */
  handleBannerClick(banner) {
    const bannerText = banner.querySelector('img')?.alt || 'Banner';
    console.log(`🖼️ کلیک روی بنر: ${bannerText}`);
  }

  /**
   * مدیریت کلیک روی star
   */
  handleStarClick(stars, index) {
    stars.forEach((star, i) => {
      if (i <= index) {
        this.utils.addClass(star, 'active');
      } else {
        this.utils.removeClass(star, 'active');
      }
    });
  }

  /**
   * باز کردن quick view modal
   */
  openQuickViewModal(card) {
    // این بخش می‌تواند در آینده پیاده‌سازی شود
    console.log('📱 باز کردن Quick View Modal');
  }

  /**
   * نمایش پیام
   */
  showMessage(message, type = 'info') {
    // استفاده از toast notification
    if (window.Footer) {
      const footer = window.App?.getModule('footer');
      if (footer) {
        footer.showMessage(message, type);
      }
    }
  }

  /**
   * مدیریت scroll
   */
  handleScroll() {
    // اجرای scroll handlers
    this.handleScrollAnimations();
  }

  /**
   * مدیریت scroll animations
   */
  handleScrollAnimations() {
    const animatedElements = this.utils.$$('.animate-on-scroll');
    
    animatedElements.forEach(element => {
      if (this.utils.isInViewport(element)) {
        this.utils.addClass(element, 'animated');
      }
    });
  }

  /**
   * تمیز کردن event listeners
   */
  destroy() {
    // توقف sliders
    if (this.mainSwiper) {
      this.mainSwiper.destroy();
    }
    
    this.isInitialized = false;
  }
}

// Export برای استفاده در ماژول‌های دیگر
if (typeof module !== 'undefined' && module.exports) {
  module.exports = HomePage;
} else {
  window.HomePage = HomePage;
}
