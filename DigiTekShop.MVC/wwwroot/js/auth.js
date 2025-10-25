/**
 * DigiTekShop Auth (OTP) – trimmed for server-managed tokens
 * هماهنگ با: /Auth/SendOtp , /Auth/VerifyOtp  (MVC endpoints)
 */

const API_SEND = '/Auth/SendOtp';
const API_VERIFY = '/Auth/VerifyOtp';
const REDIRECT_AFTER_LOGIN = '/';

// ارقام فارسی/عربی را به لاتین تبدیل می‌کند و کاراکترهای نامرئی را حذف می‌کند
function normalizeDigits(s) {
    if (!s) return '';
    const map = {
        '۰': '0', '۱': '1', '۲': '2', '۳': '3', '۴': '4', '۵': '5', '۶': '6', '۷': '7', '۸': '8', '۹': '9',
        '٠': '0', '١': '1', '٢': '2', '٣': '3', '٤': '4', '٥': '5', '٦': '6', '٧': '7', '٨': '8', '٩': '9',
    };
    return ('' + s)
        .replace(/[\u200c\u200e\u200f\u202a\u202b\u202c]/g, '') // حذف RTL/LRM/ZWNJ
        .split('')
        .map(ch => map[ch] ?? ch)
        .join('');
}

function csrf() {
    const m = document.querySelector('meta[name="request-verification-token"]');
    return m ? m.content : '';
}

// ورودی کاربر: 09xxxxxxxxx → +989xxxxxxxxx
function normalizeIranPhoneE164(input) {
    if (!input) return null;
    let s = normalizeDigits(input).replace(/[^\d+]/g, '');
    if (s.startsWith('0098')) s = s.replace(/^0098/, '+98');
    if (s.startsWith('09')) s = '+98' + s.substring(1);
    else if (s.startsWith('9') && s.length === 10) s = '+98' + s;
    else if (s.startsWith('98')) s = '+' + s;
    if (!/^\+98\d{10}$/.test(s)) return null;
    return s;
}

class AuthManager {
    constructor() {
        this.currentStep = 'phone';
        this.rawPhone = '';
        this.normalizedPhone = '';
        this.timerId = null;
        this.count = 60;
        this.inFlight = { send: false, verify: false };

        this.cacheEls();
        this.bindEvents();
        this.setupOtpInputs();
        this.phoneInput?.focus();
    }

    cacheEls() {
        this.phoneForm = document.getElementById('phoneForm');
        this.otpForm = document.getElementById('otpForm');
        this.phoneInput = document.getElementById('phoneNumber');
        this.displayPhone = document.getElementById('displayPhone');
        this.resendBtn = document.getElementById('resendBtn');
        this.countdownText = document.getElementById('countdownText');
    }

