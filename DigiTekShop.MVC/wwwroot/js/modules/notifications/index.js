/**
 * DigiTekShop - Modern Notification System
 * Vanilla JavaScript replacement for notifications.js
 */

export class DigiTekNotification {
    constructor() {
        this.defaultConfig = {
            confirmButtonText: 'باشه',
            cancelButtonText: 'لغو',
            allowOutsideClick: false,
            allowEscapeKey: true,
            showConfirmButton: true,
            showCancelButton: false,
            timer: null,
            timerProgressBar: true,
            position: 'center',
            backdrop: true,
            customClass: {
                popup: 'swal2-popup-custom',
                title: 'swal2-title-custom',
                content: 'swal2-content-custom',
                confirmButton: 'swal2-confirm-custom',
                cancelButton: 'swal2-cancel-custom'
            }
        };
        
        this.toastContainer = null;
        this.init();
    }

    init() {
        // Create toast container if it doesn't exist
        this.createToastContainer();
        
        // Set default language
        this.setLanguage('fa');
        
        // Setup global error handler
        this.setupGlobalErrorHandler();
    }

    createToastContainer() {
        if (!document.getElementById('toast-container')) {
            const container = document.createElement('div');
            container.id = 'toast-container';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            document.body.appendChild(container);
            this.toastContainer = container;
        }
    }

    setLanguage(lang) {
        if (lang === 'fa') {
            this.defaultConfig.confirmButtonText = 'باشه';
            this.defaultConfig.cancelButtonText = 'لغو';
        }
    }

    setupGlobalErrorHandler() {
        // Global fetch error handler is handled in api.js
        // This is for additional global error handling if needed
    }

    // ========================================
    // Success Notifications
    // ========================================

    showSuccess(title, text = '', options = {}) {
        const config = {
            ...this.defaultConfig,
            icon: 'success',
            title: title,
            text: text,
            confirmButtonText: 'باشه',
            confirmButtonColor: '#28a745',
            ...options
        };

        return this.showSwal(config);
    }

    showSuccessToast(message, duration = 3000) {
        this.showToast('success', 'موفقیت!', message, duration);
    }

    // ========================================
    // Error Notifications
    // ========================================

    showError(title, text = '', options = {}) {
        const config = {
            ...this.defaultConfig,
            icon: 'error',
            title: title,
            text: text,
            confirmButtonText: 'تلاش مجدد',
            confirmButtonColor: '#dc3545',
            ...options
        };

        return this.showSwal(config);
    }

    showErrorToast(message, duration = 5000) {
        this.showToast('error', 'خطا!', message, duration);
    }

    // ========================================
    // Warning Notifications
    // ========================================

    showWarning(title, text = '', options = {}) {
        const config = {
            ...this.defaultConfig,
            icon: 'warning',
            title: title,
            text: text,
            confirmButtonText: 'باشه',
            confirmButtonColor: '#ffc107',
            ...options
        };

        return this.showSwal(config);
    }

    showWarningToast(message, duration = 4000) {
        this.showToast('warning', 'هشدار!', message, duration);
    }

    // ========================================
    // Info Notifications
    // ========================================

    showInfo(title, text = '', options = {}) {
        const config = {
            ...this.defaultConfig,
            icon: 'info',
            title: title,
            text: text,
            confirmButtonText: 'باشه',
            confirmButtonColor: '#17a2b8',
            ...options
        };

        return this.showSwal(config);
    }

    showInfoToast(message, duration = 3000) {
        this.showToast('info', 'اطلاعات', message, duration);
    }

    // ========================================
    // Loading Notifications
    // ========================================

    showLoading(title = 'در حال پردازش...', text = 'لطفاً صبر کنید') {
        return this.showSwal({
            title: title,
            text: text,
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false,
            didOpen: () => {
                // Show loading spinner
                const popup = document.querySelector('.swal2-popup');
                if (popup) {
                    const loading = document.createElement('div');
                    loading.className = 'swal2-loader';
                    loading.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
                    popup.appendChild(loading);
                }
            }
        });
    }

    hideLoading() {
        this.closeSwal();
    }

    // ========================================
    // Confirmation Dialogs
    // ========================================

    showConfirm(title, text = '', options = {}) {
        const config = {
            ...this.defaultConfig,
            icon: 'question',
            title: title,
            text: text,
            showCancelButton: true,
            confirmButtonText: 'بله',
            cancelButtonText: 'خیر',
            confirmButtonColor: '#28a745',
            cancelButtonColor: '#6c757d',
            ...options
        };

        return this.showSwal(config);
    }

