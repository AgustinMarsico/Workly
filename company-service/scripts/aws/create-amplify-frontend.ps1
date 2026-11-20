param(
    [string]$Region = $env:AWS_REGION,
    [string]$AppName = "workly-frontend",
    [string]$BranchName = "main",
    [string]$Repository = $env:WORKLY_GITHUB_REPOSITORY,
    [string]$GithubAccessToken = $env:GITHUB_TOKEN,
    [string]$InfraManifestPath = "",
    [string]$ApiName = "workly-api-gateway",
    [string]$VpcLinkName = "workly-api-vpc-link",
    [string[]]$VpcLinkSubnetIds = @(),
    [string[]]$ExcludedVpcLinkAvailabilityZoneIds = @("use1-az3"),
    [string]$StageName = "`$default",
    [string]$AllowedOrigins = "*",
    [switch]$SkipDeploy
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Repository)) {
    throw "Set WORKLY_GITHUB_REPOSITORY or pass -Repository with your repository URL."
}

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
    param([string[]]$Arguments, [string]$Name = "aws $($Arguments -join ' ')")
    $output = & aws @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
    return ($output -join " ").Trim()
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

function Write-Utf8NoBom {
    param([string]$Path, [string]$Content)

    $encoding = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Get-GithubRepoPath {
    param([string]$RepositoryUrl)

    $repoPath = $RepositoryUrl.Trim()
    $repoPath = $repoPath -replace "^https://github\.com/", ""
    $repoPath = $repoPath -replace "^git@github\.com:", ""
    $repoPath = $repoPath -replace "\.git$", ""
    return $repoPath.Trim("/")
}

function Test-GithubRepositoryAccess {
    param([string]$RepositoryUrl, [string]$Token)

    if ([string]::IsNullOrWhiteSpace($Token)) {
        return
    }

    $repoPath = Get-GithubRepoPath $RepositoryUrl
    if ([string]::IsNullOrWhiteSpace($repoPath) -or -not $repoPath.Contains("/")) {
        throw "Could not parse GitHub repository URL: $RepositoryUrl"
    }

    $headers = @{
        Authorization = "Bearer $Token"
        Accept = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
    }

    try {
        Invoke-RestMethod -Uri "https://api.github.com/repos/$repoPath" -Headers $headers -Method Get | Out-Null
    } catch {
        throw "GitHub token cannot access repository '$repoPath'. For a private repo, use a token that has access to that repo."
    }

    try {
        Invoke-RestMethod -Uri "https://api.github.com/repos/$repoPath/hooks" -Headers $headers -Method Get | Out-Null
    } catch {
        throw "GitHub token can see '$repoPath' but cannot access repository webhooks. Amplify needs webhook access. Use a classic PAT with repo and admin:repo_hook scopes, or a fine-grained token with repository access plus Webhooks read/write."
    }
}

function ConvertTo-JsonArrayLiteral {
    param([string]$Csv)

    $items = @($Csv -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    return ($items | ConvertTo-Json -Compress)
}

function Get-HttpApiId {
    param([string]$Name)

    $apiId = Get-AwsText @(
        "apigatewayv2", "get-apis",
        "--region", $Region,
        "--query", "Items[?Name=='$Name'].ApiId | [0]",
        "--output", "text"
    )
    if ($apiId -eq "None") {
        return ""
    }
    return $apiId
}

function Get-IntegrationId {
    param([string]$ApiId)

    $integrationId = Get-AwsText @(
        "apigatewayv2", "get-integrations",
        "--api-id", $ApiId,
        "--region", $Region,
        "--query", "Items[?IntegrationType=='HTTP_PROXY'].IntegrationId | [0]",
        "--output", "text"
    )
    if ($integrationId -eq "None") {
        return ""
    }
    return $integrationId
}

function Get-VpcLinkId {
    param([string]$Name)

    $vpcLinkId = Get-AwsText @(
        "apigatewayv2", "get-vpc-links",
        "--region", $Region,
        "--query", "Items[?Name=='$Name'].VpcLinkId | [0]",
        "--output", "text"
    )
    if ($vpcLinkId -eq "None") {
        return ""
    }
    return $vpcLinkId
}

function Wait-VpcLinkAvailable {
    param([string]$VpcLinkId)

    for ($i = 1; $i -le 40; $i++) {
        $status = Get-AwsText @(
            "apigatewayv2", "get-vpc-link",
            "--vpc-link-id", $VpcLinkId,
            "--region", $Region,
            "--query", "VpcLinkStatus",
            "--output", "text"
        )

        if ($status -eq "AVAILABLE") {
            return
        }
        if ($status -eq "FAILED") {
            throw "VPC Link $VpcLinkId failed."
        }

        Write-Host "VPC Link $VpcLinkId status is $status; waiting..."
        Start-Sleep -Seconds 15
    }

    throw "VPC Link $VpcLinkId did not become AVAILABLE in time."
}

function Ensure-VpcLink {
    param([string[]]$SubnetIds, [string]$SecurityGroupId)

    $vpcLinkId = Get-VpcLinkId $VpcLinkName
    if ([string]::IsNullOrWhiteSpace($vpcLinkId)) {
        $vpcLinkId = Get-AwsText (@(
            "apigatewayv2", "create-vpc-link",
            "--name", $VpcLinkName,
            "--security-group-ids", $SecurityGroupId,
            "--subnet-ids"
        ) + $SubnetIds + @(
            "--region", $Region,
            "--query", "VpcLinkId",
            "--output", "text"
        ))
    }

    Wait-VpcLinkAvailable $vpcLinkId
    return $vpcLinkId
}

function Get-CompatibleVpcLinkSubnetIds {
    param([string[]]$SubnetIds)

    if ($VpcLinkSubnetIds.Count -gt 0) {
        return $VpcLinkSubnetIds
    }

    $describeSubnetArgs = @(
        "ec2", "describe-subnets",
        "--subnet-ids"
    ) + $SubnetIds + @(
        "--region", $Region,
        "--query", "Subnets[].{SubnetId:SubnetId,AvailabilityZoneId:AvailabilityZoneId}",
        "--output", "json"
    )
    $subnetsJson = & aws @describeSubnetArgs
    if ($LASTEXITCODE -ne 0) {
        throw "describe VPC Link subnets failed with exit code $LASTEXITCODE"
    }

    $subnets = $subnetsJson | ConvertFrom-Json
    $compatible = @(
        $subnets |
            Where-Object { $ExcludedVpcLinkAvailabilityZoneIds -notcontains $_.AvailabilityZoneId } |
            ForEach-Object { $_.SubnetId }
    )

    if ($compatible.Count -lt 2) {
        $excluded = $ExcludedVpcLinkAvailabilityZoneIds -join ", "
        throw "Need at least two VPC Link compatible subnets after excluding AZ IDs: $excluded. Pass -VpcLinkSubnetIds explicitly."
    }

    $excludedSubnets = @(
        $subnets |
            Where-Object { $ExcludedVpcLinkAvailabilityZoneIds -contains $_.AvailabilityZoneId } |
            ForEach-Object { "$($_.SubnetId)($($_.AvailabilityZoneId))" }
    )
    if ($excludedSubnets.Count -gt 0) {
        Write-Host "Skipping VPC Link unsupported subnets: $($excludedSubnets -join ', ')"
    }

    return $compatible
}

function Get-RouteId {
    param([string]$ApiId, [string]$RouteKey)

    $routeId = Get-AwsText @(
        "apigatewayv2", "get-routes",
        "--api-id", $ApiId,
        "--region", $Region,
        "--query", "Items[?RouteKey=='$RouteKey'].RouteId | [0]",
        "--output", "text"
    )
    if ($routeId -eq "None") {
        return ""
    }
    return $routeId
}

function Get-StageName {
    param([string]$ApiId, [string]$Name)

    $stage = Get-AwsText @(
        "apigatewayv2", "get-stages",
        "--api-id", $ApiId,
        "--region", $Region,
        "--query", "Items[?StageName=='$Name'].StageName | [0]",
        "--output", "text"
    )
    if ($stage -eq "None") {
        return ""
    }
    return $stage
}

function Ensure-HttpApi {
    param([string]$InternalListenerArn, [string]$VpcLinkId)

    $corsPath = Join-Path $RunDir "apigateway-cors.json"
    $originItems = [string[]]@($AllowedOrigins -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $cors = [ordered]@{
        AllowOrigins = $originItems
        AllowMethods = [string[]]@("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
        AllowHeaders = [string[]]@("authorization", "content-type", "x-api-key", "x-correlation-id")
        ExposeHeaders = [string[]]@("x-correlation-id")
        MaxAge = 3600
    }
    Write-Utf8NoBom $corsPath ($cors | ConvertTo-Json -Depth 5)

    $apiId = Get-HttpApiId $ApiName
    if ([string]::IsNullOrWhiteSpace($apiId)) {
        $apiId = Get-AwsText @(
            "apigatewayv2", "create-api",
            "--name", $ApiName,
            "--protocol-type", "HTTP",
            "--cors-configuration", "file://$corsPath",
            "--region", $Region,
            "--query", "ApiId",
            "--output", "text"
        )
    } else {
        Invoke-Aws @(
            "apigatewayv2", "update-api",
            "--api-id", $apiId,
            "--cors-configuration", "file://$corsPath",
            "--region", $Region,
            "--query", "ApiId",
            "--output", "text"
        ) "update HTTP API CORS $ApiName"
    }

    $integrationUri = $InternalListenerArn
    $integrationId = Get-IntegrationId $apiId
    if ([string]::IsNullOrWhiteSpace($integrationId)) {
        $integrationId = Get-AwsText @(
            "apigatewayv2", "create-integration",
            "--api-id", $apiId,
            "--integration-type", "HTTP_PROXY",
            "--integration-method", "ANY",
            "--integration-uri", $integrationUri,
            "--connection-type", "VPC_LINK",
            "--connection-id", $VpcLinkId,
            "--payload-format-version", "1.0",
            "--region", $Region,
            "--query", "IntegrationId",
            "--output", "text"
        )
    } else {
        Invoke-Aws @(
            "apigatewayv2", "update-integration",
            "--api-id", $apiId,
            "--integration-id", $integrationId,
            "--integration-method", "ANY",
            "--integration-uri", $integrationUri,
            "--connection-type", "VPC_LINK",
            "--connection-id", $VpcLinkId,
            "--payload-format-version", "1.0",
            "--region", $Region,
            "--query", "IntegrationId",
            "--output", "text"
        ) "update HTTP API integration $ApiName"
    }

    foreach ($routeKey in @("ANY /{proxy+}", "ANY /")) {
        $routeId = Get-RouteId $apiId $routeKey
        if ([string]::IsNullOrWhiteSpace($routeId)) {
            Invoke-Aws @(
                "apigatewayv2", "create-route",
                "--api-id", $apiId,
                "--route-key", $routeKey,
                "--target", "integrations/$integrationId",
                "--region", $Region,
                "--query", "RouteId",
                "--output", "text"
            ) "create HTTP API route $routeKey"
        } else {
            Invoke-Aws @(
                "apigatewayv2", "update-route",
                "--api-id", $apiId,
                "--route-id", $routeId,
                "--target", "integrations/$integrationId",
                "--region", $Region,
                "--query", "RouteId",
                "--output", "text"
            ) "update HTTP API route $routeKey"
        }
    }

    $stage = Get-StageName $apiId $StageName
    if ([string]::IsNullOrWhiteSpace($stage)) {
        Invoke-Aws @(
            "apigatewayv2", "create-stage",
            "--api-id", $apiId,
            "--stage-name", $StageName,
            "--auto-deploy",
            "--region", $Region,
            "--query", "StageName",
            "--output", "text"
        ) "create HTTP API stage $StageName"
    } else {
        Invoke-Aws @(
            "apigatewayv2", "update-stage",
            "--api-id", $apiId,
            "--stage-name", $StageName,
            "--auto-deploy",
            "--region", $Region,
            "--query", "StageName",
            "--output", "text"
        ) "update HTTP API stage $StageName"
    }

    return [ordered]@{
        apiId = $apiId
        endpoint = "https://$apiId.execute-api.$Region.amazonaws.com"
        vpcLinkId = $VpcLinkId
        integrationUri = $integrationUri
    }
}

function Get-AmplifyAppId {
    param([string]$Name)

    $appId = Get-AwsText @(
        "amplify", "list-apps",
        "--region", $Region,
        "--query", "apps[?name=='$Name'].appId | [0]",
        "--output", "text"
    )
    if ($appId -eq "None") {
        return ""
    }
    return $appId
}

function Get-AmplifyBranchName {
    param([string]$AppId, [string]$Name)

    $branch = Get-AwsText @(
        "amplify", "list-branches",
        "--app-id", $AppId,
        "--region", $Region,
        "--query", "branches[?branchName=='$Name'].branchName | [0]",
        "--output", "text"
    )
    if ($branch -eq "None") {
        return ""
    }
    return $branch
}

function Ensure-AmplifyApp {
    param([string]$ApiUrl)

    if ([string]::IsNullOrWhiteSpace($GithubAccessToken)) {
        $GithubAccessToken = $env:GITHUB_ACCESS_TOKEN
    }
    Test-GithubRepositoryAccess $Repository $GithubAccessToken

    $buildSpecPath = Join-Path $Root "frontend\amplify.yml"
    if (-not (Test-Path $buildSpecPath)) {
        throw "Missing Amplify build spec: $buildSpecPath"
    }

    $buildSpecArg = "file://$buildSpecPath"
    $envVars = [ordered]@{
        API_URL = $ApiUrl
        IDENTITY_API_URL = $ApiUrl
        RESERVATION_API_URL = $ApiUrl
        ROOMS_API_URL = $ApiUrl
        AUDIT_API_URL = $ApiUrl
    }
    $envVarArg = "API_URL=$ApiUrl,IDENTITY_API_URL=$ApiUrl,RESERVATION_API_URL=$ApiUrl,ROOMS_API_URL=$ApiUrl,AUDIT_API_URL=$ApiUrl"
    $customRulesPath = Join-Path $RunDir "amplify-custom-rules.json"
    $customRules = @(
        [ordered]@{
            source = "</^[^.]+$/>"
            target = "/index.html"
            status = "200"
        }
    )
    Write-Utf8NoBom $customRulesPath ($customRules | ConvertTo-Json -Depth 5)

    $appId = Get-AmplifyAppId $AppName
    if ([string]::IsNullOrWhiteSpace($appId)) {
        if ([string]::IsNullOrWhiteSpace($GithubAccessToken)) {
            throw "Amplify app does not exist and no GitHub token was provided. Set GITHUB_TOKEN or pass -GithubAccessToken."
        }
        $appId = Get-AwsText @(
            "amplify", "create-app",
            "--name", $AppName,
            "--repository", $Repository,
            "--platform", "WEB",
            "--build-spec", $buildSpecArg,
            "--environment-variables", $envVarArg,
            "--custom-rules", "file://$customRulesPath",
            "--access-token", $GithubAccessToken,
            "--region", $Region,
            "--query", "app.appId",
            "--output", "text"
        ) "create Amplify app $AppName"
    } else {
        $updateArgs = @(
            "amplify", "update-app",
            "--app-id", $appId,
            "--repository", $Repository,
            "--build-spec", $buildSpecArg,
            "--environment-variables", $envVarArg,
            "--custom-rules", "file://$customRulesPath",
            "--region", $Region,
            "--query", "app.appId",
            "--output", "text"
        )
        if (-not [string]::IsNullOrWhiteSpace($GithubAccessToken)) {
            $updateArgs += @("--access-token", $GithubAccessToken)
        }
        Invoke-Aws $updateArgs "update Amplify app $AppName"
    }

    $branch = Get-AmplifyBranchName $appId $BranchName
    if ([string]::IsNullOrWhiteSpace($branch)) {
        Invoke-Aws @(
            "amplify", "create-branch",
            "--app-id", $appId,
            "--branch-name", $BranchName,
            "--stage", "PRODUCTION",
            "--enable-auto-build",
            "--region", $Region,
            "--query", "branch.branchName",
            "--output", "text"
        ) "create Amplify branch $BranchName"
    } else {
        Invoke-Aws @(
            "amplify", "update-branch",
            "--app-id", $appId,
            "--branch-name", $BranchName,
            "--stage", "PRODUCTION",
            "--enable-auto-build",
            "--region", $Region,
            "--query", "branch.branchName",
            "--output", "text"
        ) "update Amplify branch $BranchName"
    }

    if (-not $SkipDeploy) {
        Invoke-Aws @(
            "amplify", "start-job",
            "--app-id", $appId,
            "--branch-name", $BranchName,
            "--job-type", "RELEASE",
            "--region", $Region,
            "--query", "jobSummary.jobId",
            "--output", "text"
        ) "start Amplify deployment"
    }

    return [ordered]@{
        appId = $appId
        branchName = $BranchName
        defaultDomain = Get-AwsText @(
            "amplify", "get-app",
            "--app-id", $appId,
            "--region", $Region,
            "--query", "app.defaultDomain",
            "--output", "text"
        )
        environmentVariables = $envVars
    }
}

$Region = Get-ValueOrDefault $Region "us-east-1"
$InfraManifestPath = Get-ValueOrDefault $InfraManifestPath (Join-Path $RunDir "ecs-infra.json")

Require-Command "aws"

if (-not (Test-Path $InfraManifestPath)) {
    throw "Missing infra manifest: $InfraManifestPath. Run create-ecs-infra-and-task-definitions.ps1 first."
}

$infra = Get-Content $InfraManifestPath -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($infra.internalListenerArn)) {
    throw "Infra manifest must contain internalListenerArn."
}
if (-not $infra.publicSubnetIds -or $infra.publicSubnetIds.Count -lt 2) {
    throw "Infra manifest must contain at least two subnet ids."
}
if ([string]::IsNullOrWhiteSpace($infra.tasksSecurityGroupId)) {
    throw "Infra manifest must contain tasksSecurityGroupId."
}

Write-Host "==> Checking AWS identity"
Invoke-Aws @("sts", "get-caller-identity", "--region", $Region) "aws sts get-caller-identity"

$vpcLinkSubnets = Get-CompatibleVpcLinkSubnetIds -SubnetIds @($infra.publicSubnetIds)
$vpcLinkId = Ensure-VpcLink -SubnetIds $vpcLinkSubnets -SecurityGroupId $infra.tasksSecurityGroupId
$httpApi = Ensure-HttpApi $infra.internalListenerArn $vpcLinkId
$amplify = Ensure-AmplifyApp $httpApi.endpoint

$manifest = [ordered]@{
    region = $Region
    apiGateway = $httpApi
    amplify = $amplify
}

$manifestPath = Join-Path $RunDir "amplify-frontend.json"
Write-Utf8NoBom $manifestPath ($manifest | ConvertTo-Json -Depth 10)

Write-Host ""
Write-Host "Amplify frontend is ready."
Write-Host "API Gateway: $($httpApi.endpoint)"
Write-Host "Amplify:     https://$BranchName.$($amplify.defaultDomain)"
Write-Host "Manifest:    $manifestPath"
