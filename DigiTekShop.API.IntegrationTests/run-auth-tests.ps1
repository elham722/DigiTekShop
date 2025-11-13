#!/usr/bin/env pwsh
# اسکریپت اجرای تست‌های Auth
# استفاده: .\run-auth-tests.ps1 [-Filter <test-filter>] [-Verbose]

param(
    [string]$Filter = "FullyQualifiedName~DigiTekShop.API.IntegrationTests.Auth",
    [switch]$Verbose,
    [string]$RedisConnection = ""
)

Write-Host "🚀 DigiTekShop Auth Integration Tests" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# بررسی پیش‌نیازها
Write-Host "✓ بررسی پیش‌نیازها..." -ForegroundColor Yellow

# چک کردن Docker (برای Testcontainers)
$dockerRunning = $false
try {
    $null = docker ps 2>&1
    if ($LASTEXITCODE -eq 0) {
        $dockerRunning = $true
        Write-Host "  ✓ Docker در حال اجرا است" -ForegroundColor Green
    }
} catch {
    Write-Host "  ⚠ Docker در دسترس نیست - از localhost:6379 استفاده می‌شود" -ForegroundColor Yellow
}

# چک کردن Redis
if ($RedisConnection) {
    Write-Host "  ✓ استفاده از Redis خارجی: $RedisConnection" -ForegroundColor Green
    $env:TEST_REDIS = $RedisConnection
} elseif (-not $dockerRunning) {
    Write-Host "  ⚠ اطمینان حاصل کنید Redis روی localhost:6379 در حال اجرا است" -ForegroundColor Yellow
    Write-Host "    یا از دستور زیر استفاده کنید:" -ForegroundColor Gray
    Write-Host "    docker run -d -p 6379:6379 redis:7-alpine" -ForegroundColor Gray
}

# چک کردن SQL LocalDB
try {
    $localDbInfo = sqllocaldb info mssqllocaldb 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ SQL LocalDB در دسترس است" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ SQL LocalDB یافت نشد - ممکن است نیاز به نصب داشته باشید" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ⚠ نمی‌توان وضعیت SQL LocalDB را بررسی کرد" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🧪 اجرای تست‌ها..." -ForegroundColor Yellow

# ساخت دستور dotnet test
$testCommand = "dotnet test --filter `"$Filter`""

if ($Verbose) {
    $testCommand += " --logger `"console;verbosity=detailed`""
} else {
    $testCommand += " --logger `"console;verbosity=normal`""
}

# نمایش دستور
Write-Host "  دستور: $testCommand" -ForegroundColor Gray
Write-Host ""

# اجرای تست‌ها
$startTime = Get-Date
Invoke-Expression $testCommand
$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host ""
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ تمام تست‌ها با موفقیت اجرا شدند!" -ForegroundColor Green
} else {
    Write-Host "❌ برخی تست‌ها fail شدند." -ForegroundColor Red
    Write-Host "   برای جزئیات بیشتر از فلگ -Verbose استفاده کنید" -ForegroundColor Yellow
}

Write-Host "⏱️  مدت زمان: $($duration.TotalSeconds.ToString('F2')) ثانیه" -ForegroundColor Cyan
Write-Host ""

# پاک‌سازی
if ($env:TEST_REDIS) {
    Remove-Item Env:\TEST_REDIS -ErrorAction SilentlyContinue
}

exit $LASTEXITCODE

