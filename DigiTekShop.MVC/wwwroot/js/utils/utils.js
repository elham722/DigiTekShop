/**
 * Utility Functions
 * توابع کمکی
 */

const Utils = {
  /**
   * بررسی وجود عنصر در DOM
   */
  exists(selector) {
    return document.querySelector(selector) !== null;
  },

  /**
   * دریافت عنصر از DOM
   */
  $(selector) {
    return document.querySelector(selector);
  },

  /**
   * دریافت تمام عناصر از DOM
   */
  $$(selector) {
    return document.querySelectorAll(selector);
  },

  /**
   * اضافه کردن event listener
   */
  on(element, event, handler) {
    if (element && typeof handler === 'function') {
      element.addEventListener(event, handler);
    }
  },

  /**
   * حذف event listener
   */
  off(element, event, handler) {
    if (element && typeof handler === 'function') {
      element.removeEventListener(event, handler);
    }
  },

  /**
   * اضافه کردن کلاس
   */
  addClass(element, className) {
    if (element) {
      element.classList.add(className);
    }
  },

  /**
   * حذف کلاس
   */
  removeClass(element, className) {
    if (element) {
      element.classList.remove(className);
    }
  },

  /**
   * toggle کلاس
   */
  toggleClass(element, className) {
    if (element) {
      element.classList.toggle(className);
    }
  },

  /**
   * بررسی وجود کلاس
   */
  hasClass(element, className) {
    return element ? element.classList.contains(className) : false;
  },

  /**
   * تنظیم style
   */
  setStyle(element, property, value) {
    if (element) {
      element.style[property] = value;
    }
  },

  /**
   * دریافت style
   */
  getStyle(element, property) {
    return element ? window.getComputedStyle(element)[property] : null;
  },

  /**
   * تنظیم attribute
   */
  setAttr(element, attribute, value) {
    if (element) {
      element.setAttribute(attribute, value);
    }
  },

  /**
   * دریافت attribute
   */
  getAttr(element, attribute) {
    return element ? element.getAttribute(attribute) : null;
  },

  /**
   * تنظیم text content
   */
  setText(element, text) {
    if (element) {
      element.textContent = text;
    }
  },

  /**
   * دریافت text content
   */
  getText(element) {
    return element ? element.textContent : '';
  },

  /**
   * تنظیم HTML content
   */
  setHTML(element, html) {
    if (element) {
      element.innerHTML = html;
    }
  },

  /**
   * دریافت HTML content
   */
  getHTML(element) {
    return element ? element.innerHTML : '';
  },

  /**
   * ایجاد عنصر جدید
   */
  createElement(tag, attributes = {}, content = '') {
    const element = document.createElement(tag);
    
    // تنظیم attributes
    Object.keys(attributes).forEach(key => {
      if (key === 'className') {
        element.className = attributes[key];
      } else if (key === 'innerHTML') {
        element.innerHTML = attributes[key];
      } else {
        element.setAttribute(key, attributes[key]);
      }
    });

    // تنظیم content
    if (content) {
      element.textContent = content;
    }

    return element;
  },

  /**
   * حذف عنصر از DOM
   */
  remove(element) {
    if (element && element.parentNode) {
      element.parentNode.removeChild(element);
    }
  },

  /**
   * اضافه کردن عنصر به DOM
   */
  append(parent, child) {
    if (parent && child) {
      parent.appendChild(child);
    }
  },

  /**
   * دریافت والدین عنصر
   */
  parent(element, selector = null) {
    if (!element) return null;
    
    if (selector) {
      return element.closest(selector);
    }
    
    return element.parentNode;
  },

  /**
   * دریافت فرزندان عنصر
   */
  children(element, selector = null) {
    if (!element) return [];
    
    if (selector) {
      return Array.from(element.querySelectorAll(selector));
    }
    
    return Array.from(element.children);
  },

  /**
   * دریافت siblings عنصر
   */
  siblings(element, selector = null) {
    if (!element || !element.parentNode) return [];
    
    const siblings = Array.from(element.parentNode.children);
    const index = siblings.indexOf(element);
    siblings.splice(index, 1);
    
    if (selector) {
      return siblings.filter(sibling => sibling.matches(selector));
    }
    
    return siblings;
  },

  /**
   * debounce function
   */
  debounce(func, wait, immediate = false) {
    let timeout;
    return function executedFunction(...args) {
      const later = () => {
        timeout = null;
        if (!immediate) func(...args);
      };
      const callNow = immediate && !timeout;
      clearTimeout(timeout);
      timeout = setTimeout(later, wait);
      if (callNow) func(...args);
    };
  },

  /**
   * throttle function
   */
  throttle(func, limit) {
    let inThrottle;
    return function(...args) {
      if (!inThrottle) {
        func.apply(this, args);
        inThrottle = true;
        setTimeout(() => inThrottle = false, limit);
      }
    };
  },

  /**
   * بررسی viewport
   */
  isInViewport(element) {
    if (!element) return false;
    
    const rect = element.getBoundingClientRect();
    return (
      rect.top >= 0 &&
      rect.left >= 0 &&
      rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
      rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
  },

  /**
   * دریافت فاصله از بالای صفحه
   */
  getOffsetTop(element) {
    if (!element) return 0;
    
    let offsetTop = 0;
    let current = element;
    
    while (current) {
      offsetTop += current.offsetTop;
      current = current.offsetParent;
    }
    
    return offsetTop;
  },

  /**
   * اسکرول به عنصر
   */
  scrollTo(element, offset = 0) {
    if (!element) return;
    
    const elementTop = this.getOffsetTop(element) - offset;
    window.scrollTo({
      top: elementTop,
      behavior: 'smooth'
    });
  },

  /**
   * کپی متن به clipboard
   */
  async copyToClipboard(text) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch (err) {
      // Fallback برای مرورگرهای قدیمی
      const textArea = document.createElement('textarea');
      textArea.value = text;
      document.body.appendChild(textArea);
      textArea.select();
      try {
        document.execCommand('copy');
        return true;
      } catch (err) {
        return false;
      } finally {
        document.body.removeChild(textArea);
      }
    }
  },

  /**
   * ذخیره در localStorage
   */
  setStorage(key, value) {
    try {
      localStorage.setItem(key, JSON.stringify(value));
      return true;
    } catch (err) {
      console.error('خطا در ذخیره localStorage:', err);
      return false;
    }
  },

  /**
   * دریافت از localStorage
   */
  getStorage(key, defaultValue = null) {
    try {
      const item = localStorage.getItem(key);
      return item ? JSON.parse(item) : defaultValue;
    } catch (err) {
      console.error('خطا در خواندن localStorage:', err);
      return defaultValue;
    }
  },

  /**
   * حذف از localStorage
   */
  removeStorage(key) {
    try {
      localStorage.removeItem(key);
      return true;
    } catch (err) {
      console.error('خطا در حذف localStorage:', err);
      return false;
    }
  },

  /**
   * تولید ID منحصر به فرد
   */
  generateId(prefix = 'id') {
    return `${prefix}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  },

  /**
   * فرمت کردن عدد فارسی
   */
  formatNumber(number) {
    return new Intl.NumberFormat('fa-IR').format(number);
  },

  /**
   * فرمت کردن قیمت
   */
  formatPrice(price, currency = 'تومان') {
    return `${this.formatNumber(price)} ${currency}`;
  }
};

// Export برای استفاده در ماژول‌های دیگر
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Utils;
} else {
  window.Utils = Utils;
}
