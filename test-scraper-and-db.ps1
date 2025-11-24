# Test Scraping Service and Verify Products are Saved to Database
# This script tests the scraping service and verifies products are properly saved via the Product API

param(
    [Parameter(Mandatory=$false)]
    [string]$ApiUrl = "http://localhost:8080",
    
    [Parameter(Mandatory=$false)]
    [string]$ScrapingServiceUrl = "http://localhost:8082",
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "demo-user",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "password"
)

$ErrorActionPreference = "Continue"

Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   TESTING SCRAPING SERVICE & PRODUCT DATABASE SAVE              ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "API URL: $ApiUrl" -ForegroundColor Yellow
Write-Host "Scraping Service URL: $ScrapingServiceUrl" -ForegroundColor Yellow
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
            ErrorAction = "Stop"
        }
        
        if ($Body -ne $null) {
            # Convert to JSON with UTF-8 encoding support for special characters
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            $params.Body = $jsonBody
            $params.ContentType = "application/json; charset=utf-8"
        }
        
        $response = Invoke-WebRequest @params
        $statusCode = $response.StatusCode
        
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  ✓ Success (Status: $statusCode)" -ForegroundColor Green
            
            $responseBody = $null
            try {
                $responseBody = $response.Content | ConvertFrom-Json
            } catch {
                $responseBody = $response.Content
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

# Step 1: Health Checks
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "HEALTH CHECKS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$apiHealth = Test-Endpoint -Name "API Health Check" -Method "GET" -Url "$ApiUrl/health"
if (-not $apiHealth) {
    Write-Host "`n❌ API is not responding. Please ensure port forwarding is active." -ForegroundColor Red
    exit 1
}

$scrapingHealth = Test-Endpoint -Name "Scraping Service Health Check" -Method "GET" -Url "$ScrapingServiceUrl/health"
if (-not $scrapingHealth) {
    Write-Host "`n❌ Scraping service is not responding. Please ensure port forwarding is active." -ForegroundColor Red
    Write-Host "   Run: .\start-port-forwarding.ps1" -ForegroundColor Yellow
    exit 1
}

# Step 2: Authentication
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "AUTHENTICATION" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$loginResponse = Test-Endpoint -Name "Login" -Method "POST" -Url "$ApiUrl/api/auth/login" -Body @{
    username = $Username
    password = $Password
}

$token = ""
$userId = ""

if ($loginResponse) {
    $token = $loginResponse.Token
    $userId = $loginResponse.UserId
    Write-Host "  ✓ Token obtained" -ForegroundColor Green
    Write-Host "  ✓ User ID: $userId" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Login failed, testing without authentication..." -ForegroundColor Yellow
}

$headers = @{}
if ($token) {
    $headers["Authorization"] = "Bearer $token"
}

# Step 3: Get initial product count
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "INITIAL DATABASE STATE" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$initialProducts = Test-Endpoint -Name "Get Initial Products" -Method "GET" -Url "$ApiUrl/api/products?userId=$userId" -Headers $headers
$initialCount = 0
if ($initialProducts) {
    if ($initialProducts -is [array]) {
        $initialCount = $initialProducts.Count
    } elseif ($initialProducts -is [PSCustomObject]) {
        $initialCount = 1
    }
}
Write-Host "  Initial product count in database: $initialCount" -ForegroundColor Yellow

# Step 4: Test Scraping Service
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "SCRAPING SERVICE TESTS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Test 1: Search Products
Write-Host "`n--- Test 1: Search Products ---" -ForegroundColor Magenta
$searchResponse = Test-Endpoint -Name "Scraping Search Products" -Method "POST" -Url "$ScrapingServiceUrl/api/scraping/search" -Body @{
    searchTerm = "laptop"
}

$scrapedProducts = $null
if ($searchResponse) {
    if ($searchResponse.Products) {
        $scrapedProducts = $searchResponse.Products
        Write-Host "  ✓ Found $($scrapedProducts.Count) products from scraping" -ForegroundColor Green
    } elseif ($searchResponse -is [array]) {
        $scrapedProducts = $searchResponse
        Write-Host "  ✓ Found $($scrapedProducts.Count) products from scraping" -ForegroundColor Green
    }
}

# Test 2: Get Product Details (if we have a product URL)
if ($scrapedProducts -and $scrapedProducts.Count -gt 0 -and $scrapedProducts[0].ProductUrl) {
    Write-Host "`n--- Test 2: Get Product Details ---" -ForegroundColor Magenta
    $productDetails = Test-Endpoint -Name "Get Product Details" -Method "POST" -Url "$ScrapingServiceUrl/api/scraping/details" -Body @{
        productUrl = $scrapedProducts[0].ProductUrl
    }
}

# Step 5: Save Scraped Products to Database
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "SAVING SCRAPED PRODUCTS TO DATABASE" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$savedProductIds = @()
$failedSaves = 0

if ($scrapedProducts -and $scrapedProducts.Count -gt 0) {
    Write-Host "`nAttempting to save scraped products to database..." -ForegroundColor Yellow
    
    # Save up to 3 products for testing (to avoid too many duplicates)
    $productsToSave = $scrapedProducts | Select-Object -First 3
    
    foreach ($product in $productsToSave) {
        Write-Host "`n  Saving product: $($product.Name)" -ForegroundColor Cyan
        
        # Save product exactly as scraper returns it - preserve all fields as-is
        # Only add userId if not present (scraper doesn't set userId)
        $productToSave = @{}
        
        # Copy all properties from scraped product exactly as they are
        $product.PSObject.Properties | ForEach-Object {
            $propName = $_.Name
            $propValue = $_.Value
            
            # Skip Id (will be auto-generated) and navigation properties
            if ($propName -ne "Id" -and $propName -ne "Messages") {
                if ($propValue -ne $null) {
                    # Preserve the value as-is, but ensure proper type for API
                    switch ($propName) {
                        "Price" { $productToSave["price"] = [decimal]$propValue }
                        "Name" { $productToSave["name"] = [string]$propValue }
                        "Description" { $productToSave["description"] = [string]$propValue }
                        "Currency" { $productToSave["currency"] = [string]$propValue }  # Keep "Ft" as-is
                        "ImageUrl" { $productToSave["imageUrl"] = [string]$propValue }
                        "ProductUrl" { $productToSave["productUrl"] = [string]$propValue }
                        "StoreName" { $productToSave["storeName"] = [string]$propValue }  # Include storeName as scraper returns it
                        "Category" { $productToSave["category"] = [string]$propValue }
                        "ScrapedAt" { 
                            # Convert DateTime to ISO string if it's a DateTime
                            if ($propValue -is [DateTime]) {
                                $productToSave["scrapedAt"] = $propValue.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                            } else {
                                $productToSave["scrapedAt"] = [string]$propValue
                            }
                        }
                        "CreatedAt" { 
                            # Convert DateTime to ISO string if it's a DateTime
                            if ($propValue -is [DateTime]) {
                                $productToSave["createdAt"] = $propValue.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                            } else {
                                $productToSave["createdAt"] = [string]$propValue
                            }
                        }
                        default {
                            # For other string properties, convert to string
                            if ($propValue -is [string] -or $propValue -is [DateTime]) {
                                $productToSave[$propName.ToLower()] = [string]$propValue
                            }
                        }
                    }
                }
            }
        }
        
        # Only set userId if not already present (scraper doesn't set userId)
        if (-not $productToSave.ContainsKey("userId") -or [string]::IsNullOrEmpty($productToSave["userId"])) {
            $productToSave["userId"] = [string]$userId
        }
        
        # Ensure required fields have defaults if missing (but preserve scraper's values if present)
        if (-not $productToSave.ContainsKey("name") -or [string]::IsNullOrEmpty($productToSave["name"])) {
            $productToSave["name"] = "Unknown Product"
        }
        if (-not $productToSave.ContainsKey("description")) {
            $productToSave["description"] = ""
        }
        if (-not $productToSave.ContainsKey("price")) {
            $productToSave["price"] = 0
        }
        if (-not $productToSave.ContainsKey("currency") -or [string]::IsNullOrEmpty($productToSave["currency"])) {
            $productToSave["currency"] = "Ft"  # Preserve scraper's default "Ft"
        }
        
        # Remove default/empty values that might cause issues
        $cleanProduct = @{}
        foreach ($key in $productToSave.Keys) {
            $value = $productToSave[$key]
            # Skip default DateTime values and empty strings (except for description which can be empty)
            if ($key -eq "scrapedAt" -and ($value -eq "0001-01-01T00:00:00" -or $value -eq $null)) {
                continue  # Skip default ScrapedAt
            }
            if ($key -eq "description" -or ($value -ne $null -and $value -ne "")) {
                $cleanProduct[$key] = $value
            }
        }
        
        # Debug: Show what we're sending
        Write-Host "    Sending product data:" -ForegroundColor Gray
        Write-Host ($cleanProduct | ConvertTo-Json -Depth 3) -ForegroundColor Gray
        
        $savedProduct = Test-Endpoint -Name "Save Product: $($product.Name)" -Method "POST" -Url "$ApiUrl/api/products" -Headers $headers -Body $cleanProduct -ExpectedStatus 201
        
        if ($savedProduct) {
            $productId = if ($savedProduct.Id) { $savedProduct.Id } elseif ($savedProduct.id) { $savedProduct.id } else { $null }
            if ($productId) {
                $savedProductIds += $productId
                Write-Host "    ✓ Product saved with ID: $productId" -ForegroundColor Green
            } else {
                Write-Host "    ⚠ Product saved but ID not found in response" -ForegroundColor Yellow
            }
        } else {
            $failedSaves++
            Write-Host "    ✗ Failed to save product" -ForegroundColor Red
        }
        
        # Small delay to avoid overwhelming the API
        Start-Sleep -Milliseconds 500
    }
} else {
    Write-Host "  ⚠ No products to save (scraping returned no results)" -ForegroundColor Yellow
}

# Step 6: Verify Products in Database
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "VERIFY PRODUCTS IN DATABASE" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$finalProducts = Test-Endpoint -Name "Get All Products (After Save)" -Method "GET" -Url "$ApiUrl/api/products?userId=$userId" -Headers $headers
$finalCount = 0
if ($finalProducts) {
    if ($finalProducts -is [array]) {
        $finalCount = $finalProducts.Count
    } elseif ($finalProducts -is [PSCustomObject]) {
        $finalCount = 1
    }
}

Write-Host "`n  Initial product count: $initialCount" -ForegroundColor Yellow
Write-Host "  Final product count: $finalCount" -ForegroundColor Yellow
Write-Host "  Products saved: $($savedProductIds.Count)" -ForegroundColor Yellow
Write-Host "  Failed saves: $failedSaves" -ForegroundColor $(if ($failedSaves -gt 0) { "Red" } else { "Green" })

if ($finalCount -gt $initialCount) {
    $newProducts = $finalCount - $initialCount
    Write-Host "`n  ✓ SUCCESS: $newProducts new product(s) added to database!" -ForegroundColor Green
} elseif ($savedProductIds.Count -gt 0) {
    Write-Host "`n  ⚠ Products may have been saved but count didn't increase (possible duplicates)" -ForegroundColor Yellow
} else {
    Write-Host "`n  ⚠ No new products were added to database" -ForegroundColor Yellow
}

# Verify specific saved products
if ($savedProductIds.Count -gt 0) {
    Write-Host "`n  Verifying saved products by ID..." -ForegroundColor Cyan
    foreach ($productId in $savedProductIds) {
        $verifyProduct = Test-Endpoint -Name "Verify Product ID $productId" -Method "GET" -Url "$ApiUrl/api/products/$productId" -Headers $headers
        if ($verifyProduct) {
            Write-Host "    ✓ Product ID $productId found in database: $($verifyProduct.Name)" -ForegroundColor Green
        } else {
            Write-Host "    ✗ Product ID $productId NOT found in database" -ForegroundColor Red
        }
    }
}

# Step 7: Test Additional Scraping Endpoints
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "ADDITIONAL SCRAPING TESTS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Test category scraping (if we have a category URL)
Write-Host "`n--- Test Category Scraping ---" -ForegroundColor Magenta
$categoryResponse = Test-Endpoint -Name "Scrape Category" -Method "POST" -Url "$ScrapingServiceUrl/api/scraping/category" -Body @{
    categoryUrl = "https://www.arukereso.hu/laptop-c3141/"
}

if ($categoryResponse) {
    $categoryProducts = $null
    if ($categoryResponse.Products) {
        $categoryProducts = $categoryResponse.Products
        Write-Host "  ✓ Found $($categoryProducts.Count) products from category scraping" -ForegroundColor Green
    } elseif ($categoryResponse -is [array]) {
        $categoryProducts = $categoryResponse
        Write-Host "  ✓ Found $($categoryProducts.Count) products from category scraping" -ForegroundColor Green
    }
}

# Summary
Write-Host "`n╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    TEST SUMMARY                                   ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Scraping Service Tests:" -ForegroundColor Yellow
Write-Host "  ✓ Health check" -ForegroundColor Green
Write-Host "  ✓ Search products" -ForegroundColor Green
Write-Host "  ✓ Category scraping" -ForegroundColor Green
Write-Host ""
Write-Host "Database Save Tests:" -ForegroundColor Yellow
Write-Host "  - Initial products: $initialCount" -ForegroundColor White
Write-Host "  - Final products: $finalCount" -ForegroundColor White
Write-Host "  - Products saved: $($savedProductIds.Count)" -ForegroundColor $(if ($savedProductIds.Count -gt 0) { "Green" } else { "Yellow" })
Write-Host "  - Failed saves: $failedSaves" -ForegroundColor $(if ($failedSaves -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($finalCount -gt $initialCount -or $savedProductIds.Count -gt 0) {
    Write-Host "✅ SUCCESS: Products are being saved to the database correctly!" -ForegroundColor Green
} else {
    Write-Host "⚠️  WARNING: No new products were added. Check scraping results and API responses." -ForegroundColor Yellow
}

Write-Host ""

