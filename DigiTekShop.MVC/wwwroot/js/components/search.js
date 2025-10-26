/**
 * Search Component
 * کامپوننت جستجو
 */

class Search {
  constructor() {
    this.config = window.CONFIG || {};
    this.utils = window.Utils || {};
    this.isInitialized = false;
    this.searchHistory = [];
    this.currentQuery = '';
    
    this.init();
  }

  init() {
    if (this.isInitialized) return;
    
    console.log('🔍 راه‌اندازی Search...');
    
    this.loadSearchHistory();
    this.bindEvents();
    this.initSearchFields();
    this.initSearchResults();
    
    this.isInitialized = true;
    console.log('✅ Search راه‌اندازی شد');
  }

  /**
   * اتصال event listeners
   */
  bindEvents() {
    // Search field events
    this.bindSearchFieldEvents();
    
    // Search result events
    this.bindSearchResultEvents();
    
    // Keyboard events
    this.bindKeyboardEvents();
  }

  /**
   * اتصال search field events
   */
  bindSearchFieldEvents() {
    const searchFields = this.utils.$$('.search-field');
    
    searchFields.forEach(field => {
      // Focus events
      this.utils.on(field, 'focus', (e) => {
        this.handleSearchFocus(e.target);
      });
      
      // Blur events
      this.utils.on(field, 'blur', (e) => {
        this.handleSearchBlur(e.target);
      });
      
      // Input events
      this.utils.on(field, 'input', this.utils.debounce((e) => {
        this.handleSearchInput(e.target);
      }, 300));
      
      // Key events
      this.utils.on(field, 'keydown', (e) => {
        this.handleSearchKeydown(e);
      });
    });
  }

  /**
   * اتصال search result events
   */
  bindSearchResultEvents() {
    const searchResults = this.utils.$$('.search-result-container');
    
    searchResults.forEach(result => {
      // Click events
      this.utils.on(result, 'click', (e) => {
        this.handleSearchResultClick(e.target);
      });
    });
  }

