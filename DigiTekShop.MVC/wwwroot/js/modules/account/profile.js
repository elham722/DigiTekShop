/**
 * DigiTekShop - Profile Module
 * مدیریت پروفایل کاربر
 */

const API_GET_PROFILE = '/api/v1/profile/me';
const API_UPDATE_PROFILE = '/api/v1/profile/me';

// ===== State =====
let currentProfile = null;

// ===== DOM Elements =====
const elements = {
    profileName: null,
    profileStatus: null,
    profileEmail: null,
    profilePhone: null,
    addressesSection: null,
    addressesList: null,
    alertContainer: null,
    editModal: null,
    profileForm: null,
    fullNameInput: null,
    emailInput: null,
    phoneInput: null
};

// ===== Initialize =====
document.addEventListener('DOMContentLoaded', () => {
    cacheElements();
    bindEvents();
    loadProfile();
});

function cacheElements() {
    elements.profileName = document.getElementById('profileName');
    elements.profileStatus = document.getElementById('profileStatus');
    elements.profileEmail = document.getElementById('profileEmail');
    elements.profilePhone = document.getElementById('profilePhone');
    elements.addressesSection = document.getElementById('addressesSection');
    elements.addressesList = document.getElementById('addressesList');
    elements.alertContainer = document.getElementById('alertContainer');
    elements.editModal = document.getElementById('editModal');
    elements.profileForm = document.getElementById('profileForm');
    elements.fullNameInput = document.getElementById('fullName');
    elements.emailInput = document.getElementById('email');
    elements.phoneInput = document.getElementById('phone');
}

function bindEvents() {
    // فرم ویرایش
    elements.profileForm?.addEventListener('submit', onProfileSubmit);
    
    // بستن مودال با کلیک بیرون
    elements.editModal?.addEventListener('click', (e) => {
        if (e.target === elements.editModal) {
            closeEditModal();
        }
    });
    
    // بستن مودال با Escape
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && elements.editModal?.style.display !== 'none') {
            closeEditModal();
        }
    });
    
    // نرمال‌سازی شماره تلفن
    elements.phoneInput?.addEventListener('input', (e) => {
        e.target.value = normalizeDigits(e.target.value).replace(/[^\d]/g, '').slice(0, 11);
    });
}

// ===== API Functions =====

async function loadProfile() {
    try {
        showLoading();
        
        const res = await fetch(API_GET_PROFILE, {
            method: 'GET',
            credentials: 'same-origin',
            headers: {
                'Accept': 'application/json'
            }
        });

        if (!res.ok) {
            if (res.status === 401) {
                window.location.href = '/auth/login?returnUrl=/account/profile';
                return;
            }
            throw new Error('خطا در دریافت پروفایل');
        }

        const data = await res.json();
        currentProfile = data;
        renderProfile(data);
        
    } catch (err) {
        console.error('Load profile error:', err);
        showAlert('خطا در دریافت اطلاعات پروفایل', 'error');
    }
}

async function onProfileSubmit(e) {
    e.preventDefault();
    
    // Validate
    if (!validateForm()) return;
    
    const payload = {
        fullName: elements.fullNameInput.value.trim(),
        email: elements.emailInput.value.trim() || null,
        phone: elements.phoneInput.value.trim() || null
    };

    setFormLoading(true);

    try {
        const csrfToken = getCsrfToken();
        
        const res = await fetch(API_UPDATE_PROFILE, {
            method: 'PUT',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'RequestVerificationToken': csrfToken
            },
            body: JSON.stringify(payload)
        });

        if (!res.ok) {
            const errorData = await res.json().catch(() => ({}));
            throw new Error(errorData.message || 'خطا در بروزرسانی پروفایل');
        }

        // موفقیت
        closeEditModal();
        showAlert('پروفایل با موفقیت بروزرسانی شد', 'success');
        
        // نوتیفیکیشن Toast
        window.DigiTekNotification?.showSuccessToast?.('پروفایل با موفقیت ذخیره شد');
        
        // بارگذاری مجدد پروفایل
        await loadProfile();
        
    } catch (err) {
        console.error('Update profile error:', err);
        showAlert(err.message, 'error');
        window.DigiTekNotification?.showErrorToast?.(err.message);
    } finally {
        setFormLoading(false);
    }
}

// ===== Render Functions =====

function renderProfile(profile) {
    // نام
    if (elements.profileName) {
        elements.profileName.textContent = profile.fullName || 'بدون نام';
    }
    
    // وضعیت
    if (elements.profileStatus) {
        elements.profileStatus.textContent = profile.isActive ? 'فعال' : 'غیرفعال';
        elements.profileStatus.className = `profile-badge ${profile.isActive ? 'active' : 'inactive'}`;
    }
    
    // ایمیل
    if (elements.profileEmail) {
        elements.profileEmail.textContent = profile.email || 'ثبت نشده';
    }
    
    // تلفن
    if (elements.profilePhone) {
        elements.profilePhone.textContent = profile.phone || 'ثبت نشده';
    }
    
    // آدرس‌ها
    renderAddresses(profile.addresses || []);
}

