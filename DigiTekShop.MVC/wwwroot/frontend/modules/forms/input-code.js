/**
 * Input Code Module
 * Vanilla JS replacement for jQuery-based input code functionality
 */

export function mountInputCode(root = document) {
    const inputCodeElements = root.querySelectorAll('.form-input-code-container .input-code');
    
    if (inputCodeElements.length === 0) {
        console.warn('Input code: No input code elements found');
        return;
    }

    inputCodeElements.forEach(input => {
        input.addEventListener('keyup', (e) => {
            if (e.target.value.length === e.target.maxLength) {
                const nextIndex = e.target.dataset.next;
                if (nextIndex) {
                    const nextInput = document.getElementById(`input-code-${nextIndex}`);
                    if (nextInput) {
                        nextInput.focus();
                    }
                }
            }
        });

        // Handle backspace to go to previous input
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Backspace' && e.target.value === '') {
                const currentIndex = e.target.dataset.next;
                if (currentIndex) {
                    const prevIndex = parseInt(currentIndex) - 1;
                    const prevInput = document.getElementById(`input-code-${prevIndex}`);
                    if (prevInput) {
                        prevInput.focus();
                    }
                }
            }
        });

        // Handle paste
        input.addEventListener('paste', (e) => {
            e.preventDefault();
            const pastedData = e.clipboardData.getData('text');
            const digits = pastedData.replace(/\D/g, '').split('');
            
            // Fill current and next inputs
            let currentIndex = parseInt(e.target.dataset.next) - 1;
            digits.forEach((digit, index) => {
                const targetInput = document.getElementById(`input-code-${currentIndex + index}`);
                if (targetInput) {
                    targetInput.value = digit;
                }
            });
            
            // Focus on the last filled input or next empty one
            const lastFilledIndex = currentIndex + digits.length - 1;
            const nextEmptyIndex = lastFilledIndex + 1;
            const nextEmptyInput = document.getElementById(`input-code-${nextEmptyIndex}`);
            if (nextEmptyInput) {
                nextEmptyInput.focus();
            }
        });
    });

    console.log('Input code module initialized');
}
