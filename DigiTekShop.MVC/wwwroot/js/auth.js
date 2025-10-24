/**
 * DigiTekShop Auth (OTP) – final
 * هماهنگ با: /api/v1/auth/send-otp , /api/v1/auth/verify-otp
 */

const API_SEND = '/Auth/SendOtp';
const API_VERIFY = '/Auth/VerifyOtp';
const REDIRECT_AFTER_LOGIN = '/';

function csrf() {
    const m = document.querySelector('meta[name="request-verification-token"]');
    return m ? m.content : '';
}

function getDeviceId() {
    const key = 'dts_device_id';
    let v = localStorage.getItem(key);
    if (!v && window.crypto?.randomUUID) {
        v = crypto.randomUUID();
        localStorage.setItem(key, v);
    } else if (!v) {
        v = 'dev_' + Math.random().toString(36).slice(2) + '_' + Date.now();
        localStorage.setItem(key, v);
    }
    return v;
}

// ورودی کاربر: 09xxxxxxxxx  → بک‌اند: +989xxxxxxxxx
function normalizeIranPhoneE164(input) {
    if (!input) return null;
    let s = ('' + input).replace(/[^\d+]/g, '');
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
        this.rememberDevice = true;
        this.deviceId = getDeviceId();
        this.timerId = null;
        this.count = 60;

        this.cacheEls();
        this.bindEvents();
        this.setupOtpInputs();
        this.phoneInput?.focus();
    }

    cacheEls() {
        this.phoneForm = document.getElementById('phoneForm');
        this.otpForm = document.getElementById('otpForm');
        this.phoneInput = document.getElementById('phoneNumber');
        this.rememberEl = document.getElementById('rememberDevice');
        this.displayPhone = document.getElementById('displayPhone');
        this.resendBtn = document.getElementById('resendBtn');
        this.countdownText = document.getElementById('countdownText');
    }

    bindEvents() {
        this.phoneForm.addEventListener('submit', (e) => { e.preventDefault(); this.sendOtp(); });
        this.otpForm.addEventListener('submit', (e) => { e.preventDefault(); this.verifyOtp(); });
        this.resendBtn.addEventListener('click', () => this.resend());
        this.rememberEl.addEventListener('change', e => this.rememberDevice = !!e.target.checked);

        // ورودی شماره فقط عدد و حداکثر 11 رقم (فرمت 09xxxxxxxxx)
        this.phoneInput.addEventListener('input', (e) => {
            e.target.value = e.target.value.replace(/[^\d]/g, '').slice(0, 11);
        });
        this.phoneInput.addEventListener('blur', () => this.validatePhone(true));

        // Enter روی OTP
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && e.target.classList.contains('otp-input')) {
                e.preventDefault();
                this.verifyOtp();
            }
        });
    }

    setupOtpInputs() {
        this.otpInputs = Array.from(document.querySelectorAll('.otp-input'));
        const moveNext = (idx) => { if (idx < this.otpInputs.length - 1) this.otpInputs[idx + 1].focus(); };

        this.otpInputs.forEach((el, idx) => {
            el.addEventListener('input', (e) => {
                e.target.value = e.target.value.replace(/[^\d]/g, '').slice(0, 1);
                if (e.target.value) moveNext(idx);
                this.updateHiddenOtp();
            });
            el.addEventListener('keydown', (e) => {
                if (e.key === 'Backspace' && !e.target.value && idx > 0) this.otpInputs[idx - 1].focus();
            });
            el.addEventListener('paste', (e) => {
                e.preventDefault();
                const s = (e.clipboardData.getData('text') || '').replace(/[^\d]/g, '').slice(0, 6);
                for (let i = 0; i < s.length && i < this.otpInputs.length; i++) this.otpInputs[i].value = s[i];
                this.updateHiddenOtp();
                this.otpInputs[Math.min(s.length, this.otpInputs.length - 1)].focus();
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
        const container = document.getElementById('toastContainer');
        const id = `t${Date.now()}`;
        const icon = { success: 'check-circle', error: 'exclamation-circle', warning: 'exclamation-triangle', info: 'info-circle' }[type] || 'info-circle';
        container.insertAdjacentHTML('beforeend', `
      <div id="${id}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="toast-header">
          <i class="fas fa-${icon} me-2 ${type === 'error' ? 'text-danger' : type === 'success' ? 'text-success' : type === 'warning' ? 'text-warning' : 'text-info'}"></i>
          <strong class="me-auto">DigiTekShop</strong>
          <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body">${message}</div>
      </div>
    `);
        const el = document.getElementById(id);
        new bootstrap.Toast(el, { autohide: true, delay: 4500 }).show();
        el.addEventListener('hidden.bs.toast', () => el.remove());
    }

    async sendOtp() {
        if (!this.validatePhone(true)) return;

        this.rawPhone = this.phoneInput.value.trim();            // 09…
        this.normalizedPhone = normalizeIranPhoneE164(this.rawPhone); // +989…
        if (!this.normalizedPhone) { this.toast('شماره معتبر نیست', 'warning'); return; }

        this.setLoading('phoneForm', true);
        try {
            const res = await fetch(API_SEND, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': csrf() || ''
                },
                body: JSON.stringify({
                    phone: this.normalizedPhone,
                    deviceId: this.deviceId,
                    rememberDevice: !!this.rememberDevice
                })
            });

            let data = {};
            try { data = await res.json(); } catch { }

            if (!res.ok) {
                this.toast(data?.errors?.[0] || data?.message || 'ارسال کد ناموفق بود', 'error');
                return;
            }

            this.displayPhone.textContent = this.rawPhone; // نمایش 09…
            this.showStep('otp');
            this.startTimer(60);
            this.toast('کد تایید ارسال شد', 'success');
        } catch (e) {
            this.toast('خطا در ارتباط با سرور', 'error');
        } finally {
            this.setLoading('phoneForm', false);
        }
    }

    async verifyOtp() {
        const hidden = document.getElementById('otpCode');
        const code = hidden ? hidden.value : this.otpInputs.map(i => i.value || '').join('');
        if (code.length !== 6) { this.toast('کد ۶ رقمی را کامل کنید', 'warning'); return; }

        this.setLoading('otpForm', true);
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
                    code: code,
                    deviceId: this.deviceId,
                    rememberDevice: !!this.rememberDevice
                })
            });

            let data = {};
            try { data = await res.json(); } catch { }

            if (!res.ok) {
                this.toast(data?.errors?.[0] || data?.message || 'کد اشتباه یا منقضی است', 'error');
                this.otpInputs.forEach(i => i.value = '');
                this.otpInputs[0].focus();
                return;
            }

            // LoginResponseDto: { userId, accessToken, accessTokenExpiresAtUtc, refreshToken, refreshTokenExpiresAtUtc, isNewUser }
            window.tokenManager?.setTokens({
                accessToken: data.accessToken,
                refreshToken: data.refreshToken,
                accessTokenExpiresAtUtc: data.accessTokenExpiresAtUtc
            });
            window.tokenManager?.setUser({ userId: data.userId });

            this.showStep('success');
            setTimeout(() => window.location.href = REDIRECT_AFTER_LOGIN, 1200);
        } catch {
            this.toast('خطا در ارتباط با سرور', 'error');
        } finally {
            this.setLoading('otpForm', false);
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
