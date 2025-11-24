# Rebuild Docker images and redeploy to Kubernetes
# This script rebuilds the Docker images using Minikube's Docker daemon
# and restarts the deployments to use the new images

param(
    [switch]$ApiService,
    [switch]$AiService,
    [switch]$ScrapingService,
    [switch]$All
)

$ErrorActionPreference = "Stop"

Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        REBUILDING DOCKER IMAGES AND REDEPLOYING                  ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Determine which services to rebuild
if ($All) {
    $ApiService = $true
    $AiService = $true
    $ScrapingService = $true
}

if (-not ($ApiService -or $AiService -or $ScrapingService)) {
    Write-Host "No services specified. Use -ApiService, -AiService, -ScrapingService, or -All" -ForegroundColor Yellow
    exit 1
}

# Get the project path in WSL format
$projectPath = (Get-Location).Path -replace '\\', '/' -replace 'C:', '/mnt/c'

# Rebuild API Service
if ($ApiService) {
    Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "Rebuilding API Service..." -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    wsl -d Ubuntu-24.04 -e bash -c "cd '$projectPath' && eval `$(minikube -p minikube docker-env) && docker build --no-cache -t product-assistant-api:latest -f Backend/ProductAssistant.Api/Dockerfile ."
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ API Service image built successfully" -ForegroundColor Green
        Write-Host "Restarting API Service deployment..." -ForegroundColor Yellow
        wsl -d Ubuntu-24.04 -e bash -c "kubectl rollout restart deployment/api-service -n product-assistant"
        Write-Host "✓ API Service deployment restarted" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to build API Service image" -ForegroundColor Red
    }
    Write-Host ""
}

# Rebuild AI Service
if ($AiService) {
    Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "Rebuilding AI Service..." -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    wsl -d Ubuntu-24.04 -e bash -c "cd '$projectPath' && eval `$(minikube -p minikube docker-env) && docker build --no-cache -t product-assistant-ai:latest -f Backend/ProductAssistant.AIService/Dockerfile ."
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ AI Service image built successfully" -ForegroundColor Green
        Write-Host "Restarting AI Service deployment..." -ForegroundColor Yellow
        wsl -d Ubuntu-24.04 -e bash -c "kubectl rollout restart deployment/ai-service -n product-assistant"
        Write-Host "✓ AI Service deployment restarted" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to build AI Service image" -ForegroundColor Red
    }
    Write-Host ""
}

# Rebuild Scraping Service
if ($ScrapingService) {
    Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "Rebuilding Scraping Service..." -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    wsl -d Ubuntu-24.04 -e bash -c "cd '$projectPath' && eval `$(minikube -p minikube docker-env) && docker build -t product-assistant-scraping:latest -f Backend/ProductAssistant.ScrapingService/Dockerfile ."
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Scraping Service image built successfully" -ForegroundColor Green
        Write-Host "Restarting Scraping Service deployment..." -ForegroundColor Yellow
        wsl -d Ubuntu-24.04 -e bash -c "kubectl rollout restart deployment/scraping-service -n product-assistant"
        Write-Host "✓ Scraping Service deployment restarted" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to build Scraping Service image" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    REBUILD COMPLETE                               ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Waiting for deployments to be ready..." -ForegroundColor Yellow
if ($ApiService) {
    wsl -d Ubuntu-24.04 -e bash -c "kubectl rollout status deployment/api-service -n product-assistant --timeout=5m" 2>&1 | Out-Null
}
if ($AiService) {
    wsl -d Ubuntu-24.04 -e bash -c "kubectl rollout status deployment/ai-service -n product-assistant --timeout=5m" 2>&1 | Out-Null
}
if ($ScrapingService) {
    wsl -d Ubuntu-24.04 -e bash -c "kubectl rollout status deployment/scraping-service -n product-assistant --timeout=5m" 2>&1 | Out-Null
}

Write-Host ""
Write-Host "✓ All services rebuilt and redeployed!" -ForegroundColor Green
Write-Host ""
Write-Host "Note: Port forwarding may need to be restarted:" -ForegroundColor Yellow
Write-Host "  .\start-port-forwarding.ps1" -ForegroundColor Gray

