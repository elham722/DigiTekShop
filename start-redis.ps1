# DigiTekShop Redis Setup Script
Write-Host "🚀 Starting Redis for DigiTekShop..." -ForegroundColor Green

# Check if Docker is running
try {
    docker --version | Out-Null
    Write-Host "✅ Docker found" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker not found. Please install Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Start Redis with Docker Compose
Write-Host "📦 Starting Redis container..." -ForegroundColor Yellow
docker compose up -d

# Wait for Redis to start
Write-Host "⏳ Waiting for Redis to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Test Redis connection
Write-Host "🔍 Testing Redis connection..." -ForegroundColor Yellow
$containerName = (docker ps --format "table {{.Names}}" | Select-String "redis").ToString().Trim()
$result = docker exec $containerName redis-cli ping

if ($result -eq "PONG") {
    Write-Host "✅ Redis is running successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🌐 Access URLs:" -ForegroundColor Cyan
    Write-Host "   Redis: localhost:6379" -ForegroundColor White
    Write-Host "   Redis Commander: http://localhost:8081" -ForegroundColor White
    Write-Host ""
    Write-Host "📋 Next steps:" -ForegroundColor Cyan
    Write-Host "   1. Run the API: dotnet run --project DigiTekShop.API" -ForegroundColor White
    Write-Host "   2. Test health check: http://localhost:7055/health/redis" -ForegroundColor White
    Write-Host "   3. Test general health: http://localhost:7055/health" -ForegroundColor White
} else {
    Write-Host "❌ Redis connection failed!" -ForegroundColor Red
    Write-Host "Check Docker logs: docker logs $containerName" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🛑 To stop Redis: docker compose down" -ForegroundColor Yellow
