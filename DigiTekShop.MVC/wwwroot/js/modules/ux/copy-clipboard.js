/**
 * Copy Clipboard Module
 * Vanilla JS replacement for jQuery-based copy clipboard functionality
 */

export function mountCopyClipboard(root = document) {
    const copyButtons = root.querySelectorAll('.copy-url-btn');
    
    if (copyButtons.length === 0) {
        console.warn('Copy clipboard: No copy buttons found');
        return;
    }

    function copyToClipboard(text) {
        // Modern clipboard API
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(text);
        }
        
        // Fallback for older browsers
        return new Promise((resolve, reject) => {
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.left = '-999999px';
            textArea.style.top = '-999999px';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            
            try {
                const successful = document.execCommand('copy');
                document.body.removeChild(textArea);
                if (successful) {
                    resolve();
                } else {
                    reject(new Error('Copy command failed'));
                }
            } catch (err) {
                document.body.removeChild(textArea);
                reject(err);
            }
        });
    }

    copyButtons.forEach(button => {
        button.addEventListener('click', async (e) => {
            e.preventDefault();
            
            const textToCopy = button.dataset.copy;
            if (!textToCopy) {
                console.warn('Copy button: No data-copy attribute found');
                return;
            }

            try {
                await copyToClipboard(textToCopy);
                
                // Update button state
                button.classList.add('copied');
                button.textContent = 'کپی شد';
                
                // Reset button after 2 seconds
                setTimeout(() => {
                    button.classList.remove('copied');
                    button.textContent = 'کپی لینک';
                }, 2000);
                
            } catch (error) {
                console.error('Copy failed:', error);
                // Show error message
                button.textContent = 'خطا در کپی';
                setTimeout(() => {
                    button.textContent = 'کپی لینک';
                }, 2000);
            }
        });
    });

    console.log('Copy clipboard module initialized');
}
