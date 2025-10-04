@echo off
echo 🚀 Starting Redis for DigiTekShop...

REM Check if Docker is available
docker --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker not found. Please install Docker Desktop first.
    pause
    exit /b 1
)

echo ✅ Docker found
echo 📦 Starting Redis container...

REM Start Redis with Docker Compose
docker compose up -d

REM Wait for Redis to start
echo ⏳ Waiting for Redis to start...
timeout /t 5 /nobreak >nul

REM Test Redis connection
echo 🔍 Testing Redis connection...
for /f "tokens=*" %%i in ('docker ps --format "table {{.Names}}" ^| findstr redis') do set containerName=%%i

docker exec %containerName% redis-cli ping
if errorlevel 1 (
    echo ❌ Redis connection failed!
    echo Check Docker logs: docker logs %containerName%
) else (
    echo ✅ Redis is running successfully!
    echo.
    echo 🌐 Access URLs:
    echo    Redis: localhost:6379
    echo    Redis Commander: http://localhost:8081
    echo.
    echo 📋 Next steps:
    echo    1. Run the API: dotnet run --project DigiTekShop.API
    echo    2. Test health check: http://localhost:7055/health/redis
    echo    3. Test general health: http://localhost:7055/health
)

echo.
echo 🛑 To stop Redis: docker compose down
pause
