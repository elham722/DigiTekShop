/**
 * Header Search Module
 * Vanilla JS replacement for jQuery-based search functionality
 */

export function mountHeaderSearch(root = document) {
    const searchContainer = root.querySelector('.search-container');
    if (!searchContainer) return;

    const searchForm = searchContainer.querySelector('.search-form');
    const searchField = searchForm?.querySelector('.search-field');
    const btnSearch = searchContainer.querySelector('.btn-search');
    const btnClose = searchContainer.querySelector('.btn-close-search-result');
    const resultsContainer = searchContainer.querySelector('.search-result-container');

    if (!searchField || !btnSearch || !btnClose || !resultsContainer) {
        console.warn('Header search: Required elements not found');
        return;
    }

    const open = () => {
        btnSearch.classList.add('d-none');
        btnClose.classList.remove('d-none');
        resultsContainer.classList.add('show');
        resultsContainer.setAttribute('aria-expanded', 'true');
        
        // Focus on search field
        searchField.focus();
    };

    const close = () => {
        btnClose.classList.add('d-none');
        btnSearch.classList.remove('d-none');
        resultsContainer.classList.remove('show');
        resultsContainer.setAttribute('aria-expanded', 'false');
        
        // Clear search field
        searchField.value = '';
    };

    // Event listeners
    searchField.addEventListener('click', open);
    searchField.addEventListener('focus', open);

    btnClose.addEventListener('click', (e) => {
        e.preventDefault();
        close();
    });

    // Close on outside click
    document.addEventListener('click', (e) => {
        if (!searchContainer.contains(e.target)) {
            close();
        }
    });

    // Close on Escape key
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && resultsContainer.classList.contains('show')) {
            close();
        }
    });

    // Handle search input
    let searchTimeout;
    searchField.addEventListener('input', (e) => {
        const query = e.target.value.trim();
        
        // Clear previous timeout
        if (searchTimeout) {
            clearTimeout(searchTimeout);
        }

        if (query.length < 2) {
            // Clear results for short queries
            resultsContainer.innerHTML = '<div class="search-no-results">لطفاً حداقل ۲ کاراکتر وارد کنید</div>';
            return;
        }

        // Debounce search
        searchTimeout = setTimeout(() => {
            performSearch(query);
        }, 300);
    });

    // Perform search
    async function performSearch(query) {
        try {
            // Show loading state
            resultsContainer.innerHTML = '<div class="search-loading">در حال جستجو...</div>';

            // Make API call (replace with actual search endpoint)
            const response = await fetch(`/api/search?q=${encodeURIComponent(query)}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                throw new Error('Search failed');
            }

            const data = await response.json();
            displaySearchResults(data);

        } catch (error) {
            console.error('Search error:', error);
            resultsContainer.innerHTML = '<div class="search-error">خطا در جستجو. لطفاً دوباره تلاش کنید.</div>';
        }
    }

    // Display search results
    function displaySearchResults(results) {
        if (!results || results.length === 0) {
            resultsContainer.innerHTML = '<div class="search-no-results">نتیجه‌ای یافت نشد</div>';
            return;
        }

        const resultsHtml = results.map(item => `
            <div class="search-result-item" data-url="${item.url}">
                <div class="search-result-title">${item.title}</div>
                <div class="search-result-description">${item.description || ''}</div>
            </div>
        `).join('');

        resultsContainer.innerHTML = `
            <div class="search-results">
                ${resultsHtml}
            </div>
        `;

        // Add click handlers to result items
        resultsContainer.querySelectorAll('.search-result-item').forEach(item => {
            item.addEventListener('click', () => {
                const url = item.dataset.url;
                if (url) {
                    window.location.href = url;
                }
            });
        });
    }

    console.log('Header search module initialized');
}
