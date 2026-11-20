param(
    [string]$Region = $env:AWS_REGION,
    [string]$RepositoryPrefix = "workly",
    [string]$Tag = "",
    [switch]$IncludeFrontend,
    [switch]$SkipBuild,
    [switch]$SkipPush,
    [string]$FrontendApiUrl = $env:FRONTEND_API_URL,
    [string]$FrontendIdentityApiUrl = $env:FRONTEND_IDENTITY_API_URL,
    [string]$FrontendReservationApiUrl = $env:FRONTEND_RESERVATION_API_URL,
    [string]$FrontendRoomsApiUrl = $env:FRONTEND_ROOMS_API_URL,
    [string]$FrontendAuditApiUrl = $env:FRONTEND_AUDIT_API_URL
)

$ErrorActionPreference = "Stop"

$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Get-ValueOrDefault {
    param([string]$Value, [string]$DefaultValue)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $DefaultValue }
    return $Value
}

function Require-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Missing required command '$Name'."
    }
}

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$ArgumentList,
        [string]$WorkingDirectory,
        [string]$Name
    )

    Write-Host "==> $Name"
    Push-Location $WorkingDirectory
    try {
        & $FilePath @ArgumentList
        if ($LASTEXITCODE -ne 0) {
            throw "$Name failed with exit code $LASTEXITCODE"
        }
    } finally {
        Pop-Location
    }
}

function Invoke-Aws {
    param([string[]]$Arguments, [string]$Name = "aws $($Arguments -join ' ')")
    Invoke-Checked "aws" $Arguments $Root $Name
}

function Get-AwsText {
    param([string[]]$Arguments)
    $output = & aws @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "aws $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
    return ($output -join " ").Trim()
}

