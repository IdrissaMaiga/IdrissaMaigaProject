# Stop all port forwarding
wsl -d Ubuntu-24.04 -e bash -c "pkill kubectl"
Write-Host "✓ Stopped" -ForegroundColor Green
