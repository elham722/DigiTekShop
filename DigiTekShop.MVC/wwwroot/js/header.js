/**
 * Header JavaScript - Bootstrap 5.3 RTL Compatible
 * Vanilla JS implementation for header functionality
 */

(function() {
    'use strict';

    // Initialize header functionality when DOM is loaded
    document.addEventListener('DOMContentLoaded', function() {
        initializeSearch();
        initializeDropdowns();
        initializeOffcanvas();
        initializeMobileMenu();
        initializeSwiper();
    });

    /**
     * Initialize search functionality
     */
    function initializeSearch() {
        const searchFields = document.querySelectorAll('.search-field');
        const searchContainers = document.querySelectorAll('.search-container');
        const searchResultContainers = document.querySelectorAll('.search-result-container');
        const btnSearch = document.querySelectorAll('.btn-search');
        const btnCloseSearch = document.querySelectorAll('.btn-close-search-result');

        searchFields.forEach(function(field) {
            field.addEventListener('focus', function() {
                const container = this.closest('.search-container');
                const resultContainer = container.querySelector('.search-result-container');
                if (resultContainer) {
                    resultContainer.classList.add('show');
                }
            });

            field.addEventListener('blur', function() {
                // Delay hiding to allow clicking on results
                setTimeout(function() {
                    const container = field.closest('.search-container');
                    const resultContainer = container.querySelector('.search-result-container');
                    if (resultContainer) {
                        resultContainer.classList.remove('show');
                    }
                }, 200);
            });

            field.addEventListener('input', function() {
                const container = this.closest('.search-container');
                const resultContainer = container.querySelector('.search-result-container');
                if (resultContainer) {
                    resultContainer.classList.add('show');
                }
            });
        });

        // Close search results when clicking outside
        document.addEventListener('click', function(e) {
            if (!e.target.closest('.search-container')) {
                searchResultContainers.forEach(function(container) {
                    container.classList.remove('show');
                });
            }
        });

        // Handle search form submission
        const searchForms = document.querySelectorAll('.search-form');
        searchForms.forEach(function(form) {
            form.addEventListener('submit', function(e) {
                const searchField = this.querySelector('.search-field');
                if (!searchField.value.trim()) {
                    e.preventDefault();
                    searchField.focus();
                }
            });
        });
    }

    /**
     * Initialize dropdown functionality
     */
    function initializeDropdowns() {
        // Initialize Bootstrap dropdowns
        const dropdownElements = document.querySelectorAll('[data-bs-toggle="dropdown"]');
        dropdownElements.forEach(function(element) {
            // Bootstrap 5 handles dropdowns automatically
            // Just ensure proper initialization
            if (typeof bootstrap !== 'undefined') {
                new bootstrap.Dropdown(element);
            }
        });

        // Handle mini cart interactions
        const miniCartProducts = document.querySelectorAll('.mini-cart-product');
        miniCartProducts.forEach(function(product) {
            const removeBtn = product.querySelector('.mini-cart-product-remove');
            if (removeBtn) {
                removeBtn.addEventListener('click', function() {
                    // Add remove animation
                    product.style.transition = 'all 0.3s ease';
                    product.style.opacity = '0';
                    product.style.transform = 'translateX(-100%)';
                    
                    setTimeout(function() {
                        product.remove();
                        updateCartCount();
                    }, 300);
                });
            }
        });
    }

    /**
     * Initialize offcanvas functionality
     */
    function initializeOffcanvas() {
        const offcanvasElements = document.querySelectorAll('[data-bs-toggle="offcanvas"]');
        offcanvasElements.forEach(function(element) {
            if (typeof bootstrap !== 'undefined') {
                new bootstrap.Offcanvas(element);
            }
        });

        // Handle category menu interactions
        const categoryLinks = document.querySelectorAll('.vertical-menu-items a');
        categoryLinks.forEach(function(link) {
            link.addEventListener('click', function(e) {
                // If it's a dropdown toggle, prevent default and handle manually
                if (this.classList.contains('dropdown-toggle')) {
                    e.preventDefault();
                    const nextElement = this.nextElementSibling;
                    if (nextElement && nextElement.classList.contains('dropdown-menu')) {
                        nextElement.classList.toggle('show');
                    }
                }
            });
        });

        // Handle existing vertical menu functionality
        const verticalMenuBtn = document.querySelector('.vertical-menu-btn');
        if (verticalMenuBtn) {
            verticalMenuBtn.addEventListener('click', function(e) {
                e.preventDefault();
                const offcanvas = document.querySelector('#categoryOffcanvas');
                if (offcanvas && typeof bootstrap !== 'undefined') {
                    const bsOffcanvas = new bootstrap.Offcanvas(offcanvas);
                    bsOffcanvas.show();
                }
            });
        }
    }

    /**
     * Initialize mobile menu functionality
     */
    function initializeMobileMenu() {
        const mobileMenuLinks = document.querySelectorAll('.menu a');
        mobileMenuLinks.forEach(function(link) {
            if (link.classList.contains('dropdown-toggle')) {
                link.addEventListener('click', function(e) {
                    e.preventDefault();
                    const targetId = this.getAttribute('data-bs-target');
                    const targetElement = document.querySelector(targetId);
                    if (targetElement) {
                        // Toggle collapse
                        if (typeof bootstrap !== 'undefined') {
                            const collapse = new bootstrap.Collapse(targetElement, {
                                toggle: true
                            });
                        }
                    }
                });
            }
        });
    }

    /**
     * Initialize Swiper for promotional messages
     */
    function initializeSwiper() {
        const swiperContainers = document.querySelectorAll('.notification-swiper-slider');
        swiperContainers.forEach(function(container) {
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
    }

    /**
     * Update cart count after item removal
     */
    function updateCartCount() {
        const cartCounters = document.querySelectorAll('.user-option--cart .badge-count');
        const miniCartProducts = document.querySelectorAll('.mini-cart-product');
        const newCount = miniCartProducts.length;
        
        cartCounters.forEach(function(counter) {
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

    /**
     * Handle responsive behavior
     */
    function handleResponsive() {
        const isMobile = window.innerWidth < 768;
        const searchContainers = document.querySelectorAll('.search-container');
        
        searchContainers.forEach(function(container) {
            if (isMobile) {
                container.classList.add('mobile-search');
            } else {
                container.classList.remove('mobile-search');
            }
        });
    }

    // Handle window resize
    window.addEventListener('resize', function() {
        handleResponsive();
    });

    // Initial responsive check
    handleResponsive();

    /**
     * Handle keyboard navigation
     */
    document.addEventListener('keydown', function(e) {
        // Close dropdowns and offcanvas on Escape
        if (e.key === 'Escape') {
            const openDropdowns = document.querySelectorAll('.dropdown-menu.show');
            openDropdowns.forEach(function(dropdown) {
                dropdown.classList.remove('show');
            });

            const openOffcanvas = document.querySelectorAll('.offcanvas.show');
            openOffcanvas.forEach(function(offcanvas) {
                if (typeof bootstrap !== 'undefined') {
                    const bsOffcanvas = bootstrap.Offcanvas.getInstance(offcanvas);
                    if (bsOffcanvas) {
                        bsOffcanvas.hide();
                    }
                }
            });
        }
    });

    /**
     * Handle touch/swipe gestures for mobile
     */
    let touchStartX = 0;
    let touchStartY = 0;

    document.addEventListener('touchstart', function(e) {
        touchStartX = e.touches[0].clientX;
        touchStartY = e.touches[0].clientY;
    });

    document.addEventListener('touchend', function(e) {
        if (!touchStartX || !touchStartY) {
            return;
        }

        const touchEndX = e.changedTouches[0].clientX;
        const touchEndY = e.changedTouches[0].clientY;

        const diffX = touchStartX - touchEndX;
        const diffY = touchStartY - touchEndY;

        // Horizontal swipe
        if (Math.abs(diffX) > Math.abs(diffY)) {
            if (Math.abs(diffX) > 50) { // Minimum swipe distance
                if (diffX > 0) {
                    // Swipe left - close offcanvas
                    const openOffcanvas = document.querySelector('.offcanvas.show');
                    if (openOffcanvas) {
                        if (typeof bootstrap !== 'undefined') {
                            const bsOffcanvas = bootstrap.Offcanvas.getInstance(openOffcanvas);
                            if (bsOffcanvas) {
                                bsOffcanvas.hide();
                            }
                        }
                    }
                }
            }
        }

        touchStartX = 0;
        touchStartY = 0;
    });

    /**
     * Initialize lazy loading for images
     */
    function initializeLazyLoading() {
        const images = document.querySelectorAll('img[data-src]');
        
        if ('IntersectionObserver' in window) {
            const imageObserver = new IntersectionObserver(function(entries, observer) {
                entries.forEach(function(entry) {
                    if (entry.isIntersecting) {
                        const img = entry.target;
                        img.src = img.dataset.src;
                        img.classList.remove('lazy');
                        imageObserver.unobserve(img);
                    }
                });
            });

            images.forEach(function(img) {
                imageObserver.observe(img);
            });
        } else {
            // Fallback for older browsers
            images.forEach(function(img) {
                img.src = img.dataset.src;
                img.classList.remove('lazy');
            });
        }
    }

    // Initialize lazy loading
    initializeLazyLoading();

    /**
     * Handle search suggestions
     */
    function initializeSearchSuggestions() {
        const searchFields = document.querySelectorAll('.search-field');
        
        searchFields.forEach(function(field) {
            let searchTimeout;
            
            field.addEventListener('input', function() {
                clearTimeout(searchTimeout);
                const query = this.value.trim();
                
                if (query.length >= 2) {
                    searchTimeout = setTimeout(function() {
                        // Here you would typically make an AJAX request to get suggestions
                        // For now, we'll just show the existing results
                        const container = field.closest('.search-container');
                        const resultContainer = container.querySelector('.search-result-container');
                        if (resultContainer) {
                            resultContainer.classList.add('show');
                        }
                    }, 300);
                }
            });
        });
    }

    // Initialize search suggestions
    initializeSearchSuggestions();

})();
