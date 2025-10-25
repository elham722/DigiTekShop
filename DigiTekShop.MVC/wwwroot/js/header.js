/**
 * Header JavaScript - Bootstrap 5.3 RTL Compatible
 * Vanilla JS implementation for header functionality
 * Tree-shake optimized and jQuery-free
 */

(function() {
    'use strict';

    // DOM ready state check
    const isDOMReady = document.readyState === 'loading' ? false : true;
    
    function domReady(fn) {
        if (isDOMReady) {
            fn();
        } else {
            document.addEventListener('DOMContentLoaded', fn);
        }
    }

    // Initialize header functionality
    domReady(function() {
        Header.init();
    });

    /**
     * Header functionality namespace
     */
    const Header = {
        /**
         * Initialize all header functionality
         */
        init: function() {
            this.initSearch();
            this.initDropdowns();
            this.initOffcanvas();
            this.initMobileMenu();
            this.initSwiper();
            this.initLazyLoading();
            this.initResponsive();
            this.initKeyboardNavigation();
            this.initTouchGestures();
        },

        /**
         * Initialize search functionality
         */
        initSearch: function() {
            const searchFields = document.querySelectorAll('.search-field');
            const searchContainers = document.querySelectorAll('.search-container');
            const searchResultContainers = document.querySelectorAll('.search-result-container');
            const searchForms = document.querySelectorAll('.search-form');

            // Search field interactions
            searchFields.forEach(field => {
                field.addEventListener('focus', () => {
                    const container = field.closest('.search-container');
                    const resultContainer = container?.querySelector('.search-result-container');
                    resultContainer?.classList.add('show');
                });

                field.addEventListener('blur', () => {
                    setTimeout(() => {
                        const container = field.closest('.search-container');
                        const resultContainer = container?.querySelector('.search-result-container');
                        resultContainer?.classList.remove('show');
                    }, 200);
                });

                field.addEventListener('input', () => {
                    const container = field.closest('.search-container');
                    const resultContainer = container?.querySelector('.search-result-container');
                    resultContainer?.classList.add('show');
                });
            });

            // Close search results when clicking outside
            document.addEventListener('click', (e) => {
                if (!e.target.closest('.search-container')) {
                    searchResultContainers.forEach(container => {
                        container.classList.remove('show');
                    });
                }
            });

            // Handle search form submission
            searchForms.forEach(form => {
                form.addEventListener('submit', (e) => {
                    const searchField = form.querySelector('.search-field');
                    if (!searchField?.value.trim()) {
                        e.preventDefault();
                        searchField?.focus();
                    }
                });
            });
        },

        /**
         * Initialize dropdown functionality
         */
        initDropdowns: function() {
            // Initialize Bootstrap dropdowns
            const dropdownElements = document.querySelectorAll('[data-bs-toggle="dropdown"]');
            dropdownElements.forEach(element => {
                if (typeof bootstrap !== 'undefined') {
                    new bootstrap.Dropdown(element);
                }
            });

            // Handle mini cart interactions
            const miniCartProducts = document.querySelectorAll('.mini-cart-product');
            miniCartProducts.forEach(product => {
                const removeBtn = product.querySelector('.mini-cart-product-remove');
                removeBtn?.addEventListener('click', () => {
                    // Add remove animation
                    product.style.transition = 'all 0.3s ease';
                    product.style.opacity = '0';
                    product.style.transform = 'translateX(-100%)';
                    
                    setTimeout(() => {
                        product.remove();
                        this.updateCartCount();
                    }, 300);
                });
            });
        },

        /**
         * Initialize offcanvas functionality
         */
        initOffcanvas: function() {
            const offcanvasElements = document.querySelectorAll('[data-bs-toggle="offcanvas"]');
            offcanvasElements.forEach(element => {
                if (typeof bootstrap !== 'undefined') {
                    new bootstrap.Offcanvas(element);
                }
            });

            // Handle category menu interactions
            const categoryLinks = document.querySelectorAll('.vertical-menu-items a');
            categoryLinks.forEach(link => {
                link.addEventListener('click', (e) => {
                    if (link.classList.contains('dropdown-toggle')) {
                        e.preventDefault();
                        const nextElement = link.nextElementSibling;
                        if (nextElement?.classList.contains('dropdown-menu')) {
                            nextElement.classList.toggle('show');
                        }
                    }
                });
            });

            // Handle existing vertical menu functionality
            const verticalMenuBtn = document.querySelector('.vertical-menu-btn');
            verticalMenuBtn?.addEventListener('click', (e) => {
                e.preventDefault();
                const offcanvas = document.querySelector('#categoryOffcanvas');
                if (offcanvas && typeof bootstrap !== 'undefined') {
                    const bsOffcanvas = new bootstrap.Offcanvas(offcanvas);
                    bsOffcanvas.show();
                }
            });
        },

        /**
         * Initialize mobile menu functionality
         */
        initMobileMenu: function() {
            const mobileMenuLinks = document.querySelectorAll('.menu a');
            mobileMenuLinks.forEach(link => {
                if (link.classList.contains('dropdown-toggle')) {
                    link.addEventListener('click', (e) => {
                        e.preventDefault();
                        const targetId = link.getAttribute('data-bs-target');
                        const targetElement = document.querySelector(targetId);
                        if (targetElement && typeof bootstrap !== 'undefined') {
                            const collapse = new bootstrap.Collapse(targetElement, { toggle: true });
                        }
                    });
                }
            });
        },

        /**
         * Initialize Swiper for promotional messages
         */
        initSwiper: function() {
            const swiperContainers = document.querySelectorAll('.notification-swiper-slider');
            swiperContainers.forEach(container => {
                if (typeof Swiper !== 'undefined') {
                    new Swiper(container, {
                        direction: 'vertical',
                        loop: true,
                        autoplay: {
                            delay: 3000,
                            disableOnInteraction: false,
                        },
                        speed: 1000,
                        effect: 'fade',
                        fadeEffect: {
                            crossFade: true
                        }
                    });
                }
            });
        },

        /**
         * Initialize lazy loading for images
         */
        initLazyLoading: function() {
            const images = document.querySelectorAll('img[data-src]');
            
            if ('IntersectionObserver' in window) {
                const imageObserver = new IntersectionObserver((entries, observer) => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            const img = entry.target;
                            img.src = img.dataset.src;
                            img.classList.remove('lazy');
                            imageObserver.unobserve(img);
                        }
                    });
                });

                images.forEach(img => imageObserver.observe(img));
            } else {
                // Fallback for older browsers
                images.forEach(img => {
                    img.src = img.dataset.src;
                    img.classList.remove('lazy');
                });
            }
        },

        /**
         * Initialize responsive behavior
         */
        initResponsive: function() {
            const handleResponsive = () => {
                const isMobile = window.innerWidth < 768;
                const searchContainers = document.querySelectorAll('.search-container');
                
                searchContainers.forEach(container => {
                    container.classList.toggle('mobile-search', isMobile);
                });
            };

            window.addEventListener('resize', handleResponsive);
            handleResponsive();
        },

        /**
         * Initialize keyboard navigation
         */
        initKeyboardNavigation: function() {
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape') {
                    // Close dropdowns
                    const openDropdowns = document.querySelectorAll('.dropdown-menu.show');
                    openDropdowns.forEach(dropdown => {
                        dropdown.classList.remove('show');
                    });

                    // Close offcanvas
                    const openOffcanvas = document.querySelectorAll('.offcanvas.show');
                    openOffcanvas.forEach(offcanvas => {
                        if (typeof bootstrap !== 'undefined') {
                            const bsOffcanvas = bootstrap.Offcanvas.getInstance(offcanvas);
                            bsOffcanvas?.hide();
                        }
                    });
                }
            });
        },

        /**
         * Initialize touch/swipe gestures for mobile
         */
        initTouchGestures: function() {
            let touchStartX = 0;
            let touchStartY = 0;

            document.addEventListener('touchstart', (e) => {
                touchStartX = e.touches[0].clientX;
                touchStartY = e.touches[0].clientY;
            });

            document.addEventListener('touchend', (e) => {
                if (!touchStartX || !touchStartY) return;

                const touchEndX = e.changedTouches[0].clientX;
                const touchEndY = e.changedTouches[0].clientY;
                const diffX = touchStartX - touchEndX;
                const diffY = touchStartY - touchEndY;

                // Horizontal swipe
                if (Math.abs(diffX) > Math.abs(diffY) && Math.abs(diffX) > 50) {
                    if (diffX > 0) {
                        // Swipe left - close offcanvas
                        const openOffcanvas = document.querySelector('.offcanvas.show');
                        if (openOffcanvas && typeof bootstrap !== 'undefined') {
                            const bsOffcanvas = bootstrap.Offcanvas.getInstance(openOffcanvas);
                            bsOffcanvas?.hide();
                        }
                    }
                }

                touchStartX = 0;
                touchStartY = 0;
            });
        },

        /**
         * Update cart count after item removal
         */
        updateCartCount: function() {
            const cartCounters = document.querySelectorAll('.user-option--cart .badge-count');
            const miniCartProducts = document.querySelectorAll('.mini-cart-product');
            const newCount = miniCartProducts.length;
            
            cartCounters.forEach(counter => {
                counter.textContent = newCount;
            });

            // Update mini cart header
            const productsCount = document.querySelector('.mini-cart-products-count');
            if (productsCount) {
                productsCount.textContent = newCount + ' کالا';
            }

            // Hide mini cart if empty
            if (newCount === 0) {
                const miniCart = document.querySelector('.mini-cart');
                if (miniCart) {
                    miniCart.style.display = 'none';
                }
            }
        }
    };

    // Export for global access if needed
    window.Header = Header;

})();