/**
 * Radio Module
 * Vanilla JS replacement for jQuery-based radio functionality
 */

export function mountRadio(root = document) {
    const radioLabels = root.querySelectorAll('.custom-radio-circle-label');
    
    if (radioLabels.length === 0) {
        console.warn('Radio: No radio labels found');
        return;
    }

    radioLabels.forEach(label => {
        label.addEventListener('click', (e) => {
            const variantLabel = label.dataset.variantLabel;
            const selectedElement = root.querySelector('.product-variant-selected');
            
            if (variantLabel && selectedElement) {
                selectedElement.textContent = variantLabel;
            }
        });
    });

    console.log('Radio module initialized');
}
