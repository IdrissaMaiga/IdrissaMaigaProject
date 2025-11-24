#!/bin/bash
# Kubernetes Deployment Script for Product Assistant (WSL/Linux)
# Run this script in WSL or Linux after setting up Minikube

set -e

echo "╔═══════════════════════════════════════════════════════════════════╗"
echo "║        KUBERNETES DEPLOYMENT SCRIPT (WSL/Linux)                   ║"
echo "╚═══════════════════════════════════════════════════════════════════╝"
echo ""

# Check if kubectl is available
if ! command -v kubectl &> /dev/null; then
    echo "✗ kubectl not found. Please install kubectl first."
    exit 1
fi

# Check if minikube is available
if ! command -v minikube &> /dev/null; then
    echo "✗ minikube not found. Please install minikube first."
    echo ""
    echo "Install Minikube:"
    echo "  curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64"
    echo "  sudo install minikube-linux-amd64 /usr/local/bin/minikube"
    exit 1
fi

# Check cluster connection
echo "Checking Kubernetes cluster connection..."
if ! kubectl cluster-info &> /dev/null; then
    echo "✗ Cannot connect to Kubernetes cluster."
    echo ""
    echo "Please set up Minikube first:"
    echo "  minikube start"
    echo "  minikube addons enable ingress"
    echo ""
    exit 1
fi

echo "✓ Connected to cluster"
echo ""

# Check if Minikube is running
if ! minikube status &> /dev/null; then
    echo "⚠ Minikube is not running. Starting Minikube..."
    minikube start
    minikube addons enable ingress
fi

# Set Docker environment to Minikube
echo "Setting Docker environment to Minikube..."
eval $(minikube docker-env)

# Deploy with Kustomize
echo "═══════════════════════════════════════════════════════════════════"
echo "Deploying Product Assistant to Kubernetes..."
echo "═══════════════════════════════════════════════════════════════════"
echo ""

echo "Step 1: Applying all resources with Kustomize..."
kubectl apply -k k8s/
if [ $? -ne 0 ]; then
    echo "✗ Deployment failed"
    exit 1
fi

echo ""
echo "Step 2: Waiting for deployments to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/api-service -n product-assistant || true
kubectl wait --for=condition=available --timeout=300s deployment/scraping-service -n product-assistant || true
kubectl wait --for=condition=available --timeout=300s deployment/ai-service -n product-assistant || true

echo ""
echo "═══════════════════════════════════════════════════════════════════"
echo "Deployment Status"
echo "═══════════════════════════════════════════════════════════════════"
echo ""

echo "Deployments:"
kubectl get deployments -n product-assistant

echo ""
echo "Services:"
kubectl get services -n product-assistant

echo ""
echo "Pods:"
kubectl get pods -n product-assistant

echo ""
echo "Ingress:"
kubectl get ingress -n product-assistant

echo ""
echo "╔═══════════════════════════════════════════════════════════════════╗"
echo "║              DEPLOYMENT COMPLETE!                                 ║"
echo "╚═══════════════════════════════════════════════════════════════════╝"
echo ""
echo "✅ Product Assistant deployed to Kubernetes!"
echo ""
echo "Useful commands:"
echo "  • View logs: kubectl logs -n product-assistant -l app=api-service"
echo "  • Scale: kubectl scale deployment api-service -n product-assistant --replicas=5"
echo "  • Port forward: kubectl port-forward -n product-assistant svc/api-service 5000:8080"
echo "  • Delete: kubectl delete -k k8s/"
echo ""

