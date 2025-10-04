# DigiTekShop Redis Test Script
Write-Host "🧪 Testing Redis Integration..." -ForegroundColor Green

$baseUrl = "http://localhost:7055"
$testResults = @()

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null,
        [int]$ExpectedStatus = 200
    )
    
    try {
        Write-Host "Testing $Name..." -ForegroundColor Yellow
        
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 10
        } else {
            $jsonBody = $Body | ConvertTo-Json
            $response = Invoke-RestMethod -Uri $Url -Method Post -Body $jsonBody -ContentType "application/json" -TimeoutSec 10
        }
        
        $testResults += [PSCustomObject]@{
            Test = $Name
            Status = "✅ PASS"
            Response = $response
        }
        Write-Host "✅ $Name - PASSED" -ForegroundColor Green
        return $true
    }
    catch {
        $testResults += [PSCustomObject]@{
            Test = $Name
            Status = "❌ FAIL"
            Error = $_.Exception.Message
        }
        Write-Host "❌ $Name - FAILED: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Test-RateLimit {
    param([string]$Key, [int]$Limit)
    
    Write-Host "Testing Rate Limiting for key: $Key (limit: $Limit)..." -ForegroundColor Yellow
    
    $successCount = 0
    $rateLimitCount = 0
    
    for ($i = 1; $i -le ($Limit + 2); $i++) {
        try {
            $body = @{
                key = $Key
                limit = $Limit
                windowMinutes = 1
            }
            
            $response = Invoke-RestMethod -Uri "$baseUrl/api/cache/rate-limit-test" -Method Post -Body ($body | ConvertTo-Json) -ContentType "application/json" -TimeoutSec 5
            
            if ($response.message -eq "Request allowed") {
                $successCount++
            } else {
                $rateLimitCount++
            }
        }
        catch {
            if ($_.Exception.Response.StatusCode -eq 429) {
                $rateLimitCount++
            }
        }
        
        Start-Sleep -Milliseconds 100
    }
    
    Write-Host "Rate Limit Test Results:" -ForegroundColor Cyan
    Write-Host "  Allowed requests: $successCount" -ForegroundColor Green
    Write-Host "  Rate limited requests: $rateLimitCount" -ForegroundColor Yellow
    
    if ($successCount -eq $Limit -and $rateLimitCount -gt 0) {
        Write-Host "✅ Rate Limiting - PASSED" -ForegroundColor Green
        return $true
    } else {
        Write-Host "❌ Rate Limiting - FAILED" -ForegroundColor Red
        return $false
    }
}

# Wait for API to be ready
Write-Host "⏳ Waiting for API to be ready..." -ForegroundColor Yellow
$maxRetries = 30
$retryCount = 0

do {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -TimeoutSec 5
        Write-Host "✅ API is ready!" -ForegroundColor Green
        break
    }
    catch {
        $retryCount++
        if ($retryCount -ge $maxRetries) {
            Write-Host "❌ API is not ready after $maxRetries attempts" -ForegroundColor Red
            exit 1
        }
        Write-Host "API not ready yet, retrying... ($retryCount/$maxRetries)" -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
} while ($true)

# Run tests
Write-Host "`n🚀 Running Redis Integration Tests..." -ForegroundColor Green

# Test 1: Basic Health Check
Test-Endpoint -Name "Health Check" -Url "$baseUrl/health"

# Test 2: Cache Stats
Test-Endpoint -Name "Cache Stats" -Url "$baseUrl/api/cache/stats"

# Test 3: Cache Set
$cacheSetBody = @{
    key = "test-key-$(Get-Date -Format 'yyyyMMddHHmmss')"
    value = "Hello Redis from PowerShell!"
    ttlMinutes = 5
}
$cacheKey = $cacheSetBody.key
Test-Endpoint -Name "Cache Set" -Url "$baseUrl/api/cache/set" -Method "POST" -Body $cacheSetBody

# Test 4: Cache Get
Test-Endpoint -Name "Cache Get" -Url "$baseUrl/api/cache/get/$cacheKey"

# Test 5: Rate Limiting
Test-RateLimit -Key "test-user-$(Get-Random)" -Limit 3

# Test 6: Cache Remove
Test-Endpoint -Name "Cache Remove" -Url "$baseUrl/api/cache/$cacheKey" -Method "DELETE" -ExpectedStatus 200

# Summary
Write-Host "`n📊 Test Summary:" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan

$passedTests = $testResults | Where-Object { $_.Status -eq "✅ PASS" }
$failedTests = $testResults | Where-Object { $_.Status -eq "❌ FAIL" }

Write-Host "✅ Passed: $($passedTests.Count)" -ForegroundColor Green
Write-Host "❌ Failed: $($failedTests.Count)" -ForegroundColor Red

if ($failedTests.Count -eq 0) {
    Write-Host "`n🎉 All tests passed! Redis integration is working correctly." -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Some tests failed. Check the details above." -ForegroundColor Yellow
    
    Write-Host "`nFailed Tests:" -ForegroundColor Red
    $failedTests | ForEach-Object {
        Write-Host "  - $($_.Test): $($_.Error)" -ForegroundColor Red
    }
}

Write-Host "`n📋 Test Details:" -ForegroundColor Cyan
$testResults | Format-Table -AutoSize

Write-Host "`n🔗 Useful URLs:" -ForegroundColor Cyan
Write-Host "  API Health: $baseUrl/health" -ForegroundColor White
Write-Host "  Cache Stats: $baseUrl/api/cache/stats" -ForegroundColor White
Write-Host "  Redis Commander: http://localhost:8081" -ForegroundColor White

Write-Host "`n✨ Redis integration testing completed!" -ForegroundColor Green
