/**
 * DigiTekShop - API Helpers
 * Helper functions for AJAX requests with automatic notification handling
 */

class DigiTekApiHelper {
    constructor() {
        this.defaultOptions = {
            dataType: 'json',
            contentType: 'application/json',
            timeout: 30000
        };
        
        this.setupGlobalHandlers();
    }

    setupGlobalHandlers() {
        // Global AJAX setup
        $.ajaxSetup({
            beforeSend: function(xhr, settings) {
                // Add CSRF token if available
                const token = $('meta[name="csrf-token"]').attr('content');
                if (token) {
                    xhr.setRequestHeader('X-CSRF-TOKEN', token);
                }
            }
        });

        // Global AJAX error handler
        $(document).ajaxError(function(event, xhr, settings, thrownError) {
            if (xhr.status === 401) {
                DigiTekNotification.showSessionExpired();
            } else if (xhr.status === 403) {
                DigiTekNotification.showPermissionDenied();
            } else if (xhr.status === 404) {
                DigiTekNotification.showError('صفحه مورد نظر یافت نشد');
            } else if (xhr.status >= 500) {
                DigiTekNotification.showServerError();
            } else if (xhr.status === 0) {
                DigiTekNotification.showNetworkError();
            }
        });
    }

    // ========================================
    // GET Requests
    // ========================================

    get(url, options = {}) {
        return this.request('GET', url, null, options);
    }

    // ========================================
    // POST Requests
    // ========================================

    post(url, data = null, options = {}) {
        return this.request('POST', url, data, options);
    }

    // ========================================
    // PUT Requests
    // ========================================

    put(url, data = null, options = {}) {
        return this.request('PUT', url, data, options);
    }

    // ========================================
    // DELETE Requests
    // ========================================

    delete(url, options = {}) {
        return this.request('DELETE', url, null, options);
    }

    // ========================================
    // Generic Request Method
    // ========================================

    request(method, url, data = null, options = {}) {
        const config = {
            ...this.defaultOptions,
            method: method,
            url: url,
            data: data,
            ...options
        };

        // Show loading if not disabled
        if (!config.hideLoading) {
            DigiTekNotification.showLoading('در حال پردازش...');
        }

        return $.ajax(config)
            .done((response) => {
                if (!config.hideLoading) {
                    DigiTekNotification.hideLoading();
                }

                // Handle response notifications
                if (response.notification) {
                    this.handleNotification(response.notification);
                } else if (response.success !== undefined) {
                    if (response.success) {
                        DigiTekNotification.showSuccessToast(response.message || 'عملیات با موفقیت انجام شد');
                    } else {
                        DigiTekNotification.showErrorToast(response.message || 'خطا در انجام عملیات');
                    }
                }

                // Call success callback if provided
                if (config.success) {
                    config.success(response);
                }
            })
            .fail((xhr, status, error) => {
                if (!config.hideLoading) {
                    DigiTekNotification.hideLoading();
                }

                // Handle specific error responses
                if (xhr.responseJSON && xhr.responseJSON.notification) {
                    this.handleNotification(xhr.responseJSON.notification);
                } else if (xhr.responseJSON && xhr.responseJSON.message) {
                    DigiTekNotification.showErrorToast(xhr.responseJSON.message);
                }

                // Call error callback if provided
                if (config.error) {
                    config.error(xhr, status, error);
                }
            });
    }

    // ========================================
    // Form Submission Helpers
    // ========================================

