-- =============================================
-- 🔍 گرفتن سریع OTP Code
-- =============================================
-- این query رو در SQL Server Management Studio یا Azure Data Studio اجرا کن

USE DigitekIdentityDB;
GO

-- 📱 گرفتن آخرین OTP برای یک شماره
DECLARE @Phone NVARCHAR(20) = '+989121234567';  -- 👈 شماره خودت رو اینجا بذار

SELECT TOP 1
    Phone AS [شماره موبایل],
    Code AS [🔑 کد OTP],
    CAST(CreatedAtUtc AS DATETIME2) AS [زمان ساخت],
    CAST(ExpiresAtUtc AS DATETIME2) AS [زمان انقضا],
    DATEDIFF(SECOND, GETUTCDATE(), ExpiresAtUtc) AS [⏰ ثانیه باقیمانده],
    IsUsed AS [استفاده شده؟],
    CASE 
        WHEN IsUsed = 1 THEN '❌ استفاده شده'
        WHEN ExpiresAtUtc < GETUTCDATE() THEN '⏰ منقضی شده'
        ELSE '✅ فعال'
    END AS [وضعیت]
FROM Identity.PhoneVerifications
WHERE Phone = @Phone
ORDER BY CreatedAtUtc DESC;

-- =============================================
-- 📊 لیست تمام OTPهای فعال (منقضی نشده)
-- =============================================
SELECT 
    Phone AS [شماره],
    Code AS [کد],
    DATEDIFF(SECOND, GETUTCDATE(), ExpiresAtUtc) AS [ثانیه باقیمانده],
    CreatedAtUtc AS [زمان ساخت]
FROM Identity.PhoneVerifications
WHERE IsUsed = 0 
  AND ExpiresAtUtc > GETUTCDATE()
ORDER BY CreatedAtUtc DESC;

-- =============================================
-- 🗑️ پاک کردن OTPهای منقضی (Cleanup)
-- =============================================
-- DELETE FROM Identity.PhoneVerifications
-- WHERE ExpiresAtUtc < DATEADD(HOUR, -1, GETUTCDATE());

