/**
 * DigiTekShop - Profile Sidebar
 * پر کردن نام و موبایل کاربر جاری از API پروفایل
 */

const API_GET_PROFILE = '/api/v1/profile/me';

document.addEventListener('DOMContentLoaded', () => {
    loadProfileSidebar();
});

async function loadProfileSidebar() {
    const nameEl = document.getElementById('profileFullName');
    const phoneEl = document.getElementById('profilePhone');

    if (!nameEl && !phoneEl) {
        return;
    }

    try {
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

        if (nameEl) {
            nameEl.textContent = data.fullName || 'کاربر';
        }

        if (phoneEl) {
            phoneEl.textContent = formatPhoneLocal(data.phone) || 'ثبت نشده';
        }
    } catch (err) {
        console.error('Load profile sidebar error:', err);
    }
}

function formatPhoneLocal(phone) {
    if (!phone) return null;

    // اگر به صورت +98... هست → 09...
    if (phone.startsWith('+98') && phone.length === 13) {
        return '0' + phone.substring(3);
    }

    return phone;
}