    showDeleteConfirm(itemName = 'این آیتم') {
        return this.showConfirm(
            'حذف آیتم',
            `آیا مطمئن هستید که می‌خواهید ${itemName} را حذف کنید؟`,
            {
                icon: 'warning',
                confirmButtonText: 'بله، حذف کن',
                cancelButtonText: 'لغو',
                confirmButtonColor: '#dc3545'
            }
        );
    }

    // ========================================
    // Input Dialogs
    // ========================================

    showInput(title, inputType = 'text', options = {}) {
        const config = {
            ...this.defaultConfig,
            title: title,
            input: inputType,
            inputPlaceholder: 'لطفاً مقدار را وارد کنید',
            showCancelButton: true,
            confirmButtonText: 'تأیید',
            cancelButtonText: 'لغو',
            inputValidator: (value) => {
                if (!value) {
                    return 'لطفاً مقدار را وارد کنید';
                }
            },
            ...options
        };

        return this.showSwal(config);
    }

    showPasswordInput(title = 'رمز عبور', options = {}) {
        return this.showInput(title, 'password', {
            inputPlaceholder: 'رمز عبور خود را وارد کنید',
            ...options
        });
    }

    showEmailInput(title = 'ایمیل', options = {}) {
        return this.showInput(title, 'email', {
            inputPlaceholder: 'ایمیل خود را وارد کنید',
            inputValidator: (value) => {
                if (!value) {
                    return 'لطفاً ایمیل را وارد کنید';
                }
                if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
                    return 'لطفاً ایمیل معتبر وارد کنید';
                }
            },
            ...options
        });
    }

    // ========================================
    // Custom Toast Notifications
    // ========================================

    showToast(type, title, message, duration = 3000) {
        const toastId = 'toast-' + Date.now();
        const iconClass = this.getIconClass(type);
        
        const toastHtml = `
            <div id="${toastId}" class="toast toast-${type} show" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header">
                    <i class="${iconClass} me-2"></i>
                    <strong class="me-auto">${title}</strong>
                    <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;

        this.toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        
        const toastElement = document.getElementById(toastId);
        
        // Use Bootstrap Toast if available, otherwise custom implementation
        if (window.bootstrap && window.bootstrap.Toast) {
            const toast = new window.bootstrap.Toast(toastElement, {
                autohide: true,
                delay: duration
            });
            toast.show();
        } else {
            // Fallback: custom toast implementation
            this.showCustomToast(toastElement, duration);
        }

        // Remove element after hide
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });

        return toastElement;
    }

    showCustomToast(element, duration) {
        // Simple custom toast implementation
        element.style.opacity = '0';
        element.style.transform = 'translateX(100%)';
        element.style.transition = 'all 0.3s ease';
        
        setTimeout(() => {
            element.style.opacity = '1';
            element.style.transform = 'translateX(0)';
        }, 100);
        
        setTimeout(() => {
            element.style.opacity = '0';
            element.style.transform = 'translateX(100%)';
            setTimeout(() => element.remove(), 300);
        }, duration);
    }

    getIconClass(type) {
        const icons = {
            success: 'fas fa-check-circle',
            error: 'fas fa-exclamation-circle',
            warning: 'fas fa-exclamation-triangle',
            info: 'fas fa-info-circle'
        };
        return icons[type] || 'fas fa-info-circle';
    }

    // ========================================
    // Progress Notifications
    // ========================================

    showProgress(title, text = '') {
        return this.showSwal({
            title: title,
            text: text,
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false,
            didOpen: () => {
                // Show progress bar
                const popup = document.querySelector('.swal2-popup');
                if (popup) {
                    const progress = document.createElement('div');
                    progress.className = 'progress mt-3';
                    progress.innerHTML = `
                        <div class="progress-bar" role="progressbar" style="width: 0%" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100">
                            0%
                        </div>
                    `;
                    popup.appendChild(progress);
                }
            }
        });
    }

    updateProgress(percent, text = '') {
        const popup = document.querySelector('.swal2-popup');
        if (popup) {
            const progressBar = popup.querySelector('.progress-bar');
            if (progressBar) {
                progressBar.style.width = `${percent}%`;
                progressBar.setAttribute('aria-valuenow', percent);
                progressBar.textContent = `${percent}%`;
            }
            
            if (text) {
                const textElement = popup.querySelector('.swal2-text');
                if (textElement) {
                    textElement.textContent = text;
                }
            }
        }
    }

    // ========================================
    // Form Validation Helpers
    // ========================================

    validateForm(formSelector, rules = {}) {
        const form = document.querySelector(formSelector);
        if (!form) return false;

        let isValid = true;
        const errors = [];

        // Check required fields
        const requiredFields = form.querySelectorAll('[required]');
        requiredFields.forEach(field => {
            if (!field.value.trim()) {
                isValid = false;
                errors.push(`فیلد ${field.name || field.id} الزامی است`);
            }
        });

        // Check email fields
        const emailFields = form.querySelectorAll('input[type="email"]');
        emailFields.forEach(field => {
            if (field.value && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(field.value)) {
                isValid = false;
                errors.push(`ایمیل ${field.name || field.id} معتبر نیست`);
            }
        });

        // Check phone fields
        const phoneFields = form.querySelectorAll('input[type="tel"]');
        phoneFields.forEach(field => {
            if (field.value && !/^(\+98|0)?9\d{9}$/.test(field.value.replace(/\s/g, ''))) {
                isValid = false;
                errors.push(`شماره تلفن ${field.name || field.id} معتبر نیست`);
            }
        });

        if (!isValid) {
            this.showError('خطا در فرم', errors.join('<br>'));
        }

        return isValid;
    }

    // ========================================
    // Utility Methods
    // ========================================

    showNetworkError() {
        this.showError('خطا در شبکه', 'لطفاً اتصال اینترنت خود را بررسی کنید');
    }

    showServerError() {
        this.showError('خطا در سرور', 'لطفاً بعداً تلاش کنید');
    }

    showValidationError(errors) {
        const errorList = Array.isArray(errors) ? errors.join('<br>') : errors;
        this.showError('خطا در اعتبارسنجی', errorList);
    }

    showSessionExpired() {
        this.showWarning('جلسه منقضی شده', 'لطفاً دوباره وارد شوید').then(() => {
            window.location.href = '/Auth/Login';
        });
    }

    showPermissionDenied() {
        this.showError('دسترسی غیرمجاز', 'شما دسترسی لازم را ندارید');
    }

    // ========================================
    // Animation Helpers
    // ========================================

    showAnimatedSuccess(title, text = '') {
        return this.showSwal({
            icon: 'success',
            title: title,
            text: text,
            showConfirmButton: false,
            timer: 2000,
            timerProgressBar: true,
            didOpen: () => {
                // Add custom animation
                const popup = document.querySelector('.swal2-popup');
                if (popup) {
                    popup.style.transform = 'scale(0.8)';
                    popup.style.transition = 'transform 0.3s ease';
                    setTimeout(() => {
                        popup.style.transform = 'scale(1)';
                    }, 100);
                }
            }
        });
    }

    showAnimatedError(title, text = '') {
        return this.showSwal({
            icon: 'error',
            title: title,
            text: text,
            showConfirmButton: true,
            confirmButtonText: 'باشه',
            didOpen: () => {
                // Add shake animation
                const popup = document.querySelector('.swal2-popup');
                if (popup) {
                    popup.style.animation = 'shake 0.5s ease-in-out';
                }
            }
        });
    }

    // ========================================
    // SweetAlert2 Integration
    // ========================================

    showSwal(config) {
        // Use SweetAlert2 if available, otherwise fallback to custom implementation
        if (window.Swal) {
            return window.Swal.fire(config);
        } else {
            // Fallback to custom modal implementation
            return this.showCustomModal(config);
        }
    }

    closeSwal() {
        if (window.Swal) {
            window.Swal.close();
        } else {
            this.closeCustomModal();
        }
    }

    showCustomModal(config) {
        // Simple custom modal implementation as fallback
        const modal = document.createElement('div');
        modal.className = 'custom-modal';
        modal.innerHTML = `
            <div class="custom-modal-backdrop"></div>
            <div class="custom-modal-dialog">
                <div class="custom-modal-content">
                    <div class="custom-modal-header">
                        <h5 class="custom-modal-title">${config.title || ''}</h5>
                        <button type="button" class="custom-modal-close">&times;</button>
                    </div>
                    <div class="custom-modal-body">
                        <p>${config.text || ''}</p>
                    </div>
                    <div class="custom-modal-footer">
                        ${config.showCancelButton ? `<button class="btn btn-secondary">${config.cancelButtonText}</button>` : ''}
                        <button class="btn btn-primary">${config.confirmButtonText}</button>
                    </div>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        
        // Add event listeners
        modal.querySelector('.custom-modal-close').addEventListener('click', () => {
            modal.remove();
        });
        
        modal.querySelector('.custom-modal-backdrop').addEventListener('click', () => {
            if (config.allowOutsideClick) {
                modal.remove();
            }
        });
        
        return Promise.resolve({ isConfirmed: true });
    }

    closeCustomModal() {
        const modal = document.querySelector('.custom-modal');
        if (modal) {
            modal.remove();
        }
    }
}

