# Update mobile app configuration with current WSL IP
$wslIp = (wsl -d Ubuntu-24.04 -e bash -c "hostname -I").Trim().Split()[0]

if (-not $wslIp) {
    Write-Host "✗ Could not get WSL IP" -ForegroundColor Red
    exit 1
}

Write-Host "Updating app configuration..." -ForegroundColor Cyan
Write-Host "WSL IP: $wslIp" -ForegroundColor Yellow

$appSettingsPath = "Mobile\ShopAssistant\appsettings.json"
$content = Get-Content $appSettingsPath -Raw | ConvertFrom-Json

$content.Services.ApiService.BaseUrl = "http://${wslIp}:8080"
$content.Services.AIService.BaseUrl = "http://${wslIp}:8081"
$content.Services.ScrapingService.BaseUrl = "http://${wslIp}:8082"

$content | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath

Write-Host "✓ Configuration updated!" -ForegroundColor Green
Write-Host ""
Write-Host "Services configured:" -ForegroundColor Cyan
Write-Host "  API:      http://${wslIp}:8080" -ForegroundColor White
Write-Host "  AI:       http://${wslIp}:8081" -ForegroundColor White
Write-Host "  Scraping: http://${wslIp}:8082" -ForegroundColor White
Write-Host ""
Write-Host "Note: For Android emulator, use http://10.0.2.2:808x instead" -ForegroundColor Gray

