/**
 * Read More Module
 * Vanilla JS replacement for jQuery-based read more functionality
 */

export function mountReadMore(root = document) {
    const expandButtons = root.querySelectorAll('.expandable-text-expand-btn');
    
    if (expandButtons.length === 0) {
        console.warn('Read more: No expand buttons found');
        return;
    }

    expandButtons.forEach(button => {
        button.addEventListener('click', (e) => {
            e.preventDefault();
            
            const contentContainer = button.closest('.expandable-text');
            const showMore = button.querySelector('.show-more');
            const showLess = button.querySelector('.show-less');
            
            if (!contentContainer) return;

            // Toggle active class
            contentContainer.classList.toggle('active');
            
            // Toggle button text
            if (showMore && showLess) {
                showMore.classList.toggle('d-none');
                showLess.classList.toggle('d-none');
            }
        });
    });

    console.log('Read more module initialized');
}