// ========================================
// Global Helper Functions (for backward compatibility)
// ========================================

// Success notifications
window.showSuccess = (title, text) => new DigiTekNotification().showSuccess(title, text);
window.showSuccessToast = (message) => new DigiTekNotification().showSuccessToast(message);

// Error notifications
window.showError = (title, text) => new DigiTekNotification().showError(title, text);
window.showErrorToast = (message) => new DigiTekNotification().showErrorToast(message);

// Warning notifications
window.showWarning = (title, text) => new DigiTekNotification().showWarning(title, text);
window.showWarningToast = (message) => new DigiTekNotification().showWarningToast(message);

// Info notifications
window.showInfo = (title, text) => new DigiTekNotification().showInfo(title, text);
window.showInfoToast = (message) => new DigiTekNotification().showInfoToast(message);

// Loading notifications
window.showLoading = (title, text) => new DigiTekNotification().showLoading(title, text);
window.hideLoading = () => new DigiTekNotification().hideLoading();

// Confirmation dialogs
window.showConfirm = (title, text) => new DigiTekNotification().showConfirm(title, text);
window.showDeleteConfirm = (itemName) => new DigiTekNotification().showDeleteConfirm(itemName);

// Input dialogs
window.showInput = (title, inputType) => new DigiTekNotification().showInput(title, inputType);
window.showPasswordInput = (title) => new DigiTekNotification().showPasswordInput(title);
window.showEmailInput = (title) => new DigiTekNotification().showEmailInput(title);

