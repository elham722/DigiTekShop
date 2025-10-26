/**
 * Initialization File
 * فایل راه‌اندازی اصلی
 */

class App {
  constructor() {
    this.config = window.CONFIG || {};
    this.utils = window.Utils || {};
    this.modules = {};
    this.isLoaded = false;
  }

  /**
   * راه‌اندازی اولیه
   */
  init() {
    console.log('🚀 راه‌اندازی YektaKala...');
    
    // راه‌اندازی preloader
    this.initPreloader();
    
    // راه‌اندازی ماژول‌ها
    this.initModules();
    
    // راه‌اندازی event listeners
    this.initEventListeners();
    
    // راه‌اندازی responsive
    this.initResponsive();
    
    this.isLoaded = true;
    console.log('✅ YektaKala راه‌اندازی شد');
  }

  /**
   * راه‌اندازی preloader
   */
  initPreloader() {
    const preloaderTime = this.config.animation?.preloaderFadeOutTime || 500;
    
    window.addEventListener('load', () => {
      setTimeout(() => {
        document.body.classList.add('loaded');
        console.log('📄 صفحه بارگذاری شد');
      }, preloaderTime);
    });
  }

  /**
   * راه‌اندازی ماژول‌ها
   */
  initModules() {
    // راه‌اندازی ماژول‌های core
    this.initCoreModules();
    
    // راه‌اندازی ماژول‌های component
    this.initComponentModules();
    
    // راه‌اندازی ماژول‌های page
    this.initPageModules();
  }

  /**
   * راه‌اندازی ماژول‌های core
   */
  initCoreModules() {
    // Dark Mode
    if (window.DarkModeToggle) {
      this.modules.darkMode = new window.DarkModeToggle();
    }
  }

  /**
   * راه‌اندازی ماژول‌های component
   */
  initComponentModules() {
    // Header
    if (window.Header) {
      this.modules.header = new window.Header();
    }

    // Search
    if (window.Search) {
      this.modules.search = new window.Search();
    }

    // Navigation
    if (window.Navigation) {
      this.modules.navigation = new window.Navigation();
    }

    // Footer
    if (window.Footer) {
      this.modules.footer = new window.Footer();
    }
  }

  /**
   * راه‌اندازی ماژول‌های page
   */
  initPageModules() {
    const currentPage = this.getCurrentPage();
    
    switch (currentPage) {
      case 'home':
        if (window.HomePage) {
          this.modules.homePage = new window.HomePage();
        }
        break;
      case 'product':
        if (window.ProductPage) {
          this.modules.productPage = new window.ProductPage();
        }
        break;
      case 'cart':
        if (window.CartPage) {
          this.modules.cartPage = new window.CartPage();
        }
        break;
      default:
        console.log(`📄 صفحه ${currentPage} شناسایی نشد`);
    }
  }

  /**
   * تشخیص صفحه فعلی
   */
  getCurrentPage() {
    const path = window.location.pathname;
    const filename = path.split('/').pop().split('.')[0];
    
    // تشخیص بر اساس نام فایل
    if (filename === 'index' || filename === '') {
      return 'home';
    } else if (filename.includes('product')) {
      return 'product';
    } else if (filename.includes('cart')) {
      return 'cart';
    } else if (filename.includes('checkout')) {
      return 'checkout';
    } else if (filename.includes('profile')) {
      return 'profile';
    }
    
    return 'unknown';
  }

  /**
   * راه‌اندازی event listeners
   */
  initEventListeners() {
    // Resize event
    window.addEventListener('resize', this.utils.debounce(() => {
      this.handleResize();
    }, 250));

    // Scroll event
    window.addEventListener('scroll', this.utils.throttle(() => {
      this.handleScroll();
    }, 100));

    // قبل از بسته شدن صفحه
    window.addEventListener('beforeunload', () => {
      this.handleBeforeUnload();
    });
  }

  /**
   * راه‌اندازی responsive
   */
  initResponsive() {
    this.currentBreakpoint = this.getCurrentBreakpoint();
    
    // راه‌اندازی responsive modules
    this.initResponsiveModules();
  }

  /**
   * تشخیص breakpoint فعلی
   */
  getCurrentBreakpoint() {
    const width = window.innerWidth;
    const breakpoints = this.config.breakpoints || {};
    
    if (width >= breakpoints.xl) return 'xl';
    if (width >= breakpoints.lg) return 'lg';
    if (width >= breakpoints.md) return 'md';
    if (width >= breakpoints.sm) return 'sm';
    return 'xs';
  }

  /**
   * راه‌اندازی ماژول‌های responsive
   */
  initResponsiveModules() {
    // Mobile Navigation
    if (this.currentBreakpoint === 'xs' || this.currentBreakpoint === 'sm') {
      if (window.MobileNavigation) {
        this.modules.mobileNavigation = new window.MobileNavigation();
      }
    }
  }

  /**
   * مدیریت resize
   */
  handleResize() {
    const newBreakpoint = this.getCurrentBreakpoint();
    
    if (newBreakpoint !== this.currentBreakpoint) {
      this.currentBreakpoint = newBreakpoint;
      this.handleBreakpointChange();
    }
  }

  /**
   * مدیریت تغییر breakpoint
   */
  handleBreakpointChange() {
    console.log(`📱 Breakpoint تغییر کرد: ${this.currentBreakpoint}`);
    
    // راه‌اندازی مجدد ماژول‌های responsive
    this.initResponsiveModules();
  }

  /**
   * مدیریت scroll
   */
  handleScroll() {
    // اجرای ماژول‌های scroll
    Object.values(this.modules).forEach(module => {
      if (module && typeof module.handleScroll === 'function') {
        module.handleScroll();
      }
    });
  }

  /**
   * مدیریت beforeunload
   */
  handleBeforeUnload() {
    // ذخیره وضعیت فعلی
    this.saveState();
  }

  /**
   * ذخیره وضعیت
   */
  saveState() {
    const state = {
      timestamp: Date.now(),
      breakpoint: this.currentBreakpoint,
      scrollPosition: window.pageYOffset
    };
    
    this.utils.setStorage('app_state', state);
  }

  /**
   * بازیابی وضعیت
   */
  restoreState() {
    const state = this.utils.getStorage('app_state');
    
    if (state && Date.now() - state.timestamp < 300000) { // 5 دقیقه
      if (state.scrollPosition) {
        window.scrollTo(0, state.scrollPosition);
      }
    }
  }

  /**
   * دریافت ماژول
   */
  getModule(name) {
    return this.modules[name];
  }

  /**
   * ثبت ماژول جدید
   */
  registerModule(name, module) {
    this.modules[name] = module;
  }

  /**
   * حذف ماژول
   */
  unregisterModule(name) {
    if (this.modules[name]) {
      delete this.modules[name];
    }
  }
}

// ایجاد instance اصلی
const app = new App();

// راه‌اندازی خودکار
document.addEventListener('DOMContentLoaded', () => {
  app.init();
});

// Export برای استفاده در ماژول‌های دیگر
if (typeof module !== 'undefined' && module.exports) {
  module.exports = App;
} else {
  window.App = app;
}
