/**
 * DigiTekShop - Modern API Client
 * Vanilla JavaScript replacement for jQuery-based api-helpers.js
 */

export class Api {
    constructor() {
        this.defaultOptions = {
            timeout: 30000,
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        };
        
        this.setupGlobalHandlers();
    }

    /**
     * Setup global error handlers and CSRF token management
     */
    setupGlobalHandlers() {
        // Global fetch error handler
        const originalFetch = window.fetch;
        window.fetch = async (...args) => {
            try {
                const response = await originalFetch(...args);
                
                // Handle common HTTP errors globally
                if (response.status === 401) {
                    DigiTekNotification?.showSessionExpired?.();
                } else if (response.status === 403) {
                    DigiTekNotification?.showPermissionDenied?.();
                } else if (response.status === 404) {
                    DigiTekNotification?.showError?.('صفحه مورد نظر یافت نشد');
                } else if (response.status >= 500) {
                    DigiTekNotification?.showServerError?.();
                } else if (response.status === 0) {
                    DigiTekNotification?.showNetworkError?.();
                }
                
                return response;
            } catch (error) {
                console.error('Fetch error:', error);
                DigiTekNotification?.showNetworkError?.();
                throw error;
            }
        };
    }

    /**
     * Get CSRF token from meta tag
     */
    static _getCsrfToken() {
        const meta = document.querySelector('meta[name="request-verification-token"]');
        return meta?.content ?? '';
    }

    /**
     * Generic request method
     */
    async request(method, url, data = null, options = {}) {
        const config = {
            ...this.defaultOptions,
            method: method,
            ...options
        };

        // Prepare headers
        const headers = {
            ...config.headers,
            'RequestVerificationToken': Api._getCsrfToken()
        };

        // Prepare body
        let body = null;
        if (data !== null) {
            if (data instanceof FormData) {
                body = data;
                // Remove Content-Type for FormData (browser will set it with boundary)
                delete headers['Content-Type'];
            } else {
                body = JSON.stringify(data);
            }
        }

        // Show loading if not disabled
        if (!options.hideLoading) {
            DigiTekNotification?.showLoading?.('در حال پردازش...');
        }

        try {
            const response = await fetch(url, {
                method,
                headers,
                body,
                credentials: 'same-origin',
                signal: options.signal || AbortSignal.timeout(config.timeout)
            });

            let responseData = null;
            try {
                responseData = await response.json();
            } catch (e) {
                // Response might not be JSON
                responseData = await response.text();
            }

            if (!options.hideLoading) {
                DigiTekNotification?.hideLoading?.();
            }

            // Handle response notifications
            if (responseData?.notification) {
                this.handleNotification(responseData.notification);
            } else if (responseData?.success !== undefined) {
                if (responseData.success) {
                    DigiTekNotification?.showSuccessToast?.(responseData.message || 'عملیات با موفقیت انجام شد');
                } else {
                    DigiTekNotification?.showErrorToast?.(responseData.message || 'خطا در انجام عملیات');
                }
            }

            // Call success callback if provided
            if (response.ok && options.onSuccess) {
                options.onSuccess(responseData);
            }

            // Call error callback if provided
            if (!response.ok && options.onError) {
                options.onError(responseData);
            }

            return {
                ok: response.ok,
                status: response.status,
                data: responseData
            };

        } catch (error) {
            if (!options.hideLoading) {
                DigiTekNotification?.hideLoading?.();
            }

            if (options.onError) {
                options.onError(error);
            }

            throw error;
        }
    }

    // ========================================
    // HTTP Methods
    // ========================================

    get(url, options = {}) {
        return this.request('GET', url, null, options);
    }

    post(url, data = null, options = {}) {
        return this.request('POST', url, data, options);
    }

    put(url, data = null, options = {}) {
        return this.request('PUT', url, data, options);
    }

    delete(url, options = {}) {
        return this.request('DELETE', url, null, options);
    }

    // ========================================
    // Form Submission Helpers
    // ========================================

    submitForm(formSelector, options = {}) {
        const form = document.querySelector(formSelector);
        if (!form) {
            DigiTekNotification?.showError?.('فرم مورد نظر یافت نشد');
            return Promise.reject(new Error('Form not found'));
        }

        const formData = new FormData(form);
        const url = form.action || window.location.href;
        const method = form.method || 'POST';

        return this.request(method, url, formData, {
            ...options
        });
    }

    // ========================================
    // File Upload Helpers
    // ========================================

    uploadFile(url, file, options = {}) {
        const formData = new FormData();
        formData.append('file', file);

        return this.request('POST', url, formData, {
            ...options
        });
    }

    // ========================================
    // Authentication Helpers
    // ========================================

