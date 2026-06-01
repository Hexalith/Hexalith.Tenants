#Requires -Version 7.0
<#
.SYNOPSIS
    Hexalith.Tenants "Aha Moment" demo automation.

.DESCRIPTION
    Runs the add-user to remove-user reactive access proof against a running AppHost.
    The default local AppHost uses Keycloak. Supply a token from the quickstart flow
    with -Token or TOKEN. Use -HmacDevToken only when the AppHost was started with
    EnableKeycloak=false.
#>

param(
    [Parameter()]
    [string]$BaseUrl = $env:COMMANDAPI_URL,

    [Parameter()]
    [string]$SampleUrl = $env:SAMPLE_URL,

    [Parameter()]
    [string]$TenantsUrl = $env:TENANTS_URL,

    [Parameter()]
    [string]$Token = $env:TOKEN,

    [Parameter()]
    [switch]$HmacDevToken,

    [Parameter()]
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($BaseUrl) -or [string]::IsNullOrWhiteSpace($SampleUrl)) {
    Write-Host "ERROR: -BaseUrl and -SampleUrl are required." -ForegroundColor Red
    Write-Host "Find dynamic endpoints in the Aspire dashboard resources: eventstore and sample." -ForegroundColor Yellow
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Token) -and -not $HmacDevToken) {
    Write-Host "ERROR: provide TOKEN/-Token from Keycloak, or pass -HmacDevToken only when EnableKeycloak=false." -ForegroundColor Red
    exit 1
}

function ConvertTo-Base64Url {
    param([byte[]]$Bytes)
    [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function New-HmacDevToken {
    $header = @{ alg = "HS256"; typ = "JWT" } | ConvertTo-Json -Compress
    $exp = [int](Get-Date -Date (Get-Date).AddHours(8).ToUniversalTime() -UFormat %s)
    $payload = @{ sub = "admin-user"; iss = "hexalith-dev"; aud = "hexalith-eventstore"; tenants = @("system"); exp = $exp } | ConvertTo-Json -Compress

    $headerB64 = ConvertTo-Base64Url([System.Text.Encoding]::UTF8.GetBytes($header))
    $payloadB64 = ConvertTo-Base64Url([System.Text.Encoding]::UTF8.GetBytes($payload))
    $signingInput = "$headerB64.$payloadB64"
    $key = [System.Text.Encoding]::UTF8.GetBytes("DevOnlySigningKey-AtLeast32Chars!")
    $hmac = [System.Security.Cryptography.HMACSHA256]::new($key)
    $sig = ConvertTo-Base64Url($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signingInput)))
    "$signingInput.$sig"
}

function New-Ulid {
    $alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"
    $value = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $chars = New-Object char[] 26

    for ($i = 9; $i -ge 0; $i--) {
        $index = [int]($value % 32)
        $chars[$i] = $alphabet[$index]
        $value = [Math]::Floor($value / 32)
    }

    $bytes = New-Object byte[] 16
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    for ($i = 0; $i -lt 16; $i++) {
        $chars[$i + 10] = $alphabet[[int]($bytes[$i] % 32)]
    }

    -join $chars
}

if ($HmacDevToken) {
    $Token = New-HmacDevToken
}

$BaseUrl = $BaseUrl.TrimEnd('/')
$SampleUrl = $SampleUrl.TrimEnd('/')
$TenantsUrl = $TenantsUrl.TrimEnd('/')
$CommandEndpoint = "$BaseUrl/api/v1/commands"
$StatusEndpoint = "$BaseUrl/api/v1/commands/status"
$Headers = @{
    Authorization = "Bearer $Token"
}

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$tenantId = "acme-demo-$timestamp"
$userId = "jane-doe-$timestamp"
$commandsAccepted = 0
$statusSummary = [System.Collections.Generic.List[string]]::new()

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Hexalith.Tenants - Aha Moment Demo" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "EventStore: $BaseUrl" -ForegroundColor Gray
Write-Host "Sample:     $SampleUrl" -ForegroundColor Gray
if (-not [string]::IsNullOrWhiteSpace($TenantsUrl)) {
    Write-Host "Tenants:    $TenantsUrl" -ForegroundColor Gray
}
Write-Host "Tenant ID:  $tenantId" -ForegroundColor Gray
Write-Host "User ID:    $userId" -ForegroundColor Gray
Write-Host ""

function Test-ServiceHealth {
    param(
        [string]$Name,
        [string]$Url
    )

    Write-Host "[Setup] Checking $Name health..." -ForegroundColor Yellow
    try {
        $null = Invoke-RestMethod -Uri "$Url/health" -Method Get -SkipCertificateCheck -TimeoutSec 5
        Write-Host "[Setup] $Name is reachable." -ForegroundColor Green
    }
    catch {
        try {
            $null = Invoke-RestMethod -Uri "$Url/alive" -Method Get -SkipCertificateCheck -TimeoutSec 5
            Write-Host "[Setup] $Name is reachable." -ForegroundColor Green
        }
        catch {
            Write-Host "ERROR: $Name is not reachable at $Url/health or $Url/alive." -ForegroundColor Red
            exit 1
        }
    }
}