// Utility functions
window.showNetworkError = () => new DigiTekNotification().showNetworkError();
window.showServerError = () => new DigiTekNotification().showServerError();
window.showValidationError = (errors) => new DigiTekNotification().showValidationError(errors);
window.showSessionExpired = () => new DigiTekNotification().showSessionExpired();
window.showPermissionDenied = () => new DigiTekNotification().showPermissionDenied();

// ========================================
// Auto-initialization
// ========================================

document.addEventListener('DOMContentLoaded', () => {
    const notification = new DigiTekNotification();
    
    // Auto-show TempData messages
    if (typeof TempData !== 'undefined') {
        if (TempData.SuccessMessage) {
            notification.showSuccess('موفقیت!', TempData.SuccessMessage);
        }
        if (TempData.ErrorMessage) {
            notification.showError('خطا!', TempData.ErrorMessage);
        }
        if (TempData.WarningMessage) {
            notification.showWarning('هشدار!', TempData.WarningMessage);
        }
        if (TempData.InfoMessage) {
            notification.showInfo('اطلاعات', TempData.InfoMessage);
        }
    }

    // Form validation
    document.querySelectorAll('form[data-validate]').forEach(form => {
        form.addEventListener('submit', (e) => {
            if (!notification.validateForm(form)) {
                e.preventDefault();
            }
        });
    });

    // Auto-hide alerts after 5 seconds
    document.querySelectorAll('.alert').forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            alert.style.transition = 'opacity 0.5s ease';
            setTimeout(() => alert.remove(), 500);
        }, 5000);
    });
});

// ========================================
// CSS Animations
// ========================================

const style = document.createElement('style');
style.textContent = `
    @keyframes shake {
        0%, 100% { transform: translateX(0); }
        10%, 30%, 50%, 70%, 90% { transform: translateX(-10px); }
        20%, 40%, 60%, 80% { transform: translateX(10px); }
    }
    
    @keyframes pulse {
        0% { transform: scale(1); }
        50% { transform: scale(1.05); }
        100% { transform: scale(1); }
    }
    
    .swal2-popup-custom {
        border-radius: 12px !important;
        box-shadow: 0 10px 40px rgba(0, 0, 0, 0.1) !important;
    }
    
    .custom-modal {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        z-index: 1050;
    }
    
    .custom-modal-backdrop {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-color: rgba(0, 0, 0, 0.5);
    }
    
    .custom-modal-dialog {
        position: relative;
        width: auto;
        margin: 1.75rem;
        max-width: 500px;
        transform: translate(-50%, -50%);
        top: 50%;
        left: 50%;
    }
    
    .custom-modal-content {
        background-color: white;
        border-radius: 0.3rem;
        box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
    }
    
    .custom-modal-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 1rem;
        border-bottom: 1px solid #dee2e6;
    }
    
    .custom-modal-body {
        padding: 1rem;
    }
    
    .custom-modal-footer {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        padding: 1rem;
        border-top: 1px solid #dee2e6;
        gap: 0.5rem;
    }
    
    .custom-modal-close {
        background: none;
        border: none;
        font-size: 1.5rem;
        cursor: pointer;
    }
`;
document.head.appendChild(style);