    submitForm(formSelector, options = {}) {
        const form = $(formSelector);
        if (!form.length) {
            DigiTekNotification.showError('فرم مورد نظر یافت نشد');
            return Promise.reject();
        }

        const formData = new FormData(form[0]);
        const url = form.attr('action') || window.location.href;
        const method = form.attr('method') || 'POST';

        return this.request(method, url, formData, {
            processData: false,
            contentType: false,
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
            processData: false,
            contentType: false,
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
        return this.get('/Products', { data: filters });
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

    validateEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    validatePhone(phone) {
        return /^(\+98|0)?9\d{9}$/.test(phone.replace(/\s/g, ''));
    }

    validateRequired(value) {
        return value && value.toString().trim().length > 0;
    }

    // ========================================
    // Form Validation
    // ========================================

    validateForm(formSelector, rules = {}) {
        const form = $(formSelector);
        if (!form.length) return false;

        let isValid = true;
        const errors = [];

        // Check required fields
        form.find('[required]').each(function() {
            const field = $(this);
            if (!field.val() || field.val().trim() === '') {
                isValid = false;
                errors.push(`فیلد ${field.attr('name') || field.attr('id')} الزامی است`);
            }
        });

        // Check email fields
        form.find('input[type="email"]').each(function() {
            const field = $(this);
            if (field.val() && !DigiTekApiHelper.validateEmail(field.val())) {
                isValid = false;
                errors.push(`ایمیل ${field.attr('name') || field.attr('id')} معتبر نیست`);
            }
        });

        // Check phone fields
        form.find('input[type="tel"]').each(function() {
            const field = $(this);
            if (field.val() && !DigiTekApiHelper.validatePhone(field.val())) {
                isValid = false;
                errors.push(`شماره تلفن ${field.attr('name') || field.attr('id')} معتبر نیست`);
            }
        });

        // Custom validation rules
        Object.keys(rules).forEach(fieldName => {
            const field = form.find(`[name="${fieldName}"]`);
            const rule = rules[fieldName];
            
            if (field.length && rule.required && !field.val()) {
                isValid = false;
                errors.push(rule.message || `فیلد ${fieldName} الزامی است`);
            }
            
            if (field.length && field.val() && rule.pattern && !rule.pattern.test(field.val())) {
                isValid = false;
                errors.push(rule.message || `فیلد ${fieldName} معتبر نیست`);
            }
        });

        if (!isValid) {
            DigiTekNotification.showValidationError(errors);
        }

        return isValid;
    }

    // ========================================
    // Static Methods
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
}

// ========================================
// Global Instance
// ========================================

window.DigiTekApi = new DigiTekApiHelper();

// ========================================
// Global Helper Functions
// ========================================

// API Methods
window.apiGet = (url, options) => DigiTekApi.get(url, options);
window.apiPost = (url, data, options) => DigiTekApi.post(url, data, options);
window.apiPut = (url, data, options) => DigiTekApi.put(url, data, options);
window.apiDelete = (url, options) => DigiTekApi.delete(url, options);

// Form Methods
window.submitForm = (formSelector, options) => DigiTekApi.submitForm(formSelector, options);
window.validateForm = (formSelector, rules) => DigiTekApi.validateForm(formSelector, rules);

// Authentication Methods
window.login = (credentials) => DigiTekApi.login(credentials);
window.logout = () => DigiTekApi.logout();

// OTP Methods
window.sendOtp = (phone) => DigiTekApi.sendOtp(phone);
window.verifyOtp = (phone, code) => DigiTekApi.verifyOtp(phone, code);

// User Methods
window.getCurrentUser = () => DigiTekApi.getCurrentUser();
window.updateProfile = (data) => DigiTekApi.updateProfile(data);

// Product Methods
window.getProducts = (filters) => DigiTekApi.getProducts(filters);
window.getProduct = (id) => DigiTekApi.getProduct(id);
window.createProduct = (data) => DigiTekApi.createProduct(data);
window.updateProduct = (id, data) => DigiTekApi.updateProduct(id, data);
window.deleteProduct = (id) => DigiTekApi.deleteProduct(id);

// Order Methods
window.getOrders = (filters) => DigiTekApi.getOrders(filters);
window.getOrder = (id) => DigiTekApi.getOrder(id);
window.createOrder = (data) => DigiTekApi.createOrder(data);
window.updateOrder = (id, data) => DigiTekApi.updateOrder(id, data);
window.cancelOrder = (id) => DigiTekApi.cancelOrder(id);

// Validation Methods
window.validateEmail = (email) => DigiTekApiHelper.validateEmail(email);
window.validatePhone = (phone) => DigiTekApiHelper.validatePhone(phone);
window.validateRequired = (value) => DigiTekApiHelper.validateRequired(value);

// ========================================
// jQuery Integration
// ========================================

$(document).ready(function() {
    // Auto-submit forms with data-ajax attribute
    $('form[data-ajax]').on('submit', function(e) {
        e.preventDefault();
        const form = $(this);
        const options = form.data('ajax-options') || {};
        DigiTekApi.submitForm(this, options);
    });

    // Auto-validate forms with data-validate attribute
    $('form[data-validate]').on('submit', function(e) {
        if (!DigiTekApi.validateForm(this)) {
            e.preventDefault();
        }
    });

    // Auto-handle delete buttons
    $('[data-delete]').on('click', function(e) {
        e.preventDefault();
        const url = $(this).data('delete');
        const itemName = $(this).data('item-name') || 'این آیتم';
        
        DigiTekNotification.showDeleteConfirm(itemName).then((result) => {
            if (result.isConfirmed) {
                DigiTekApi.delete(url);
            }
        });
    });

    // Auto-handle confirm buttons
    $('[data-confirm]').on('click', function(e) {
        e.preventDefault();
        const message = $(this).data('confirm');
        const url = $(this).data('url');
        const method = $(this).data('method') || 'POST';
        
        DigiTekNotification.showConfirm('تأیید عملیات', message).then((result) => {
            if (result.isConfirmed) {
                DigiTekApi.request(method, url);
            }
        });
    });
});
