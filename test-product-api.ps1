# Test Product API Endpoints Script
# This script specifically tests the Product API endpoints

param(
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "http://localhost:5000",
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "demo-user",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "password"
)

$ErrorActionPreference = "Continue"

Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        TESTING PRODUCT API ENDPOINTS                            ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

# Function to test endpoints
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int]$ExpectedStatus = 200
    )
    
    Write-Host "`n[$Method] $Name" -ForegroundColor Cyan
    Write-Host "  URL: $Url" -ForegroundColor Gray
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
            ErrorAction = "Stop"
        }
        
        if ($Body -ne $null) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-WebRequest @params
        $statusCode = $response.StatusCode
        
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  ✓ Success (Status: $statusCode)" -ForegroundColor Green
            
            $responseBody = $null
            try {
                $responseBody = $response.Content | ConvertFrom-Json
                Write-Host "  Response:" -ForegroundColor Gray
                Write-Host ($responseBody | ConvertTo-Json -Depth 5) -ForegroundColor White
            } catch {
                Write-Host "  Response: $($response.Content)" -ForegroundColor White
            }
            
            return $responseBody
        } else {
            Write-Host "  ✗ Failed (Status: $statusCode, Expected: $ExpectedStatus)" -ForegroundColor Red
            return $null
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if (-not $statusCode) { 
            if ($_.Exception.Message -match "404") { $statusCode = 404 }
            elseif ($_.Exception.Message -match "500") { $statusCode = 500 }
            elseif ($_.Exception.Message -match "401") { $statusCode = 401 }
            else { $statusCode = "Error" }
        }
        
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  ✓ Success (Status: $statusCode - Expected)" -ForegroundColor Green
            return $null
        } else {
            Write-Host "  ✗ Failed (Status: $statusCode, Expected: $ExpectedStatus)" -ForegroundColor Red
            Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
            
            # Try to get error details from response
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                $reader.Close()
                Write-Host "  Error Details: $responseBody" -ForegroundColor Yellow
            } catch {
                # Ignore if we can't read the error stream
            }
            
            return $null
        }
    }
}

# Step 1: Health Check
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "HEALTH CHECK" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$healthResponse = Test-Endpoint -Name "Health Check" -Method "GET" -Url "$BaseUrl/health"

if (-not $healthResponse) {
    Write-Host "`n❌ API is not responding. Please ensure the API service is running." -ForegroundColor Red
    Write-Host "   Try: docker-compose up" -ForegroundColor Yellow
    Write-Host "   Or check if the service is running on: $BaseUrl" -ForegroundColor Yellow
    exit 1
}

# Step 2: Authentication (if needed)
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "AUTHENTICATION" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$loginResponse = Test-Endpoint -Name "Login" -Method "POST" -Url "$BaseUrl/api/auth/login" -Body @{
    username = $Username
    password = $Password
}

$token = ""
$userId = ""

if ($loginResponse) {
    $token = $loginResponse.Token
    $userId = $loginResponse.UserId
    Write-Host "  ✓ Token obtained: $($token.Substring(0, [Math]::Min(20, $token.Length)))..." -ForegroundColor Green
    Write-Host "  ✓ User ID: $userId" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Login failed, testing without authentication..." -ForegroundColor Yellow
}

$headers = @{}
if ($token) {
    $headers["Authorization"] = "Bearer $token"
}

# Step 3: Test Product Endpoints
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "PRODUCT ENDPOINTS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Get all products
Write-Host "`n--- GET All Products ---" -ForegroundColor Magenta
$allProducts = Test-Endpoint -Name "Get All Products" -Method "GET" -Url "$BaseUrl/api/products?userId=$userId" -Headers $headers

# Search products
Write-Host "`n--- Search Products ---" -ForegroundColor Magenta
$searchResults = Test-Endpoint -Name "Search Products" -Method "GET" -Url "$BaseUrl/api/products/search?term=laptop" -Headers $headers

