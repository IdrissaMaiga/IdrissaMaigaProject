# Kubernetes Deployment Script for Product Assistant
# Run this script after setting up a Kubernetes cluster

Write-Host "╔═══════════════════════════════════════════════════════════════════╗"
Write-Host "║        KUBERNETES DEPLOYMENT SCRIPT                              ║"
Write-Host "╚═══════════════════════════════════════════════════════════════════╝"
Write-Host ""

# Check if kubectl is available
if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Host "✗ kubectl not found. Please install kubectl first." -ForegroundColor Red
    exit 1
}

# Check cluster connection
Write-Host "Checking Kubernetes cluster connection..."
kubectl cluster-info 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Cannot connect to Kubernetes cluster." -ForegroundColor Red
    Write-Host ""
    Write-Host "Please set up a Kubernetes cluster first:" -ForegroundColor Yellow
    Write-Host "  • Docker Desktop: Settings → Kubernetes → Enable"
    Write-Host "  • Minikube: minikube start"
    Write-Host "  • Kind: kind create cluster --name product-assistant"
    Write-Host ""
    exit 1
}

Write-Host "✓ Connected to cluster" -ForegroundColor Green
Write-Host ""

# Deploy with Kustomize
Write-Host "═══════════════════════════════════════════════════════════════════"
Write-Host "Deploying Product Assistant to Kubernetes..."
Write-Host "═══════════════════════════════════════════════════════════════════"
Write-Host ""

Write-Host "Step 1: Applying all resources with Kustomize..."
kubectl apply -k k8s/
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Deployment failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Waiting for deployments to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/api-service -n product-assistant
kubectl wait --for=condition=available --timeout=300s deployment/scraping-service -n product-assistant
kubectl wait --for=condition=available --timeout=300s deployment/ai-service -n product-assistant

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════"
Write-Host "Deployment Status"
Write-Host "═══════════════════════════════════════════════════════════════════"
Write-Host ""

Write-Host "Deployments:"
kubectl get deployments -n product-assistant

Write-Host ""
Write-Host "Services:"
kubectl get services -n product-assistant

Write-Host ""
Write-Host "Pods:"
kubectl get pods -n product-assistant

Write-Host ""
Write-Host "Ingress:"
kubectl get ingress -n product-assistant

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════════════╗"
Write-Host "║              DEPLOYMENT COMPLETE!                                 ║"
Write-Host "╚═══════════════════════════════════════════════════════════════════╝"
Write-Host ""
Write-Host "✅ Product Assistant deployed to Kubernetes!"
Write-Host ""
Write-Host "Useful commands:"
Write-Host "  • View logs: kubectl logs -n product-assistant -l app=api-service"
Write-Host "  • Scale: kubectl scale deployment api-service -n product-assistant --replicas=5"
Write-Host "  • Delete: kubectl delete -k k8s/"
Write-Host ""