  /**
   * اتصال keyboard events
   */
  bindKeyboardEvents() {
    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') {
        this.handleEscapeKey();
      }
    });
  }

  /**
   * راه‌اندازی search fields
   */
  initSearchFields() {
    this.searchFields = this.utils.$$('.search-field');
    
    this.searchFields.forEach(field => {
      this.initSearchField(field);
    });
  }

  /**
   * راه‌اندازی search field
   */
  initSearchField(field) {
    // تنظیم placeholder
    if (!field.getAttribute('placeholder')) {
      field.setAttribute('placeholder', 'جستجو کنید...');
    }
    
    // تنظیم autocomplete
    field.setAttribute('autocomplete', 'off');
  }

  /**
   * راه‌اندازی search results
   */
  initSearchResults() {
    this.searchResults = this.utils.$$('.search-result-container');
    
    this.searchResults.forEach(result => {
      this.initSearchResult(result);
    });
  }

  /**
   * راه‌اندازی search result
   */
  initSearchResult(result) {
    // مخفی کردن initial state
    this.utils.removeClass(result, 'show');
    
    // راه‌اندازی search tags
    this.initSearchTags(result);
    
    // راه‌اندازی search items
    this.initSearchItems(result);
  }

  /**
   * راه‌اندازی search tags
   */
  initSearchTags(result) {
    const tags = result.querySelectorAll('.search-result-tag');
    
    tags.forEach(tag => {
      this.utils.on(tag, 'click', (e) => {
        e.preventDefault();
        this.handleTagClick(tag);
      });
    });
  }

  /**
   * راه‌اندازی search items
   */
  initSearchItems(result) {
    const items = result.querySelectorAll('.search-result-items a');
    
    items.forEach(item => {
      this.utils.on(item, 'click', (e) => {
        this.handleItemClick(item);
      });
    });
  }

  /**
   * مدیریت focus روی search field
   */
  handleSearchFocus(field) {
    const container = this.utils.parent(field, '.search-container');
    if (!container) return;
    
    this.showSearchResult(container);
    this.updateSearchButtons(container, true);
  }

  /**
   * مدیریت blur روی search field
   */
  handleSearchBlur(field) {
    // تأخیر برای اجازه دادن به کلیک روی نتایج
    setTimeout(() => {
      const container = this.utils.parent(field, '.search-container');
      if (!container) return;
      
      this.hideSearchResult(container);
      this.updateSearchButtons(container, false);
    }, 200);
  }

  /**
   * مدیریت input روی search field
   */
  handleSearchInput(field) {
    const query = field.value.trim();
    this.currentQuery = query;
    
    if (query.length > 0) {
      this.performSearch(query, field);
    } else {
      this.showSearchHistory(field);
    }
  }

  /**
   * مدیریت keydown روی search field
   */
  handleSearchKeydown(e) {
    if (e.key === 'Enter') {
      e.preventDefault();
      this.handleSearchSubmit(e.target);
    } else if (e.key === 'ArrowDown') {
      e.preventDefault();
      this.navigateSearchResults(e.target, 'down');
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      this.navigateSearchResults(e.target, 'up');
    }
  }

  /**
   * مدیریت کلیک روی search result
   */
  handleSearchResultClick(target) {
    // جلوگیری از بسته شدن search result
    if (target.closest('.search-result-container')) {
      return;
    }
  }

  /**
   * مدیریت کلیک روی tag
   */
  handleTagClick(tag) {
    const query = tag.textContent.trim();
    this.setSearchQuery(query);
    this.performSearch(query);
    this.addToSearchHistory(query);
  }

  /**
   * مدیریت کلیک روی item
   */
  handleItemClick(item) {
    const query = item.textContent.trim();
    this.setSearchQuery(query);
    this.performSearch(query);
    this.addToSearchHistory(query);
  }

  /**
   * مدیریت escape key
   */
  handleEscapeKey() {
    this.hideAllSearchResults();
  }

  /**
   * نمایش search result
   */
  showSearchResult(container) {
    const result = container.querySelector('.search-result-container');
    if (result) {
      this.utils.addClass(result, 'show');
    }
  }

  /**
   * مخفی کردن search result
   */
  hideSearchResult(container) {
    const result = container.querySelector('.search-result-container');
    if (result) {
      this.utils.removeClass(result, 'show');
    }
  }

  /**
   * مخفی کردن تمام search results
   */
  hideAllSearchResults() {
    this.searchResults.forEach(result => {
      this.utils.removeClass(result, 'show');
    });
  }

  /**
   * به‌روزرسانی search buttons
   */
  updateSearchButtons(container, isActive) {
    const searchButton = container.querySelector('.btn-search');
    const closeButton = container.querySelector('.btn-close-search-result');
    
    if (searchButton) {
      if (isActive) {
        this.utils.addClass(searchButton, 'd-none');
      } else {
        this.utils.removeClass(searchButton, 'd-none');
      }
    }
    
    if (closeButton) {
      if (isActive) {
        this.utils.removeClass(closeButton, 'd-none');
      } else {
        this.utils.addClass(closeButton, 'd-none');
      }
    }
  }

  /**
   * تنظیم search query
   */
  setSearchQuery(query) {
    this.searchFields.forEach(field => {
      field.value = query;
    });
  }

  /**
   * اجرای جستجو
   */
  performSearch(query, field = null) {
    console.log(`🔍 جستجو برای: ${query}`);
    
    // نمایش loading
    this.showSearchLoading(field);
    
    // شبیه‌سازی API call
    setTimeout(() => {
      this.hideSearchLoading(field);
      this.displaySearchResults(query, field);
    }, 500);
  }

  /**
   * نمایش loading
   */
  showSearchLoading(field) {
    if (field) {
      const container = this.utils.parent(field, '.search-container');
      if (container) {
        const result = container.querySelector('.search-result-container');
        if (result) {
          this.utils.setHTML(result, '<div class="search-loading">در حال جستجو...</div>');
        }
      }
    }
  }

  /**
   * مخفی کردن loading
   */
  hideSearchLoading(field) {
    // این بخش می‌تواند در آینده پیاده‌سازی شود
  }

  /**
   * نمایش نتایج جستجو
   */
  displaySearchResults(query, field) {
    if (field) {
      const container = this.utils.parent(field, '.search-container');
      if (container) {
        const result = container.querySelector('.search-result-container');
        if (result) {
          // نمایش نتایج شبیه‌سازی شده
          const results = this.generateMockResults(query);
          this.utils.setHTML(result, results);
        }
      }
    }
  }

  /**
   * تولید نتایج شبیه‌سازی شده
   */
  generateMockResults(query) {
    const mockResults = [
      'گوشی موبایل',
      'گوشی موبایل اپل',
      'گوشی موبایل سامسونگ',
      'گوشی موبایل شیائومی',
      'قاب گوشی موبایل'
    ];
    
    const filteredResults = mockResults.filter(result => 
      result.toLowerCase().includes(query.toLowerCase())
    );
    
    if (filteredResults.length === 0) {
      return '<div class="search-no-results">نتیجه‌ای یافت نشد</div>';
    }
    
    return `
      <div class="search-result-items">
        ${filteredResults.map(result => 
          `<li><a href="#">${result}</a></li>`
        ).join('')}
      </div>
    `;
  }

  /**
   * نمایش search history
   */
  showSearchHistory(field) {
    if (this.searchHistory.length === 0) return;
    
    const container = this.utils.parent(field, '.search-container');
    if (container) {
      const result = container.querySelector('.search-result-container');
      if (result) {
        const historyHTML = this.generateHistoryHTML();
        this.utils.setHTML(result, historyHTML);
      }
    }
  }

  /**
   * تولید HTML برای history
   */
  generateHistoryHTML() {
    return `
      <div class="search-result-tags-container">
        <div class="search-result-tags-label">
          <i class="ri-fire-line"></i> بیشترین جستجوهای اخیر
        </div>
        <ul class="search-result-tags">
          ${this.searchHistory.slice(0, 5).map(item => 
            `<li><a href="#" class="search-result-tag">${item}</a></li>`
          ).join('')}
        </ul>
      </div>
    `;
  }

  /**
   * اضافه کردن به search history
   */
  addToSearchHistory(query) {
    if (query && !this.searchHistory.includes(query)) {
      this.searchHistory.unshift(query);
      
      // محدود کردن به 10 آیتم
      if (this.searchHistory.length > 10) {
        this.searchHistory = this.searchHistory.slice(0, 10);
      }
      
      this.saveSearchHistory();
    }
  }

  /**
   * بارگذاری search history
   */
  loadSearchHistory() {
    this.searchHistory = this.utils.getStorage('search_history', []);
  }

  /**
   * ذخیره search history
   */
  saveSearchHistory() {
    this.utils.setStorage('search_history', this.searchHistory);
  }

  /**
   * مدیریت submit جستجو
   */
  handleSearchSubmit(field) {
    const query = field.value.trim();
    if (query) {
      this.performSearch(query, field);
      this.addToSearchHistory(query);
    }
  }

  /**
   * تمیز کردن event listeners
   */
  destroy() {
    this.isInitialized = false;
  }
}

// Export برای استفاده در ماژول‌های دیگر
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Search;
} else {
  window.Search = Search;
}
