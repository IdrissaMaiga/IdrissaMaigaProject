# Test All API Endpoints Script
# This script tests all endpoints of the Product Assistant API

param(
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "http://localhost:8080",
    
    [Parameter(Mandatory=$false)]
    [string]$AiServiceUrl = "http://localhost:8081",
    
    [Parameter(Mandatory=$false)]
    [string]$ScrapingServiceUrl = "http://localhost:8082",
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "demo-user",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "password",
    
    [Parameter(Mandatory=$false)]
    [string]$ApiKey = "AIzaSyDk4sifW4idrGAJW7emWFS23ziDKcW6X4k"
)

$ErrorActionPreference = "Continue"
$script:results = @()

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
        
        # Check if status code matches expected status
        if ($statusCode -eq $ExpectedStatus) {
        Write-Host "  ✓ Success (Status: $statusCode)" -ForegroundColor Green
        
        $responseBody = $null
        try {
            $responseBody = $response.Content | ConvertFrom-Json
        } catch {
            $responseBody = $response.Content
        }
        
        $script:results += [PSCustomObject]@{
            Name = $Name
            Method = $Method
            Url = $Url
            Status = "Success"
            StatusCode = $statusCode
        }
        return $responseBody
        } else {
            # Status code doesn't match expected - treat as failure
            Write-Host "  ✗ Failed (Status: $statusCode, Expected: $ExpectedStatus)" -ForegroundColor Red
            $script:results += [PSCustomObject]@{
                Name = $Name
                Method = $Method
                Url = $Url
                Status = "Failed"
                StatusCode = $statusCode
                Error = "Expected status $ExpectedStatus but got $statusCode"
            }
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
        
        # Check if the error status code matches expected status (e.g., 404 for deleted resource)
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  ✓ Success (Status: $statusCode - Expected)" -ForegroundColor Green
            $script:results += [PSCustomObject]@{
                Name = $Name
                Method = $Method
                Url = $Url
                Status = "Success"
                StatusCode = $statusCode
            }
            return $null
        } else {
            Write-Host "  ✗ Failed (Status: $statusCode, Expected: $ExpectedStatus)" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        
        $script:results += [PSCustomObject]@{
            Name = $Name
            Method = $Method
            Url = $Url
            Status = "Failed"
            StatusCode = $statusCode
            Error = $_.Exception.Message
        }
        return $null
        }
    }
}

Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        TESTING PRODUCT ASSISTANT API ENDPOINTS                   ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "AI Service URL: $AiServiceUrl" -ForegroundColor Yellow
Write-Host "Scraping Service URL: $ScrapingServiceUrl" -ForegroundColor Yellow
Write-Host ""

# Step 1: Health Checks
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "HEALTH CHECKS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Test-Endpoint -Name "Health Check" -Method "GET" -Url "$BaseUrl/health"
Test-Endpoint -Name "Readiness Check" -Method "GET" -Url "$BaseUrl/health/ready"

# Step 2: Authentication
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
    Write-Host "  Token obtained: $($token.Substring(0, [Math]::Min(20, $token.Length)))..." -ForegroundColor Green
    Write-Host "  User ID: $userId" -ForegroundColor Green
}

$headers = @{
    "Authorization" = "Bearer $token"
}

Test-Endpoint -Name "Validate Token" -Method "GET" -Url "$BaseUrl/api/auth/validate?token=$token"

# Step 3: Conversation Endpoints
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "CONVERSATION ENDPOINTS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Get all conversations for the user
$conversationsResponse = Test-Endpoint -Name "Get Conversations" -Method "GET" -Url "$BaseUrl/api/conversations?userId=$userId" -Headers $headers

$conversationId = $null
$testConversationId = $null

# Always create a new conversation for testing to ensure it exists
$createConvoResponse = Test-Endpoint -Name "Create Conversation" -Method "POST" -Url "$BaseUrl/api/conversations" -Headers $headers -Body @{
    userId = $userId
    title = "Test Conversation"
} -ExpectedStatus 201