function renderAddresses(addresses) {
    if (!addresses.length) {
        elements.addressesSection.style.display = 'none';
        return;
    }
    
    elements.addressesSection.style.display = 'block';
    elements.addressesList.innerHTML = '';
    
    addresses.forEach(addr => {
        const card = document.createElement('div');
        card.className = `address-card ${addr.isDefault ? 'default' : ''}`;
        
        card.innerHTML = `
            ${addr.isDefault ? '<span class="default-tag">پیش‌فرض</span>' : ''}
            <p><strong>${addr.line1}</strong></p>
            ${addr.line2 ? `<p>${addr.line2}</p>` : ''}
            <p>${addr.city}${addr.state ? `، ${addr.state}` : ''}</p>
            <p>کدپستی: <span dir="ltr">${addr.postalCode}</span></p>
        `;
        
        elements.addressesList.appendChild(card);
    });
}

// ===== Modal Functions =====

window.openEditModal = function() {
    if (!currentProfile) return;
    
    // پر کردن فرم
    elements.fullNameInput.value = currentProfile.fullName || '';
    elements.emailInput.value = currentProfile.email || '';
    elements.phoneInput.value = currentProfile.phone?.replace('+98', '0') || '';
    
    // پاک کردن خطاها
    clearFormErrors();
    
    // نمایش مودال
    elements.editModal.style.display = 'flex';
    elements.fullNameInput.focus();
    
    // جلوگیری از اسکرول body
    document.body.style.overflow = 'hidden';
};

window.closeEditModal = function() {
    elements.editModal.style.display = 'none';
    document.body.style.overflow = '';
};

// ===== Validation =====

function validateForm() {
    let isValid = true;
    clearFormErrors();
    
    // نام کامل
    const fullName = elements.fullNameInput.value.trim();
    if (!fullName) {
        showFieldError('fullName', 'نام کامل الزامی است');
        isValid = false;
    } else if (fullName.length < 2) {
        showFieldError('fullName', 'نام کامل حداقل 2 کاراکتر باشد');
        isValid = false;
    }
    
    // ایمیل
    const email = elements.emailInput.value.trim();
    if (email && !isValidEmail(email)) {
        showFieldError('email', 'فرمت ایمیل صحیح نیست');
        isValid = false;
    }
    
    // تلفن
    const phone = elements.phoneInput.value.trim();
    if (phone && !isValidPhone(phone)) {
        showFieldError('phone', 'فرمت شماره موبایل صحیح نیست');
        isValid = false;
    }
    
    return isValid;
}

function showFieldError(fieldName, message) {
    const input = document.getElementById(fieldName);
    const errorEl = document.getElementById(fieldName + 'Error');
    
    input?.classList.add('is-invalid');
    if (errorEl) errorEl.textContent = message;
}

function clearFormErrors() {
    document.querySelectorAll('.form-control').forEach(el => {
        el.classList.remove('is-invalid');
    });
    document.querySelectorAll('.field-error').forEach(el => {
        el.textContent = '';
    });
}

// ===== Helpers =====

function getCsrfToken() {
    const meta = document.querySelector('meta[name="request-verification-token"]');
    return meta?.content ?? '';
}

function normalizeDigits(s) {
    if (!s) return '';
    const map = {
        '۰': '0', '۱': '1', '۲': '2', '۳': '3', '۴': '4',
        '۵': '5', '۶': '6', '۷': '7', '۸': '8', '۹': '9',
        '٠': '0', '١': '1', '٢': '2', '٣': '3', '٤': '4',
        '٥': '5', '٦': '6', '٧': '7', '٨': '8', '٩': '9'
    };
    return s.split('').map(ch => map[ch] ?? ch).join('');
}

function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function isValidPhone(phone) {
    return /^09\d{9}$/.test(phone);
}

function showLoading() {
    if (elements.profileName) {
        elements.profileName.textContent = 'در حال بارگذاری...';
    }
}

function setFormLoading(loading) {
    const btn = elements.profileForm?.querySelector('.btn-save');
    if (!btn) return;
    
    btn.disabled = loading;
    btn.querySelector('.btn-text').style.display = loading ? 'none' : 'inline';
    btn.querySelector('.btn-spinner').style.display = loading ? 'inline-flex' : 'none';
}

function showAlert(message, type = 'success') {
    if (!elements.alertContainer) return;
    
    elements.alertContainer.innerHTML = `
        <div class="alert alert-${type}">
            <i class="ri-${type === 'success' ? 'checkbox-circle' : 'error-warning'}-line"></i>
            ${message}
        </div>
    `;
    
    // حذف بعد از 5 ثانیه
    setTimeout(() => {
        elements.alertContainer.innerHTML = '';
    }, 5000);
}

