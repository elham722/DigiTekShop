/**
 * DigiTekShop MVC - YARP Authentication Example
 * 
 * این فایل نمونه‌ای است برای نحوه استفاده از Authentication با معماری YARP
 * همه درخواست‌های /api/* مستقیماً به Backend API فوروارد می‌شوند
 */

// ====================================
// 1. Send OTP
// ====================================

async function sendOtp(phoneNumber) {
    try {
        const response = await fetch('/api/Auth/SendOtp', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Correlation-ID': generateCorrelationId() // اختیاری برای tracking
            },
            body: JSON.stringify({ phoneNumber })
        });

        const data = await response.json();

        if (data.isSuccess) {
            console.log('✅ کد OTP با موفقیت ارسال شد');
            showNotification('success', 'کد تأیید ارسال شد');
            return true;
        } else {
            console.error('❌ خطا در ارسال OTP:', data.message);
            showNotification('error', data.message || 'خطا در ارسال کد');
            return false;
        }
    } catch (error) {
        console.error('❌ Network error:', error);
        showNotification('error', 'خطا در برقراری ارتباط');
        return false;
    }
}

// ====================================
// 2. Verify OTP & Login
// ====================================

async function verifyOtpAndLogin(phoneNumber, code, returnUrl = '/') {
    try {
        // Step 1: Verify OTP با Backend API (از طریق YARP)
        const verifyResponse = await fetch('/api/Auth/VerifyOtp', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Correlation-ID': generateCorrelationId()
            },
            body: JSON.stringify({ phoneNumber, code })
        });

        const verifyData = await verifyResponse.json();

        if (!verifyData.isSuccess || !verifyData.data) {
            console.error('❌ خطا در تأیید OTP:', verifyData.message);
            showNotification('error', verifyData.message || 'کد تأیید اشتباه است');
            return false;
        }

        console.log('✅ OTP تأیید شد:', verifyData.data);

        // Step 2: Set Cookie در MVC برای UI Authentication
        const cookieResponse = await fetch('/Auth/SetAuthCookie', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                accessToken: verifyData.data.accessToken,
                returnUrl: returnUrl,
                isNewUser: verifyData.data.isNewUser
            })
        });

        const cookieResult = await cookieResponse.json();

        if (cookieResult.success) {
            console.log('✅ ورود با موفقیت انجام شد');
            showNotification('success', 'ورود موفق!');

            // Redirect بعد از 1 ثانیه
            setTimeout(() => {
                window.location.href = cookieResult.redirectUrl;
            }, 1000);

            return true;
        } else {
            console.error('❌ خطا در تنظیم Cookie:', cookieResult.message);
            showNotification('error', 'خطا در فرآیند ورود');
            return false;
        }
    } catch (error) {
        console.error('❌ Network error:', error);
        showNotification('error', 'خطا در برقراری ارتباط');
        return false;
    }
}

// ====================================
// 3. Logout
// ====================================

async function logout() {
    try {
        // Step 1: Logout از API (revoke tokens)
        // توجه: اگر userId و refreshToken دارید، ارسال کنید
        const apiLogoutResponse = await fetch('/api/Auth/Logout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                userId: getCurrentUserId(), // باید از Claims بگیرید
                refreshToken: null // اگر ذخیره کرده‌اید
            })
        });

        // حتی اگر API logout شکست خورد، Cookie UI را پاک کنیم
        const apiLogoutOk = apiLogoutResponse.ok;
        if (!apiLogoutOk) {
            console.warn('⚠️ API logout failed, but continuing with UI logout');
        }

        // Step 2: Logout از MVC (clear Cookie)
        const mvcLogoutResponse = await fetch('/Auth/Logout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const mvcLogoutData = await mvcLogoutResponse.json();

        if (mvcLogoutData.success) {
            console.log('✅ خروج با موفقیت انجام شد');
            showNotification('success', 'خروج موفق');
            window.location.href = mvcLogoutData.redirectUrl;
            return true;
        } else {
            console.error('❌ خطا در خروج از UI');
            showNotification('error', 'خطا در خروج');
            return false;
        }
    } catch (error) {
        console.error('❌ Network error:', error);
        showNotification('error', 'خطا در برقراری ارتباط');
        return false;
    }
}

