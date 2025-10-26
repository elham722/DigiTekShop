/**
 * Header Component
 * کامپوننت هدر
 */

class Header {
  constructor() {
    this.config = window.CONFIG || {};
    this.utils = window.Utils || {};
    this.isInitialized = false;
    
    this.init();
  }

  init() {
    if (this.isInitialized) return;
    
    console.log('🔧 راه‌اندازی Header...');
    
    this.bindEvents();
    this.initSearch();
    this.initNavigation();
    this.initUserOptions();
    
    this.isInitialized = true;
    console.log('✅ Header راه‌اندازی شد');
  }

  /**
   * اتصال event listeners
   */
  bindEvents() {
    // Search events
    this.bindSearchEvents();
    
    // Navigation events
    this.bindNavigationEvents();
    
    // User options events
    this.bindUserOptionsEvents();
  }

  /**
   * اتصال search events
   */
  bindSearchEvents() {
    const searchFields = this.utils.$$('.search-field');
    const closeButtons = this.utils.$$('.btn-close-search-result');
    
    searchFields.forEach(field => {
      this.utils.on(field, 'click', (e) => {
        this.handleSearchFieldClick(e.target);
      });
    });
    
    closeButtons.forEach(button => {
      this.utils.on(button, 'click', (e) => {
        e.preventDefault();
        this.handleSearchClose(e.target);
      });
    });
  }

  /**
   * اتصال navigation events
   */
  bindNavigationEvents() {
    const toggleButtons = this.utils.$$('.toggle-navigation');
    const submenuToggles = this.utils.$$('.toggle-submenu');
    const submenuCloses = this.utils.$$('.close-submenu');
    const overlays = this.utils.$$('.navigation-overlay, .close-navigation');
    
    toggleButtons.forEach(button => {
      this.utils.on(button, 'click', (e) => {
        this.handleNavigationToggle();
      });
    });
    
    submenuToggles.forEach(toggle => {
      this.utils.on(toggle, 'click', (e) => {
        e.preventDefault();
        this.handleSubmenuToggle(e.target);
      });
    });
    
    submenuCloses.forEach(close => {
      this.utils.on(close, 'click', (e) => {
        e.preventDefault();
        this.handleSubmenuClose(e.target);
      });
    });
    
    overlays.forEach(overlay => {
      this.utils.on(overlay, 'click', (e) => {
        e.preventDefault();
        this.handleNavigationClose();
      });
    });
  }

  /**
   * اتصال user options events
   */
  bindUserOptionsEvents() {
    const userOptionButtons = this.utils.$$('.user-option-btn');
    
    userOptionButtons.forEach(button => {
      this.utils.on(button, 'click', (e) => {
        this.handleUserOptionClick(e.target);
      });
    });
  }

  /**
   * راه‌اندازی search
   */
  initSearch() {
    this.searchContainers = this.utils.$$('.search-container');
    
    // راه‌اندازی search برای هر container
    this.searchContainers.forEach(container => {
      this.initSearchContainer(container);
    });
  }

  /**
   * راه‌اندازی search container
   */
  initSearchContainer(container) {
    const searchField = container.querySelector('.search-field');
    const searchResult = container.querySelector('.search-result-container');
    const searchButton = container.querySelector('.btn-search');
    const closeButton = container.querySelector('.btn-close-search-result');
    
    if (searchField && searchResult) {
      // تنظیم initial state
      this.utils.removeClass(searchResult, 'show');
      if (closeButton) {
        this.utils.addClass(closeButton, 'd-none');
      }
      if (searchButton) {
        this.utils.removeClass(searchButton, 'd-none');
      }
    }
  }

  /**
   * راه‌اندازی navigation
   */
  initNavigation() {
    this.navigation = this.utils.$('.navigation');
    this.navigationOverlay = this.utils.$('.navigation-overlay');
    
    // راه‌اندازی vertical menu
    this.initVerticalMenu();
  }

  /**
   * راه‌اندازی vertical menu
   */
  initVerticalMenu() {
    const verticalMenuItems = this.utils.$$('.vertical-menu-items > ul > li');
    
    verticalMenuItems.forEach(item => {
      this.utils.on(item, 'mouseenter', () => {
        this.handleVerticalMenuHover(item);
      });
    });
  }

  /**
   * راه‌اندازی user options
   */
  initUserOptions() {
    this.userOptions = this.utils.$$('.user-option');
    
    // راه‌اندازی dropdowns
    this.initUserDropdowns();
  }

  /**
   * راه‌اندازی user dropdowns
   */
  initUserDropdowns() {
    const dropdownButtons = this.utils.$$('.user-option-btn--account');
    
    dropdownButtons.forEach(button => {
      this.utils.on(button, 'click', (e) => {
        e.preventDefault();
        this.handleUserDropdownToggle(button);
      });
    });
  }

