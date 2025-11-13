/**
 * Box Module
 * Vanilla JS replacement for jQuery-based box functionality
 */

export function mountBox(root = document) {
    // Box toggle functionality
    const boxButtons = root.querySelectorAll('[data-btn-box]');
    boxButtons.forEach(button => {
        button.addEventListener('click', (e) => {
            e.preventDefault();
            
            const parent = button.dataset.parent;
            const target = button.dataset.target;
            
            if (parent && target) {
                const parentElement = root.querySelector(parent);
                const targetElement = root.querySelector(target);
                
                if (parentElement && targetElement) {
                    parentElement.classList.add('d-none');
                    targetElement.classList.remove('d-none');
                }
            }
        });
    });

    // Box close functionality
    const boxCloseButtons = root.querySelectorAll('[data-btn-box-close]');
    boxCloseButtons.forEach(button => {
        button.addEventListener('click', (e) => {
            e.preventDefault();
            
            const parent = button.dataset.parent;
            const show = button.dataset.show;
            
            if (parent && show) {
                const parentElement = root.querySelector(parent);
                const showElement = root.querySelector(show);
                
                if (parentElement && showElement) {
                    parentElement.classList.add('d-none');
                    showElement.classList.remove('d-none');
                }
            }
        });
    });

    // Responsive sidebar toggle
    const toggleResponsiveSidebar = root.querySelector('.toggle-responsive-sidebar');
    const responsiveSidebar = root.querySelector('.responsive-sidebar');
    const responsiveSidebarOverlay = root.querySelector('.responsive-sidebar-overlay');

    if (toggleResponsiveSidebar && responsiveSidebar && responsiveSidebarOverlay) {
        toggleResponsiveSidebar.addEventListener('click', (e) => {
            e.preventDefault();
            responsiveSidebar.classList.add('show');
            responsiveSidebarOverlay.classList.add('show');
        });

        responsiveSidebarOverlay.addEventListener('click', (e) => {
            e.preventDefault();
            responsiveSidebar.classList.remove('show');
            responsiveSidebarOverlay.classList.remove('show');
        });
    }

    // Fancybox configuration
    if (typeof Fancybox !== 'undefined') {
        Fancybox.defaults.Hash = false;
    }

    console.log('Box module initialized');
}
