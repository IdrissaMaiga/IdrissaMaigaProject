# Port forwarding - WSL kubectl with proper config
wsl -d Ubuntu-24.04 -e bash -c "pkill kubectl 2>/dev/null"
Start-Process powershell -WindowStyle Minimized -ArgumentList "-NoExit","-Command","wsl -d Ubuntu-24.04 -- kubectl port-forward -n product-assistant svc/api-service 8080:8080 --address=0.0.0.0"
Start-Process powershell -WindowStyle Minimized -ArgumentList "-NoExit","-Command","wsl -d Ubuntu-24.04 -- kubectl port-forward -n product-assistant svc/ai-service 8081:8080 --address=0.0.0.0"
Start-Process powershell -WindowStyle Minimized -ArgumentList "-NoExit","-Command","wsl -d Ubuntu-24.04 -- kubectl port-forward -n product-assistant svc/scraping-service 8082:8080 --address=0.0.0.0"
Write-Host "✓ Started - Access via WSL IP or wait 5s for Windows localhost" -ForegroundColor Green
