/**
 * Main JavaScript File - Legacy Version
 * فایل اصلی JavaScript - نسخه legacy
 */

(function() {
  'use strict';

  // ==========================================================================
  // Core Files
  // ==========================================================================
  
  // Load config
  if (typeof CONFIG === 'undefined') {
    console.warn('CONFIG not found, loading default config');
    window.CONFIG = {
      app: { name: 'YektaKala', version: '1.0.0', debug: true },
      animation: { duration: 300, easing: 'ease-in-out' },
      breakpoints: { xs: 576, sm: 768, md: 992, lg: 1200, xl: 1400 }
    };
  }

  // Load utils
  if (typeof Utils === 'undefined') {
    console.warn('Utils not found, loading default utils');
    window.Utils = {
      $: function(selector) { return document.querySelector(selector); },
      $$: function(selector) { return document.querySelectorAll(selector); },
      addClass: function(el, className) { if (el) el.classList.add(className); },
      removeClass: function(el, className) { if (el) el.classList.remove(className); },
      hasClass: function(el, className) { return el ? el.classList.contains(className) : false; },
      toggleClass: function(el, className) { if (el) el.classList.toggle(className); },
      debounce: function(func, wait) {
        let timeout;
        return function executedFunction(...args) {
          const later = () => { timeout = null; func(...args); };
          clearTimeout(timeout);
          timeout = setTimeout(later, wait);
        };
      },
      throttle: function(func, limit) {
        let inThrottle;
        return function(...args) {
          if (!inThrottle) {
            func.apply(this, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
          }
        };
      }
    };
  }

  // ==========================================================================
  // App Initialization
  // ==========================================================================
  
  class App {
    constructor() {
      this.config = window.CONFIG || {};
      this.utils = window.Utils || {};
      this.modules = {};
      this.isLoaded = false;
    }

    init() {
      console.log('🚀 راه‌اندازی YektaKala (Legacy)...');
      
      // راه‌اندازی preloader
      this.initPreloader();
      
      // راه‌اندازی ماژول‌ها
      this.initModules();
      
      // راه‌اندازی event listeners
      this.initEventListeners();
      
      this.isLoaded = true;
      console.log('✅ YektaKala راه‌اندازی شد');
    }

    initPreloader() {
      const preloaderTime = this.config.animation?.preloaderFadeOutTime || 500;
      
      window.addEventListener('load', () => {
        setTimeout(() => {
          document.body.classList.add('loaded');
          console.log('📄 صفحه بارگذاری شد');
        }, preloaderTime);
      });
    }

    initModules() {
      // راه‌اندازی ماژول‌های component
      this.initComponentModules();
      
      // راه‌اندازی ماژول‌های page
      this.initPageModules();
    }

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

    initPageModules() {
      const currentPage = this.getCurrentPage();
      
      switch (currentPage) {
        case 'home':
          if (window.HomePage) {
            this.modules.homePage = new window.HomePage();
          }
          break;
        default:
          console.log(`📄 صفحه ${currentPage} شناسایی نشد`);
      }
    }

    getCurrentPage() {
      const path = window.location.pathname;
      const filename = path.split('/').pop().split('.')[0];
      
      if (filename === 'index' || filename === '') {
        return 'home';
      } else if (filename.includes('product')) {
        return 'product';
      } else if (filename.includes('cart')) {
        return 'cart';
      }
      
      return 'unknown';
    }

    initEventListeners() {
      // Resize event
      window.addEventListener('resize', this.utils.debounce(() => {
        this.handleResize();
      }, 250));

      // Scroll event
      window.addEventListener('scroll', this.utils.throttle(() => {
        this.handleScroll();
      }, 100));
    }

    handleResize() {
      // اجرای ماژول‌های resize
      Object.values(this.modules).forEach(module => {
        if (module && typeof module.handleResize === 'function') {
          module.handleResize();
        }
      });
    }

    handleScroll() {
      // اجرای ماژول‌های scroll
      Object.values(this.modules).forEach(module => {
        if (module && typeof module.handleScroll === 'function') {
          module.handleScroll();
        }
      });
    }

    getModule(name) {
      return this.modules[name];
    }
  }

  // ایجاد instance اصلی
  const app = new App();

  // راه‌اندازی خودکار
  document.addEventListener('DOMContentLoaded', () => {
    app.init();
  });

  // Export برای استفاده در ماژول‌های دیگر
  window.App = app;

})();
