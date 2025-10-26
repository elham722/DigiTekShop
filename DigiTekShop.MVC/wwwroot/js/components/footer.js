/**
 * Footer Component
 * کامپوننت فوتر
 */

class Footer {
  constructor() {
    this.config = window.CONFIG || {};
    this.utils = window.Utils || {};
    this.isInitialized = false;
    
    this.init();
  }

  init() {
    if (this.isInitialized) return;
    
    console.log('🦶 راه‌اندازی Footer...');
    
    this.bindEvents();
    this.initNewsletter();
    this.initSocialLinks();
    this.initExpandableText();
    
    this.isInitialized = true;
    console.log('✅ Footer راه‌اندازی شد');
  }

  /**
   * اتصال event listeners
   */
  bindEvents() {
    // Newsletter events
    this.bindNewsletterEvents();
    
    // Social links events
    this.bindSocialLinksEvents();
    
    // Expandable text events
    this.bindExpandableTextEvents();
    
    // Back to top events
    this.bindBackToTopEvents();
  }

  /**
   * اتصال newsletter events
   */
  bindNewsletterEvents() {
    const newsletterForms = this.utils.$$('.widget-newsletter form');
    
    newsletterForms.forEach(form => {
      this.utils.on(form, 'submit', (e) => {
        e.preventDefault();
        this.handleNewsletterSubmit(form);
      });
    });
  }

  /**
   * اتصال social links events
   */
  bindSocialLinksEvents() {
    const socialLinks = this.utils.$$('.widget-socials a');
    
    socialLinks.forEach(link => {
      this.utils.on(link, 'click', (e) => {
        this.handleSocialLinkClick(link);
      });
    });
  }

  /**
   * اتصال expandable text events
   */
  bindExpandableTextEvents() {
    const expandableTexts = this.utils.$$('.expandable-text');
    
    expandableTexts.forEach(text => {
      this.setupExpandableText(text);
    });
  }

  /**
   * اتصال back to top events
   */
  bindBackToTopEvents() {
    // ایجاد back to top button
    this.createBackToTopButton();
  }

  /**
   * راه‌اندازی newsletter
   */
  initNewsletter() {
    this.newsletterForms = this.utils.$$('.widget-newsletter form');
    
    this.newsletterForms.forEach(form => {
      this.setupNewsletterForm(form);
    });
  }

  /**
   * راه‌اندازی social links
   */
  initSocialLinks() {
    this.socialLinks = this.utils.$$('.widget-socials a');
    
    this.socialLinks.forEach(link => {
      this.setupSocialLink(link);
    });
  }

  /**
   * راه‌اندازی expandable text
   */
  initExpandableText() {
    this.expandableTexts = this.utils.$$('.expandable-text');
    
    this.expandableTexts.forEach(text => {
      this.setupExpandableText(text);
    });
  }

  /**
   * راه‌اندازی newsletter form
   */
  setupNewsletterForm(form) {
    const emailInput = form.querySelector('input[type="text"]');
    const submitButton = form.querySelector('button[type="submit"]');
    
    if (emailInput) {
      // تنظیم placeholder
      if (!emailInput.getAttribute('placeholder')) {
        emailInput.setAttribute('placeholder', 'آدرس ایمیل خود را وارد کنید');
      }
      
      // تنظیم validation
      emailInput.setAttribute('type', 'email');
    }
    
    if (submitButton) {
      // تنظیم loading state
      this.setupSubmitButton(submitButton);
    }
  }

  /**
   * راه‌اندازی submit button
   */
  setupSubmitButton(button) {
    this.utils.on(button, 'click', (e) => {
      e.preventDefault();
      this.handleNewsletterSubmit(button.closest('form'));
    });
  }

  /**
   * راه‌اندازی social link
   */
  setupSocialLink(link) {
    // تنظیم target="_blank" برای لینک‌های خارجی
    if (link.href && !link.href.includes(window.location.hostname)) {
      link.setAttribute('target', '_blank');
      link.setAttribute('rel', 'noopener noreferrer');
    }
  }

  /**
   * راه‌اندازی expandable text
   */
  setupExpandableText(text) {
    const showMore = text.querySelector('.show-more');
    const showLess = text.querySelector('.show-less');
    
    if (showMore) {
      this.utils.on(showMore, 'click', (e) => {
        e.preventDefault();
        this.expandText(text);
      });
    }
    
    if (showLess) {
      this.utils.on(showLess, 'click', (e) => {
        e.preventDefault();
        this.collapseText(text);
      });
    }
  }

