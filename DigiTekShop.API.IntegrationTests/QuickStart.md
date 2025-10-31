# 🚀 Quick Start - اجرای سریع تست‌ها

## دستور سریع برای VM/Docker خارجی

```powershell
# ۱. IP ماشین مجازی یا سرور Redis خودتون رو اینجا بذارید:
$VM_IP = "192.168.1.100"  # ⬅️ تغییر بدید

# ۲. بدون پسورد:
$env:TEST_REDIS = "$VM_IP:6379"
dotnet test DigiTekShop.API.IntegrationTests

# یا با پسورد:
$env:TEST_REDIS = "$VM_IP:6379,password=yourPassword"
dotnet test DigiTekShop.API.IntegrationTests
```

## چک‌لیست قبل از اجرا ✅

### ۱. Redis در VM/Docker باید در حال اجرا باشد
```bash
# در VM
redis-cli ping
# خروجی: PONG ✓
```

### ۲. Redis باید از بیرون قابل دسترسی باشد
```bash
# در redis.conf
bind 0.0.0.0
protected-mode no
```

### ۳. پورت 6379 باز باشد
```bash
# فایروال VM
sudo ufw allow 6379/tcp  # Ubuntu
firewall-cmd --add-port=6379/tcp --permanent  # CentOS
```

### ۴. تست اتصال از Host
```powershell
# Windows
Test-NetConnection 192.168.1.100 -Port 6379

# PowerShell با redis-cli
redis-cli -h 192.168.1.100 -p 6379 ping
# خروجی: PONG ✓
```

---

## مثال‌های واقعی

### مثال ۱: VMware با IP استاتیک
```powershell
$env:TEST_REDIS = "192.168.88.130:6379"
cd D:\Projects\DigiTekShop
dotnet test DigiTekShop.API.IntegrationTests --logger "console;verbosity=normal"
```

### مثال ۲: Docker Desktop با Port Forward
```powershell
# اگر Docker با port forward روی localhost
$env:TEST_REDIS = "localhost:6379"
dotnet test DigiTekShop.API.IntegrationTests
```

### مثال ۳: با پسورد و SSL
```powershell
$env:TEST_REDIS = "192.168.1.100:6380,password=MySecurePass123,ssl=true,abortConnect=false"
dotnet test DigiTekShop.API.IntegrationTests
```

### مثال ۴: اجرای یک تست خاص
```powershell
$env:TEST_REDIS = "192.168.1.100:6379"
dotnet test DigiTekShop.API.IntegrationTests --filter "FullyQualifiedName~WithinLimitTests"
```

---

## خروجی موفق چه شکلیه؟ 📊

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
Using external Redis: 192.168.1.100:6379

Passed! - Failed:     0, Passed:    27, Skipped:     0, Total:    27, Duration: 12s
```

---

## خطاهای رایج و راه‌حل 🔧

### ❌ "No connection could be made"
```
Connection Timeout Occurred
```
**راه‌حل:**
1. `ping 192.168.1.100` ببینید VM پاسخ می‌دهد؟
2. فایروال را چک کنید
3. `bind 0.0.0.0` در redis.conf

---

### ❌ "NOAUTH Authentication required"
```
Error: NOAUTH Authentication required.
```
**راه‌حل:**
```powershell
$env:TEST_REDIS = "192.168.1.100:6379,password=yourPassword"
```

---

### ❌ "Docker is either not running"
```
Docker is either not running or misconfigured
```
**راه‌حل:** متغیر `TEST_REDIS` را تنظیم کنید! Factory به صورت خودکار از Redis شما استفاده می‌کند.

---

## دستورات مفید 🛠️

### پاک کردن داده‌های تست از Redis
```bash
# در VM
redis-cli FLUSHDB  # فقط database فعلی
redis-cli FLUSHALL # همه databases
```

### مشاهده کلیدهای Rate Limit
```bash
# در VM
redis-cli KEYS "ApiPolicy:*"
redis-cli KEYS "*Policy*"
```

### مانیتور درخواست‌های Redis
```bash
# در VM
redis-cli MONITOR
# حالا تست‌ها رو اجرا کنید و ببینید چه اتفاقی می‌افتد
```

---

## تست دستی با curl 🌐

```bash
# اجرای API
cd DigiTekShop.API
dotnet run

# در ترمینال دیگر - ارسال درخواست‌های متوالی
for i in {1..15}; do
  curl -i http://localhost:5000/api/v1/test/ping
  echo "Request $i"
done

# خروجی پس از درخواست ۱۱:
# HTTP/1.1 429 Too Many Requests
# Retry-After: 55
# X-RateLimit-Remaining: 0
```

---

## نکات نهایی 💡

1. **اولین بار:** ممکن است کمی طول بکشد (راه‌اندازی WebApplicationFactory)
2. **تست‌های همزمان:** ممکن است تا 20 ثانیه طول بکشند
3. **Clean Redis:** قبل از هر تست‌، بهتر است Redis را پاک کنید:
   ```bash
   redis-cli FLUSHDB
   ```
4. **CI/CD:** در CI می‌توانید از docker-compose استفاده کنید:
   ```yaml
   services:
     redis:
       image: redis:7-alpine
       ports:
         - "6379:6379"
   ```

---

## حالا برو تست بزن! 🎯

```powershell
# همینو کپی کن و IP خودت رو جایگذاری کن:
$env:TEST_REDIS = "192.168.1.100:6379"
dotnet test DigiTekShop.API.IntegrationTests

# موفق باشی! 🚀
```

