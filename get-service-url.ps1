# Get service URL (works with WSL port forwarding)
$wslIp = (wsl -d Ubuntu-24.04 -e bash -c "hostname -I" 2>$null).Trim().Split()[0]
if ($wslIp) {
    Write-Host "Services available at:" -ForegroundColor Green
    Write-Host "  API:      http://${wslIp}:8080" -ForegroundColor Cyan
    Write-Host "  AI:       http://${wslIp}:8081" -ForegroundColor Cyan
    Write-Host "  Scraping: http://${wslIp}:8082" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Use this in your app: http://${wslIp}:8080" -ForegroundColor Yellow
} else {
    Write-Host "WSL not available or not running" -ForegroundColor Red
}

