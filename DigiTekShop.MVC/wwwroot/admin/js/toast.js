// ---------------------
// Modern Toast Notification System
// برای پیام‌های معمولی: Success, Error, Info
// ---------------------
class Toast {
    static show({ message, type = 'info', duration = 4000, position = 'top-end' }) {
        // Create toast container if it doesn't exist
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.className = 'toast-container';
            document.body.appendChild(container);
        }

        // Create toast element
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');

        // Icon based on type
        let icon = 'ℹ️';
        let iconClass = 'toast-icon-info';
        if (type === 'success') {
            icon = '✅';
            iconClass = 'toast-icon-success';
        } else if (type === 'error') {
            icon = '❌';
            iconClass = 'toast-icon-error';
        } else if (type === 'warning') {
            icon = '⚠️';
            iconClass = 'toast-icon-warning';
        }

        toast.innerHTML = `
            <div class="toast-content">
                <span class="toast-icon ${iconClass}">${icon}</span>
                <span class="toast-message">${this.escapeHtml(message)}</span>
                <button class="toast-close" aria-label="بستن">×</button>
            </div>
            <div class="toast-progress"></div>
        `;

        // Add to container
        container.appendChild(toast);

        // Trigger animation
        setTimeout(() => {
            toast.classList.add('show');
        }, 10);

        // Auto remove
        const progressBar = toast.querySelector('.toast-progress');
        if (progressBar && duration > 0) {
            progressBar.style.animation = `toast-progress ${duration}ms linear forwards`;
        }

        const removeToast = () => {
            toast.classList.remove('show');
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        };

        // Close button
        toast.querySelector('.toast-close').onclick = removeToast;

        // Auto remove after duration
        if (duration > 0) {
            setTimeout(removeToast, duration);
        }

        return toast;
    }

    static success(message, duration = 4000) {
        return this.show({ message, type: 'success', duration });
    }

    static error(message, duration = 5000) {
        return this.show({ message, type: 'error', duration });
    }

    static info(message, duration = 4000) {
        return this.show({ message, type: 'info', duration });
    }

    static warning(message, duration = 4500) {
        return this.show({ message, type: 'warning', duration });
    }

    static escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Helper: نمایش Toast از Result API Response
    static fromApiResponse(response) {
        if (!response || !response.ok) {
            return;
        }

        return response.json().then(data => {
            // بررسی ساختار Response
            const apiResponse = data.data || data;
            const isSuccess = data.success !== false && !data.errorCode;
            const message = apiResponse.message || data.message || (isSuccess ? 'عملیات با موفقیت انجام شد' : 'خطا در انجام عملیات');

            if (isSuccess) {
                this.success(message);
            } else {
                this.error(message);
            }
        }).catch(() => {
            // اگر JSON parse نشد، از status code استفاده کن
            if (response.status >= 400) {
                this.error('خطا در ارتباط با سرور');
            }
        });
    }
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = Toast;
}

// Make available globally
window.Toast = Toast;

