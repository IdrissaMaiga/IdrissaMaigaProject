#!/bin/bash
# Complete WSL + Minikube Setup Script for Product Assistant
# Run this script in WSL (Ubuntu-24.04)

set -e

echo "╔═══════════════════════════════════════════════════════════════════╗"
echo "║        WSL + MINIKUBE COMPLETE SETUP SCRIPT                       ║"
echo "╚═══════════════════════════════════════════════════════════════════╝"
echo ""

# Check if running in WSL
if [ ! -f /proc/version ] || ! grep -q Microsoft /proc/version; then
    echo "⚠ Warning: This script is designed for WSL. Continuing anyway..."
fi

# Step 1: Install kubectl
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 1: Installing kubectl..."
echo "═══════════════════════════════════════════════════════════════════"

if command -v kubectl &> /dev/null; then
    echo "✓ kubectl already installed"
    kubectl version --client
else
    echo "Installing kubectl..."
    # Add Kubernetes repository
    sudo apt-get update
    sudo apt-get install -y apt-transport-https ca-certificates curl gpg
    
    # Add Kubernetes signing key
    curl -fsSL https://pkgs.k8s.io/core:/stable:/v1.28/deb/Release.key | sudo gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
    
    # Add Kubernetes repository
    echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v1.28/deb/ /' | sudo tee /etc/apt/sources.list.d/kubernetes.list
    
    # Install kubectl
    sudo apt-get update
    sudo apt-get install -y kubectl
    
    echo "✓ kubectl installed"
    kubectl version --client
fi

echo ""

# Step 2: Check Docker
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 2: Checking Docker..."
echo "═══════════════════════════════════════════════════════════════════"

if command -v docker &> /dev/null && docker ps &> /dev/null; then
    echo "✓ Docker is available"
    docker --version
else
    echo "⚠ Docker is not available in WSL"
    echo ""
    echo "Please enable WSL integration in Docker Desktop:"
    echo "1. Open Docker Desktop"
    echo "2. Go to Settings > Resources > WSL Integration"
    echo "3. Enable integration for 'Ubuntu-24.04'"
    echo "4. Click 'Apply & Restart'"
    echo ""
    read -p "Press Enter after enabling Docker Desktop WSL integration..."
    
    # Verify Docker again
    if command -v docker &> /dev/null && docker ps &> /dev/null; then
        echo "✓ Docker is now available"
    else
        echo "✗ Docker is still not available. Please check Docker Desktop settings."
        exit 1
    fi
fi

echo ""

# Step 3: Install Minikube
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 3: Installing Minikube..."
echo "═══════════════════════════════════════════════════════════════════"

if command -v minikube &> /dev/null; then
    echo "✓ Minikube already installed"
    minikube version
else
    echo "Installing Minikube..."
    curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
    sudo install minikube-linux-amd64 /usr/local/bin/minikube
    rm minikube-linux-amd64
    echo "✓ Minikube installed"
    minikube version
fi

echo ""

# Step 4: Start Minikube
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 4: Starting Minikube..."
echo "═══════════════════════════════════════════════════════════════════"

if minikube status &> /dev/null; then
    echo "✓ Minikube is already running"
    minikube status
else
    echo "Starting Minikube (this may take a few minutes)..."
    # Check if running as root and use --force if needed
    if [ "$EUID" -eq 0 ]; then
        echo "Running as root, using --force flag..."
        minikube start --driver=docker --force
    else
        minikube start --driver=docker
    fi
    echo "✓ Minikube started"
fi

echo ""

# Step 5: Enable Ingress
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 5: Enabling Ingress addon..."
echo "═══════════════════════════════════════════════════════════════════"

minikube addons enable ingress
echo "✓ Ingress addon enabled"

echo ""

# Step 6: Build Docker Images
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 6: Building Docker Images in Minikube..."
echo "═══════════════════════════════════════════════════════════════════"

# Set Docker environment to Minikube
eval $(minikube docker-env)

# Get project directory
PROJECT_DIR="/mnt/c/Users/user/Asztal/IdrissaMaigaProject"

if [ ! -d "$PROJECT_DIR" ]; then
    echo "⚠ Project directory not found at: $PROJECT_DIR"
    echo "Please provide the correct path to your project:"
    read -p "Project path: " PROJECT_DIR
fi

cd "$PROJECT_DIR"

echo "Building API service image..."
docker build -f Backend/ProductAssistant.Api/Dockerfile -t product-assistant-api:latest .

echo "Building Scraping service image..."
docker build -f Backend/ProductAssistant.ScrapingService/Dockerfile -t product-assistant-scraping:latest .

echo "Building AI service image..."
docker build -f Backend/ProductAssistant.AIService/Dockerfile -t product-assistant-ai:latest .

echo "✓ All images built"
docker images | grep product-assistant

echo ""

# Step 7: Deploy to Kubernetes
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 7: Deploying to Kubernetes..."
echo "═══════════════════════════════════════════════════════════════════"

kubectl apply -k k8s/
echo "✓ Resources deployed"

echo ""

# Step 8: Wait for Pods
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 8: Waiting for pods to be ready..."
echo "═══════════════════════════════════════════════════════════════════"

kubectl wait --for=condition=ready pod --all -n product-assistant --timeout=300s || true

echo ""

# Step 9: Show Status
echo "═══════════════════════════════════════════════════════════════════"
echo "Deployment Status"
echo "═══════════════════════════════════════════════════════════════════"

echo ""
echo "Pods:"
kubectl get pods -n product-assistant -o wide

echo ""
echo "Services:"
kubectl get svc -n product-assistant

echo ""
echo "Deployments:"
kubectl get deployments -n product-assistant

echo ""
echo "╔═══════════════════════════════════════════════════════════════════╗"
echo "║              SETUP AND DEPLOYMENT COMPLETE!                      ║"
echo "╚═══════════════════════════════════════════════════════════════════╝"
echo ""
echo "✅ All services are deployed to Kubernetes!"
echo ""
echo "📋 Access Services:"
echo "   kubectl port-forward -n product-assistant svc/api-service 5000:8080"
echo "   kubectl port-forward -n product-assistant svc/scraping-service 5002:8080"
echo "   kubectl port-forward -n product-assistant svc/ai-service 5003:8080"
echo ""
echo "🔍 Useful Commands:"
echo "   kubectl get all -n product-assistant"
echo "   kubectl logs -n product-assistant -l app=api-service"
echo "   kubectl describe pod <pod-name> -n product-assistant"
echo ""

