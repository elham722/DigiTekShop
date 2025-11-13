/**
 * Sticky Module
 * Vanilla JS replacement for jQuery-based sticky functionality
 */

export function mountSticky(root = document) {
    const stickyElements = root.querySelectorAll('.compare-container .compare-list');
    
    if (stickyElements.length === 0) {
        console.warn('Sticky: No sticky elements found');
        return;
    }

    stickyElements.forEach(element => {
        const top = element.offsetTop;
        let ticking = false;

        function updateSticky() {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            
            if (scrollTop >= top - 100) {
                element.classList.add('is-sticky');
            } else {
                element.classList.remove('is-sticky');
            }
            
            ticking = false;
        }

        function requestTick() {
            if (!ticking) {
                requestAnimationFrame(updateSticky);
                ticking = true;
            }
        }

        window.addEventListener('scroll', requestTick);
        
        // Initial call
        updateSticky();
    });

    console.log('Sticky module initialized');
}
