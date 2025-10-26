/**
 * Navigation Component
 * کامپوننت ناوبری
 */

class Navigation {
  constructor() {
    this.config = window.CONFIG || {};
    this.utils = window.Utils || {};
    this.isInitialized = false;
    this.isOpen = false;
    
    this.init();
  }

  init() {
    if (this.isInitialized) return;
    
    console.log('🧭 راه‌اندازی Navigation...');
    
    this.bindEvents();
    this.initNavigation();
    this.initSubmenus();
    
    this.isInitialized = true;
    console.log('✅ Navigation راه‌اندازی شد');
  }

  /**
   * اتصال event listeners
   */
  bindEvents() {
    // Toggle events
    this.bindToggleEvents();
    
    // Submenu events
    this.bindSubmenuEvents();
    
    // Overlay events
    this.bindOverlayEvents();
    
    // Keyboard events
    this.bindKeyboardEvents();
  }

  /**
   * اتصال toggle events
   */
  bindToggleEvents() {
    const toggleButtons = this.utils.$$('.toggle-navigation');
    
    toggleButtons.forEach(button => {
      this.utils.on(button, 'click', (e) => {
        e.preventDefault();
        this.toggleNavigation();
      });
    });
  }

  /**
   * اتصال submenu events
   */
  bindSubmenuEvents() {
    const submenuToggles = this.utils.$$('.toggle-submenu');
    const submenuCloses = this.utils.$$('.close-submenu');
    
    submenuToggles.forEach(toggle => {
      this.utils.on(toggle, 'click', (e) => {
        e.preventDefault();
        this.toggleSubmenu(toggle);
      });
    });
    
    submenuCloses.forEach(close => {
      this.utils.on(close, 'click', (e) => {
        e.preventDefault();
        this.closeSubmenu(close);
      });
    });
  }

  /**
   * اتصال overlay events
   */
  bindOverlayEvents() {
    const overlays = this.utils.$$('.navigation-overlay, .close-navigation');
    
    overlays.forEach(overlay => {
      this.utils.on(overlay, 'click', (e) => {
        e.preventDefault();
        this.closeNavigation();
      });
    });
  }

