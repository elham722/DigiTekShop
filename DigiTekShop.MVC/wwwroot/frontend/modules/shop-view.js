/**
 * Shop View Module
 * Vanilla JS replacement for jQuery-based shop view functionality
 */

export function mountShopView(root = document) {
    const btnListView = root.querySelector('.btn-list-view');
    const btnGridView = root.querySelector('.btn-grid-view');
    const productContainers = root.querySelectorAll('.listing-products-content .product-card-container');
    const productCards = root.querySelectorAll('.product-card');

    if (!btnListView || !btnGridView || productContainers.length === 0) {
        console.warn('Shop view: Required elements not found');
        return;
    }

    const switchToListView = () => {
        productContainers.forEach(container => {
            // Remove grid classes
            container.classList.remove('col-xl-3', 'col-lg-4', 'col-md-6', 'col-sm-6');
            
            // Add list classes
            container.classList.add('col-lg-6', 'col-md-12', 'col-sm-6');
        });

        // Add horizontal class to product cards
        productCards.forEach(card => {
            card.classList.add('product-card-horizontal');
        });

        // Update button states
        btnListView.classList.add('active');
        btnGridView.classList.remove('active');

        // Store preference in localStorage
        localStorage.setItem('shop-view-preference', 'list');
    };

    const switchToGridView = () => {
        productContainers.forEach(container => {
            // Remove list classes
            container.classList.remove('col-lg-6', 'col-md-12', 'col-sm-6');
            
            // Add grid classes
            container.classList.add('col-xl-3', 'col-lg-4', 'col-md-6', 'col-sm-6');
        });

        // Remove horizontal class from product cards
        productCards.forEach(card => {
            card.classList.remove('product-card-horizontal');
        });

        // Update button states
        btnGridView.classList.add('active');
        btnListView.classList.remove('active');

        // Store preference in localStorage
        localStorage.setItem('shop-view-preference', 'grid');
    };

    // Event listeners
    btnListView.addEventListener('click', (e) => {
        e.preventDefault();
        switchToListView();
    });

    btnGridView.addEventListener('click', (e) => {
        e.preventDefault();
        switchToGridView();
    });

    // Restore saved preference on page load
    const savedPreference = localStorage.getItem('shop-view-preference');
    if (savedPreference === 'list') {
        switchToListView();
    } else {
        switchToGridView();
    }

    console.log('Shop view module initialized');
}
