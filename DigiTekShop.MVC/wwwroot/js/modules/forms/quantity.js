/**
 * Quantity Module
 * Vanilla JS replacement for jQuery-based quantity functionality
 */

export function mountQuantity(root = document) {
    const quantityControls = root.querySelectorAll('.num-in span');
    
    if (quantityControls.length === 0) {
        console.warn('Quantity: No quantity controls found');
        return;
    }

    quantityControls.forEach(control => {
        control.addEventListener('click', (e) => {
            e.preventDefault();
            
            const numBlock = control.closest('.num-block');
            const input = numBlock?.querySelector('input.in-num');
            const minusBtn = numBlock?.querySelector('.minus');
            
            if (!input) return;

            let count = parseFloat(input.value) || 1;

            if (control.classList.contains('minus')) {
                count = Math.max(1, count - 1);
                
                // Update minus button state
                if (minusBtn) {
                    if (count < 2) {
                        minusBtn.classList.add('dis');
                    } else {
                        minusBtn.classList.remove('dis');
                    }
                }
            } else {
                count = count + 1;
                
                // Enable minus button if count > 1
                if (minusBtn && count > 1) {
                    minusBtn.classList.remove('dis');
                }
            }

            input.value = count;
            
            // Trigger change event
            input.dispatchEvent(new Event('change', { bubbles: true }));
        });
    });

    console.log('Quantity module initialized');
}
