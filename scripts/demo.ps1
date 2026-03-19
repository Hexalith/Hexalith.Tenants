#Requires -Version 7.0
<#
.SYNOPSIS
    Hexalith.Tenants "Aha Moment" Demo — automated reactive access revocation demo.

.DESCRIPTION
    Sends the 6-step demo sequence to the running AppHost topology, demonstrating
    reactive cross-service access revocation via event-sourced tenant management.

    Prerequisites: The AppHost must be running before executing this script.
    Start it with: dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj

.PARAMETER BaseUrl
    The CommandApi base URL (e.g., https://localhost:7234). Required.
    Also accepts COMMANDAPI_URL environment variable.

.PARAMETER SampleUrl
    The Sample service base URL (e.g., https://localhost:7235). Required.
    Also accepts SAMPLE_URL environment variable.

.EXAMPLE
    ./scripts/demo.ps1 -BaseUrl https://localhost:7234 -SampleUrl https://localhost:7235
#>

param(
    [Parameter()]
    [string]$BaseUrl = $env:COMMANDAPI_URL,

    [Parameter()]
    [string]$SampleUrl = $env:SAMPLE_URL
)

$ErrorActionPreference = 'Stop'

# --- Validate required parameters ---
if ([string]::IsNullOrWhiteSpace($BaseUrl) -or [string]::IsNullOrWhiteSpace($SampleUrl)) {
    Write-Host "ERROR: --BaseUrl and --SampleUrl are required." -ForegroundColor Red
    Write-Host "Find your service URLs in the Aspire dashboard (typically https://localhost:17225)." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Example:" -ForegroundColor Yellow
    Write-Host "  ./scripts/demo.ps1 -BaseUrl https://localhost:7234 -SampleUrl https://localhost:7235" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or set environment variables:" -ForegroundColor Yellow
    Write-Host '  $env:COMMANDAPI_URL = "https://localhost:7234"' -ForegroundColor Yellow
    Write-Host '  $env:SAMPLE_URL = "https://localhost:7235"' -ForegroundColor Yellow
    exit 1
}

$BaseUrl = $BaseUrl.TrimEnd('/')
$SampleUrl = $SampleUrl.TrimEnd('/')
$CommandEndpoint = "$BaseUrl/api/v1/commands"

# --- Generate unique IDs to avoid conflicts on re-run ---
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$tenantId = "acme-demo-$timestamp"
$userId = "jane-doe-$timestamp"

# --- Generate JWT token ---
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Hexalith.Tenants - Aha Moment Demo" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "CommandApi: $BaseUrl" -ForegroundColor Gray
Write-Host "Sample:     $SampleUrl" -ForegroundColor Gray
Write-Host "Tenant ID:  $tenantId" -ForegroundColor Gray
Write-Host "User ID:    $userId" -ForegroundColor Gray
Write-Host ""

Write-Host "[Setup] Generating JWT token..." -ForegroundColor Yellow
$header = @{alg = "HS256"; typ = "JWT" } | ConvertTo-Json -Compress
$exp = [int](Get-Date -Date (Get-Date).AddHours(8).ToUniversalTime() -UFormat %s)
$payload = @{sub = "admin-user"; iss = "hexalith-dev"; aud = "hexalith-tenants"; tenants = @("system"); exp = $exp } | ConvertTo-Json -Compress

function ConvertTo-Base64Url($bytes) { [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') }

$headerB64 = ConvertTo-Base64Url([System.Text.Encoding]::UTF8.GetBytes($header))
$payloadB64 = ConvertTo-Base64Url([System.Text.Encoding]::UTF8.GetBytes($payload))
$signingInput = "$headerB64.$payloadB64"

# Dev signing key — must match the key configured in quickstart.md and AppHost dev settings.
# If you get 401 Unauthorized, verify the key has not changed in docs/quickstart.md.
$key = [System.Text.Encoding]::UTF8.GetBytes("this-is-a-development-signing-key-minimum-32-chars")
$hmac = New-Object System.Security.Cryptography.HMACSHA256(, $key)
$sig = ConvertTo-Base64Url($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signingInput)))
$token = "$signingInput.$sig"

$headers = @{
    Authorization  = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "[Setup] JWT token generated." -ForegroundColor Green
Write-Host ""

# --- Prerequisite check ---
Write-Host "[Setup] Checking CommandApi is reachable..." -ForegroundColor Yellow
try {
    $null = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get -SkipCertificateCheck -TimeoutSec 5
    Write-Host "[Setup] CommandApi is healthy." -ForegroundColor Green
}
catch {
    Write-Host "ERROR: CommandApi is not reachable at $BaseUrl/health" -ForegroundColor Red
    Write-Host "Ensure the AppHost is running:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj" -ForegroundColor Yellow
    exit 1
}

Write-Host "[Setup] Checking Sample service is reachable..." -ForegroundColor Yellow
try {
    $null = Invoke-RestMethod -Uri "$SampleUrl/health" -Method Get -SkipCertificateCheck -TimeoutSec 5
    Write-Host "[Setup] Sample service is healthy." -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Sample service is not reachable at $SampleUrl/health" -ForegroundColor Red
    exit 1
}

$commandsSent = 0
$accessVerified = $false

function Send-Command {
    param(
        [string]$StepName,
        [string]$Body
    )
    Write-Host ""
    Write-Host "--- $StepName ---" -ForegroundColor Cyan
    Write-Host "POST $CommandEndpoint" -ForegroundColor Gray
    try {
        $response = Invoke-RestMethod -Uri $CommandEndpoint -Method Post -Body $Body -Headers $headers -SkipCertificateCheck -TimeoutSec 30
        $script:commandsSent++
        Write-Host "  202 Accepted — correlationId: $($response.correlationId ?? 'unknown')" -ForegroundColor Green
        return $response
    }
    catch {
        $statusCode = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { 0 }
        Write-Host "  Response: $statusCode" -ForegroundColor Yellow
        if ($statusCode -eq 422) {
            Write-Host "  Command rejected (business rule). This may be expected on re-runs." -ForegroundColor Yellow
        }
        else {
            Write-Host "  Error: $_" -ForegroundColor Red
        }
        return $null
    }
}

# --- Step 1: Bootstrap Global Admin ---
Send-Command -StepName "Step 1: Bootstrap Global Admin" -Body (@{
        messageId   = "demo-$timestamp-01-bootstrap"
        tenant      = "system"
        domain      = "tenants"
        aggregateId = "global-administrators"
        commandType = "BootstrapGlobalAdmin"
        payload     = @{ UserId = "admin-user" }
    } | ConvertTo-Json -Depth 3)

Start-Sleep -Seconds 2

# --- Step 2: Create a Tenant ---
Send-Command -StepName "Step 2: Create Tenant '$tenantId'" -Body (@{
        messageId   = "demo-$timestamp-02-create-tenant"
        tenant      = "system"
        domain      = "tenants"
        aggregateId = $tenantId
        commandType = "CreateTenant"
        payload     = @{ TenantId = $tenantId; Name = "Acme Demo Corp"; Description = "Demo tenant for aha moment" }
    } | ConvertTo-Json -Depth 3)

Start-Sleep -Seconds 2

# --- Step 3: Add a User with TenantContributor Role ---
Send-Command -StepName "Step 3: Add User '$userId' with TenantContributor Role" -Body (@{
        messageId   = "demo-$timestamp-03-add-user"
        tenant      = "system"
        domain      = "tenants"
        aggregateId = $tenantId
        commandType = "AddUserToTenant"
        payload     = @{ TenantId = $tenantId; UserId = $userId; Role = 1 }
    } | ConvertTo-Json -Depth 3)

Start-Sleep -Seconds 2

# --- Step 4: Verify Access Granted ---
Write-Host ""
Write-Host "--- Step 4: Verify Access Granted ---" -ForegroundColor Cyan
Write-Host "GET $SampleUrl/access/$tenantId/$userId" -ForegroundColor Gray
try {
    $accessResult = Invoke-RestMethod -Uri "$SampleUrl/access/$tenantId/$userId" -Method Get -SkipCertificateCheck -TimeoutSec 10
    Write-Host "  Access: $($accessResult.access ?? 'unknown') | Role: $($accessResult.role ?? 'unknown')" -ForegroundColor Green
}
catch {
    Write-Host "  Error checking access: $_" -ForegroundColor Yellow
    Write-Host "  Event may not have propagated yet. Retrying in 3 seconds..." -ForegroundColor Yellow
    Start-Sleep -Seconds 3
    try {
        $accessResult = Invoke-RestMethod -Uri "$SampleUrl/access/$tenantId/$userId" -Method Get -SkipCertificateCheck -TimeoutSec 10
        Write-Host "  Access: $($accessResult.access ?? 'unknown') | Role: $($accessResult.role ?? 'unknown')" -ForegroundColor Green
    }
    catch {
        Write-Host "  Still unable to check access: $_" -ForegroundColor Red
    }
}

Start-Sleep -Seconds 2

# --- Step 5: Remove the User — THE AHA MOMENT ---
Send-Command -StepName "Step 5: Remove User '$userId' — THE AHA MOMENT" -Body (@{
        messageId   = "demo-$timestamp-05-remove-user"
        tenant      = "system"
        domain      = "tenants"
        aggregateId = $tenantId
        commandType = "RemoveUserFromTenant"
        payload     = @{ TenantId = $tenantId; UserId = $userId }
    } | ConvertTo-Json -Depth 3)

Start-Sleep -Seconds 2

# --- Step 6: Verify Access Denied ---
Write-Host ""
Write-Host "--- Step 6: Verify Access DENIED ---" -ForegroundColor Cyan
Write-Host "GET $SampleUrl/access/$tenantId/$userId" -ForegroundColor Gray
try {
    $accessResult = Invoke-RestMethod -Uri "$SampleUrl/access/$tenantId/$userId" -Method Get -SkipCertificateCheck -TimeoutSec 10
    $actualAccess = $accessResult.access ?? 'unknown'
    Write-Host "  Access: $actualAccess | Reason: $($accessResult.reason ?? 'unknown')" -ForegroundColor Magenta
    if ($actualAccess -eq 'denied') { $script:accessVerified = $true }
}
catch {
    Write-Host "  Error checking access: $_" -ForegroundColor Yellow
    Start-Sleep -Seconds 3
    try {
        $accessResult = Invoke-RestMethod -Uri "$SampleUrl/access/$tenantId/$userId" -Method Get -SkipCertificateCheck -TimeoutSec 10
        $actualAccess = $accessResult.access ?? 'unknown'
        Write-Host "  Access: $actualAccess | Reason: $($accessResult.reason ?? 'unknown')" -ForegroundColor Magenta
        if ($actualAccess -eq 'denied') { $script:accessVerified = $true }
    }
    catch {
        Write-Host "  Still unable to check access: $_" -ForegroundColor Red
    }
}

# --- Summary ---
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Demo Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Commands sent:        $commandsSent" -ForegroundColor White
$transitionStatus = if ($accessVerified) { 'granted -> denied (VERIFIED)' } else { 'UNVERIFIED — check logs' }
$transitionColor = if ($accessVerified) { 'Green' } else { 'Yellow' }
Write-Host "  Access transitions:   $transitionStatus" -ForegroundColor $transitionColor
Write-Host "  Demo cycle:           COMPLETED" -ForegroundColor Green
Write-Host ""
Write-Host "  The consuming service automatically revoked access" -ForegroundColor Yellow
Write-Host "  via DAPR pub/sub — no custom integration needed." -ForegroundColor Yellow
Write-Host ""