  /**
   * اتصال keyboard events
   */
  bindKeyboardEvents() {
    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && this.isOpen) {
        this.closeNavigation();
      }
    });
  }

  /**
   * راه‌اندازی navigation
   */
  initNavigation() {
    this.navigation = this.utils.$('.navigation');
    this.navigationOverlay = this.utils.$('.navigation-overlay');
    
    if (this.navigation) {
      this.setupNavigation();
    }
  }

  /**
   * راه‌اندازی submenus
   */
  initSubmenus() {
    this.submenus = this.utils.$$('.submenu');
    
    this.submenus.forEach(submenu => {
      this.setupSubmenu(submenu);
    });
  }

  /**
   * راه‌اندازی navigation
   */
  setupNavigation() {
    // تنظیم initial state
    this.utils.removeClass(this.navigation, 'toggle');
    
    // تنظیم accessibility
    this.navigation.setAttribute('aria-hidden', 'true');
    this.navigation.setAttribute('role', 'navigation');
  }

  /**
   * راه‌اندازی submenu
   */
  setupSubmenu(submenu) {
    // تنظیم initial state
    this.utils.removeClass(submenu, 'toggle');
    
    // تنظیم accessibility
    submenu.setAttribute('aria-hidden', 'true');
  }

  /**
   * toggle navigation
   */
  toggleNavigation() {
    if (this.isOpen) {
      this.closeNavigation();
    } else {
      this.openNavigation();
    }
  }

  /**
   * باز کردن navigation
   */
  openNavigation() {
    if (this.navigation) {
      this.utils.addClass(this.navigation, 'toggle');
      this.navigation.setAttribute('aria-hidden', 'false');
    }
    
    if (this.navigationOverlay) {
      this.navigationOverlay.style.display = 'block';
      this.navigationOverlay.style.opacity = '1';
    }
    
    this.isOpen = true;
    
    // جلوگیری از scroll body
    document.body.style.overflow = 'hidden';
    
    console.log('📱 Navigation باز شد');
  }

  /**
   * بستن navigation
   */
  closeNavigation() {
    if (this.navigation) {
      this.utils.removeClass(this.navigation, 'toggle');
      this.navigation.setAttribute('aria-hidden', 'true');
    }
    
    // بستن تمام submenu ها
    this.closeAllSubmenus();
    
    if (this.navigationOverlay) {
      this.navigationOverlay.style.opacity = '0';
      setTimeout(() => {
        this.navigationOverlay.style.display = 'none';
      }, 300);
    }
    
    this.isOpen = false;
    
    // بازگرداندن scroll body
    document.body.style.overflow = '';
    
    console.log('📱 Navigation بسته شد');
  }

  /**
   * toggle submenu
   */
  toggleSubmenu(toggle) {
    const submenu = this.utils.parent(toggle).querySelector('.submenu');
    if (!submenu) return;
    
    if (this.utils.hasClass(submenu, 'toggle')) {
      this.closeSubmenu(toggle);
    } else {
      this.openSubmenu(toggle);
    }
  }

  /**
   * باز کردن submenu
   */
  openSubmenu(toggle) {
    const submenu = this.utils.parent(toggle).querySelector('.submenu');
    if (!submenu) return;
    
    // بستن سایر submenu ها
    this.closeAllSubmenus();
    
    // باز کردن submenu فعلی
    this.utils.addClass(submenu, 'toggle');
    submenu.setAttribute('aria-hidden', 'false');
    
    console.log('📂 Submenu باز شد');
  }

  /**
   * بستن submenu
   */
  closeSubmenu(close) {
    const submenu = this.utils.parent(close, '.submenu');
    if (!submenu) return;
    
    this.utils.removeClass(submenu, 'toggle');
    submenu.setAttribute('aria-hidden', 'true');
    
    console.log('📂 Submenu بسته شد');
  }

  /**
   * بستن تمام submenu ها
   */
  closeAllSubmenus() {
    this.submenus.forEach(submenu => {
      this.utils.removeClass(submenu, 'toggle');
      submenu.setAttribute('aria-hidden', 'true');
    });
  }

  /**
   * مدیریت scroll
   */
  handleScroll() {
    // اضافه کردن sticky behavior
    this.handleStickyNavigation();
  }

  /**
   * مدیریت sticky navigation
   */
  handleStickyNavigation() {
    const navigation = this.utils.$('.navigation');
    if (!navigation) return;
    
    const scrollTop = window.pageYOffset;
    const threshold = 100;
    
    if (scrollTop > threshold) {
      this.utils.addClass(navigation, 'sticky');
    } else {
      this.utils.removeClass(navigation, 'sticky');
    }
  }

  /**
   * مدیریت resize
   */
  handleResize() {
    const width = window.innerWidth;
    const breakpoint = this.config.breakpoints?.md || 768;
    
    // بستن navigation در desktop
    if (width >= breakpoint && this.isOpen) {
      this.closeNavigation();
    }
  }

  /**
   * دریافت وضعیت navigation
   */
  getState() {
    return {
      isOpen: this.isOpen,
      hasSubmenus: this.submenus.length > 0
    };
  }

  /**
   * تنظیم وضعیت navigation
   */
  setState(state) {
    if (state.isOpen && !this.isOpen) {
      this.openNavigation();
    } else if (!state.isOpen && this.isOpen) {
      this.closeNavigation();
    }
  }

  /**
   * تمیز کردن event listeners
   */
  destroy() {
    // بستن navigation
    this.closeNavigation();
    
    this.isInitialized = false;
  }
}

// Export برای استفاده در ماژول‌های دیگر
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Navigation;
} else {
  window.Navigation = Navigation;
}
