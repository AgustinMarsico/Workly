param(
    [string]$Region = $env:AWS_REGION,
    [string]$RoleName = "LabRole",
    [string]$FamilyPrefix = "workly",
    [string]$RepositoryPrefix = "workly",
    [string]$Tag = "latest",
    [string]$ImagesManifestPath = "",
    [string]$LogGroupPrefix = "/ecs/workly",
    [string]$Cpu = "512",
    [string]$Memory = "1024",
    [switch]$IncludeAwsCredentialEnv,
    [switch]$IncludeFrontend,

    [string]$CompanyBaseUrl = "",
    [string]$IdentityBaseUrl = "",
    [string]$ReservationBaseUrl = "",
    [string]$RoomsBaseUrl = "",
    [string]$AuditBaseUrl = "",
    [string]$InvitationBaseUrl = ""
)

$ErrorActionPreference = "Stop"

$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$RunDir = Join-Path $Root ".run"
New-Item -ItemType Directory -Force -Path $RunDir | Out-Null

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

function Get-AwsText {
    param([string[]]$Arguments)
    $output = & aws @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "aws $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
    return ($output -join " ").Trim()
}

function Invoke-Aws {
    param([string[]]$Arguments, [string]$Name = "aws $($Arguments -join ' ')")
    Write-Host "==> $Name"
    & aws @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
}

function Read-DotEnv {
    param([string]$Path)

    $values = [ordered]@{}
    if (-not (Test-Path $Path)) {
        throw "Missing env file: $Path"
    }

    foreach ($line in Get-Content $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#") -or -not $trimmed.Contains("=")) {
            continue
        }

        $idx = $trimmed.IndexOf("=")
        $key = $trimmed.Substring(0, $idx).Trim()
        $value = $trimmed.Substring($idx + 1).Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $values[$key] = $value
    }

    return $values
}

function Set-EnvValue {
    param([hashtable]$Values, [string]$Key, [string]$Value)
    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        $Values[$Key] = $Value
    }
}

function Remove-TemporaryAwsCredentialEnv {
    param([System.Collections.IDictionary]$Values)
    if ($IncludeAwsCredentialEnv) {
        return
    }

    $credentialKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($key in @("AWS_ACCESS_KEY_ID", "AWS_SECRET_ACCESS_KEY", "AWS_SESSION_TOKEN")) {
        [void]$credentialKeys.Add($key)
    }

    foreach ($key in @($Values.Keys)) {
        if ($credentialKeys.Contains([string]$key)) {
            $Values.Remove($key)
        }
    }
}

function ConvertTo-EcsEnvironment {
    param([System.Collections.IDictionary]$Values)

    $environment = @()
    foreach ($key in $Values.Keys) {
        $environment += [ordered]@{
            name = [string]$key
            value = [string]$Values[$key]
        }
    }
    return $environment
}

