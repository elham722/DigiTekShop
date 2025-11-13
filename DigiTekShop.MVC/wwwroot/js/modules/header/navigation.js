/**
 * Navigation Module
 * Vanilla JS replacement for jQuery-based navigation functionality
 */

export function mountNavigation(root = document) {
    const toggleNavigation = root.querySelector('.toggle-navigation');
    const navigation = root.querySelector('.navigation');
    const navigationOverlay = root.querySelector('.navigation-overlay');
    const closeNavigation = root.querySelector('.close-navigation');
    const toggleSubmenu = root.querySelectorAll('.toggle-submenu');
    const closeSubmenu = root.querySelectorAll('.close-submenu');
    const submenus = root.querySelectorAll('.submenu');

    if (!toggleNavigation || !navigation || !navigationOverlay) {
        console.warn('Navigation: Required elements not found');
        return;
    }

    const open = () => {
        navigation.classList.add('toggle');
        navigationOverlay.style.display = 'block';
        navigationOverlay.style.opacity = '1';
        document.body.style.overflow = 'hidden'; // Prevent background scrolling
    };

    const close = () => {
        navigation.classList.remove('toggle');
        navigationOverlay.style.display = 'none';
        navigationOverlay.style.opacity = '0';
        document.body.style.overflow = ''; // Restore scrolling
        
        // Close all submenus
        submenus.forEach(submenu => {
            submenu.classList.remove('toggle');
        });
    };

    // Toggle navigation
    toggleNavigation.addEventListener('click', (e) => {
        e.preventDefault();
        open();
    });

    // Close navigation
    closeNavigation?.addEventListener('click', (e) => {
        e.preventDefault();
        close();
    });

    // Close on overlay click
    navigationOverlay.addEventListener('click', (e) => {
        e.preventDefault();
        close();
    });

    // Close on Escape key
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && navigation.classList.contains('toggle')) {
            close();
        }
    });

    // Toggle submenus
    toggleSubmenu.forEach(button => {
        button.addEventListener('click', (e) => {
            e.preventDefault();
            const submenu = button.nextElementSibling;
            if (submenu && submenu.classList.contains('submenu')) {
                submenu.classList.add('toggle');
            }
        });
    });

    // Close submenus
    closeSubmenu.forEach(button => {
        button.addEventListener('click', (e) => {
            e.preventDefault();
            const submenu = button.closest('.submenu');
            if (submenu) {
                submenu.classList.remove('toggle');
            }
        });
    });

    // Close submenu when clicking outside
    document.addEventListener('click', (e) => {
        if (!e.target.closest('.navigation')) {
            submenus.forEach(submenu => {
                submenu.classList.remove('toggle');
            });
        }
    });

    console.log('Navigation module initialized');
}
