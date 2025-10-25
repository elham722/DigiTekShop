# DigiTekShop - Modern Notification System

سیستم نوتیفیکیشن مدرن و زیبا برای پروژه DigiTekShop با استفاده از SweetAlert2 و توابع کمکی سفارشی.

## ویژگی‌ها

- ✅ **SweetAlert2** - کتابخانه مدرن و زیبا برای نوتیفیکیشن‌ها
- ✅ **پشتیبانی از RTL** - مناسب برای زبان فارسی
- ✅ **انیمیشن‌های زیبا** - تجربه کاربری بهتر
- ✅ **Toast Notifications** - نوتیفیکیشن‌های غیرمزاحم
- ✅ **Auto Error Handling** - مدیریت خودکار خطاها
- ✅ **Form Validation** - اعتبارسنجی فرم‌ها
- ✅ **AJAX Helpers** - توابع کمکی برای درخواست‌های AJAX

## نصب و راه‌اندازی

### 1. فایل‌های CSS و JS

فایل‌های زیر در Layout اصلی قرار گرفته‌اند:

```html
<!-- CSS -->
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.css">
<link rel="stylesheet" href="~/css/notifications.css">

<!-- JavaScript -->
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script src="~/js/notifications.js"></script>
<script src="~/js/api-helpers.js"></script>
```

### 2. Extension Methods برای کنترلرها

```csharp
using DigiTekShop.MVC.Extensions;

public class AuthController : Controller
{
    public IActionResult SendOtp(SendOtpRequestDto dto)
    {
        // Success notification
        return this.JsonSuccess("کد تأیید با موفقیت ارسال شد");
        
        // Error notification
        return this.JsonError("خطا در ارسال کد");
        
        // Validation error
        return this.JsonValidationError("لطفاً اطلاعات را صحیح وارد کنید", ModelState);
    }
}
```

## استفاده

### 1. نوتیفیکیشن‌های ساده

```javascript
// Success
showSuccess('موفقیت!', 'عملیات با موفقیت انجام شد');
showSuccessToast('کد ارسال شد!');

// Error
showError('خطا!', 'مشکلی پیش آمد');
showErrorToast('خطا در ارسال کد');

// Warning
showWarning('هشدار!', 'لطفاً دوباره تلاش کنید');
showWarningToast('اطلاعات ناقص است');

// Info
showInfo('اطلاعات', 'سیستم به‌روزرسانی شد');
showInfoToast('پیام جدید دریافت شد');
```

### 2. دیالوگ‌های تأیید

```javascript
// Confirmation dialog
showConfirm('حذف آیتم', 'آیا مطمئن هستید؟')
    .then((result) => {
        if (result.isConfirmed) {
            // Delete item
        }
    });

// Delete confirmation
showDeleteConfirm('محصول')
    .then((result) => {
        if (result.isConfirmed) {
            // Delete product
        }
    });
```

### 3. ورودی کاربر

```javascript
// Text input
showInput('نام محصول', 'text')
    .then((result) => {
        if (result.isConfirmed) {
            console.log(result.value);
        }
    });

// Password input
showPasswordInput('رمز عبور')
    .then((result) => {
        if (result.isConfirmed) {
            console.log(result.value);
        }
    });

// Email input
showEmailInput('ایمیل')
    .then((result) => {
        if (result.isConfirmed) {
            console.log(result.value);
        }
    });
```

### 4. Loading States

```javascript
// Show loading
showLoading('در حال پردازش...');

// Hide loading
hideLoading();

// Progress notification
showProgress('آپلود فایل', 'در حال آپلود...');
updateProgress(50, '50% تکمیل شد');
```

### 5. AJAX Requests

```javascript
// GET request
apiGet('/api/products')
    .then(response => {
        console.log(response.data);
    });

// POST request
apiPost('/api/products', { name: 'محصول جدید' })
    .then(response => {
        showSuccessToast('محصول ایجاد شد');
    });

// Form submission
submitForm('#productForm')
    .then(response => {
        showSuccessToast('فرم با موفقیت ارسال شد');
    });
```

### 6. Form Validation

```javascript
// Validate form
if (validateForm('#loginForm')) {
    // Form is valid
}

// Custom validation rules
const rules = {
    phone: {
        required: true,
        pattern: /^(\+98|0)?9\d{9}$/,
        message: 'شماره تلفن معتبر نیست'
    },
    email: {
        required: true,
        pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
        message: 'ایمیل معتبر نیست'
    }
};

if (validateForm('#contactForm', rules)) {
    // Form is valid
}
```

