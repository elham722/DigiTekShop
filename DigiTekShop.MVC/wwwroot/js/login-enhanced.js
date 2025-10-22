/**
 * Enhanced Login Form JavaScript
 * Features: Form validation, password toggle, social login, responsive image handling
 */

class LoginForm {
    constructor() {
        this.form = document.getElementById('login-form');
        this.passwordInput = document.getElementById('passwordInput');
        this.togglePassword = document.getElementById('togglePassword');
        this.rightImage = document.getElementById('rightImage');
        
        this.init();
    }

    init() {
        this.setupPasswordToggle();
        this.setupFormValidation();
        this.setupImageHandling();
        this.setupSocialLogin();
        this.setupForgotPassword();
        this.setupFormSubmission();
        this.setupResponsiveHandlers();
    }

    /**
     * Password visibility toggle with smooth animations
     */
    setupPasswordToggle() {
        if (!this.togglePassword || !this.passwordInput) return;

        this.togglePassword.addEventListener('click', (e) => {
            e.preventDefault();
            
            if (this.passwordInput.type === 'password') {
                this.showPassword();
            } else {
                this.hidePassword();
            }
        });

        // Add keyboard support
        this.togglePassword.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                this.togglePassword.click();
            }
        });
    }

    showPassword() {
        this.passwordInput.type = 'text';
        this.togglePassword.classList.remove('closed');
        this.togglePassword.classList.add('open');
        this.togglePassword.setAttribute('aria-label', 'Hide password');
        
        // Add smooth transition
        this.passwordInput.style.transition = 'all 0.3s ease';
    }

    hidePassword() {
        this.passwordInput.type = 'password';
        this.togglePassword.classList.remove('open');
        this.togglePassword.classList.add('closed');
        this.togglePassword.setAttribute('aria-label', 'Show password');
    }

    /**
     * Enhanced form validation with real-time feedback
     */
    setupFormValidation() {
        if (!this.form) return;

        const inputs = this.form.querySelectorAll('input[required]');
        
        inputs.forEach(input => {
            // Real-time validation
            input.addEventListener('blur', () => this.validateField(input));
            input.addEventListener('input', () => this.clearFieldError(input));
            
            // Visual feedback on focus
            input.addEventListener('focus', () => {
                input.style.transform = 'translateY(-2px)';
            });
            
            input.addEventListener('blur', () => {
                if (!input.classList.contains('is-invalid')) {
                    input.style.transform = 'translateY(0)';
                }
            });
        });
    }

    validateField(field) {
        const value = field.value.trim();
        const fieldName = field.name;
        let isValid = true;
        let errorMessage = '';

        // Clear previous errors
        this.clearFieldError(field);

        // Email validation
        if (fieldName === 'EmailOrUsername') {
            if (!value) {
                errorMessage = 'ایمیل یا نام کاربری الزامی است';
                isValid = false;
            } else if (!this.isValidEmail(value) && !this.isValidUsername(value)) {
                errorMessage = 'لطفاً یک ایمیل یا نام کاربری معتبر وارد کنید';
                isValid = false;
            }
        }

        // Password validation
        if (fieldName === 'Password') {
            if (!value) {
                errorMessage = 'رمز عبور الزامی است';
                isValid = false;
            } else if (value.length < 6) {
                errorMessage = 'رمز عبور باید حداقل ۶ کاراکتر باشد';
                isValid = false;
            }
        }

        // Apply validation result
        if (isValid) {
            field.classList.remove('is-invalid');
            field.classList.add('is-valid');
        } else {
            field.classList.remove('is-valid');
            field.classList.add('is-invalid');
            this.showFieldError(field, errorMessage);
        }

        return isValid;
    }

    isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    isValidUsername(username) {
        const usernameRegex = /^[a-zA-Z0-9_]{3,20}$/;
        return usernameRegex.test(username);
    }

    showFieldError(field, message) {
        // Remove existing error
        const existingError = field.parentNode.querySelector('.field-error');
        if (existingError) {
            existingError.remove();
        }

        // Create error element
        const errorElement = document.createElement('div');
        errorElement.className = 'field-error';
        errorElement.textContent = message;
        errorElement.style.display = 'block';
        
        // Insert after the field
        field.parentNode.insertBefore(errorElement, field.nextSibling);
    }

    clearFieldError(field) {
        field.classList.remove('is-invalid');
        const errorElement = field.parentNode.querySelector('.field-error');
        if (errorElement) {
            errorElement.remove();
        }
    }

    /**
     * Responsive image handling
     */
    setupImageHandling() {
        if (!this.rightImage) return;

        const fitImage = () => {
            const viewportHeight = window.innerHeight;
            const maxHeight = Math.min(viewportHeight * 0.7, 600);
            this.rightImage.style.maxHeight = maxHeight + 'px';
        };

        // Initial fit
        fitImage();
        
        // Fit on resize with debouncing
        let resizeTimeout;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(fitImage, 100);
        });

        // Fit on load
        window.addEventListener('load', fitImage);
    }

    /**
     * Social login functionality
     */
    setupSocialLogin() {
        const socialButtons = document.querySelectorAll('.social-btn');
        
        socialButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                const provider = button.getAttribute('onclick')?.match(/'([^']+)'/)?.[1] || 
                               button.classList.contains('google') ? 'google' :
                               button.classList.contains('github') ? 'github' :
                               button.classList.contains('facebook') ? 'facebook' : 'unknown';
                
                this.handleSocialLogin(provider);
            });
        });
    }

    handleSocialLogin(provider) {
        // Add loading state
        const button = document.querySelector(`.social-btn.${provider}`);
        if (button) {
            const originalContent = button.innerHTML;
            button.innerHTML = '<span>در حال اتصال...</span>';
            button.disabled = true;
            
            // Simulate API call
            setTimeout(() => {
                button.innerHTML = originalContent;
                button.disabled = false;
                this.showNotification(`ورود با ${provider} به زودی فعال خواهد شد`, 'info');
            }, 2000);
        }
    }

    /**
     * Forgot password functionality
     */
    setupForgotPassword() {
        const forgotLink = document.querySelector('.forgot-link');
        if (forgotLink) {
            forgotLink.addEventListener('click', (e) => {
                e.preventDefault();
                this.showForgotPasswordModal();
            });
        }
    }

    showForgotPasswordModal() {
        this.showNotification('قابلیت بازیابی رمز عبور به زودی فعال خواهد شد', 'info');
    }

    /**
     * Form submission with enhanced UX
     */
    setupFormSubmission() {
        if (!this.form) return;

        this.form.addEventListener('submit', (e) => {
            e.preventDefault();
            this.handleFormSubmission();
        });
    }

    async handleFormSubmission() {
        const submitButton = this.form.querySelector('.btn-primary');
        const formData = new FormData(this.form);
        
        // Validate all fields
        const inputs = this.form.querySelectorAll('input[required]');
        let isFormValid = true;
        
        inputs.forEach(input => {
            if (!this.validateField(input)) {
                isFormValid = false;
            }
        });

        if (!isFormValid) {
            this.showNotification('لطفاً تمام فیلدهای الزامی را به درستی پر کنید', 'error');
            return;
        }

        // Add loading state
        this.setButtonLoading(submitButton, true);

        try {
            // Simulate API call
            await this.simulateApiCall(formData);
            
            // Success handling
            this.showNotification('ورود با موفقیت انجام شد', 'success');
            
            // Redirect after delay
            setTimeout(() => {
                window.location.href = '/Dashboard';
            }, 1500);
            
        } catch (error) {
            this.showNotification('خطا در ورود. لطفاً دوباره تلاش کنید', 'error');
        } finally {
            this.setButtonLoading(submitButton, false);
        }
    }

    setButtonLoading(button, isLoading) {
        if (isLoading) {
            button.classList.add('loading');
            button.disabled = true;
            button.setAttribute('data-original-text', button.textContent);
            button.textContent = 'در حال ورود...';
        } else {
            button.classList.remove('loading');
            button.disabled = false;
            button.textContent = button.getAttribute('data-original-text') || 'ورود';
        }
    }

    async simulateApiCall(formData) {
        return new Promise((resolve, reject) => {
            setTimeout(() => {
                // Simulate random success/failure for demo
                if (Math.random() > 0.3) {
                    resolve({ success: true });
                } else {
                    reject(new Error('Authentication failed'));
                }
            }, 2000);
        });
    }

    /**
     * Responsive handlers
     */
    setupResponsiveHandlers() {
        // Handle mobile menu if needed
        this.handleMobileLayout();
        
        // Handle orientation changes
        window.addEventListener('orientationchange', () => {
            setTimeout(() => {
                this.handleMobileLayout();
            }, 100);
        });
    }

    handleMobileLayout() {
        const isMobile = window.innerWidth <= 768;
        
        if (isMobile) {
            // Mobile-specific adjustments
            document.body.classList.add('mobile-layout');
        } else {
            document.body.classList.remove('mobile-layout');
        }
    }

    /**
     * Notification system
     */
    showNotification(message, type = 'info') {
        // Remove existing notifications
        const existingNotifications = document.querySelectorAll('.notification');
        existingNotifications.forEach(notification => notification.remove());

        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="fas fa-${this.getNotificationIcon(type)}"></i>
                <span>${message}</span>
                <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
            </div>
        `;

        // Add styles
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${this.getNotificationColor(type)};
            color: white;
            padding: 16px 20px;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            z-index: 10000;
            max-width: 400px;
            animation: slideInRight 0.3s ease;
        `;

        // Add to page
        document.body.appendChild(notification);

        // Auto remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.style.animation = 'slideOutRight 0.3s ease';
                setTimeout(() => notification.remove(), 300);
            }
        }, 5000);
    }

    getNotificationIcon(type) {
        const icons = {
            success: 'check-circle',
            error: 'exclamation-circle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };
        return icons[type] || 'info-circle';
    }

    getNotificationColor(type) {
        const colors = {
            success: 'linear-gradient(135deg, #28a745, #20c997)',
            error: 'linear-gradient(135deg, #dc3545, #e74c3c)',
            warning: 'linear-gradient(135deg, #ffc107, #f39c12)',
            info: 'linear-gradient(135deg, #17a2b8, #3498db)'
        };
        return colors[type] || colors.info;
    }
}

// CSS for notifications
const notificationStyles = `
    @keyframes slideInRight {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
    
    @keyframes slideOutRight {
        from { transform: translateX(0); opacity: 1; }
        to { transform: translateX(100%); opacity: 0; }
    }
    
    .notification-content {
        display: flex;
        align-items: center;
        gap: 12px;
    }
    
    .notification-close {
        background: none;
        border: none;
        color: white;
        font-size: 18px;
        cursor: pointer;
        padding: 4px;
        border-radius: 50%;
        transition: background 0.2s ease;
    }
    
    .notification-close:hover {
        background: rgba(255,255,255,0.2);
    }
`;

// Add notification styles to head
const styleSheet = document.createElement('style');
styleSheet.textContent = notificationStyles;
document.head.appendChild(styleSheet);

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new LoginForm();
});