    login(credentials) {
        return this.post('/Auth/Login', credentials);
    }

    logout() {
        return this.post('/Auth/Logout');
    }

    // ========================================
    // OTP Helpers
    // ========================================

    sendOtp(phone) {
        return this.post('/Auth/SendOtp', { phone: phone });
    }

    verifyOtp(phone, code) {
        return this.post('/Auth/VerifyOtp', { phone: phone, code: code });
    }

    // ========================================
    // User Management Helpers
    // ========================================

    getCurrentUser() {
        return this.get('/Auth/Me');
    }

    updateProfile(data) {
        return this.put('/User/Profile', data);
    }

    // ========================================
    // Product Management Helpers
    // ========================================

    getProducts(filters = {}) {
        return this.get('/Products', { 
            data: filters,
            onSuccess: (data) => {
                // Handle product list response
                console.log('Products loaded:', data);
            }
        });
    }

    getProduct(id) {
        return this.get(`/Products/${id}`);
    }

    createProduct(data) {
        return this.post('/Products', data);
    }

    updateProduct(id, data) {
        return this.put(`/Products/${id}`, data);
    }

    deleteProduct(id) {
        return this.delete(`/Products/${id}`);
    }

    // ========================================
    // Order Management Helpers
    // ========================================

    getOrders(filters = {}) {
        return this.get('/Orders', { data: filters });
    }

    getOrder(id) {
        return this.get(`/Orders/${id}`);
    }

    createOrder(data) {
        return this.post('/Orders', data);
    }

    updateOrder(id, data) {
        return this.put(`/Orders/${id}`, data);
    }

    cancelOrder(id) {
        return this.put(`/Orders/${id}/Cancel`);
    }

    // ========================================
    // Utility Methods
    // ========================================

    handleNotification(notification) {
        if (!DigiTekNotification) return;

        switch (notification.type) {
            case 'success':
                DigiTekNotification.showSuccess(notification.title, notification.message);
                break;
            case 'error':
                DigiTekNotification.showError(notification.title, notification.message);
                break;
            case 'warning':
                DigiTekNotification.showWarning(notification.title, notification.message);
                break;
            case 'info':
                DigiTekNotification.showInfo(notification.title, notification.message);
                break;
            default:
                DigiTekNotification.showInfoToast(notification.message);
        }
    }

    // ========================================
    // Validation Helpers
    // ========================================

    static validateEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    static validatePhone(phone) {
        return /^(\+98|0)?9\d{9}$/.test(phone.replace(/\s/g, ''));
    }

    static validateRequired(value) {
        return value && value.toString().trim().length > 0;
    }

    // ========================================
    // Form Validation
    // ========================================

    validateForm(formSelector, rules = {}) {
        const form = document.querySelector(formSelector);
        if (!form) return false;

        let isValid = true;
        const errors = [];

        // Check required fields
        const requiredFields = form.querySelectorAll('[required]');
        requiredFields.forEach(field => {
            if (!field.value || field.value.trim() === '') {
                isValid = false;
                errors.push(`فیلد ${field.name || field.id} الزامی است`);
            }
        });

        // Check email fields
        const emailFields = form.querySelectorAll('input[type="email"]');
        emailFields.forEach(field => {
            if (field.value && !Api.validateEmail(field.value)) {
                isValid = false;
                errors.push(`ایمیل ${field.name || field.id} معتبر نیست`);
            }
        });

        // Check phone fields
        const phoneFields = form.querySelectorAll('input[type="tel"]');
        phoneFields.forEach(field => {
            if (field.value && !Api.validatePhone(field.value)) {
                isValid = false;
                errors.push(`شماره تلفن ${field.name || field.id} معتبر نیست`);
            }
        });

        // Custom validation rules
        Object.keys(rules).forEach(fieldName => {
            const field = form.querySelector(`[name="${fieldName}"]`);
            const rule = rules[fieldName];
            
            if (field && rule.required && !field.value) {
                isValid = false;
                errors.push(rule.message || `فیلد ${fieldName} الزامی است`);
            }
            
            if (field && field.value && rule.pattern && !rule.pattern.test(field.value)) {
                isValid = false;
                errors.push(rule.message || `فیلد ${fieldName} معتبر نیست`);
            }
        });

        if (!isValid) {
            DigiTekNotification?.showValidationError?.(errors);
        }

        return isValid;
    }
}

// ========================================
// Global Helper Functions (for backward compatibility)
// ========================================

// API Methods
window.apiGet = (url, options) => new Api().get(url, options);
window.apiPost = (url, data, options) => new Api().post(url, data, options);
window.apiPut = (url, data, options) => new Api().put(url, data, options);
window.apiDelete = (url, options) => new Api().delete(url, options);

