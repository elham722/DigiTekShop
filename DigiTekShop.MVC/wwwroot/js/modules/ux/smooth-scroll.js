/**
 * Smooth Scroll Module
 * Vanilla JS replacement for jQuery-based smooth scroll functionality
 */

export function mountSmoothScroll(root = document) {
    const scrollLinks = root.querySelectorAll('.product-tabs a.nav-link');
    
    if (scrollLinks.length === 0) {
        console.warn('Smooth scroll: No scroll links found');
        return;
    }

    // Move to specific section when click on menu link
    scrollLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            
            const targetId = link.getAttribute('href');
            const target = document.querySelector(targetId);
            
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
            
            // Update active state
            scrollLinks.forEach(l => l.classList.remove('active'));
            link.classList.add('active');
        });
    });

    // Run the scrNav when scroll
    let ticking = false;
    
    function updateActiveNav() {
        const sTop = window.pageYOffset || document.documentElement.scrollTop;
        const tabContents = root.querySelectorAll('.tab-content');
        
        tabContents.forEach(tabContent => {
            const id = tabContent.getAttribute('id');
            const offset = tabContent.offsetTop - 1;
            const height = tabContent.offsetHeight;
            
            if (sTop >= offset && sTop < offset + height) {
                scrollLinks.forEach(link => link.classList.remove('active'));
                const activeLink = root.querySelector(`[data-scroll="${id}"]`);
                if (activeLink) {
                    activeLink.classList.add('active');
                }
            }
        });
        
        ticking = false;
    }

    function requestTick() {
        if (!ticking) {
            requestAnimationFrame(updateActiveNav);
            ticking = true;
        }
    }

    window.addEventListener('scroll', requestTick);
    
    // Initial call
    updateActiveNav();

    console.log('Smooth scroll module initialized');
}
