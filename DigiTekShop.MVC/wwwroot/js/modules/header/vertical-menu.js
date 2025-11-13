/**
 * Vertical Menu Module
 * Vanilla JS replacement for jQuery-based vertical menu functionality
 */

export function mountVerticalMenu(root = document) {
    const verticalMenuItems = root.querySelector('.vertical-menu-items');
    if (!verticalMenuItems) return;

    const menuItems = verticalMenuItems.querySelectorAll('ul > li');
    if (menuItems.length === 0) return;

    // Add hover functionality
    menuItems.forEach(item => {
        item.addEventListener('mouseenter', () => {
            // Remove show class from siblings
            menuItems.forEach(sibling => {
                if (sibling !== item) {
                    sibling.classList.remove('show');
                }
            });
            
            // Add show class to current item
            item.classList.add('show');
        });

        // Optional: Remove show class on mouse leave
        item.addEventListener('mouseleave', () => {
            // Only remove if not hovering over submenu
            const submenu = item.querySelector('.submenu');
            if (submenu) {
                submenu.addEventListener('mouseleave', () => {
                    item.classList.remove('show');
                });
            } else {
                item.classList.remove('show');
            }
        });
    });

    console.log('Vertical menu module initialized');
}