function Write-Utf8NoBom {
    param([string]$Path, [string]$Content)

    $encoding = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Get-AmplifyFrontendBaseUrl {
    $manifestPath = Join-Path $RunDir "amplify-frontend.json"
    if (-not (Test-Path $manifestPath)) {
        return ""
    }

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $branchName = [string]$manifest.amplify.branchName
    $defaultDomain = [string]$manifest.amplify.defaultDomain
    if ([string]::IsNullOrWhiteSpace($branchName) -or [string]::IsNullOrWhiteSpace($defaultDomain)) {
        return ""
    }

    return "https://$branchName.$defaultDomain"
}

function Ensure-LogGroup {
    param([string]$LogGroupName)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $existingLogGroup = & aws logs describe-log-groups `
            --log-group-name-prefix $LogGroupName `
            --region $Region `
            --query "logGroups[?logGroupName=='$LogGroupName'].logGroupName" `
            --output text 2>$null
        $exists = $LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace(($existingLogGroup -join " ").Trim())
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if (-not $exists) {
        Invoke-Aws @("logs", "create-log-group", "--log-group-name", $LogGroupName, "--region", $Region) "create log group $LogGroupName"
    }
}

function Get-ImageMap {
    param([string]$ManifestPath, [string]$AccountId)

    $imageMap = @{}
    if (Test-Path $ManifestPath) {
        $items = Get-Content $ManifestPath -Raw | ConvertFrom-Json
        foreach ($item in @($items)) {
            $imageMap[$item.name] = [string]$item.image
        }
    }

    return $imageMap
}

function Get-ServiceImage {
    param([string]$ServiceName, [string]$Repository)

    if ($imageMap.ContainsKey($ServiceName)) {
        return $imageMap[$ServiceName]
    }

    return "$registry/$Repository`:$Tag"
}

function Register-TaskDefinition {
    param([pscustomobject]$Service)

    $env = Read-DotEnv (Join-Path $Root $Service.EnvFile)
    Remove-TemporaryAwsCredentialEnv $env

    foreach ($override in $Service.EnvOverrides.GetEnumerator()) {
        if (-not [string]::IsNullOrWhiteSpace([string]$override.Value)) {
            $env[$override.Key] = [string]$override.Value
        }
    }

    $logGroup = "$LogGroupPrefix/$($Service.Name)"
    Ensure-LogGroup $logGroup

    $container = [ordered]@{
        name = $Service.Name
        image = $Service.Image
        essential = $true
        environment = ConvertTo-EcsEnvironment $env
        logConfiguration = [ordered]@{
            logDriver = "awslogs"
            options = [ordered]@{
                "awslogs-group" = $logGroup
                "awslogs-region" = $Region
                "awslogs-stream-prefix" = $Service.Name
            }
        }
    }

    if ($Service.ContainerPort -gt 0) {
        $container.portMappings = @(
            [ordered]@{
                containerPort = $Service.ContainerPort
                protocol = "tcp"
                appProtocol = "http"
            }
        )
    }

    $task = [ordered]@{
        family = "$FamilyPrefix-$($Service.Name)"
        taskRoleArn = $roleArn
        executionRoleArn = $roleArn
        networkMode = "awsvpc"
        requiresCompatibilities = @("FARGATE")
        cpu = $Cpu
        memory = $Memory
        runtimePlatform = [ordered]@{
            operatingSystemFamily = "LINUX"
            cpuArchitecture = "X86_64"
        }
        containerDefinitions = @($container)
    }

    $jsonPath = Join-Path $RunDir "task-definition-$($Service.Name).json"
    Write-Utf8NoBom $jsonPath ($task | ConvertTo-Json -Depth 20)

    Write-Host "==> register task definition $($task.family)"
    $taskDefinitionArn = Get-AwsText @(
        "ecs", "register-task-definition",
        "--cli-input-json", "file://$jsonPath",
        "--region", $Region,
        "--query", "taskDefinition.taskDefinitionArn",
        "--output", "text"
    )

    return [pscustomobject]@{
        name = $Service.Name
        family = $task.family
        image = $Service.Image
        taskDefinitionArn = $taskDefinitionArn
        taskDefinitionFile = $jsonPath
    }
}

$Region = Get-ValueOrDefault $Region "us-east-1"
$ImagesManifestPath = Get-ValueOrDefault $ImagesManifestPath (Join-Path $RunDir "ecr-images.json")

Require-Command "aws"

Write-Host "==> Checking AWS identity"
Invoke-Aws @("sts", "get-caller-identity", "--region", $Region) "aws sts get-caller-identity"

$accountId = Get-AwsText @("sts", "get-caller-identity", "--query", "Account", "--output", "text", "--region", $Region)
$registry = "$accountId.dkr.ecr.$Region.amazonaws.com"
$roleArn = Get-AwsText @("iam", "get-role", "--role-name", $RoleName, "--query", "Role.Arn", "--output", "text")
$imageMap = Get-ImageMap $ImagesManifestPath $accountId

$defaultCompanyBaseUrl = Get-ValueOrDefault $CompanyBaseUrl "http://workly-company-api.local"
$defaultIdentityBaseUrl = Get-ValueOrDefault $IdentityBaseUrl "http://workly-identity-api.local"
$defaultReservationBaseUrl = Get-ValueOrDefault $ReservationBaseUrl "http://workly-reservation-service.local"
$defaultRoomsBaseUrl = Get-ValueOrDefault $RoomsBaseUrl "http://workly-rooms-service.local"
$defaultFrontendBaseUrl = Get-ValueOrDefault $env:FRONTEND_BASE_URL (Get-AmplifyFrontendBaseUrl)
$defaultInvitationBaseUrl = Get-ValueOrDefault $InvitationBaseUrl ""
if ([string]::IsNullOrWhiteSpace($defaultInvitationBaseUrl) -and -not [string]::IsNullOrWhiteSpace($defaultFrontendBaseUrl)) {
    $defaultInvitationBaseUrl = "$($defaultFrontendBaseUrl.TrimEnd('/'))/invitations/accept"
}

$services = @(
    [pscustomobject]@{
        Name = "company-api"
        Repository = "$RepositoryPrefix/company-api"
        EnvFile = "company-service/backend/.env"
        Image = Get-ServiceImage "company-api" "$RepositoryPrefix/company-api"
        ContainerPort = 8080
        EnvOverrides = @{
            ASPNETCORE_URLS = "http://+:8080"
            IdentityService__BaseUrl = $defaultIdentityBaseUrl
            ReservationService__BaseUrl = $defaultReservationBaseUrl
            NEW_RELIC_APP_NAME = "workly-api-ecs"
        }
    },
    [pscustomobject]@{
        Name = "identity-api"
        Repository = "$RepositoryPrefix/identity-api"
        EnvFile = "identity-service/backend/.env"
        Image = Get-ServiceImage "identity-api" "$RepositoryPrefix/identity-api"
        ContainerPort = 8080
        EnvOverrides = @{
            ASPNETCORE_URLS = "http://+:8080"
            OrganizationService__BaseUrl = $defaultCompanyBaseUrl
            NEW_RELIC_APP_NAME = "identity-service-ecs"
        }
    },
    [pscustomobject]@{
        Name = "reservation-service"
        Repository = "$RepositoryPrefix/reservation-service"
        EnvFile = "reservation-service/.env"
        Image = Get-ServiceImage "reservation-service" "$RepositoryPrefix/reservation-service"
        ContainerPort = 8080
        EnvOverrides = @{
            ASPNETCORE_URLS = "http://+:8080"
            MainService__BaseUrl = $defaultCompanyBaseUrl
            IdentityService__BaseUrl = $defaultIdentityBaseUrl
            RoomsService__BaseUrl = $defaultRoomsBaseUrl
            NEW_RELIC_APP_NAME = "reservation-service-ecs"
        }
    },
    [pscustomobject]@{
        Name = "rooms-service"
        Repository = "$RepositoryPrefix/rooms-service"
        EnvFile = "rooms-service/backend/.env"
        Image = Get-ServiceImage "rooms-service" "$RepositoryPrefix/rooms-service"
        ContainerPort = 8080
        EnvOverrides = @{
            ASPNETCORE_URLS = "http://+:8080"
            IdentityService__BaseUrl = $defaultIdentityBaseUrl
            ReservationService__BaseUrl = $defaultReservationBaseUrl
            NEW_RELIC_APP_NAME = "rooms-service-ecs"
        }
    },
    [pscustomobject]@{
        Name = "audit-api"
        Repository = "$RepositoryPrefix/audit-api"
        EnvFile = "audit-service/.env"
        Image = Get-ServiceImage "audit-api" "$RepositoryPrefix/audit-api"
        ContainerPort = 8080
        EnvOverrides = @{
            ASPNETCORE_URLS = "http://+:8080"
            NEW_RELIC_APP_NAME = "audit-service-ecs"
        }
    },
    [pscustomobject]@{
        Name = "notification-worker"
        Repository = "$RepositoryPrefix/notification-worker"
        EnvFile = "notification-service/.env"
        Image = Get-ServiceImage "notification-worker" "$RepositoryPrefix/notification-worker"
        ContainerPort = 8080
        EnvOverrides = @{
            HEALTH_PORT = "8080"
            NEW_RELIC_APP_NAME = "notification-worker-ecs"
            INVITATION_BASE_URL = $defaultInvitationBaseUrl
        }
    }
)

if ($IncludeFrontend) {
    $services += [pscustomobject]@{
        Name = "frontend"
        Repository = "$RepositoryPrefix/frontend"
        EnvFile = "frontend/workly-front/.env"
        Image = Get-ServiceImage "frontend" "$RepositoryPrefix/frontend"
        ContainerPort = 80
        EnvOverrides = @{}
    }
}

$registered = @()
foreach ($service in $services) {
    $registered += Register-TaskDefinition $service
}

$manifestPath = Join-Path $RunDir "ecs-task-definitions.json"
Write-Utf8NoBom $manifestPath ($registered | ConvertTo-Json -Depth 6)

Write-Host ""
Write-Host "ECS task definitions registered."
Write-Host "Role:     $roleArn"
Write-Host "Manifest: $manifestPath"
$registered | Format-Table -AutoSize
