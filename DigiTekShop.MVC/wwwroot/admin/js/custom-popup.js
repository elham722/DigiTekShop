// ---------------------
// Custom Modern Popup
// ---------------------
class CustomPopup {
    static show({ title, message, type = 'info', confirmText = 'بله', cancelText = 'انصراف', showCancel = true, onConfirm, onCancel }) {
        return new Promise((resolve) => {
            // Remove existing popup if any
            const existing = document.querySelector('.custom-popup-overlay');
            if (existing) {
                existing.remove();
            }

            // Create overlay
            const overlay = document.createElement('div');
            overlay.className = 'custom-popup-overlay';
            overlay.setAttribute('dir', 'rtl');

            // Create container
            const container = document.createElement('div');
            container.className = 'custom-popup-container';

            // Create header
            const header = document.createElement('div');
            header.className = `custom-popup-header ${type}`;

            // Icon based on type
            let icon = '❓';
            if (type === 'warning') icon = '⚠️';
            else if (type === 'success') icon = '✅';
            else if (type === 'error') icon = '❌';
            else if (type === 'info') icon = 'ℹ️';

            header.innerHTML = `
                <button class="custom-popup-close" aria-label="بستن">×</button>
                <span class="custom-popup-icon">${icon}</span>
                <h3 class="custom-popup-title">${title}</h3>
            `;

            // Create body
            const body = document.createElement('div');
            body.className = 'custom-popup-body';
            body.innerHTML = `<p class="custom-popup-message">${message}</p>`;

            // Create footer
            const footer = document.createElement('div');
            footer.className = 'custom-popup-footer';

            if (showCancel) {
                const cancelBtn = document.createElement('button');
                cancelBtn.className = 'custom-popup-btn custom-popup-btn-secondary';
                cancelBtn.textContent = cancelText;
                cancelBtn.onclick = () => {
                    this.close(overlay);
                    if (onCancel) onCancel();
                    resolve({ isConfirmed: false });
                };
                footer.appendChild(cancelBtn);
            }

            const confirmBtn = document.createElement('button');
            confirmBtn.className = `custom-popup-btn custom-popup-btn-${type === 'error' || type === 'warning' ? 'danger' : type === 'success' ? 'success' : 'primary'}`;
            confirmBtn.textContent = confirmText;
            confirmBtn.onclick = () => {
                this.close(overlay);
                if (onConfirm) onConfirm();
                resolve({ isConfirmed: true });
            };
            footer.appendChild(confirmBtn);

            // Assemble
            container.appendChild(header);
            container.appendChild(body);
            container.appendChild(footer);
            overlay.appendChild(container);

            // Close on overlay click (outside popup)
            overlay.onclick = (e) => {
                if (e.target === overlay) {
                    this.close(overlay);
                    if (onCancel) onCancel();
                    resolve({ isConfirmed: false });
                }
            };

            // Close button
            header.querySelector('.custom-popup-close').onclick = () => {
                this.close(overlay);
                if (onCancel) onCancel();
                resolve({ isConfirmed: false });
            };

            // Add to DOM
            document.body.appendChild(overlay);

            // Trigger animation
            setTimeout(() => {
                overlay.classList.add('show');
            }, 10);

            // ESC key to close
            const escHandler = (e) => {
                if (e.key === 'Escape') {
                    this.close(overlay);
                    if (onCancel) onCancel();
                    resolve({ isConfirmed: false });
                    document.removeEventListener('keydown', escHandler);
                }
            };
            document.addEventListener('keydown', escHandler);
        });
    }

    static close(overlay) {
        if (overlay) {
            overlay.classList.remove('show');
            setTimeout(() => {
                overlay.remove();
            }, 300);
        }
    }

    static success(title, message, confirmText = 'باشه') {
        return this.show({
            title,
            message,
            type: 'success',
            showCancel: false,
            confirmText
        });
    }

    static error(title, message, confirmText = 'باشه') {
        return this.show({
            title,
            message,
            type: 'error',
            showCancel: false,
            confirmText
        });
    }

    static warning(title, message, confirmText = 'باشه') {
        return this.show({
            title,
            message,
            type: 'warning',
            showCancel: false,
            confirmText
        });
    }

    static info(title, message, confirmText = 'باشه') {
        return this.show({
            title,
            message,
            type: 'info',
            showCancel: false,
            confirmText
        });
    }

    static confirm(title, message, confirmText = 'بله', cancelText = 'انصراف') {
        return this.show({
            title,
            message,
            type: 'warning',
            showCancel: true,
            confirmText,
            cancelText
        });
    }
}

// Export for use in modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CustomPopup;
}

// Make available globally
window.CustomPopup = CustomPopup;