  /**
   * مدیریت کلیک روی search field
   */
  handleSearchFieldClick(field) {
    const container = this.utils.parent(field, '.search-container');
    if (!container) return;
    
    const searchButton = container.querySelector('.btn-search');
    const closeButton = container.querySelector('.btn-close-search-result');
    const searchResult = container.querySelector('.search-result-container');
    
    if (searchButton) {
      this.utils.addClass(searchButton, 'd-none');
    }
    
    if (closeButton) {
      this.utils.removeClass(closeButton, 'd-none');
    }
    
    if (searchResult) {
      this.utils.addClass(searchResult, 'show');
    }
  }

  /**
   * مدیریت بستن search
   */
  handleSearchClose(button) {
    const container = this.utils.parent(button, '.search-container');
    if (!container) return;
    
    const searchButton = container.querySelector('.btn-search');
    const searchResult = container.querySelector('.search-result-container');
    
    this.utils.addClass(button, 'd-none');
    
    if (searchButton) {
      this.utils.removeClass(searchButton, 'd-none');
    }
    
    if (searchResult) {
      this.utils.removeClass(searchResult, 'show');
    }
  }

  /**
   * مدیریت toggle navigation
   */
  handleNavigationToggle() {
    if (this.navigation) {
      this.utils.addClass(this.navigation, 'toggle');
    }
    
    if (this.navigationOverlay) {
      this.navigationOverlay.style.display = 'block';
      this.navigationOverlay.style.opacity = '1';
    }
  }

  /**
   * مدیریت بستن navigation
   */
  handleNavigationClose() {
    if (this.navigation) {
      this.utils.removeClass(this.navigation, 'toggle');
    }
    
    // بستن تمام submenu ها
    const submenus = this.utils.$$('.submenu');
    submenus.forEach(submenu => {
      this.utils.removeClass(submenu, 'toggle');
    });
    
    if (this.navigationOverlay) {
      this.navigationOverlay.style.opacity = '0';
      setTimeout(() => {
        this.navigationOverlay.style.display = 'none';
      }, 300);
    }
  }

  /**
   * مدیریت toggle submenu
   */
  handleSubmenuToggle(toggle) {
    const submenu = this.utils.parent(toggle).querySelector('.submenu');
    if (submenu) {
      this.utils.addClass(submenu, 'toggle');
    }
  }

  /**
   * مدیریت بستن submenu
   */
  handleSubmenuClose(close) {
    const submenu = this.utils.parent(close, '.submenu');
    if (submenu) {
      this.utils.removeClass(submenu, 'toggle');
    }
  }

  /**
   * مدیریت hover روی vertical menu
   */
  handleVerticalMenuHover(item) {
    // حذف show از سایر items
    const allItems = this.utils.$$('.vertical-menu-items > ul > li');
    allItems.forEach(otherItem => {
      if (otherItem !== item) {
        this.utils.removeClass(otherItem, 'show');
      }
    });
    
    // اضافه کردن show به item فعلی
    this.utils.addClass(item, 'show');
  }

  /**
   * مدیریت کلیک روی user option
   */
  handleUserOptionClick(button) {
    const userOption = this.utils.parent(button, '.user-option');
    if (!userOption) return;
    
    // مدیریت dropdown
    if (this.utils.hasClass(button, 'user-option-btn--account')) {
      this.handleUserDropdownToggle(button);
    }
  }

  /**
   * مدیریت toggle user dropdown
   */
  handleUserDropdownToggle(button) {
    const dropdown = this.utils.parent(button).querySelector('.user-option--dropdown');
    if (!dropdown) return;
    
    // بستن سایر dropdown ها
    const allDropdowns = this.utils.$$('.user-option--dropdown');
    allDropdowns.forEach(otherDropdown => {
      if (otherDropdown !== dropdown) {
        this.utils.removeClass(otherDropdown, 'show');
      }
    });
    
    // toggle dropdown فعلی
    this.utils.toggleClass(dropdown, 'show');
  }

  /**
   * مدیریت scroll
   */
  handleScroll() {
    // اضافه کردن sticky behavior
    this.handleStickyHeader();
  }

  /**
   * مدیریت sticky header
   */
  handleStickyHeader() {
    const header = this.utils.$('.page-header');
    if (!header) return;
    
    const scrollTop = window.pageYOffset;
    const threshold = 100;
    
    if (scrollTop > threshold) {
      this.utils.addClass(header, 'sticky');
    } else {
      this.utils.removeClass(header, 'sticky');
    }
  }

  /**
   * تمیز کردن event listeners
   */
  destroy() {
    // حذف event listeners
    // این بخش می‌تواند در آینده پیاده‌سازی شود
    this.isInitialized = false;
  }
}

// Export برای استفاده در ماژول‌های دیگر
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Header;
} else {
  window.Header = Header;
}