function Wait-CommandStatus {
    param(
        [string]$CorrelationId,
        [string]$Label
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -le $deadline) {
        $response = $null
        try {
            $response = Invoke-RestMethod -Uri "$StatusEndpoint/$CorrelationId" -Headers $Headers -Method Get -SkipCertificateCheck -TimeoutSec 10
        }
        catch {
            Start-Sleep -Seconds 1
            continue
        }

        $status = [string]$response.status
        if ($status -eq "Completed") {
            Write-Host "  Status: Completed ($Label)" -ForegroundColor Green
            $statusSummary.Add("${Label}=Completed:$CorrelationId")
            return
        }

        if ($status -eq "Rejected") {
            $rejection = $response.rejectionEventType ?? "Rejected"
            Write-Host "  Status: Rejected ($Label) - $rejection" -ForegroundColor Yellow
            $statusSummary.Add("${Label}=Rejected:$CorrelationId")
            return
        }

        if ($status -in @("PublishFailed", "TimedOut")) {
            Write-Host "  Status: $status ($Label)" -ForegroundColor Red
            $statusSummary.Add("${Label}=${status}:$CorrelationId")
            throw "Command failed: $Label"
        }

        Start-Sleep -Seconds 1
    }

    throw "Timed out waiting for command status: $Label ($CorrelationId)"
}

function Send-Command {
    param(
        [string]$Label,
        [hashtable]$Request
    )

    Write-Host ""
    Write-Host "--- $Label ---" -ForegroundColor Cyan
    $body = $Request | ConvertTo-Json -Depth 5
    $response = Invoke-RestMethod -Uri $CommandEndpoint -Method Post -Body $body -Headers ($Headers + @{ "Content-Type" = "application/json" }) -SkipCertificateCheck -TimeoutSec 30
    $script:commandsAccepted++

    $correlationId = [string]$response.correlationId
    if ([string]::IsNullOrWhiteSpace($correlationId)) {
        throw "Command accepted but correlationId was not found in the response."
    }

    Write-Host "  202 Accepted - status: $StatusEndpoint/$correlationId" -ForegroundColor Green
    Wait-CommandStatus -CorrelationId $correlationId -Label $Label
}

function Wait-Access {
    param(
        [string]$Expected,
        [string]$Label
    )

    Write-Host ""
    Write-Host "--- $Label ---" -ForegroundColor Cyan
    Write-Host "GET $SampleUrl/access/$tenantId/$userId" -ForegroundColor Gray
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastAccess = ""

    while ((Get-Date) -le $deadline) {
        try {
            $response = Invoke-RestMethod -Uri "$SampleUrl/access/$tenantId/$userId" -Method Get -SkipCertificateCheck -TimeoutSec 10
            $lastAccess = [string]$response.access
            if ($lastAccess -eq $Expected) {
                if ($response.role) {
                    Write-Host "  Access: $lastAccess | Role: $($response.role)" -ForegroundColor Green
                }
                else {
                    Write-Host "  Access: $lastAccess | Reason: $($response.reason)" -ForegroundColor Magenta
                }

                return
            }
        }
        catch {
            $lastAccess = "unavailable"
        }

        Start-Sleep -Seconds 1
    }

    throw "Timed out waiting for access '$Expected'. Last response access='$lastAccess'."
}

Test-ServiceHealth -Name "EventStore" -Url $BaseUrl
Test-ServiceHealth -Name "Sample" -Url $SampleUrl

Send-Command -Label "Bootstrap Global Admin" -Request @{
    messageId = New-Ulid
    tenant = "system"
    domain = "global-administrators"
    aggregateId = "global-administrators"
    commandType = "BootstrapGlobalAdmin"
    payload = @{ UserId = "admin-user" }
}

Send-Command -Label "Create Tenant" -Request @{
    messageId = New-Ulid
    tenant = "system"
    domain = "tenants"
    aggregateId = $tenantId
    commandType = "CreateTenant"
    payload = @{ TenantId = $tenantId; Name = "Acme Demo Corp"; Description = "Demo tenant for aha moment" }
}

Send-Command -Label "Add User" -Request @{
    messageId = New-Ulid
    tenant = "system"
    domain = "tenants"
    aggregateId = $tenantId
    commandType = "AddUserToTenant"
    payload = @{ TenantId = $tenantId; UserId = $userId; Role = "TenantContributor" }
}

Wait-Access -Expected "granted" -Label "Verify Access Granted"

Send-Command -Label "Remove User" -Request @{
    messageId = New-Ulid
    tenant = "system"
    domain = "tenants"
    aggregateId = $tenantId
    commandType = "RemoveUserFromTenant"
    payload = @{ TenantId = $tenantId; UserId = $userId }
}

Wait-Access -Expected "denied" -Label "Verify Access Denied"

$queryEvidence = "not requested"
if (-not [string]::IsNullOrWhiteSpace($TenantsUrl)) {
    $tenantStatus = try {
        (Invoke-WebRequest -Uri "$TenantsUrl/api/tenants/$tenantId" -Headers $Headers -Method Get -SkipCertificateCheck -TimeoutSec 10).StatusCode
    }
    catch {
        $_.Exception.Response.StatusCode.value__
    }

    $auditStatus = try {
        (Invoke-WebRequest -Uri "$TenantsUrl/api/tenants/$tenantId/audit" -Headers $Headers -Method Get -SkipCertificateCheck -TimeoutSec 10).StatusCode
    }
    catch {
        $_.Exception.Response.StatusCode.value__
    }

    $queryEvidence = "tenant=$tenantStatus audit=$auditStatus"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Demo Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Commands accepted:    $commandsAccepted"
foreach ($item in $statusSummary) {
    $parts = $item.Split(":", 2)
    Write-Host "  Command status:       $($parts[0]) ($StatusEndpoint/$($parts[1]))"
}
Write-Host "  Access transition:    granted -> denied (verified)" -ForegroundColor Green
Write-Host "  Query evidence:       $queryEvidence"
Write-Host ""
Write-Host "  The sample subscribing service revoked local access via tenants.events." -ForegroundColor Yellow
Write-Host "  No Tenants/EventStore polling is used by the access endpoint." -ForegroundColor Yellow
