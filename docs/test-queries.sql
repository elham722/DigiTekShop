-- ========================================
-- 🗄️ DigiTekShop - Test Queries
-- ========================================
-- این queryها رو بعد از هر تست اجرا کن تا ببینی داده‌ها درست ذخیره شدن

-- ========================================
-- 📊 QUICK OVERVIEW
-- ========================================

-- تعداد کل کاربران
SELECT 
    COUNT(*) AS TotalUsers,
    SUM(CASE WHEN "EmailVerified" = true THEN 1 ELSE 0 END) AS VerifiedUsers,
    SUM(CASE WHEN "EmailVerified" = false THEN 1 ELSE 0 END) AS UnverifiedUsers
FROM "Identity"."Users";

-- آخرین 5 کاربر ثبت‌نام شده
SELECT 
    "Id", 
    "Email", 
    "FirstName", 
    "LastName", 
    "EmailVerified",
    "CreatedAtUtc",
    "LastLoginAtUtc"
FROM "Identity"."Users"
ORDER BY "CreatedAtUtc" DESC
LIMIT 5;

-- ========================================
-- 👤 USER DETAILS
-- ========================================

-- جزئیات کامل یک کاربر خاص
SELECT 
    "Id",
    "Email",
    "FirstName" || ' ' || "LastName" AS FullName,
    "EmailVerified",
    "PhoneNumberVerified",
    "TwoFactorEnabled",
    "IsActive",
    "LockoutEnd",
    "AccessFailedCount",
    "CreatedAtUtc",
    "LastLoginAtUtc"
FROM "Identity"."Users"
WHERE "Email" = 'test@digitek.shop'; -- 👈 جایگزین کن با email تست خودت

-- ========================================
-- 🔑 OTP (One-Time Password)
-- ========================================

-- آخرین OTP برای یک ایمیل
SELECT 
    "Id",
    "Email",
    "Code",
    "Purpose",
    "ExpiresAtUtc",
    "IsUsed",
    "UsedAtUtc",
    "AttemptsCount",
    "CreatedAtUtc",
    CASE 
        WHEN "IsUsed" = true THEN '✅ استفاده شده'
        WHEN "ExpiresAtUtc" < NOW() AT TIME ZONE 'UTC' THEN '❌ منقضی شده'
        ELSE '⏳ فعال'
    END AS Status
FROM "Identity"."OneTimePasswords"
WHERE "Email" = 'test@digitek.shop' -- 👈 جایگزین کن
ORDER BY "CreatedAtUtc" DESC
LIMIT 10;

-- تمام OTPهای فعال (منقضی نشده و استفاده نشده)
SELECT 
    "Email",
    "Code",
    "Purpose",
    "ExpiresAtUtc",
    "AttemptsCount",
    EXTRACT(EPOCH FROM ("ExpiresAtUtc" - (NOW() AT TIME ZONE 'UTC'))) / 60 AS MinutesRemaining
FROM "Identity"."OneTimePasswords"
WHERE "IsUsed" = false 
  AND "ExpiresAtUtc" > NOW() AT TIME ZONE 'UTC'
ORDER BY "ExpiresAtUtc" ASC;

-- ========================================
-- 🔄 REFRESH TOKENS
-- ========================================

-- تمام RefreshTokenهای یک کاربر
SELECT 
    rt."Id",
    u."Email",
    rt."DeviceInfo",
    rt."CreatedAtUtc",
    rt."ExpiresAtUtc",
    rt."RevokedAtUtc",
    rt."ReplacedByTokenId",
    CASE 
        WHEN rt."RevokedAtUtc" IS NOT NULL THEN '❌ Revoked'
        WHEN rt."ExpiresAtUtc" < NOW() AT TIME ZONE 'UTC' THEN '⏰ Expired'
        ELSE '✅ Active'
    END AS Status
FROM "Identity"."RefreshTokens" rt
JOIN "Identity"."Users" u ON rt."UserId" = u."Id"
WHERE u."Email" = 'test@digitek.shop' -- 👈 جایگزین کن
ORDER BY rt."CreatedAtUtc" DESC;

-- تمام RefreshTokenهای فعال (Active)
SELECT 
    u."Email",
    rt."DeviceInfo",
    rt."CreatedAtUtc",
    rt."ExpiresAtUtc",
    EXTRACT(EPOCH FROM (rt."ExpiresAtUtc" - (NOW() AT TIME ZONE 'UTC'))) / 3600 AS HoursRemaining
