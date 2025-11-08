# ✅ چک‌لیست بررسی تنظیمات Docker Compose

## مشکلات برطرف شده

### ✅ 1. فایل `.env` به جای `env.example`
- **قبل**: `env_file: ./env.example`
- **بعد**: `env_file: .env`
- **نکته**: کاربر باید `env.example` را به `.env` کپی کند

### ✅ 2. ساختار Command در Redis
- **قبل**: چند خط با `>` که ممکن بود مشکل داشته باشد
- **بعد**: یک خط ساده و واضح
- **نکته**: از `$$REDIS_PASS` برای escape کردن `$` در YAML استفاده شده

### ✅ 3. فایل `.gitignore`
- اضافه شد برای محافظت از `.env`

## بررسی نهایی

### فایل‌های موجود:
- ✅ `docker-compose.yml` - تنظیمات اصلی
- ✅ `redis/conf/redis.conf` - تنظیمات Redis (بدون requirepass چون در command line می‌آید)
- ✅ `redis/healthcheck.sh` - Health check script
- ✅ `env.example` - فایل نمونه
- ✅ `.gitignore` - محافظت از `.env`

### نکات مهم:

1. **قبل از اجرا**:
   ```bash
   cp env.example .env
   # ویرایش .env و تنظیم رمزها
   ```

2. **ساختار Command Redis**:
   - از `entrypoint: ["/bin/sh", "-lc"]` استفاده می‌شود
   - `command` به صورت یک خط اجرا می‌شود
   - `$$REDIS_PASS` در YAML به `$REDIS_PASS` در shell تبدیل می‌شود

3. **Health Check**:
   - `healthcheck.sh` باید executable باشد (در container)
   - از `$REDIS_PASS` environment variable استفاده می‌کند

4. **RabbitMQ**:
   - از environment variables از `.env` استفاده می‌کند
   - Management UI روی پورت 15672 در دسترس است

## تست پیشنهادی

```bash
# 1. کپی فایل .env
cp env.example .env

# 2. بررسی syntax
docker compose config

# 3. راه‌اندازی
docker compose up -d

# 4. بررسی لاگ‌ها
docker compose logs -f

# 5. تست Redis
docker exec -it digitekshop-redis redis-cli -a "Strong#Redis_Pass123!" PING

# 6. تست RabbitMQ
docker exec -it digitekshop-rabbitmq rabbitmq-diagnostics ping
```

## مشکلات احتمالی

### اگر Redis start نشد:
- بررسی کنید که `.env` وجود دارد
- بررسی کنید که `REDIS_PASS` در `.env` تنظیم شده
- لاگ‌ها را بررسی کنید: `docker compose logs redis`

### اگر Health Check fail شد:
- بررسی کنید که `healthcheck.sh` executable است
- بررسی کنید که `REDIS_PASS` در container در دسترس است

### اگر RabbitMQ start نشد:
- بررسی کنید که `.env` وجود دارد
- بررسی کنید که `RABBIT_USER` و `RABBIT_PASS` تنظیم شده‌اند