// Form Methods
window.submitForm = (formSelector, options) => new Api().submitForm(formSelector, options);
window.validateForm = (formSelector, rules) => new Api().validateForm(formSelector, rules);

// Authentication Methods
window.login = (credentials) => new Api().login(credentials);
window.logout = () => new Api().logout();

// OTP Methods
window.sendOtp = (phone) => new Api().sendOtp(phone);
window.verifyOtp = (phone, code) => new Api().verifyOtp(phone, code);

// User Methods
window.getCurrentUser = () => new Api().getCurrentUser();
window.updateProfile = (data) => new Api().updateProfile(data);

// Product Methods
window.getProducts = (filters) => new Api().getProducts(filters);
window.getProduct = (id) => new Api().getProduct(id);
window.createProduct = (data) => new Api().createProduct(data);
window.updateProduct = (id, data) => new Api().updateProduct(id, data);
window.deleteProduct = (id) => new Api().deleteProduct(id);

// Order Methods
window.getOrders = (filters) => new Api().getOrders(filters);
window.getOrder = (id) => new Api().getOrder(id);
window.createOrder = (data) => new Api().createOrder(data);
window.updateOrder = (id, data) => new Api().updateOrder(id, data);
window.cancelOrder = (id) => new Api().cancelOrder(id);

// Validation Methods
window.validateEmail = (email) => Api.validateEmail(email);
window.validatePhone = (phone) => Api.validatePhone(phone);
window.validateRequired = (value) => Api.validateRequired(value);

// ========================================
// Auto-initialization for forms
// ========================================

document.addEventListener('DOMContentLoaded', () => {
    const api = new Api();
    
    // Auto-submit forms with data-ajax attribute
    document.querySelectorAll('form[data-ajax]').forEach(form => {
        form.addEventListener('submit', (e) => {
            e.preventDefault();
            const options = JSON.parse(form.dataset.ajaxOptions || '{}');
            api.submitForm(form, options);
        });
    });

    // Auto-validate forms with data-validate attribute
    document.querySelectorAll('form[data-validate]').forEach(form => {
        form.addEventListener('submit', (e) => {
            if (!api.validateForm(form)) {
                e.preventDefault();
            }
        });
    });

    // Auto-handle delete buttons
    document.querySelectorAll('[data-delete]').forEach(button => {
        button.addEventListener('click', (e) => {
            e.preventDefault();
            const url = button.dataset.delete;
            const itemName = button.dataset.itemName || 'این آیتم';
            
            DigiTekNotification?.showDeleteConfirm?.(itemName).then((result) => {
                if (result.isConfirmed) {
                    api.delete(url);
                }
            });
        });
    });

    // Auto-handle confirm buttons
    document.querySelectorAll('[data-confirm]').forEach(button => {
        button.addEventListener('click', (e) => {
            e.preventDefault();
            const message = button.dataset.confirm;
            const url = button.dataset.url;
            const method = button.dataset.method || 'POST';
            
            DigiTekNotification?.showConfirm?.('تأیید عملیات', message).then((result) => {
                if (result.isConfirmed) {
                    api.request(method, url);
                }
            });
        });
    });

    // Auto-handle logout links
    document.querySelectorAll('[data-logout]').forEach(link => {
        link.addEventListener('click', async (e) => {
            e.preventDefault();
            
            // نمایش پیام تأیید - استفاده از global function یا instance
            const showConfirmFn = window.showConfirm || 
                                 (window.DigiTekNotification && window.DigiTekNotification.showConfirm) ||
                                 ((title, text) => {
                                     const notification = new window.DigiTekNotification();
                                     return notification.showConfirm(title, text);
                                 });
            
            const confirmed = await showConfirmFn('خروج از حساب کاربری', 'آیا مطمئن هستید که می‌خواهید خارج شوید؟');
            if (!confirmed?.isConfirmed) {
                return;
            }

            try {
                // فراخوانی API logout
                const result = await api.post('/Auth/Logout', null, {
                    hideLoading: false,
                    onSuccess: () => {
                        // بعد از logout موفق، redirect به صفحه اصلی
                        window.location.href = '/';
                    },
                    onError: (error) => {
                        // حتی اگر logout در API fail شد، باز هم redirect کن
                        // چون ممکن است session در سمت کلاینت پاک شده باشد
                        console.warn('Logout API error:', error);
                        window.location.href = '/';
                    }
                });

                // اگر response موفق بود اما callback اجرا نشد، redirect کن
                if (result.ok || result.status === 204) {
                    window.location.href = '/';
                }
            } catch (error) {
                // در صورت خطا، باز هم redirect کن
                console.error('Logout error:', error);
                window.location.href = '/';
            }
        });
    });
});