// ====================================
// 4. Fetch User Info (Me)
// ====================================

async function fetchUserInfo() {
    try {
        const response = await fetch('/api/Auth/Me', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const data = await response.json();
            console.log('✅ اطلاعات کاربر:', data);
            return data;
        } else if (response.status === 401) {
            console.warn('⚠️ Unauthorized - redirecting to login');
            window.location.href = '/Auth/Login';
            return null;
        } else {
            console.error('❌ خطا در دریافت اطلاعات کاربر');
            return null;
        }
    } catch (error) {
        console.error('❌ Network error:', error);
        return null;
    }
}

// ====================================
// 5. Generic API Call با YARP
// ====================================

/**
 * درخواست عمومی به API از طریق YARP
 * @param {string} endpoint - مسیر API (مثلاً: '/api/Products/List')
 * @param {object} options - گزینه‌های fetch (method, body, headers, ...)
 */
async function callApi(endpoint, options = {}) {
    const defaultHeaders = {
        'Content-Type': 'application/json',
        'X-Correlation-ID': generateCorrelationId()
    };

    const finalOptions = {
        ...options,
        headers: {
            ...defaultHeaders,
            ...options.headers
        }
    };

    try {
        const response = await fetch(endpoint, finalOptions);

        // Handle 401 Unauthorized
        if (response.status === 401) {
            console.warn('⚠️ Unauthorized - redirecting to login');
            window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
            return null;
        }

        // Handle 429 Too Many Requests
        if (response.status === 429) {
            const retryAfter = response.headers.get('Retry-After') || 30;
            console.warn(`⚠️ Rate Limited - retry after ${retryAfter} seconds`);
            showNotification('warning', `درخواست زیاد است. لطفاً ${retryAfter} ثانیه صبر کنید`);
            return null;
        }

        // Parse JSON
        const data = await response.json();

        if (response.ok) {
            return data;
        } else {
            console.error('❌ API Error:', data);
            showNotification('error', data.message || 'خطا در درخواست');
            return null;
        }
    } catch (error) {
        console.error('❌ Network error:', error);
        showNotification('error', 'خطا در برقراری ارتباط');
        return null;
    }
}

// ====================================
// Helpers
// ====================================

function generateCorrelationId() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function getCurrentUserId() {
    // باید از Claims در Cookie بگیرید
    // این فقط یک placeholder است
    // در واقعیت، باید از server-side render یا یک endpoint دریافت کنید
    return null;
}

function showNotification(type, message) {
    // اینجا می‌توانید از NotificationExtensions استفاده کنید
    // یا یک کتابخانه notification مانند Toastr
    console.log(`[${type.toUpperCase()}] ${message}`);
    
    // مثال: استفاده از Toastr (اگر نصب شده باشد)
    if (typeof toastr !== 'undefined') {
        toastr[type](message);
    } else {
        alert(message);
    }
}

// ====================================
// Example Usage
// ====================================

/*

// در صفحه Login:
document.getElementById('sendOtpBtn').addEventListener('click', async () => {
    const phoneNumber = document.getElementById('phoneNumber').value;
    await sendOtp(phoneNumber);
});

document.getElementById('verifyOtpBtn').addEventListener('click', async () => {
    const phoneNumber = document.getElementById('phoneNumber').value;
    const code = document.getElementById('otpCode').value;
    const returnUrl = document.getElementById('returnUrl').value || '/';
    await verifyOtpAndLogin(phoneNumber, code, returnUrl);
});

// در صفحه Dashboard:
document.getElementById('logoutBtn').addEventListener('click', async () => {
    if (confirm('آیا مطمئن هستید که می‌خواهید خارج شوید؟')) {
        await logout();
    }
});

// دریافت اطلاعات کاربر:
window.addEventListener('DOMContentLoaded', async () => {
    const userInfo = await fetchUserInfo();
    if (userInfo) {
        document.getElementById('userName').textContent = userInfo.fullName;
    }
});

// فراخوانی عمومی API:
const products = await callApi('/api/Products/List', {
    method: 'POST',
    body: JSON.stringify({ page: 1, pageSize: 10 })
});

*/