### 7. Authentication Helpers

```javascript
// Send OTP
sendOtp('09123456789')
    .then(response => {
        showSuccessToast('کد ارسال شد');
    });

// Verify OTP
verifyOtp('09123456789', '123456')
    .then(response => {
        showSuccessToast('ورود موفق');
        window.location.href = '/dashboard';
    });

// Login
login({ phone: '09123456789', password: 'password' })
    .then(response => {
        showSuccessToast('ورود موفق');
    });

// Logout
logout()
    .then(response => {
        showSuccessToast('خروج موفق');
        window.location.href = '/login';
    });
```

### 8. Product Management

```javascript
// Get products
getProducts({ category: 'electronics' })
    .then(response => {
        // Display products
    });

// Create product
createProduct({
    name: 'محصول جدید',
    price: 100000,
    category: 'electronics'
})
    .then(response => {
        showSuccessToast('محصول ایجاد شد');
    });

// Update product
updateProduct(1, { name: 'محصول به‌روزرسانی شده' })
    .then(response => {
        showSuccessToast('محصول به‌روزرسانی شد');
    });

// Delete product
deleteProduct(1)
    .then(response => {
        showSuccessToast('محصول حذف شد');
    });
```

### 9. Order Management

```javascript
// Get orders
getOrders({ status: 'pending' })
    .then(response => {
        // Display orders
    });

// Create order
createOrder({
    productId: 1,
    quantity: 2,
    address: 'تهران، خیابان ولیعصر'
})
    .then(response => {
        showSuccessToast('سفارش ایجاد شد');
    });

// Cancel order
cancelOrder(1)
    .then(response => {
        showSuccessToast('سفارش لغو شد');
    });
```

## سفارشی‌سازی

### 1. تغییر تم و رنگ‌ها

```css
/* در فایل notifications.css */
.swal2-confirm {
    background-color: #your-color !important;
}

.swal2-popup {
    border-radius: 12px !important;
}
```

### 2. تغییر متن‌های پیش‌فرض

```javascript
// در فایل notifications.js
window.DigiTekNotification.defaultConfig = {
    confirmButtonText: 'تأیید',
    cancelButtonText: 'لغو',
    // ... سایر تنظیمات
};
```

### 3. اضافه کردن انیمیشن‌های سفارشی

```css
@keyframes customAnimation {
    0% { transform: scale(0.8); opacity: 0; }
    100% { transform: scale(1); opacity: 1; }
}

.swal2-popup {
    animation: customAnimation 0.3s ease-out;
}
```

## مثال‌های کاربردی

### 1. فرم ورود با OTP

```html
<form id="loginForm" data-ajax>
    <input type="tel" name="phone" id="phone" required>
    <button type="button" id="sendOtpBtn">ارسال کد</button>
    
    <input type="text" name="otpCode" id="otpCode" disabled>
    <button type="submit" id="verifyOtpBtn" disabled>تأیید کد</button>
</form>
```

```javascript
$('#sendOtpBtn').click(function() {
    const phone = $('#phone').val();
    sendOtp(phone)
        .then(response => {
            showSuccessToast('کد ارسال شد');
            $('#otpCode').prop('disabled', false);
        });
});
```

### 2. حذف با تأیید

```html
<button data-delete="/api/products/1" data-item-name="محصول">
    حذف محصول
</button>
```

### 3. فرم با اعتبارسنجی

```html
<form id="contactForm" data-validate>
    <input type="text" name="name" required>
    <input type="email" name="email" required>
    <input type="tel" name="phone" required>
    <button type="submit">ارسال</button>
</form>
```

## نکات مهم

1. **همیشه از توابع کمکی استفاده کنید** - به جای SweetAlert2 مستقیم
2. **خطاها را مدیریت کنید** - از try/catch استفاده کنید
3. **Loading states را نشان دهید** - برای عملیات طولانی
4. **پیام‌ها را واضح بنویسید** - کاربر باید بداند چه اتفاقی افتاده
5. **از RTL پشتیبانی کنید** - برای زبان فارسی

## پشتیبانی

برای سوالات و مشکلات، لطفاً با تیم توسعه تماس بگیرید.

---

**DigiTekShop Team** - 2024
