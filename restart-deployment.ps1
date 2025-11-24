# Restart Kubernetes Deployment Script
# This script restarts a deployment in the product-assistant namespace using WSL

param(
    [Parameter(Mandatory=$false)]
    [string]$Deployment = "api-service",
    
    [Parameter(Mandatory=$false)]
    [string]$Namespace = "product-assistant",
    
    [Parameter(Mandatory=$false)]
    [string]$WslDistro = "Ubuntu-24.04"
)

Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        RESTARTING KUBERNETES DEPLOYMENT                           ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "Deployment: $Deployment" -ForegroundColor Yellow
Write-Host "Namespace:  $Namespace" -ForegroundColor Yellow
Write-Host "WSL Distro:  $WslDistro" -ForegroundColor Yellow
Write-Host ""

# Run kubectl rollout restart in WSL
Write-Host "Restarting deployment..." -ForegroundColor Green
$command = "kubectl rollout restart deployment/$Deployment -n $Namespace"
wsl -d $WslDistro -e bash -c $command

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Deployment restarted successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Checking rollout status..." -ForegroundColor Cyan
    $statusCommand = "kubectl rollout status deployment/$Deployment -n $Namespace"
    wsl -d $WslDistro -e bash -c $statusCommand
} else {
    Write-Host ""
    Write-Host "✗ Failed to restart deployment" -ForegroundColor Red
    exit 1
}


