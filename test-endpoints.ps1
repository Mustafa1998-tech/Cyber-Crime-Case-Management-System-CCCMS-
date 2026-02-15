# Test various API endpoints
$headers = @{
    'Content-Type' = 'application/json'
}

# Test OPTIONS request (CORS preflight)
Write-Host "Testing OPTIONS request (CORS preflight)..."
try {
    $optionsResponse = Invoke-RestMethod -Uri "https://localhost:7261/api/v1/auth/login" -Method OPTIONS -Headers $headers
    Write-Host "OPTIONS Response: Success"
} catch {
    Write-Host "OPTIONS Error: $($_.Exception.Message)"
}

# Test non-existent endpoint
Write-Host "`nTesting non-existent endpoint..."
try {
    $notFoundResponse = Invoke-RestMethod -Uri "https://localhost:7261/api/v1/nonexistent" -Method GET -Headers $headers
    Write-Host "Response: $notFoundResponse"
} catch {
    Write-Host "Expected 404 error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
    }
}

# Test root endpoint
Write-Host "`nTesting root endpoint..."
try {
    $rootResponse = Invoke-RestMethod -Uri "https://localhost:7261/" -Method GET -Headers $headers
    Write-Host "Root Response: $rootResponse"
} catch {
    Write-Host "Root Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
    }
}

# Test OpenAPI/Swagger endpoint (if available in development)
Write-Host "`nTesting OpenAPI endpoint..."
try {
    $openApiResponse = Invoke-RestMethod -Uri "https://localhost:7261/openapi/v1.json" -Method GET -Headers $headers
    Write-Host "OpenAPI available: $($openApiResponse.openapi)"
    Write-Host "API Info: $($openApiResponse.info.title) v$($openApiResponse.info.version)"
} catch {
    Write-Host "OpenAPI Error: $($_.Exception.Message)"
}
