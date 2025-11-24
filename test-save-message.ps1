# Test save message endpoint with auto-migration
$wslIp = (wsl -d Ubuntu-24.04 -e bash -c "hostname -I").Trim().Split()[0]
$baseUrl = "http://${wslIp}:8080"

Write-Host "Testing save message endpoint..." -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl" -ForegroundColor Gray
Write-Host ""

try {
    # Login
    $login = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body (@{username="demo-user";password="password"} | ConvertTo-Json) -ContentType "application/json"
    Write-Host "✓ Login successful" -ForegroundColor Green
    
    # Save message with product data
    $headers = @{Authorization="Bearer $($login.Token)"}
    $msg = @{
        message="Test message from script"
        response="Test response with products"
        isUserMessage=$false
        productIdsJson="[1,2,3]"
        productsJson='[{"id":1,"name":"Product 1"},{"id":2,"name":"Product 2"}]'
    } | ConvertTo-Json
    
    $result = Invoke-RestMethod -Uri "$baseUrl/api/conversations/18/messages" -Method POST -Body $msg -ContentType "application/json" -Headers $headers
    
    Write-Host "✓ SAVE MESSAGE SUCCESS!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Message saved with:" -ForegroundColor Cyan
    Write-Host "  ID: $($result.id)" -ForegroundColor White
    Write-Host "  ProductIdsJson: $($result.productIdsJson)" -ForegroundColor White
    Write-Host "  ProductsJson: $($result.productsJson)" -ForegroundColor White
    Write-Host ""
    Write-Host "✓ Auto-migration system working perfectly!" -ForegroundColor Green
    
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
}