FROM "Identity"."RefreshTokens" rt
JOIN "Identity"."Users" u ON rt."UserId" = u."Id"
WHERE rt."RevokedAtUtc" IS NULL 
  AND rt."ExpiresAtUtc" > NOW() AT TIME ZONE 'UTC'
ORDER BY rt."CreatedAtUtc" DESC;

-- Token Rotation History (برای دیباگ)
WITH RECURSIVE TokenChain AS (
    -- شروع از توکن اصلی (بدون parent)
    SELECT 
        "Id",
        "UserId",
        "CreatedAtUtc",
        "RevokedAtUtc",
        "ReplacedByTokenId",
        1 AS Level
    FROM "Identity"."RefreshTokens"
    WHERE "ReplacedByTokenId" IS NULL
    
    UNION ALL
    
    -- پیدا کردن توکن‌هایی که این توکن رو جایگزین کردن
    SELECT 
        rt."Id",
        rt."UserId",
        rt."CreatedAtUtc",
        rt."RevokedAtUtc",
        rt."ReplacedByTokenId",
        tc.Level + 1
    FROM "Identity"."RefreshTokens" rt
    INNER JOIN TokenChain tc ON rt."Id" = tc."ReplacedByTokenId"
)
SELECT 
    u."Email",
    tc.Level AS RotationLevel,
    tc."CreatedAtUtc",
    tc."RevokedAtUtc",
    CASE 
        WHEN tc."RevokedAtUtc" IS NOT NULL THEN '❌ Revoked'
        ELSE '✅ Active'
    END AS Status
FROM TokenChain tc
JOIN "Identity"."Users" u ON tc."UserId" = u."Id"
WHERE u."Email" = 'test@digitek.shop' -- 👈 جایگزین کن
ORDER BY tc.Level;

-- ========================================
-- 📝 LOGIN ATTEMPTS (Audit Log)
-- ========================================

-- آخرین 20 تلاش ورود برای یک ایمیل
SELECT 
    "Email",
    "IsSuccessful",
    "FailureReason",
    "IpAddress",
    "UserAgent",
    "Timestamp",
    CASE 
        WHEN "IsSuccessful" = true THEN '✅ موفق'
        ELSE '❌ ناموفق: ' || COALESCE("FailureReason", 'نامشخص')
    END AS Result
FROM "Identity"."LoginAttempts"
WHERE "Email" = 'test@digitek.shop' -- 👈 جایگزین کن
ORDER BY "Timestamp" DESC
LIMIT 20;

-- آمار تلاش‌های ورود در 24 ساعت اخیر
SELECT 
    "IsSuccessful",
    COUNT(*) AS Count,
    ARRAY_AGG(DISTINCT "IpAddress") AS UniqueIPs
FROM "Identity"."LoginAttempts"
WHERE "Timestamp" > NOW() AT TIME ZONE 'UTC' - INTERVAL '24 hours'
GROUP BY "IsSuccessful";

-- IPهایی که بیشترین تلاش ناموفق دارند (Brute-force detection)
SELECT 
    "IpAddress",
    COUNT(*) AS FailedAttempts,
    MIN("Timestamp") AS FirstAttempt,
    MAX("Timestamp") AS LastAttempt
FROM "Identity"."LoginAttempts"
WHERE "IsSuccessful" = false
  AND "Timestamp" > NOW() AT TIME ZONE 'UTC' - INTERVAL '1 hour'
GROUP BY "IpAddress"
HAVING COUNT(*) > 5
ORDER BY FailedAttempts DESC;

-- ========================================
-- 🧹 CLEANUP QUERIES (برای تست)
-- ========================================

-- ⚠️ حذف یک کاربر تست و تمام داده‌های مرتبط
-- (فقط برای محیط تست!)
DO $$
DECLARE
    v_user_id UUID;
BEGIN
    -- پیدا کردن userId
    SELECT "Id" INTO v_user_id 
    FROM "Identity"."Users" 
    WHERE "Email" = 'test@digitek.shop'; -- 👈 جایگزین کن
    
    IF v_user_id IS NOT NULL THEN
        -- حذف RefreshTokens
        DELETE FROM "Identity"."RefreshTokens" WHERE "UserId" = v_user_id;
        
        -- حذف OTPs
        DELETE FROM "Identity"."OneTimePasswords" WHERE "Email" = 'test@digitek.shop';
        
        -- حذف LoginAttempts
        DELETE FROM "Identity"."LoginAttempts" WHERE "Email" = 'test@digitek.shop';
        
        -- حذف User
        DELETE FROM "Identity"."Users" WHERE "Id" = v_user_id;
        
        RAISE NOTICE 'User and related data deleted successfully.';
    ELSE
        RAISE NOTICE 'User not found.';
    END IF;
