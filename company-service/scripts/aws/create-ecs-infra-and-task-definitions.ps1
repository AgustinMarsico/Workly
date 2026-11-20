param(
    [string]$Region = $env:AWS_REGION,
    [string]$VpcId = "",
    [string[]]$PublicSubnetIds = @(),
    [string]$AllowedHttpCidr = "0.0.0.0/0",

    [string]$PublicAlbName = "workly-public-alb",
    [string]$InternalAlbName = "workly-internal-alb",
    [string]$AlbSecurityGroupName = "workly-alb-public-sg",
    [string]$InternalAlbSecurityGroupName = "workly-alb-internal-sg",
    [string]$TasksSecurityGroupName = "workly-ecs-tasks-sg",
    [string]$TargetGroupPrefix = "workly",

    [string]$RoleName = "LabRole",
    [string]$FamilyPrefix = "workly",
    [string]$RepositoryPrefix = "workly",
    [string]$Tag = "latest",
    [string]$ImagesManifestPath = "",
    [string]$LogGroupPrefix = "/ecs/workly",
    [string]$Cpu = "512",
    [string]$Memory = "1024",
    [switch]$IncludeFrontend,
    [switch]$IncludeAwsCredentialEnv
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
    & aws @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
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

function Get-SecurityGroupId {
    param([string]$GroupName)

    return Get-AwsText @(
        "ec2", "describe-security-groups",
        "--filters", "Name=group-name,Values=$GroupName", "Name=vpc-id,Values=$VpcId",
        "--region", $Region,
        "--query", "SecurityGroups[0].GroupId",
        "--output", "text"
    )
}

function Ensure-SecurityGroup {
    param([string]$GroupName, [string]$Description)

    $groupId = Get-SecurityGroupId $GroupName
    if ([string]::IsNullOrWhiteSpace($groupId) -or $groupId -eq "None") {
        $groupId = Get-AwsText @(
            "ec2", "create-security-group",
            "--group-name", $GroupName,
            "--description", $Description,
            "--vpc-id", $VpcId,
            "--region", $Region,
            "--query", "GroupId",
            "--output", "text"
        )
    }

    return $groupId
}

function Ensure-IngressCidr {
    param([string]$GroupId, [int]$Port, [string]$Cidr)

    $exitCode = Invoke-AwsAllowFailure @(
        "ec2", "authorize-security-group-ingress",
        "--group-id", $GroupId,
        "--protocol", "tcp",
        "--port", "$Port",
        "--cidr", $Cidr,
        "--region", $Region
    ) "authorize ingress tcp/$Port from $Cidr on $GroupId"

    if ($exitCode -ne 0) {
        Write-Host "Ingress tcp/$Port from $Cidr may already exist; continuing."
    }
}

function Ensure-IngressSecurityGroup {
    param([string]$GroupId, [int]$Port, [string]$SourceGroupId)

    $exitCode = Invoke-AwsAllowFailure @(
        "ec2", "authorize-security-group-ingress",
        "--group-id", $GroupId,
        "--protocol", "tcp",
        "--port", "$Port",
        "--source-group", $SourceGroupId,
        "--region", $Region
    ) "authorize ingress tcp/$Port from $SourceGroupId on $GroupId"

    if ($exitCode -ne 0) {
        Write-Host "Ingress tcp/$Port from $SourceGroupId may already exist; continuing."
    }
}

function Get-LoadBalancer {
    param([string]$Name, [string]$Query)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & aws elbv2 describe-load-balancers `
            --names $Name `
            --region $Region `
            --query $Query `
            --output text 2>$null
        if ($LASTEXITCODE -ne 0) {
            return ""
        }
        return ($output -join " ").Trim()
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Ensure-LoadBalancer {
    param(
        [string]$Name,
        [string]$Scheme,
        [string]$SecurityGroupId
    )

    $albArn = Get-LoadBalancer $Name "LoadBalancers[0].LoadBalancerArn"
    if ([string]::IsNullOrWhiteSpace($albArn) -or $albArn -eq "None") {
        $albArn = Get-AwsText (@(
            "elbv2", "create-load-balancer",
            "--name", $Name,
            "--type", "application",
            "--scheme", $Scheme,
            "--ip-address-type", "ipv4",
            "--security-groups", $SecurityGroupId,
            "--subnets"
        ) + $PublicSubnetIds + @(
            "--region", $Region,
            "--query", "LoadBalancers[0].LoadBalancerArn",
            "--output", "text"
        ))

        Invoke-Aws @("elbv2", "wait", "load-balancer-available", "--load-balancer-arns", $albArn, "--region", $Region) "wait ALB available"
    }

    return $albArn
}

function Get-TargetGroupArn {
    param([string]$Name)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & aws elbv2 describe-target-groups `
            --names $Name `
            --region $Region `
            --query "TargetGroups[0].TargetGroupArn" `
            --output text 2>$null
        if ($LASTEXITCODE -ne 0) {
            return ""
        }
        return ($output -join " ").Trim()
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Ensure-TargetGroup {
    param([string]$Name, [string]$HealthPath = "/health")

    $targetGroupArn = Get-TargetGroupArn $Name
    if ([string]::IsNullOrWhiteSpace($targetGroupArn) -or $targetGroupArn -eq "None") {
        $targetGroupArn = Get-AwsText @(
            "elbv2", "create-target-group",
            "--name", $Name,
            "--protocol", "HTTP",
            "--port", "8080",
            "--vpc-id", $VpcId,
            "--target-type", "ip",
            "--health-check-protocol", "HTTP",
            "--health-check-path", $HealthPath,
            "--matcher", "HttpCode=200-399",
            "--region", $Region,
            "--query", "TargetGroups[0].TargetGroupArn",
            "--output", "text"
        )
    }

    return $targetGroupArn
}

function Get-HttpListenerArn {
    param([string]$LoadBalancerArn)

    $listenerArn = Get-AwsText @(
        "elbv2", "describe-listeners",
        "--load-balancer-arn", $LoadBalancerArn,
        "--region", $Region,
        "--query", 'Listeners[?Port==`80`].ListenerArn | [0]',
        "--output", "text"
    )
    if ($listenerArn -eq "None") {
        return ""
    }
    return $listenerArn
}

function Ensure-HttpListener {
    param([string]$LoadBalancerArn)

    $listenerArn = Get-HttpListenerArn $LoadBalancerArn
    if ([string]::IsNullOrWhiteSpace($listenerArn)) {
        $listenerArn = Get-AwsText @(
            "elbv2", "create-listener",
            "--load-balancer-arn", $LoadBalancerArn,
            "--protocol", "HTTP",
            "--port", "80",
            "--default-actions", "Type=fixed-response,FixedResponseConfig={StatusCode=404,ContentType=text/plain,MessageBody=Not found}",
            "--region", $Region,
            "--query", "Listeners[0].ListenerArn",
            "--output", "text"
        )
    }
    return $listenerArn
}

function Ensure-ListenerRule {
    param(
        [string]$ListenerArn,
        [int]$Priority,
        [string[]]$PathPatterns,
        [string]$TargetGroupArn,
        [string]$Name
    )

    $conditions = "Field=path-pattern,Values=$($PathPatterns -join ',')"
    $exitCode = Invoke-AwsAllowFailure @(
        "elbv2", "create-rule",
        "--listener-arn", $ListenerArn,
        "--priority", "$Priority",
        "--conditions", $conditions,
        "--actions", "Type=forward,TargetGroupArn=$TargetGroupArn",
        "--region", $Region
    ) "create listener rule $Name"

    if ($exitCode -ne 0) {
        Write-Host "Listener rule $Name may already exist; continuing."
    }
}

$Region = Get-ValueOrDefault $Region "us-east-1"
$ImagesManifestPath = Get-ValueOrDefault $ImagesManifestPath (Join-Path $RunDir "ecr-images.json")

Require-Command "aws"

Write-Host "==> Checking AWS identity"
Invoke-Aws @("sts", "get-caller-identity", "--region", $Region) "aws sts get-caller-identity"

if ([string]::IsNullOrWhiteSpace($VpcId)) {
    $VpcId = Get-AwsText @(
        "ec2", "describe-vpcs",
        "--filters", "Name=isDefault,Values=true",
        "--region", $Region,
        "--query", "Vpcs[0].VpcId",
        "--output", "text"
    )
}

if ($PublicSubnetIds.Count -eq 0) {
    $subnetText = Get-AwsText @(
        "ec2", "describe-subnets",
        "--filters", "Name=vpc-id,Values=$VpcId",
        "--region", $Region,
        "--query", "Subnets[].SubnetId",
        "--output", "text"
    )
    $PublicSubnetIds = @($subnetText -split "\s+" | Where-Object { $_ })
}

if ($PublicSubnetIds.Count -lt 2) {
    throw "At least two public subnets are required for an ALB. Pass -PublicSubnetIds."
}

$albSecurityGroupId = Ensure-SecurityGroup $AlbSecurityGroupName "Public ALB access for Workly"
$internalAlbSecurityGroupId = Ensure-SecurityGroup $InternalAlbSecurityGroupName "Internal ALB access for Workly"
$tasksSecurityGroupId = Ensure-SecurityGroup $TasksSecurityGroupName "ECS task access for Workly"
Ensure-IngressCidr $albSecurityGroupId 80 $AllowedHttpCidr
Ensure-IngressSecurityGroup $internalAlbSecurityGroupId 80 $tasksSecurityGroupId
Ensure-IngressSecurityGroup $internalAlbSecurityGroupId 80 $albSecurityGroupId
Ensure-IngressSecurityGroup $tasksSecurityGroupId 8080 $albSecurityGroupId
Ensure-IngressSecurityGroup $tasksSecurityGroupId 8080 $internalAlbSecurityGroupId
Ensure-IngressSecurityGroup $tasksSecurityGroupId 8080 $tasksSecurityGroupId

$publicAlbArn = Ensure-LoadBalancer $PublicAlbName "internet-facing" $albSecurityGroupId
$publicAlbDnsName = Get-LoadBalancer $PublicAlbName "LoadBalancers[0].DNSName"
$publicListenerArn = Ensure-HttpListener $publicAlbArn

$internalAlbArn = Ensure-LoadBalancer $InternalAlbName "internal" $internalAlbSecurityGroupId
$internalAlbDnsName = Get-LoadBalancer $InternalAlbName "LoadBalancers[0].DNSName"
$internalListenerArn = Ensure-HttpListener $internalAlbArn

$publicTargetGroups = [ordered]@{
    "company-api" = Ensure-TargetGroup "$TargetGroupPrefix-pub-company"
    "identity-api" = Ensure-TargetGroup "$TargetGroupPrefix-pub-identity"
    "reservation-service" = Ensure-TargetGroup "$TargetGroupPrefix-pub-reserve"
    "rooms-service" = Ensure-TargetGroup "$TargetGroupPrefix-pub-rooms"
    "audit-api" = Ensure-TargetGroup "$TargetGroupPrefix-pub-audit"
}

$internalTargetGroups = [ordered]@{
    "company-api" = Ensure-TargetGroup "$TargetGroupPrefix-int-company"
    "identity-api" = Ensure-TargetGroup "$TargetGroupPrefix-int-identity"
    "reservation-service" = Ensure-TargetGroup "$TargetGroupPrefix-int-reserve"
    "rooms-service" = Ensure-TargetGroup "$TargetGroupPrefix-int-rooms"
    "audit-api" = Ensure-TargetGroup "$TargetGroupPrefix-int-audit"
    "notification-worker" = Ensure-TargetGroup "$TargetGroupPrefix-int-notify"
}

Ensure-ListenerRule $publicListenerArn 10 @("/api/auth*", "/api/users*", "/api/invitations*", "/api/api-keys*") $publicTargetGroups["identity-api"] "public identity paths"
Ensure-ListenerRule $publicListenerArn 20 @("/api/reservations*") $publicTargetGroups["reservation-service"] "public reservation paths"
Ensure-ListenerRule $publicListenerArn 30 @("/api/rooms*") $publicTargetGroups["rooms-service"] "public rooms paths"
Ensure-ListenerRule $publicListenerArn 40 @("/api/audit-records*") $publicTargetGroups["audit-api"] "public audit paths"
Ensure-ListenerRule $publicListenerArn 50 @("/api/companies*", "/api/areas*") $publicTargetGroups["company-api"] "public company paths"

Ensure-ListenerRule $internalListenerArn 10 @("/api/auth*", "/api/users*", "/api/invitations*", "/api/api-keys*") $internalTargetGroups["identity-api"] "internal identity paths"
Ensure-ListenerRule $internalListenerArn 20 @("/api/reservations*") $internalTargetGroups["reservation-service"] "internal reservation paths"
Ensure-ListenerRule $internalListenerArn 30 @("/api/rooms*") $internalTargetGroups["rooms-service"] "internal rooms paths"
Ensure-ListenerRule $internalListenerArn 40 @("/api/audit-records*") $internalTargetGroups["audit-api"] "internal audit paths"
Ensure-ListenerRule $internalListenerArn 50 @("/api/companies*", "/api/areas*") $internalTargetGroups["company-api"] "internal company paths"

$companyBaseUrl = "http://$internalAlbDnsName"
$identityBaseUrl = "http://$internalAlbDnsName"
$reservationBaseUrl = "http://$internalAlbDnsName"
$roomsBaseUrl = "http://$internalAlbDnsName"
$auditBaseUrl = "http://$internalAlbDnsName"

$registerArgs = @(
    "-Region", $Region,
    "-RoleName", $RoleName,
    "-FamilyPrefix", $FamilyPrefix,
    "-RepositoryPrefix", $RepositoryPrefix,
    "-Tag", $Tag,
    "-ImagesManifestPath", $ImagesManifestPath,
    "-LogGroupPrefix", $LogGroupPrefix,
    "-Cpu", $Cpu,
    "-Memory", $Memory,
    "-CompanyBaseUrl", $companyBaseUrl,
    "-IdentityBaseUrl", $identityBaseUrl,
    "-ReservationBaseUrl", $reservationBaseUrl,
    "-RoomsBaseUrl", $roomsBaseUrl,
    "-AuditBaseUrl", $auditBaseUrl
)
if ($IncludeFrontend) {
    $registerArgs += "-IncludeFrontend"
}
if ($IncludeAwsCredentialEnv) {
    $registerArgs += "-IncludeAwsCredentialEnv"
}

& powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "register-ecs-task-definitions.ps1") @registerArgs
if ($LASTEXITCODE -ne 0) {
    throw "register-ecs-task-definitions.ps1 failed with exit code $LASTEXITCODE"
}

$manifest = [ordered]@{
    region = $Region
    vpcId = $VpcId
    publicSubnetIds = $PublicSubnetIds
    publicAlbArn = $publicAlbArn
    publicAlbDnsName = $publicAlbDnsName
    publicListenerArn = $publicListenerArn
    internalAlbArn = $internalAlbArn
    internalAlbDnsName = $internalAlbDnsName
    internalListenerArn = $internalListenerArn
    albSecurityGroupId = $albSecurityGroupId
    internalAlbSecurityGroupId = $internalAlbSecurityGroupId
    tasksSecurityGroupId = $tasksSecurityGroupId
    publicTargetGroups = $publicTargetGroups
    internalTargetGroups = $internalTargetGroups
    serviceBaseUrls = [ordered]@{
        company = $companyBaseUrl
        identity = $identityBaseUrl
        reservation = $reservationBaseUrl
        rooms = $roomsBaseUrl
        audit = $auditBaseUrl
        publicAlb = "http://$publicAlbDnsName"
        internalAlb = "http://$internalAlbDnsName"
    }
}

$manifestPath = Join-Path $RunDir "ecs-infra.json"
$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host ""
Write-Host "ECS infrastructure and task definitions are ready."
Write-Host "Public ALB:   http://$publicAlbDnsName"
Write-Host "Internal ALB: http://$internalAlbDnsName"
Write-Host "Manifest:     $manifestPath"
