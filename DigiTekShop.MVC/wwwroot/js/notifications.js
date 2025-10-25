/**
 * DigiTekShop - Modern Notification System
 * Advanced notification system with SweetAlert2 and custom toast notifications
 */

// ========================================
// Global Notification System
// ========================================

class DigiTekNotification {
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
            container.className = 'toast-container';
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
        // Global AJAX error handler
        $(document).ajaxError(function(event, xhr, settings, thrownError) {
            if (xhr.status === 401) {
                DigiTekNotification.showError('لطفاً ابتدا وارد شوید');
            } else if (xhr.status === 403) {
                DigiTekNotification.showError('شما دسترسی لازم را ندارید');
            } else if (xhr.status === 404) {
                DigiTekNotification.showError('صفحه مورد نظر یافت نشد');
            } else if (xhr.status >= 500) {
                DigiTekNotification.showError('خطا در سرور. لطفاً بعداً تلاش کنید');
            } else if (xhr.status === 0) {
                DigiTekNotification.showError('خطا در ارتباط با سرور');
            }
        });
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

        return Swal.fire(config);
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

        return Swal.fire(config);
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

        return Swal.fire(config);
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

        return Swal.fire(config);
    }

    showInfoToast(message, duration = 3000) {
        this.showToast('info', 'اطلاعات', message, duration);
    }

    // ========================================
    // Loading Notifications
    // ========================================

    showLoading(title = 'در حال پردازش...', text = 'لطفاً صبر کنید') {
        return Swal.fire({
            title: title,
            text: text,
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });
    }

    hideLoading() {
        Swal.close();
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

        return Swal.fire(config);
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

        return Swal.fire(config);
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
        const toast = new bootstrap.Toast(toastElement, {
            autohide: true,
            delay: duration
        });

        toast.show();

        // Remove element after hide
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });

        return toast;
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
        return Swal.fire({
            title: title,
            text: text,
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });
    }

    updateProgress(percent, text = '') {
        Swal.update({
            text: text,
            html: `
                <div class="progress" style="width: 100%; margin-top: 20px;">
                    <div class="progress-bar" role="progressbar" style="width: ${percent}%" aria-valuenow="${percent}" aria-valuemin="0" aria-valuemax="100">
                        ${percent}%
                    </div>
                </div>
            `
        });
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
    // AJAX Helpers
    // ========================================

    ajaxRequest(url, options = {}) {
        const defaultOptions = {
            method: 'POST',
            dataType: 'json',
            beforeSend: () => {
                this.showLoading('در حال پردازش...');
            },
            success: (response) => {
                this.hideLoading();
                if (response.success) {
                    this.showSuccess('موفقیت!', response.message);
                } else {
                    this.showError('خطا!', response.message);
                }
            },
            error: (xhr) => {
                this.hideLoading();
                this.showError('خطا!', 'خطا در ارتباط با سرور');
            }
        };

        return $.ajax(url, { ...defaultOptions, ...options });
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
        return Swal.fire({
            icon: 'success',
            title: title,
            text: text,
            showConfirmButton: false,
            timer: 2000,
            timerProgressBar: true,
            didOpen: () => {
                // Add custom animation
                const popup = Swal.getPopup();
                popup.style.transform = 'scale(0.8)';
                popup.style.transition = 'transform 0.3s ease';
                setTimeout(() => {
                    popup.style.transform = 'scale(1)';
                }, 100);
            }
        });
    }

    showAnimatedError(title, text = '') {
        return Swal.fire({
            icon: 'error',
            title: title,
            text: text,
            showConfirmButton: true,
            confirmButtonText: 'باشه',
            didOpen: () => {
                // Add shake animation
                const popup = Swal.getPopup();
                popup.style.animation = 'shake 0.5s ease-in-out';
            }
        });
    }
}

// ========================================
// Global Instance
// ========================================

window.DigiTekNotification = new DigiTekNotification();

// ========================================
// Global Helper Functions
// ========================================

// Success notifications
window.showSuccess = (title, text) => DigiTekNotification.showSuccess(title, text);
window.showSuccessToast = (message) => DigiTekNotification.showSuccessToast(message);

// Error notifications
window.showError = (title, text) => DigiTekNotification.showError(title, text);
window.showErrorToast = (message) => DigiTekNotification.showErrorToast(message);

// Warning notifications
window.showWarning = (title, text) => DigiTekNotification.showWarning(title, text);
window.showWarningToast = (message) => DigiTekNotification.showWarningToast(message);

// Info notifications
window.showInfo = (title, text) => DigiTekNotification.showInfo(title, text);
window.showInfoToast = (message) => DigiTekNotification.showInfoToast(message);

// Loading notifications
window.showLoading = (title, text) => DigiTekNotification.showLoading(title, text);
window.hideLoading = () => DigiTekNotification.hideLoading();

// Confirmation dialogs
window.showConfirm = (title, text) => DigiTekNotification.showConfirm(title, text);
window.showDeleteConfirm = (itemName) => DigiTekNotification.showDeleteConfirm(itemName);

// Input dialogs
window.showInput = (title, inputType) => DigiTekNotification.showInput(title, inputType);
window.showPasswordInput = (title) => DigiTekNotification.showPasswordInput(title);
window.showEmailInput = (title) => DigiTekNotification.showEmailInput(title);

// Utility functions
window.showNetworkError = () => DigiTekNotification.showNetworkError();
window.showServerError = () => DigiTekNotification.showServerError();
window.showValidationError = (errors) => DigiTekNotification.showValidationError(errors);
window.showSessionExpired = () => DigiTekNotification.showSessionExpired();
window.showPermissionDenied = () => DigiTekNotification.showPermissionDenied();

// ========================================
// jQuery Integration
// ========================================

$(document).ready(function() {
    // Auto-show TempData messages
    if (typeof TempData !== 'undefined') {
        if (TempData.SuccessMessage) {
            showSuccess('موفقیت!', TempData.SuccessMessage);
        }
        if (TempData.ErrorMessage) {
            showError('خطا!', TempData.ErrorMessage);
        }
        if (TempData.WarningMessage) {
            showWarning('هشدار!', TempData.WarningMessage);
        }
        if (TempData.InfoMessage) {
            showInfo('اطلاعات', TempData.InfoMessage);
        }
    }

    // Form validation
    $('form[data-validate]').on('submit', function(e) {
        if (!DigiTekNotification.validateForm(this)) {
            e.preventDefault();
        }
    });

    // Auto-hide alerts after 5 seconds
    $('.alert').each(function() {
        const alert = $(this);
        setTimeout(() => {
            alert.fadeOut();
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
`;
document.head.appendChild(style);