END $$;

-- حذف تمام OTPهای منقضی شده (Maintenance)
DELETE FROM "Identity"."OneTimePasswords"
WHERE "ExpiresAtUtc" < NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day';

-- حذف RefreshTokenهای منقضی شده (Maintenance)
DELETE FROM "Identity"."RefreshTokens"
WHERE "ExpiresAtUtc" < NOW() AT TIME ZONE 'UTC' - INTERVAL '7 days';

-- حذف LoginAttempts قدیمی (Maintenance)
DELETE FROM "Identity"."LoginAttempts"
WHERE "Timestamp" < NOW() AT TIME ZONE 'UTC' - INTERVAL '30 days';

-- ========================================
-- 🔍 DEBUGGING QUERIES
-- ========================================

-- چک کردن Index Usage (Performance)
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan AS IndexScans,
    idx_tup_read AS TuplesRead,
    idx_tup_fetch AS TuplesFetched
FROM pg_stat_user_indexes
WHERE schemaname = 'Identity'
ORDER BY idx_scan DESC;

-- چک کردن Table Sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS TotalSize,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) AS TableSize,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename)) AS IndexesSize
FROM pg_tables
WHERE schemaname = 'Identity'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- تعداد رکوردها در هر جدول
SELECT 
    'Users' AS Table, COUNT(*) AS RowCount FROM "Identity"."Users"
UNION ALL
SELECT 'RefreshTokens', COUNT(*) FROM "Identity"."RefreshTokens"
UNION ALL
SELECT 'OneTimePasswords', COUNT(*) FROM "Identity"."OneTimePasswords"
UNION ALL
SELECT 'LoginAttempts', COUNT(*) FROM "Identity"."LoginAttempts";

-- ========================================
-- 🧪 VALIDATION CHECKS
-- ========================================

-- چک کردن Constraint Violations (Test Integrity)
-- 1. RefreshTokens بدون User
SELECT COUNT(*) AS OrphanedTokens
FROM "Identity"."RefreshTokens" rt
LEFT JOIN "Identity"."Users" u ON rt."UserId" = u."Id"
WHERE u."Id" IS NULL;

-- 2. OTPs با تاریخ نامعتبر
SELECT COUNT(*) AS InvalidOTPs
FROM "Identity"."OneTimePasswords"
WHERE "CreatedAtUtc" > "ExpiresAtUtc";

-- 3. RefreshTokens با Token Reuse
SELECT 
    "UserId",
    COUNT(*) AS ActiveTokensCount
FROM "Identity"."RefreshTokens"
WHERE "RevokedAtUtc" IS NULL 
  AND "ExpiresAtUtc" > NOW() AT TIME ZONE 'UTC'
GROUP BY "UserId"
HAVING COUNT(*) > 10  -- بیشتر از 10 توکن فعال (مشکوک!)
ORDER BY ActiveTokensCount DESC;

-- ========================================
-- 📊 STATISTICS
-- ========================================

-- آمار کلی سیستم
SELECT 
    (SELECT COUNT(*) FROM "Identity"."Users") AS TotalUsers,
    (SELECT COUNT(*) FROM "Identity"."Users" WHERE "EmailVerified" = true) AS VerifiedUsers,
    (SELECT COUNT(*) FROM "Identity"."RefreshTokens" WHERE "RevokedAtUtc" IS NULL) AS ActiveTokens,
    (SELECT COUNT(*) FROM "Identity"."OneTimePasswords" WHERE "IsUsed" = false AND "ExpiresAtUtc" > NOW() AT TIME ZONE 'UTC') AS ActiveOTPs,
    (SELECT COUNT(*) FROM "Identity"."LoginAttempts" WHERE "Timestamp" > NOW() AT TIME ZONE 'UTC' - INTERVAL '24 hours') AS LoginsLast24h;

-- ========================================
-- 💡 TIPS
-- ========================================
-- 1. برای اجرای سریع در CLI:
--    psql -U postgres -d digitek_identity -f test-queries.sql
--
-- 2. برای export به CSV:
--    \copy (SELECT ...) TO '/path/to/output.csv' CSV HEADER
--
-- 3. برای تایمینگ queryها:
--    \timing on
--
-- 4. برای دیدن Execution Plan:
--    EXPLAIN ANALYZE SELECT ...
--
-- 5. برای Watch mode (هر 2 ثانیه):
--    watch -n 2 "psql -U postgres -d digitek_identity -c 'SELECT ...'"
-- ========================================

