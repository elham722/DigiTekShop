// ---------------------
// API Toast Helper
// نمایش خودکار Toast از Result Pattern
// ---------------------

/**
 * نمایش Toast از Response API
 * @param {Response} response - Fetch response
 * @param {Object} options - Options
 */
async function showToastFromApiResponse(response, options = {}) {
    const {
        successMessage = null, // پیام سفارشی برای success
        errorMessage = null,   // پیام سفارشی برای error
        showOnSuccess = true,  // نمایش Toast در success
        showOnError = true     // نمایش Toast در error
    } = options;

    try {
        const contentType = response.headers.get('content-type') || '';
        const isJson = contentType.includes('application/json') || contentType.includes('application/problem+json');
        
        if (!isJson) {
            // اگر JSON نیست، از status code استفاده کن
            if (response.ok && showOnSuccess) {
                Toast.success(successMessage || 'عملیات با موفقیت انجام شد');
            } else if (!response.ok && showOnError) {
                Toast.error(errorMessage || 'خطا در انجام عملیات');
            }
            return;
        }

        const data = await response.json();

        // بررسی ساختار ApiResponse (success)
        if (response.ok && data.data !== undefined) {
            // ApiResponse<T> structure
            const message = successMessage || 'عملیات با موفقیت انجام شد';
            if (showOnSuccess) {
                Toast.success(message);
            }
            return;
        }

        // بررسی ساختار ProblemDetails (error)
        if (!response.ok) {
            // ProblemDetails structure
            const detail = data.detail || data.title || errorMessage || 'خطا در انجام عملیات';
            if (showOnError) {
                Toast.error(detail);
            }
            return;
        }
    } catch (err) {
        // اگر JSON parse نشد، از status code استفاده کن
        if (response.status >= 400 && showOnError) {
            Toast.error(errorMessage || 'خطا در ارتباط با سرور');
        }
    }
}

/**
 * Wrapper برای fetch که خودکار Toast نمایش می‌دهد
 * @param {string} url - API URL
 * @param {RequestInit} options - Fetch options
 * @param {Object} toastOptions - Toast options
 */
async function fetchWithToast(url, options = {}, toastOptions = {}) {
    try {
        const response = await fetch(url, options);
        await showToastFromApiResponse(response, toastOptions);
        return response;
    } catch (err) {
        if (toastOptions.showOnError !== false) {
            Toast.error(toastOptions.errorMessage || 'خطا در ارتباط با سرور');
        }
        throw err;
    }
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { showToastFromApiResponse, fetchWithToast };
}

// Make available globally
window.showToastFromApiResponse = showToastFromApiResponse;
window.fetchWithToast = fetchWithToast;