$conversationId = $null
if ($createConvoResponse) {
    $conversationId = $createConvoResponse.id
    Write-Host "  Created new conversation ID: $conversationId" -ForegroundColor Green
    # Delay to ensure conversation is fully saved and available
    Start-Sleep -Seconds 1
} elseif ($conversationsResponse -and $conversationsResponse.Count -gt 0) {
    # Fallback to existing conversation if creation failed
    $conversationId = $conversationsResponse[0].id
    Write-Host "  Using existing conversation ID: $conversationId" -ForegroundColor Green
}

# Test conversation operations if we have a conversation ID
if ($conversationId) {
    # Get conversation by ID - use the one we just created
    Test-Endpoint -Name "Get Conversation by ID" -Method "GET" -Url "$BaseUrl/api/conversations/$conversationId" -Headers $headers
    
    # Get conversation messages
    Test-Endpoint -Name "Get Conversation Messages" -Method "GET" -Url "$BaseUrl/api/conversations/$conversationId/messages?limit=10" -Headers $headers
    
    # Update conversation title
    Test-Endpoint -Name "Update Conversation Title" -Method "PUT" -Url "$BaseUrl/api/conversations/$conversationId/title" -Headers $headers -Body @{
        title = "Updated Test Conversation"
    } -ExpectedStatus 204
    
    # Save a test message to the conversation
    Test-Endpoint -Name "Save Message to Conversation" -Method "POST" -Url "$BaseUrl/api/conversations/$conversationId/messages" -Headers $headers -Body @{
        userId = $userId
        message = "This is a test user message"
        response = ""
        isUserMessage = $true
    } -ExpectedStatus 201
    
    # Save an assistant response message
    Test-Endpoint -Name "Save Assistant Response" -Method "POST" -Url "$BaseUrl/api/conversations/$conversationId/messages" -Headers $headers -Body @{
        userId = $userId
        message = ""
        response = "This is a test assistant response"
        isUserMessage = $false
    } -ExpectedStatus 201
    
    # Verify messages were saved
    Test-Endpoint -Name "Get Conversation Messages (After Save)" -Method "GET" -Url "$BaseUrl/api/conversations/$conversationId/messages?limit=10" -Headers $headers
    
    # Create a test conversation for deletion test
    $testConvoResponse = Test-Endpoint -Name "Create Test Conversation for Deletion" -Method "POST" -Url "$BaseUrl/api/conversations" -Headers $headers -Body @{
        userId = $userId
        title = "Conversation to Delete"
    } -ExpectedStatus 201
    
    if ($testConvoResponse) {
        $testConversationId = $testConvoResponse.id
        Write-Host "  Created test conversation for deletion: $testConversationId" -ForegroundColor Green
        # Small delay to ensure conversation is fully saved
        Start-Sleep -Milliseconds 500
        
        # Delete the test conversation
        Test-Endpoint -Name "Delete Conversation" -Method "DELETE" -Url "$BaseUrl/api/conversations/$testConversationId" -Headers $headers -ExpectedStatus 204
        
        # Small delay to ensure deletion is complete
        Start-Sleep -Milliseconds 500
        
        # Verify it was deleted (should return 404)
        Test-Endpoint -Name "Verify Conversation Deleted" -Method "GET" -Url "$BaseUrl/api/conversations/$testConversationId" -Headers $headers -ExpectedStatus 404
    }
}

# Step 4: Chat Endpoints
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "CHAT ENDPOINTS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$encodedApiKey = [System.Uri]::EscapeDataString($ApiKey)
Test-Endpoint -Name "Test API Key" -Method "GET" -Url "$BaseUrl/api/chat/test-apikey?apiKey=$encodedApiKey"

$chatResponse = Test-Endpoint -Name "Send Chat Message" -Method "POST" -Url "$BaseUrl/api/chat/message" -Headers $headers -Body @{
    message = "Find me a good laptop under 1000 euros"
    userId = $userId
    apiKey = $ApiKey
    conversationId = $conversationId
}

