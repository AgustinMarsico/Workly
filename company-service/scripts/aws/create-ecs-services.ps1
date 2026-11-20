param(
    [string]$Region = $env:AWS_REGION,
    [string]$ClusterName = "workly-api-cluster",
    [string]$FamilyPrefix = "workly",
    [string]$InfraManifestPath = "",
    [string]$RdsEnvPath = "",
    [string]$LogGroupPrefix = "/ecs/workly",
    [int]$DesiredCount = 1,
    [switch]$DisablePublicIp,
    [switch]$AttachPublicTargetGroups,
    [switch]$SkipRdsIngress,
    [switch]$WaitForStable
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

function Invoke-Aws {
    param([string[]]$Arguments, [string]$Name = "aws $($Arguments -join ' ')")
    Write-Host "==> $Name"
    $output = & aws @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
    foreach ($line in $output) {
        Write-Host $line
    }
}

function Invoke-AwsAllowFailure {
    param([string[]]$Arguments, [string]$Message)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        Write-Host "==> $Message"
        & aws @Arguments 1>$null 2>$null
        return $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
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

function Write-Utf8NoBom {
    param([string]$Path, [string]$Content)

    $encoding = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Read-DotEnv {
    param([string]$Path)

    $values = [ordered]@{}
    if (-not (Test-Path $Path)) {
        return $values
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

function Get-ObjectValue {
    param([object]$Object, [string]$Name)

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }
    return $property.Value
}

function Get-ServiceStatus {
    param([string]$ServiceName)

    $status = Get-AwsText @(
        "ecs", "describe-services",
        "--cluster", $ClusterName,
        "--services", $ServiceName,
        "--region", $Region,
        "--query", "services[0].status",
        "--output", "text"
    )

    if ([string]::IsNullOrWhiteSpace($status) -or $status -eq "None") {
        return ""
    }
    return $status
}

function Get-LatestTaskDefinitionArn {
    param([string]$Family)

    return Get-AwsText @(
        "ecs", "describe-task-definition",
        "--task-definition", $Family,
        "--region", $Region,
        "--query", "taskDefinition.taskDefinitionArn",
        "--output", "text"
    )
}

function New-NetworkConfiguration {
    param([string[]]$SubnetIds, [string]$SecurityGroupId)

    $assign = "ENABLED"
    if ($DisablePublicIp) {
        $assign = "DISABLED"
    }

    return "awsvpcConfiguration={subnets=[$($SubnetIds -join ',')],securityGroups=[$SecurityGroupId],assignPublicIp=$assign}"
}

function Ensure-InternalListenerAssociation {
    param(
        [string]$PathPattern,
        [string]$TargetGroupArn,
        [int]$Priority
    )

    if ([string]::IsNullOrWhiteSpace($infra.internalListenerArn)) {
        return
    }

    $conditions = "Field=path-pattern,Values=$PathPattern"
    $exitCode = Invoke-AwsAllowFailure @(
        "elbv2", "create-rule",
        "--listener-arn", $infra.internalListenerArn,
        "--priority", "$Priority",
        "--conditions", $conditions,
        "--actions", "Type=forward,TargetGroupArn=$TargetGroupArn",
        "--region", $Region
    ) "ensure internal listener association for $PathPattern"

    if ($exitCode -ne 0) {
        Write-Host "Internal listener rule for $PathPattern may already exist; continuing."
    }
}

function Ensure-DatabaseIngress {
    param([string]$RdsSecurityGroupId, [string]$TasksSecurityGroupId)

    if ([string]::IsNullOrWhiteSpace($RdsSecurityGroupId)) {
        return
    }

    foreach ($port in @(3306, 1433)) {
        $exitCode = Invoke-AwsAllowFailure @(
            "ec2", "authorize-security-group-ingress",
            "--group-id", $RdsSecurityGroupId,
            "--protocol", "tcp",
            "--port", "$port",
            "--source-group", $TasksSecurityGroupId,
            "--region", $Region
        ) "authorize RDS ingress tcp/$port from ECS tasks"

        if ($exitCode -ne 0) {
            Write-Host "RDS ingress tcp/$port from ECS tasks may already exist; continuing."
        }
    }
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

function New-LoadBalancerArg {
    param([string]$TargetGroupArn, [string]$ContainerName, [int]$ContainerPort)

    return "targetGroupArn=$TargetGroupArn,containerName=$ContainerName,containerPort=$ContainerPort"
}

function Ensure-EcsService {
    param([pscustomobject]$Service)

    $serviceName = "$FamilyPrefix-$($Service.Name)"
    $family = "$FamilyPrefix-$($Service.Name)"
    $taskDefinitionArn = Get-LatestTaskDefinitionArn $family
    $loadBalancers = @()

    Ensure-LogGroup "$LogGroupPrefix/$($Service.Name)"

    $internalTargetGroupArn = Get-ObjectValue $infra.internalTargetGroups $Service.Name
    if ([string]::IsNullOrWhiteSpace($internalTargetGroupArn)) {
        throw "Missing internal target group for $($Service.Name) in $InfraManifestPath"
    }
    if ($Service.InternalPathPattern) {
        Ensure-InternalListenerAssociation $Service.InternalPathPattern $internalTargetGroupArn $Service.InternalRulePriority
    }
    $loadBalancers += New-LoadBalancerArg $internalTargetGroupArn $Service.Name $Service.ContainerPort

    if ($AttachPublicTargetGroups) {
        $publicTargetGroupArn = Get-ObjectValue $infra.publicTargetGroups $Service.Name
        if (-not [string]::IsNullOrWhiteSpace($publicTargetGroupArn)) {
            $loadBalancers += New-LoadBalancerArg $publicTargetGroupArn $Service.Name $Service.ContainerPort
        }
    }

    $status = Get-ServiceStatus $serviceName
    if ($status -and $status -ne "INACTIVE") {
        Invoke-Aws (@(
            "ecs", "update-service",
            "--cluster", $ClusterName,
            "--service", $serviceName,
            "--task-definition", $taskDefinitionArn,
            "--desired-count", "$DesiredCount",
            "--network-configuration", $networkConfiguration,
            "--load-balancers"
        ) + $loadBalancers + @(
            "--health-check-grace-period-seconds", "120",
            "--force-new-deployment",
            "--region", $Region,
            "--query", "service.serviceArn",
            "--output", "text"
        )) "update ECS service $serviceName"
    } else {
        Invoke-Aws (@(
            "ecs", "create-service",
            "--cluster", $ClusterName,
            "--service-name", $serviceName,
            "--task-definition", $taskDefinitionArn,
            "--desired-count", "$DesiredCount",
            "--launch-type", "FARGATE",
            "--platform-version", "LATEST",
            "--network-configuration", $networkConfiguration,
            "--load-balancers"
        ) + $loadBalancers + @(
            "--health-check-grace-period-seconds", "120",
            "--region", $Region,
            "--query", "service.serviceArn",
            "--output", "text"
        )) "create ECS service $serviceName"
    }

    if ($WaitForStable) {
        Invoke-Aws @(
            "ecs", "wait", "services-stable",
            "--cluster", $ClusterName,
            "--services", $serviceName,
            "--region", $Region
        ) "wait ECS service stable $serviceName"
    }

    return [ordered]@{
        name = $Service.Name
        ecsServiceName = $serviceName
        taskDefinitionArn = $taskDefinitionArn
        internalTargetGroupArn = $internalTargetGroupArn
        loadBalancers = $loadBalancers
    }
}

$Region = Get-ValueOrDefault $Region "us-east-1"
$InfraManifestPath = Get-ValueOrDefault $InfraManifestPath (Join-Path $RunDir "ecs-infra.json")
$RdsEnvPath = Get-ValueOrDefault $RdsEnvPath (Join-Path $RunDir "aws-rds.env")

Require-Command "aws"

if (-not (Test-Path $InfraManifestPath)) {
    throw "Missing infra manifest: $InfraManifestPath. Run create-ecs-infra-and-task-definitions.ps1 first."
}

$infra = Get-Content $InfraManifestPath -Raw | ConvertFrom-Json
if (-not $infra.publicSubnetIds -or $infra.publicSubnetIds.Count -lt 2) {
    throw "Infra manifest must contain at least two subnet ids."
}
if ([string]::IsNullOrWhiteSpace($infra.tasksSecurityGroupId)) {
    throw "Infra manifest must contain tasksSecurityGroupId."
}

Write-Host "==> Checking AWS identity"
Invoke-Aws @("sts", "get-caller-identity", "--region", $Region) "aws sts get-caller-identity"

$networkConfiguration = New-NetworkConfiguration @($infra.publicSubnetIds) $infra.tasksSecurityGroupId

if (-not $SkipRdsIngress) {
    $rdsEnv = Read-DotEnv $RdsEnvPath
    Ensure-DatabaseIngress $rdsEnv["WORKLY_RDS_SECURITY_GROUP_ID"] $infra.tasksSecurityGroupId
}

$services = @(
    [pscustomobject]@{ Name = "company-api"; ContainerPort = 8080 },
    [pscustomobject]@{ Name = "identity-api"; ContainerPort = 8080 },
    [pscustomobject]@{ Name = "reservation-service"; ContainerPort = 8080 },
    [pscustomobject]@{ Name = "rooms-service"; ContainerPort = 8080 },
    [pscustomobject]@{ Name = "audit-api"; ContainerPort = 8080 },
    [pscustomobject]@{ Name = "notification-worker"; ContainerPort = 8080; InternalPathPattern = "/internal/notification-worker*"; InternalRulePriority = 60 }
)

$created = @()
foreach ($service in $services) {
    $created += Ensure-EcsService $service
}

$manifestPath = Join-Path $RunDir "ecs-services.json"
Write-Utf8NoBom $manifestPath ($created | ConvertTo-Json -Depth 8)

Write-Host ""
Write-Host "ECS services are ready."
Write-Host "Cluster:  $ClusterName"
Write-Host "Manifest: $manifestPath"
$created | Format-Table -AutoSize