  /**
   * ایجاد back to top button
   */
  createBackToTopButton() {
    const button = this.utils.createElement('button', {
      className: 'back-to-top',
      'aria-label': 'بازگشت به بالا'
    }, '↑');
    
    // اضافه کردن styles
    button.style.cssText = `
      position: fixed;
      bottom: 20px;
      left: 20px;
      width: 50px;
      height: 50px;
      background: #2962ff;
      color: white;
      border: none;
      border-radius: 50%;
      cursor: pointer;
      font-size: 20px;
      z-index: 1000;
      opacity: 0;
      visibility: hidden;
      transition: all 0.3s ease;
    `;
    
    // اضافه کردن به صفحه
    document.body.appendChild(button);
    
    // اتصال event
    this.utils.on(button, 'click', () => {
      this.scrollToTop();
    });
    
    // نمایش/مخفی کردن بر اساس scroll
    window.addEventListener('scroll', this.utils.throttle(() => {
      this.handleBackToTopScroll(button);
    }, 100));
  }

  /**
   * مدیریت submit newsletter
   */
  handleNewsletterSubmit(form) {
    const emailInput = form.querySelector('input[type="email"]');
    const submitButton = form.querySelector('button[type="submit"]');
    
    if (!emailInput || !emailInput.value) {
      this.showMessage('لطفا آدرس ایمیل را وارد کنید', 'error');
      return;
    }
    
    if (!this.isValidEmail(emailInput.value)) {
      this.showMessage('لطفا آدرس ایمیل معتبر وارد کنید', 'error');
      return;
    }
    
    // نمایش loading
    this.setButtonLoading(submitButton, true);
    
    // شبیه‌سازی API call
    setTimeout(() => {
      this.setButtonLoading(submitButton, false);
      this.showMessage('عضویت در خبرنامه با موفقیت انجام شد', 'success');
      emailInput.value = '';
    }, 1000);
  }

  /**
   * مدیریت کلیک روی social link
   */
  handleSocialLinkClick(link) {
    // ردیابی کلیک
    console.log(`🔗 کلیک روی لینک اجتماعی: ${link.href}`);
    
    // باز کردن در tab جدید
    if (link.getAttribute('target') === '_blank') {
      window.open(link.href, '_blank', 'noopener,noreferrer');
    }
  }

  /**
   * گسترش متن
   */
  expandText(text) {
    this.utils.addClass(text, 'expanded');
    
    const showMore = text.querySelector('.show-more');
    const showLess = text.querySelector('.show-less');
    
    if (showMore) {
      this.utils.addClass(showMore, 'd-none');
    }
    
    if (showLess) {
      this.utils.removeClass(showLess, 'd-none');
    }
  }

  /**
   * جمع کردن متن
   */
  collapseText(text) {
    this.utils.removeClass(text, 'expanded');
    
    const showMore = text.querySelector('.show-more');
    const showLess = text.querySelector('.show-less');
    
    if (showMore) {
      this.utils.removeClass(showMore, 'd-none');
    }
    
    if (showLess) {
      this.utils.addClass(showLess, 'd-none');
    }
  }

  /**
   * مدیریت scroll برای back to top
   */
  handleBackToTopScroll(button) {
    const scrollTop = window.pageYOffset;
    const threshold = 300;
    
    if (scrollTop > threshold) {
      button.style.opacity = '1';
      button.style.visibility = 'visible';
    } else {
      button.style.opacity = '0';
      button.style.visibility = 'hidden';
    }
  }

  /**
   * اسکرول به بالا
   */
  scrollToTop() {
    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  /**
   * تنظیم loading state برای button
   */
  setButtonLoading(button, isLoading) {
    if (isLoading) {
      button.disabled = true;
      button.textContent = 'در حال ارسال...';
      this.utils.addClass(button, 'loading');
    } else {
      button.disabled = false;
      button.textContent = 'ثبت';
      this.utils.removeClass(button, 'loading');
    }
  }

  /**
   * نمایش پیام
   */
  showMessage(message, type = 'info') {
    // ایجاد toast notification
    const toast = this.utils.createElement('div', {
      className: `toast toast-${type}`,
      style: `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${type === 'success' ? '#28a745' : type === 'error' ? '#dc3545' : '#17a2b8'};
        color: white;
        padding: 15px 20px;
        border-radius: 5px;
        z-index: 10000;
        opacity: 0;
        transform: translateX(100%);
        transition: all 0.3s ease;
      `
    }, message);
    
    document.body.appendChild(toast);
    
    // نمایش toast
    setTimeout(() => {
      toast.style.opacity = '1';
      toast.style.transform = 'translateX(0)';
    }, 100);
    
    // حذف toast
    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateX(100%)';
      setTimeout(() => {
        this.utils.remove(toast);
      }, 300);
    }, 3000);
  }

  /**
   * بررسی معتبر بودن ایمیل
   */
  isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  /**
   * مدیریت scroll
   */
  handleScroll() {
    // اجرای scroll handlers
    this.handleBackToTopScroll(this.utils.$('.back-to-top'));
  }

  /**
   * تمیز کردن event listeners
   */
  destroy() {
    // حذف back to top button
    const backToTopButton = this.utils.$('.back-to-top');
    if (backToTopButton) {
      this.utils.remove(backToTopButton);
    }
    
    this.isInitialized = false;
  }
}

// Export برای استفاده در ماژول‌های دیگر
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Footer;
} else {
  window.Footer = Footer;
}