    bindEvents() {
        this.phoneForm.addEventListener('submit', (e) => { e.preventDefault(); this.sendOtp(); });
        this.otpForm.addEventListener('submit', (e) => { e.preventDefault(); this.verifyOtp(); });
        this.resendBtn.addEventListener('click', () => this.resend());

        // ورودی شماره فقط عدد و حداکثر 11 رقم (09xxxxxxxxx) + نرمال‌سازی ارقام
        this.phoneInput.addEventListener('input', (e) => {
            const raw = normalizeDigits(e.target.value);
            e.target.value = raw.replace(/[^\d]/g, '').slice(0, 11);
            // حذف کلاس خطا اگر شماره معتبر شد
            if (/^09\d{9}$/.test(e.target.value)) {
                e.target.classList.remove('is-invalid');
            }
        });

        // جلوگیری از submit ناخواسته با Enter روی فیلد شماره
        this.phoneInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') { e.preventDefault(); this.sendOtp(); }
        });

        this.phoneInput.addEventListener('blur', () => this.validatePhone(true));

        // Enter روی هر کادر OTP، به‌جای submit فرم، verifyOtp را صدا بزن
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && e.target.classList.contains('otp-input')) {
                e.preventDefault();
                this.verifyOtp();
            }
        });
    }

    setupOtpInputs() {
        this.otpInputs = Array.from(document.querySelectorAll('.otp-input'));
        this.otpContainer = document.querySelector('.otp-input-container');
        
        // اطمینان از اینکه همه فیلدها خالی هستند
        this.otpInputs.forEach(input => input.value = '');

        // اگر کاربر روی کانتینر کلیک کرد، روی اولین خانه‌ی خالی فوکوس کن
        if (this.otpContainer) {
            this.otpContainer.addEventListener('click', (e) => {
                // اگر روی خود input کلیک شده، کاری نکن
                if (e.target.classList.contains('otp-input')) return;
                
                const firstEmpty = this.otpInputs.find(i => !i.value);
                if (firstEmpty) {
                    firstEmpty.focus();
                } else {
                    // اگر همه فیلدها پر هستند، روی آخرین فیلد فوکوس کن
                    this.otpInputs[this.otpInputs.length - 1].focus();
                }
            });
        }

        this.otpInputs.forEach((el, idx) => {
            el.addEventListener('input', (e) => {
                const raw = normalizeDigits(e.target.value);
                
                if (raw.length > 1) {
                    // پاک کردن همه فیلدها از موقعیت فعلی به بعد
                    for (let i = idx; i < this.otpInputs.length; i++) {
                        this.otpInputs[i].value = '';
                    }
                    // پر کردن از موقعیت فعلی (چپ به راست)
                    for (let i = 0; i < raw.length && (idx + i) < this.otpInputs.length; i++) {
                        this.otpInputs[idx + i].value = raw[i];
                    }
                    // فوکوس روی اولین فیلد خالی
                    const firstEmpty = this.otpInputs.find(i => !i.value);
                    if (firstEmpty) {
                        firstEmpty.focus();
                    } else {
                        this.otpInputs[this.otpInputs.length - 1].focus();
                    }
                } else {
                    e.target.value = raw.replace(/[^\d]/g, '').slice(0, 1);
                    if (e.target.value && idx < this.otpInputs.length - 1) {
                        this.otpInputs[idx + 1].focus();
                    }
                }
                this.updateHiddenOtp();
            });

            el.addEventListener('keydown', (e) => {
                if (e.key === 'Backspace') {
                    if (!e.target.value && idx > 0) {
                        this.otpInputs[idx - 1].focus();
                    } else if (e.target.value) {
                        e.target.value = '';
                        this.updateHiddenOtp();
                    }
                }
            });

            el.addEventListener('paste', (e) => {
                e.preventDefault();
                const s = normalizeDigits(e.clipboardData.getData('text') || '').replace(/[^\d]/g, '').slice(0, 6);
                // پاک کردن همه فیلدها
                this.otpInputs.forEach(input => input.value = '');
                // پر کردن از اول (چپ به راست)
                for (let i = 0; i < s.length && i < this.otpInputs.length; i++) {
                    this.otpInputs[i].value = s[i];
                }
                this.updateHiddenOtp();
                // فوکوس روی اولین فیلد خالی یا آخرین فیلد پر شده
                const firstEmpty = this.otpInputs.find(input => !input.value);
                if (firstEmpty) {
                    firstEmpty.focus();
                } else {
                    this.otpInputs[this.otpInputs.length - 1].focus();
                }
            });
        });
    }



    updateHiddenOtp() {
        const code = this.otpInputs.map(i => i.value || '').join('');
        const hidden = document.getElementById('otpCode');
        if (hidden) hidden.value = code;
    }

    validatePhone(showToast = false) {
        const ok = /^09\d{9}$/.test(this.phoneInput.value);
        this.phoneInput.classList.toggle('is-invalid', !ok && this.phoneInput.value.length > 0);
        if (!ok && showToast) this.toast('شماره موبایل معتبر نیست', 'warning');
        return ok;
    }

    setLoading(formId, on) {
        const form = document.getElementById(formId);
        const btn = form.querySelector('.auth-btn');
        form.classList.toggle('loading', on);
        btn.disabled = on;
        btn.querySelector('.btn-text').classList.toggle('d-none', on);
        btn.querySelector('.btn-spinner').classList.toggle('d-none', !on);
    }

    showLoading(title, text) {
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

    showStep(name) {
        document.querySelectorAll('.auth-step').forEach(s => s.classList.remove('active'));
        document.getElementById(name + 'Step').classList.add('active');
        this.currentStep = name;
        if (name === 'otp') this.otpInputs[0].focus();
    }

    startTimer(sec = 60) {
        this.count = sec;
        this.resendBtn.disabled = true;
        this.tick();
        if (this.timerId) clearInterval(this.timerId);
        this.timerId = setInterval(() => {
            this.count--;
            this.tick();
            if (this.count <= 0) {
                clearInterval(this.timerId);
                this.resendBtn.disabled = false;
                this.countdownText.textContent = '0:00';
            }
        }, 1000);
    }

    tick() {
        const m = Math.floor(this.count / 60);
        const s = (this.count % 60).toString().padStart(2, '0');
        this.countdownText.textContent = `${m}:${s}`;
    }

    toast(message, type = 'info') {
        // استفاده از سیستم نوتیفیکیشن مدرن SweetAlert2
        const config = {
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: type === 'error' ? 5000 : 3000,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer);
                toast.addEventListener('mouseleave', Swal.resumeTimer);
            }
        };

        switch (type) {
            case 'success':
                Swal.fire({
                    ...config,
                    icon: 'success',
                    title: message
                });
                break;
            case 'error':
                Swal.fire({
                    ...config,
                    icon: 'error',
                    title: message
                });
                break;
            case 'warning':
                Swal.fire({
                    ...config,
                    icon: 'warning',
                    title: message
                });
                break;
            case 'info':
            default:
                Swal.fire({
                    ...config,
                    icon: 'info',
                    title: message
                });
                break;
        }
    }

    async sendOtp() {
        if (!this.validatePhone(true)) return;
        if (this.inFlight.send) return;

        this.rawPhone = normalizeDigits(this.phoneInput.value.trim());   // 09…
        this.normalizedPhone = normalizeIranPhoneE164(this.rawPhone);    // +989…
        if (!this.normalizedPhone) { 
            this.toast('شماره معتبر نیست', 'warning'); 
            return; 
        }

        this.inFlight.send = true;
        this.setLoading('phoneForm', true);
        
        // نمایش loading با SweetAlert2
        this.showLoading('در حال ارسال کد...', 'لطفاً صبر کنید');
        
        try {
            const res = await fetch(API_SEND, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': csrf() || ''
                },
                body: JSON.stringify({ phone: this.normalizedPhone })
            });

            let data = {};
            try { data = await res.json(); } catch { /* 204 */ }

            if (!res.ok) {
                this.hideLoading();
                const msg = (data?.errorCode === 'RATE_LIMIT_EXCEEDED')
                    ? 'خیلی سریع درخواست دادید؛ چند دقیقه‌ی دیگر دوباره تلاش کنید.'
                    : (data?.errors?.[0] || data?.message || 'ارسال کد ناموفق بود');
                this.toast(msg, 'warning');
                return;
            }

            this.hideLoading();
            this.displayPhone.textContent = this.rawPhone; // نمایش 09…
            this.showStep('otp');
            this.startTimer(60);
            this.toast('کد تأیید ارسال شد', 'success');
        } catch (e) {
            this.hideLoading();
            this.toast('خطا در ارتباط با سرور', 'error');
        } finally {
            this.setLoading('phoneForm', false);
            this.inFlight.send = false;
        }
    }

    async verifyOtp() {
        if (this.inFlight.verify) return;

        const hidden = document.getElementById('otpCode');
        const rawCode = hidden ? hidden.value : this.otpInputs.map(i => i.value || '').join('');
        const code = normalizeDigits(rawCode).replace(/[^\d]/g, '');
        if (code.length !== 6) { 
            this.toast('کد ۶ رقمی را کامل کنید', 'warning'); 
            return; 
        }

        this.inFlight.verify = true;
        this.setLoading('otpForm', true);
        
        // نمایش loading با SweetAlert2
        this.showLoading('در حال تأیید کد...', 'لطفاً صبر کنید');
        
        try {
            const res = await fetch(API_VERIFY, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': csrf() || ''
                },
                body: JSON.stringify({
                    phone: this.normalizedPhone,
                    code: code
                })
            });

            let data = {};
            try { data = await res.json(); } catch { }

            if (!res.ok) {
                this.hideLoading();
                this.toast(data?.errors?.[0] || data?.message || 'کد اشتباه یا منقضی است', 'error');
                this.otpInputs.forEach(i => i.value = '');
                this.updateHiddenOtp();
                this.otpInputs[0].focus();
                return;
            }

            this.hideLoading();
            // موفق: کوکی Auth سمت MVC ست می‌شود، RT سمت API HttpOnly
            this.toast('ورود موفق! خوش آمدید به DigiTekShop', 'success');
            this.showStep('success');
            setTimeout(() => window.location.href = REDIRECT_AFTER_LOGIN, 2000);
        } catch {
            this.hideLoading();
            this.toast('خطا در ارتباط با سرور', 'error');
        } finally {
            this.setLoading('otpForm', false);
            this.inFlight.verify = false;
        }
    }

    async resend() {
        if (this.count > 0) return;
        await this.sendOtp();
    }
}

function goBackToPhone() {
    if (window.authManager) {
        window.authManager.showStep('phone');
        window.authManager.otpInputs.forEach(i => i.value = '');
        const hidden = document.getElementById('otpCode');
        if (hidden) hidden.value = '';
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.authManager = new AuthManager();
});