function Test-EcrRepositoryExists {
    param([string]$RepositoryName)
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & aws ecr describe-repositories `
            --repository-names $RepositoryName `
            --region $Region `
            --query "repositories[0].repositoryName" `
            --output text 1>$null 2>$null
        return $LASTEXITCODE -eq 0
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Ensure-EcrRepository {
    param([string]$RepositoryName)

    if (Test-EcrRepositoryExists $RepositoryName) {
        Write-Host "==> ECR repo exists: $RepositoryName"
        return
    }

    Invoke-Aws @(
        "ecr", "create-repository",
        "--repository-name", $RepositoryName,
        "--image-scanning-configuration", "scanOnPush=true",
        "--region", $Region
    ) "create ECR repo $RepositoryName"
}

function Login-Ecr {
    param([string]$Registry)

    Write-Host "==> Logging in to ECR $Registry"
    $password = & aws ecr get-login-password --region $Region
    if ($LASTEXITCODE -ne 0) {
        throw "aws ecr get-login-password failed with exit code $LASTEXITCODE"
    }

    $password | docker login --username AWS --password-stdin $Registry
    if ($LASTEXITCODE -ne 0) {
        throw "docker login failed with exit code $LASTEXITCODE"
    }
}

function Build-Image {
    param(
        [pscustomobject]$Service,
        [string]$ImageUri
    )

    $args = @("build", "-t", $ImageUri, "-f", $Service.Dockerfile)
    if ($Service.BuildArgs) {
        foreach ($buildArg in $Service.BuildArgs.GetEnumerator()) {
            if (-not [string]::IsNullOrWhiteSpace([string]$buildArg.Value)) {
                $args += @("--build-arg", "$($buildArg.Key)=$($buildArg.Value)")
            }
        }
    }
    $args += $Service.Context

    Invoke-Checked "docker" $args $Root "docker build $($Service.Name)"
}

function Push-Image {
    param([string]$ImageUri)
    Invoke-Checked "docker" @("push", $ImageUri) $Root "docker push $ImageUri"
}

$Region = Get-ValueOrDefault $Region "us-east-1"
$Tag = Get-ValueOrDefault $Tag (Get-Date -Format "yyyyMMdd-HHmmss")
$FrontendApiUrl = Get-ValueOrDefault $FrontendApiUrl "http://localhost:5000"
$FrontendIdentityApiUrl = Get-ValueOrDefault $FrontendIdentityApiUrl "http://localhost:5100"
$FrontendReservationApiUrl = Get-ValueOrDefault $FrontendReservationApiUrl "http://localhost:5200"
$FrontendRoomsApiUrl = Get-ValueOrDefault $FrontendRoomsApiUrl "http://localhost:5300"
$FrontendAuditApiUrl = Get-ValueOrDefault $FrontendAuditApiUrl "http://localhost:5151"

Require-Command "aws"
Require-Command "docker"

Write-Host "==> Checking AWS identity"
Invoke-Aws @("sts", "get-caller-identity", "--region", $Region) "aws sts get-caller-identity"

$AccountId = Get-AwsText @(
    "sts", "get-caller-identity",
    "--query", "Account",
    "--output", "text",
    "--region", $Region
)
$Registry = "$AccountId.dkr.ecr.$Region.amazonaws.com"

$services = @(
    [pscustomobject]@{
        Name = "company-api"
        Repository = "$RepositoryPrefix/company-api"
        Context = "company-service/backend"
        Dockerfile = "company-service/backend/workly-api/Dockerfile"
        BuildArgs = $null
    },
    [pscustomobject]@{
        Name = "identity-api"
        Repository = "$RepositoryPrefix/identity-api"
        Context = "identity-service/backend"
        Dockerfile = "identity-service/backend/IdentityService.Api/Dockerfile"
        BuildArgs = $null
    },
    [pscustomobject]@{
        Name = "reservation-service"
        Repository = "$RepositoryPrefix/reservation-service"
        Context = "reservation-service"
        Dockerfile = "reservation-service/Dockerfile"
        BuildArgs = $null
    },
    [pscustomobject]@{
        Name = "rooms-service"
        Repository = "$RepositoryPrefix/rooms-service"
        Context = "rooms-service/backend"
        Dockerfile = "rooms-service/backend/Dockerfile"
        BuildArgs = $null
    },
    [pscustomobject]@{
        Name = "audit-api"
        Repository = "$RepositoryPrefix/audit-api"
        Context = "audit-service"
        Dockerfile = "audit-service/Dockerfile"
        BuildArgs = $null
    },
    [pscustomobject]@{
        Name = "notification-worker"
        Repository = "$RepositoryPrefix/notification-worker"
        Context = "notification-service/worker"
        Dockerfile = "notification-service/worker/Dockerfile"
        BuildArgs = $null
    }
)

if ($IncludeFrontend) {
    $services += [pscustomobject]@{
        Name = "frontend"
        Repository = "$RepositoryPrefix/frontend"
        Context = "frontend/workly-front"
        Dockerfile = "frontend/workly-front/Dockerfile"
        BuildArgs = @{
            API_URL = $FrontendApiUrl
            IDENTITY_API_URL = $FrontendIdentityApiUrl
            RESERVATION_API_URL = $FrontendReservationApiUrl
            ROOMS_API_URL = $FrontendRoomsApiUrl
            AUDIT_API_URL = $FrontendAuditApiUrl
        }
    }
}

foreach ($service in $services) {
    Ensure-EcrRepository $service.Repository
}

if (-not $SkipPush) {
    Login-Ecr $Registry
}

$manifest = @()
foreach ($service in $services) {
    $imageUri = "$Registry/$($service.Repository):$Tag"
    if (-not $SkipBuild) {
        Build-Image $service $imageUri
    }
    if (-not $SkipPush) {
        Push-Image $imageUri
    }

    $manifest += [pscustomobject]@{
        name = $service.Name
        repository = $service.Repository
        image = $imageUri
    }
}

$outDir = Join-Path $Root ".run"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$manifestPath = Join-Path $outDir "ecr-images.json"
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host ""
Write-Host "ECR build/push complete."
Write-Host "Region:   $Region"
Write-Host "Account:  $AccountId"
Write-Host "Tag:      $Tag"
Write-Host "Manifest: $manifestPath"
$manifest | Format-Table -AutoSize
