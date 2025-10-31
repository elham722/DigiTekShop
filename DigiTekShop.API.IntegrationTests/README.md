# DigiTekShop Integration Tests - راهنمای اجرا

این پروژه شامل تست‌های Integration برای بررسی Rate Limiting با Redis است.

## روش‌های اجرا

### 🎯 روش ۱: استفاده از Redis موجود (VM/Docker خارجی)

اگر Redis شما در VM یا Docker جداگانه در حال اجرا است:

#### مرحله ۱: اطمینان از دسترسی به Redis

```bash
# در VM/Docker، redis.conf را چک کنید:
bind 0.0.0.0
protected-mode no
# یا با پسورد: requirepass <YOUR_PASSWORD>
```

```bash
# فایروال: پورت 6379 باز باشد
# تست اتصال از Host:
redis-cli -h <VM_IP> -p 6379 ping
# یا با پسورد:
redis-cli -h <VM_IP> -p 6379 -a <PASSWORD> ping
```

#### مرحله ۲: تنظیم متغیر محیطی و اجرای تست

**PowerShell (Windows):**
```powershell
# بدون پسورد
$env:TEST_REDIS = "192.168.1.100:6379"
dotnet test

# با پسورد
$env:TEST_REDIS = "192.168.1.100:6379,password=yourPassword,allowAdmin=true"
dotnet test

# با تنظیمات بیشتر
$env:TEST_REDIS = "192.168.1.100:6379,password=yourPassword,ssl=false,abortConnect=false"
dotnet test
```

**Bash/zsh (Linux/Mac):**
```bash
# بدون پسورد
export TEST_REDIS="192.168.1.100:6379"
dotnet test

# با پسورد
export TEST_REDIS="192.168.1.100:6379,password=yourPassword"
dotnet test
```

**یک‌خطی:**
```powershell
# Windows
$env:TEST_REDIS="192.168.1.100:6379"; dotnet test DigiTekShop.API.IntegrationTests

# Linux/Mac
TEST_REDIS="192.168.1.100:6379" dotnet test DigiTekShop.API.IntegrationTests
```

---

### 🐳 روش ۲: استفاده از Testcontainers (نیاز به Docker Desktop)

اگر Docker Desktop روی Host شما در حال اجرا است:

```bash
# متغیر TEST_REDIS را تنظیم نکنید
dotnet test DigiTekShop.API.IntegrationTests
```

Factory به صورت خودکار یک Redis Container موقت می‌سازد و بعد از تست حذفش می‌کند.

---

## 📊 تست‌های موجود

### 1. **WithinLimitTests** 
درخواست‌های داخل سقف - بررسی کاهش `Remaining`

### 2. **ExceedingLimitTests**
عبور از سقف - بررسی `429 Too Many Requests` و `Retry-After`

### 3. **ResetWindowTests**
ریست پنجره - بعد از پایان Window دوباره اجازه می‌دهد

### 4. **ExemptPathsTests**
مسیرهای معاف - `/health`, `/swagger`, `OPTIONS`, `HEAD`

### 5. **ConcurrencyTests**
همزمانی - بدون overcount یا خطای 5xx

### 6. **HeaderShapeTests**
شکل هدرها - بررسی وجود و صحت همه هدرهای Rate Limit

---

## 🔧 تنظیمات پیش‌فرض تست

```csharp
Limit = 10           // تعداد درخواست‌های مجاز
WindowSeconds = 60   // پنجره به ثانیه
```

می‌توانید در `ApiFactoryWithRedis.cs` تغییر دهید.

---

## 🐛 عیب‌یابی

### خطا: "Docker is either not running or misconfigured"
✅ **راه‌حل:** از روش ۱ استفاده کنید (متغیر `TEST_REDIS`)

### خطا: "Connection timeout" یا "No connection"
```bash
# چک کردن اتصال به Redis
Test-NetConnection <VM_IP> -Port 6379  # Windows
nc -zv <VM_IP> 6379                    # Linux/Mac

# تست Redis CLI
redis-cli -h <VM_IP> -p 6379 ping
```

✅ **راه‌حل:**
- فایروال VM را چک کنید
- `bind 0.0.0.0` در redis.conf
- پورت 6379 باز باشد

### خطا: "NOAUTH Authentication required"
✅ **راه‌حل:** پسورد را در connection string اضافه کنید:
```powershell
$env:TEST_REDIS = "192.168.1.100:6379,password=yourPassword"
```

### تست‌ها خیلی کند هستند
- اگر از Redis خارجی استفاده می‌کنید، latency شبکه را چک کنید
- برای سرعت بیشتر، از Redis محلی یا Testcontainers استفاده کنید

---

## 📝 نکات مهم

1. **امنیت:** در production هیچوقت Redis را بدون پسورد باز نگذارید
2. **VM Port Forwarding:** اگر از VMware/VirtualBox استفاده می‌کنید، Port Forwarding تنظیم کنید
3. **Cleanup:** تست‌ها داده‌های موقت در Redis ایجاد می‌کنند که با TTL خودکار پاک می‌شوند

---

## 🎓 مثال کامل

```powershell
# 1. چک Redis در VM
ssh user@192.168.1.100
redis-cli ping
# PONG ✓

# 2. روی Host (Windows)
cd D:\Projects\DigiTekShop
$env:TEST_REDIS = "192.168.1.100:6379"
dotnet test DigiTekShop.API.IntegrationTests --logger "console;verbosity=detailed"

# 3. مشاهده نتایج
# Passed: 27/27 ✓
```

---

## 🆘 پشتیبانی

اگر مشکلی داشتید:
1. لاگ‌های Console را چک کنید (خط "Using external Redis" یا "Started Redis container")
2. مطمئن شوید API بدون خطا کامپایل می‌شود
3. تست ساده بنویسید فقط برای چک اتصال Redis

```csharp
[Fact]
public async Task Redis_Connection_Should_Work()
{
    var factory = new ApiFactoryWithRedis();
    await factory.InitializeAsync();
    // اگر exception نزد، اتصال موفق است ✓
}
```