Test-Endpoint -Name "Search Products (Legacy)" -Method "POST" -Url "$BaseUrl/api/chat/search" -Body @{
    query = "laptop"
    apiKey = $ApiKey
}

# Step 5: Products Endpoints
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "PRODUCTS ENDPOINTS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Test-Endpoint -Name "Get All Products" -Method "GET" -Url "$BaseUrl/api/products?userId=$userId" -Headers $headers
Test-Endpoint -Name "Search Products" -Method "GET" -Url "$BaseUrl/api/products/search?term=laptop" -Headers $headers

$createProductResponse = Test-Endpoint -Name "Create Product" -Method "POST" -Url "$BaseUrl/api/products" -Headers $headers -Body @{
    name = "Test Product"
    price = 99.99
    description = "Test product description"
    url = "https://example.com/product"
    imageUrl = "https://example.com/image.jpg"
    userId = $userId
} -ExpectedStatus 201

$productId = 1
if ($createProductResponse -and $createProductResponse.Id) {
    $productId = $createProductResponse.Id
}

Test-Endpoint -Name "Get Product by ID" -Method "GET" -Url "$BaseUrl/api/products/$productId" -Headers $headers

Test-Endpoint -Name "Update Product" -Method "PUT" -Url "$BaseUrl/api/products/$productId" -Headers $headers -Body @{
    id = $productId
    name = "Updated Product"
    price = 149.99
    description = "Updated description"
    url = "https://example.com/product"
    imageUrl = "https://example.com/image.jpg"
    userId = $userId
}

# Step 6: Product Comparisons
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "PRODUCT COMPARISONS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Test-Endpoint -Name "Get User Comparisons" -Method "GET" -Url "$BaseUrl/api/products/comparisons?userId=$userId" -Headers $headers

# Step 7: AI Service
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "AI SERVICE" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Test-Endpoint -Name "AI Chat" -Method "POST" -Url "$AiServiceUrl/api/ai/chat" -Body @{
    message = "What are the best laptops for programming?"
    userId = $userId
    apiKey = $ApiKey
    conversationId = $null
}

Test-Endpoint -Name "AI Search Products" -Method "POST" -Url "$AiServiceUrl/api/ai/search" -Body @{
    query = "gaming laptop"
}

# Step 8: Scraping Service
Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "SCRAPING SERVICE" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Test-Endpoint -Name "Scraping Search Products" -Method "POST" -Url "$ScrapingServiceUrl/api/scraping/search" -Body @{
    searchTerm = "laptop"
}

Test-Endpoint -Name "Get Product Details" -Method "POST" -Url "$ScrapingServiceUrl/api/scraping/details" -Body @{
    productUrl = "https://www.arukereso.hu/laptop-c3141/"
}

Test-Endpoint -Name "Scrape Category" -Method "POST" -Url "$ScrapingServiceUrl/api/scraping/category" -Body @{
    categoryUrl = "https://www.arukereso.hu/laptop-c3141/"
}

# Summary
Write-Host "`n╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    TEST SUMMARY                                   ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$successCount = ($script:results | Where-Object { $_.Status -eq "Success" }).Count
$failCount = ($script:results | Where-Object { $_.Status -eq "Failed" }).Count
$totalCount = $script:results.Count

Write-Host "Total Tests: $totalCount" -ForegroundColor Yellow
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($failCount -gt 0) {
    Write-Host "Failed Endpoints:" -ForegroundColor Red
    $script:results | Where-Object { $_.Status -eq "Failed" } | ForEach-Object {
        Write-Host "  ✗ $($_.Name) - $($_.Url)" -ForegroundColor Red
        if ($_.Error) {
            Write-Host "    Error: $($_.Error)" -ForegroundColor Gray
        }
    }
}

Write-Host "`n✅ Testing complete!" -ForegroundColor Green

