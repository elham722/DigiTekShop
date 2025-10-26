/**
 * Configuration File
 * تنظیمات کلی پروژه
 */

const CONFIG = {
  // تنظیمات عمومی
  app: {
    name: 'YektaKala',
    version: '1.0.0',
    debug: true
  },

  // تنظیمات انیمیشن
  animation: {
    duration: 300,
    easing: 'ease-in-out',
    preloaderFadeOutTime: 500
  },

  // تنظیمات API
  api: {
    baseUrl: '/api',
    timeout: 10000,
    retryAttempts: 3
  },

  // تنظیمات localStorage
  storage: {
    prefix: 'yektakala_',
    darkMode: 'darkMode',
    userPreferences: 'userPreferences'
  },

  // تنظیمات breakpoints
  breakpoints: {
    xs: 576,
    sm: 768,
    md: 992,
    lg: 1200,
    xl: 1400
  },

  // تنظیمات selectors
  selectors: {
    preloader: '.preloader',
    navigation: '.navigation',
    navigationOverlay: '.navigation-overlay',
    searchContainer: '.search-container',
    searchField: '.search-field',
    searchResult: '.search-result-container',
    darkModeToggle: '.dark-mode-toggle'
  },

  // تنظیمات events
  events: {
    click: 'click',
    load: 'load',
    resize: 'resize',
    scroll: 'scroll',
    change: 'change',
    submit: 'submit'
  },

  // تنظیمات messages
  messages: {
    success: 'عملیات با موفقیت انجام شد',
    error: 'خطا در انجام عملیات',
    loading: 'در حال بارگذاری...',
    noResults: 'نتیجه‌ای یافت نشد'
  }
};

// Export برای استفاده در ماژول‌های دیگر
if (typeof module !== 'undefined' && module.exports) {
  module.exports = CONFIG;
} else {
  window.CONFIG = CONFIG;
}
