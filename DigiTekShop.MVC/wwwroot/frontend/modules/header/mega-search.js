/**
 * Mega Search Module
 * Vanilla JS replacement for jQuery-based mega search functionality
 */

export function mountMegaSearch(root = document) {
    const megaSearchContainer = root.querySelector('.mega-search-container');
    if (!megaSearchContainer) return;

    const searchBtn = root.querySelector('.user-option-btn--search');
    const closeBtn = megaSearchContainer.querySelector('.mega-search-box-close');
    const overlay = megaSearchContainer.querySelector('.mega-search-overlay');
    const searchInput = megaSearchContainer.querySelector('.mega-search-input');

    if (!searchBtn || !closeBtn || !overlay) {
        console.warn('Mega search: Required elements not found');
        return;
    }

    const open = () => {
        megaSearchContainer.classList.add('show');
        document.body.style.overflow = 'hidden'; // Prevent background scrolling
        
        // Focus on search input if available
        if (searchInput) {
            setTimeout(() => searchInput.focus(), 100);
        }
    };

    const close = () => {
        megaSearchContainer.classList.remove('show');
        document.body.style.overflow = ''; // Restore scrolling
    };

    // Event listeners
    searchBtn.addEventListener('click', (e) => {
        e.preventDefault();
        open();
    });

    closeBtn.addEventListener('click', (e) => {
        e.preventDefault();
        close();
    });

    overlay.addEventListener('click', (e) => {
        e.preventDefault();
        close();
    });

    // Close on Escape key
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && megaSearchContainer.classList.contains('show')) {
            close();
        }
    });

    // Handle search input if available
    if (searchInput) {
        let searchTimeout;
        
        searchInput.addEventListener('input', (e) => {
            const query = e.target.value.trim();
            
            // Clear previous timeout
            if (searchTimeout) {
                clearTimeout(searchTimeout);
            }

            if (query.length < 2) {
                // Clear results for short queries
                clearSearchResults();
                return;
            }

            // Debounce search
            searchTimeout = setTimeout(() => {
                performMegaSearch(query);
            }, 300);
        });

        // Perform mega search
        async function performMegaSearch(query) {
            try {
                // Show loading state
                showSearchLoading();

                // Make API call (replace with actual search endpoint)
                const response = await fetch(`/api/mega-search?q=${encodeURIComponent(query)}`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                if (!response.ok) {
                    throw new Error('Mega search failed');
                }

                const data = await response.json();
                displayMegaSearchResults(data);

            } catch (error) {
                console.error('Mega search error:', error);
                showSearchError();
            }
        }

        // Show search loading
        function showSearchLoading() {
            const resultsContainer = megaSearchContainer.querySelector('.mega-search-results');
            if (resultsContainer) {
                resultsContainer.innerHTML = '<div class="mega-search-loading">در حال جستجو...</div>';
            }
        }

        // Show search error
        function showSearchError() {
            const resultsContainer = megaSearchContainer.querySelector('.mega-search-results');
            if (resultsContainer) {
                resultsContainer.innerHTML = '<div class="mega-search-error">خطا در جستجو. لطفاً دوباره تلاش کنید.</div>';
            }
        }

        // Clear search results
        function clearSearchResults() {
            const resultsContainer = megaSearchContainer.querySelector('.mega-search-results');
            if (resultsContainer) {
                resultsContainer.innerHTML = '';
            }
        }

        // Display mega search results
        function displayMegaSearchResults(results) {
            const resultsContainer = megaSearchContainer.querySelector('.mega-search-results');
            if (!resultsContainer) return;

            if (!results || Object.keys(results).length === 0) {
                resultsContainer.innerHTML = '<div class="mega-search-no-results">نتیجه‌ای یافت نشد</div>';
                return;
            }

            let resultsHtml = '';

            // Categories section
            if (results.categories && results.categories.length > 0) {
                resultsHtml += `
                    <div class="mega-search-section">
                        <h3 class="mega-search-section-title">دسته‌بندی‌ها</h3>
                        <div class="mega-search-categories">
                            ${results.categories.map(category => `
                                <a href="${category.url}" class="mega-search-category-item">
                                    <span class="category-name">${category.name}</span>
                                    <span class="category-count">(${category.count})</span>
                                </a>
                            `).join('')}
                        </div>
                    </div>
                `;
            }

            // Products section
            if (results.products && results.products.length > 0) {
                resultsHtml += `
                    <div class="mega-search-section">
                        <h3 class="mega-search-section-title">محصولات</h3>
                        <div class="mega-search-products">
                            ${results.products.map(product => `
                                <a href="${product.url}" class="mega-search-product-item">
                                    <div class="product-image">
                                        <img src="${product.image}" alt="${product.name}" loading="lazy">
                                    </div>
                                    <div class="product-info">
                                        <div class="product-name">${product.name}</div>
                                        <div class="product-price">${product.price}</div>
                                    </div>
                                </a>
                            `).join('')}
                        </div>
                    </div>
                `;
            }

            // Brands section
            if (results.brands && results.brands.length > 0) {
                resultsHtml += `
                    <div class="mega-search-section">
                        <h3 class="mega-search-section-title">برندها</h3>
                        <div class="mega-search-brands">
                            ${results.brands.map(brand => `
                                <a href="${brand.url}" class="mega-search-brand-item">
                                    <img src="${brand.logo}" alt="${brand.name}" loading="lazy">
                                    <span class="brand-name">${brand.name}</span>
                                </a>
                            `).join('')}
                        </div>
                    </div>
                `;
            }

            resultsContainer.innerHTML = resultsHtml;

            // Add click handlers to result items
            resultsContainer.querySelectorAll('a').forEach(link => {
                link.addEventListener('click', () => {
                    close();
                });
            });
        }
    }

    console.log('Mega search module initialized');
}