# Create a new product
Write-Host "`n--- CREATE Product ---" -ForegroundColor Magenta
$newProduct = @{
    name = "Test Laptop"
    description = "A test laptop product for API testing"
    price = 99999.99
    currency = "HUF"
    imageUrl = "https://example.com/laptop.jpg"
    productUrl = "https://example.com/product/laptop"
    storeName = "Test Store"
    category = "Electronics"
    userId = $userId
    scrapedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
}

$createResponse = Test-Endpoint -Name "Create Product" -Method "POST" -Url "$BaseUrl/api/products" -Headers $headers -Body $newProduct -ExpectedStatus 201

$productId = $null
if ($createResponse) {
    if ($createResponse.Id) {
        $productId = $createResponse.Id
    } elseif ($createResponse.id) {
        $productId = $createResponse.id
    }
    Write-Host "  ✓ Product created with ID: $productId" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Could not create product, using ID 1 for testing..." -ForegroundColor Yellow
    $productId = 1
}

# Get product by ID
if ($productId) {
    Write-Host "`n--- GET Product by ID ---" -ForegroundColor Magenta
    $productById = Test-Endpoint -Name "Get Product by ID" -Method "GET" -Url "$BaseUrl/api/products/$productId" -Headers $headers
}

# Update product
if ($productId) {
    Write-Host "`n--- UPDATE Product ---" -ForegroundColor Magenta
    $updatedProduct = @{
        id = $productId
        name = "Updated Test Laptop"
        description = "An updated test laptop product"
        price = 89999.99
        currency = "HUF"
        imageUrl = "https://example.com/laptop-updated.jpg"
        productUrl = "https://example.com/product/laptop"
        storeName = "Updated Store"
        category = "Electronics"
        userId = $userId
    }
    
    $updateResponse = Test-Endpoint -Name "Update Product" -Method "PUT" -Url "$BaseUrl/api/products/$productId" -Headers $headers -Body $updatedProduct
}

# Verify the product was saved by getting all products again
Write-Host "`n--- VERIFY Product Saved ---" -ForegroundColor Magenta
$verifyProducts = Test-Endpoint -Name "Get All Products (Verify)" -Method "GET" -Url "$BaseUrl/api/products?userId=$userId" -Headers $headers

if ($verifyProducts) {
    $productCount = 0
    if ($verifyProducts -is [array]) {
        $productCount = $verifyProducts.Count
    } elseif ($verifyProducts -is [PSCustomObject]) {
        $productCount = 1
    }
    
    Write-Host "  ✓ Found $productCount product(s) in database" -ForegroundColor Green
    
    if ($productId -and $productCount -gt 0) {
        $foundProduct = $null
        if ($verifyProducts -is [array]) {
            $foundProduct = $verifyProducts | Where-Object { ($_.Id -eq $productId) -or ($_.id -eq $productId) } | Select-Object -First 1
        } elseif (($verifyProducts.Id -eq $productId) -or ($verifyProducts.id -eq $productId)) {
            $foundProduct = $verifyProducts
        }
        
        if ($foundProduct) {
            Write-Host "  ✓ Created product found in database!" -ForegroundColor Green
            Write-Host "    Product Name: $($foundProduct.Name)" -ForegroundColor White
            Write-Host "    Product Price: $($foundProduct.Price) $($foundProduct.Currency)" -ForegroundColor White
        } else {
            Write-Host "  ⚠ Created product not found in results" -ForegroundColor Yellow
        }
    }
}

# Summary
Write-Host "`n╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    TEST SUMMARY                                   ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Product API testing complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Tested Endpoints:" -ForegroundColor Yellow
Write-Host "  - GET /api/products (Get all products)" -ForegroundColor White
Write-Host "  - GET /api/products/search (Search products)" -ForegroundColor White
Write-Host "  - POST /api/products (Create product)" -ForegroundColor White
Write-Host "  - GET /api/products/{id} (Get product by ID)" -ForegroundColor White
Write-Host "  - PUT /api/products/{id} (Update product)" -ForegroundColor White
Write-Host ""

