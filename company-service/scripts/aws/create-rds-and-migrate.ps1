param(
    [string]$Region = $env:AWS_REGION,
    [string]$AllowedCidr = "",
    [string]$VpcId = "",
    [string[]]$SubnetIds = @(),

    [string]$DbSubnetGroupName = "workly-rds-subnets",
    [string]$SecurityGroupName = "workly-rds-public-sg",

    [string]$MysqlInstanceId = "workly-mysql",
    [string]$SqlServerInstanceId = "workly-audit-sqlserver",
    [string]$MysqlInstanceClass = "db.t3.micro",
    [string]$SqlServerInstanceClass = "db.t3.small",
    [int]$MysqlAllocatedStorage = 20,
    [int]$SqlServerAllocatedStorage = 20,

    [string]$MysqlMasterUsername = "worklyadmin",
    [string]$MysqlMasterPassword = "",
    [string]$SqlServerMasterUsername = "worklyadmin",
    [string]$SqlServerMasterPassword = "",

    [string]$CompanyDatabase = "worklydb",
    [string]$IdentityDatabase = "workly_identity",
    [string]$ReservationDatabase = "workly_reservations",
    [string]$RoomsDatabase = "workly_rooms",
    [string]$AuditDatabase = "workly_audit",

    [string]$CompanyDbUsername = "workly_company_user",
    [string]$IdentityDbUsername = "workly_identity_user",
    [string]$ReservationDbUsername = "workly_user",
    [string]$RoomsDbUsername = "workly_rooms_user",
    [string]$CompanyDbPassword = "",
    [string]$IdentityDbPassword = "",
    [string]$ReservationDbPassword = "",
    [string]$RoomsDbPassword = "",
    [string]$AuditDbUsername = "audit_user",
    [string]$AuditDbPassword = "",

    [switch]$SkipCreateInstances,
    [switch]$ResetMasterPasswords,
    [switch]$SkipDatabaseSetup,
    [switch]$SkipMigrations,
    [switch]$KeepLocalProcesses,
    [switch]$WriteServiceEnvFiles
)

$ErrorActionPreference = "Stop"

$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$RunDir = Join-Path $Root ".run"
$GeneratedEnvPath = Join-Path $RunDir "aws-rds.env"
New-Item -ItemType Directory -Force -Path $RunDir | Out-Null

function Get-ValueOrDefault {
    param([string]$Value, [string]$DefaultValue)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $DefaultValue }
    return $Value
}

function Read-DotEnv {
    param([string]$Path)

    $values = @{}
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
        $values[$key] = $value
    }

    return $values
}

function Get-ConnectionPassword {
    param([string]$ConnectionString)

    if ($ConnectionString -match "(?i)(^|;)password=([^;]+)") {
        return $matches[2]
    }

    return ""
}

function New-WorklyPassword {
    $chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
    $bytes = New-Object byte[] 20
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
    } finally {
        $rng.Dispose()
    }
    $body = -join ($bytes | ForEach-Object { $chars[[int]$_ % $chars.Length] })
    return "Wk1!$body"
}

function Require-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Missing required command '$Name'. Install it and run this script again."
    }
}

function Invoke-Aws {
    param([string[]]$Arguments)
    & aws @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "aws $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
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

function Test-RdsInstanceExists {
    param([string]$Identifier)
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & aws rds describe-db-instances `
            --db-instance-identifier $Identifier `
            --region $Region `
            --query "DBInstances[0].DBInstanceIdentifier" `
            --output text 1>$null 2>$null
        return $LASTEXITCODE -eq 0
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Wait-RdsAvailable {
    param([string]$Identifier)
    Write-Host "==> Waiting for RDS instance '$Identifier' to become available"
    Invoke-Aws @("rds", "wait", "db-instance-available", "--db-instance-identifier", $Identifier, "--region", $Region)
}

function Reset-RdsMasterPassword {
    param(
        [string]$Identifier,
        [string]$Password
    )

    Write-Host "==> Resetting master password for RDS instance '$Identifier'"
    Invoke-Aws @(
        "rds", "modify-db-instance",
        "--db-instance-identifier", $Identifier,
        "--master-user-password", $Password,
        "--apply-immediately",
        "--region", $Region
    )
}

function Get-RdsEndpoint {
    param([string]$Identifier)
    return Get-AwsText @(
        "rds", "describe-db-instances",
        "--db-instance-identifier", $Identifier,
        "--region", $Region,
        "--query", "DBInstances[0].Endpoint.Address",
        "--output", "text"
    )
}

function Assert-SqlIdentifier {
    param([string]$Value, [string]$Name)
    if ($Value -notmatch "^[A-Za-z_][A-Za-z0-9_]*$") {
        throw "$Name must contain only letters, numbers, and underscore, and cannot start with a number."
    }
}

function Escape-MySqlString {
    param([string]$Value)
    return $Value.Replace("\", "\\").Replace("'", "''")
}

function Invoke-MySql {
    param([string]$Endpoint, [string]$Username, [string]$Password, [string]$Sql, [string]$Name)
    Write-Host "==> $Name"
    if (Get-Command "mysql" -ErrorAction SilentlyContinue) {
        & mysql "--host=$Endpoint" "--port=3306" "--user=$Username" "--password=$Password" "--execute=$Sql"
    } else {
        Require-Command "docker"
        & docker run --rm mysql:8.0 mysql "--host=$Endpoint" "--port=3306" "--user=$Username" "--password=$Password" "--execute=$Sql"
    }
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
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
    $process = Start-Process `
        -FilePath $FilePath `
        -ArgumentList $ArgumentList `
        -WorkingDirectory $WorkingDirectory `
        -NoNewWindow `
        -Wait `
        -PassThru

    if ($process.ExitCode -ne 0) {
        throw "$Name failed with exit code $($process.ExitCode)"
    }
}

function Set-EnvMap {
    param([hashtable]$Values)
    foreach ($entry in $Values.GetEnumerator()) {
        [Environment]::SetEnvironmentVariable($entry.Key, [string]$entry.Value, "Process")
    }
}

function Update-EnvFile {
    param([string]$Path, [hashtable]$Values)

    $lines = @()
    if (Test-Path $Path) {
        $lines = @(Get-Content $Path)
    }

    foreach ($key in $Values.Keys) {
        $updated = $false
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "^\s*$([regex]::Escape($key))=") {
                $lines[$i] = "$key=$($Values[$key])"
                $updated = $true
                break
            }
        }
        if (-not $updated) {
            $lines += "$key=$($Values[$key])"
        }
    }

    Set-Content -Path $Path -Value $lines -Encoding UTF8
}

$mysqlMasterPasswordProvided = -not [string]::IsNullOrWhiteSpace($MysqlMasterPassword)
$sqlServerMasterPasswordProvided = -not [string]::IsNullOrWhiteSpace($SqlServerMasterPassword)

$previousGeneratedEnv = Read-DotEnv $GeneratedEnvPath

$Region = Get-ValueOrDefault $Region "us-east-1"
$MysqlMasterPassword = Get-ValueOrDefault $MysqlMasterPassword (New-WorklyPassword)
$SqlServerMasterPassword = Get-ValueOrDefault $SqlServerMasterPassword (New-WorklyPassword)
$CompanyDbPassword = Get-ValueOrDefault $CompanyDbPassword (Get-ConnectionPassword ([string]$previousGeneratedEnv["COMPANY_DB_CONNECTION"]))
$IdentityDbPassword = Get-ValueOrDefault $IdentityDbPassword (Get-ConnectionPassword ([string]$previousGeneratedEnv["IDENTITY_DB_CONNECTION"]))
$ReservationDbPassword = Get-ValueOrDefault $ReservationDbPassword (Get-ConnectionPassword ([string]$previousGeneratedEnv["RESERVATION_DB_CONNECTION"]))
$RoomsDbPassword = Get-ValueOrDefault $RoomsDbPassword (Get-ConnectionPassword ([string]$previousGeneratedEnv["ROOMS_DB_CONNECTION"]))
$AuditDbPassword = Get-ValueOrDefault $AuditDbPassword ([string]$previousGeneratedEnv["AUDIT_DB_APP_PASSWORD"])

if (-not $SkipDatabaseSetup) {
    $CompanyDbPassword = Get-ValueOrDefault $CompanyDbPassword (New-WorklyPassword)
    $IdentityDbPassword = Get-ValueOrDefault $IdentityDbPassword (New-WorklyPassword)
    $ReservationDbPassword = Get-ValueOrDefault $ReservationDbPassword (New-WorklyPassword)
    $RoomsDbPassword = Get-ValueOrDefault $RoomsDbPassword (New-WorklyPassword)
    $AuditDbPassword = Get-ValueOrDefault $AuditDbPassword (New-WorklyPassword)
}

$missingAppPasswords = @()
if ([string]::IsNullOrWhiteSpace($CompanyDbPassword)) { $missingAppPasswords += "CompanyDbPassword" }
if ([string]::IsNullOrWhiteSpace($IdentityDbPassword)) { $missingAppPasswords += "IdentityDbPassword" }
if ([string]::IsNullOrWhiteSpace($ReservationDbPassword)) { $missingAppPasswords += "ReservationDbPassword" }
if ([string]::IsNullOrWhiteSpace($RoomsDbPassword)) { $missingAppPasswords += "RoomsDbPassword" }
if ([string]::IsNullOrWhiteSpace($AuditDbPassword)) { $missingAppPasswords += "AuditDbPassword" }
if ($missingAppPasswords.Count -gt 0) {
    throw "Missing app DB passwords: $($missingAppPasswords -join ', '). Pass them explicitly, or run once without -SkipDatabaseSetup using master passwords."
}

foreach ($pair in @(
    @($CompanyDatabase, "CompanyDatabase"),
    @($IdentityDatabase, "IdentityDatabase"),
    @($ReservationDatabase, "ReservationDatabase"),
    @($RoomsDatabase, "RoomsDatabase"),
    @($CompanyDbUsername, "CompanyDbUsername"),
    @($IdentityDbUsername, "IdentityDbUsername"),
    @($ReservationDbUsername, "ReservationDbUsername"),
    @($RoomsDbUsername, "RoomsDbUsername"),
    @($AuditDbUsername, "AuditDbUsername")
)) {
    Assert-SqlIdentifier $pair[0] $pair[1]
}

Require-Command "aws"
Require-Command "dotnet"
if (-not (Get-Command "mysql" -ErrorAction SilentlyContinue)) {
    Require-Command "docker"
}

Write-Host "==> Checking AWS credentials"
Invoke-Aws @("sts", "get-caller-identity", "--region", $Region)

if ([string]::IsNullOrWhiteSpace($AllowedCidr)) {
    Write-Host "==> Detecting current public IP"
    $publicIp = (Invoke-RestMethod -Uri "https://checkip.amazonaws.com").Trim()
    $AllowedCidr = "$publicIp/32"
}

if ([string]::IsNullOrWhiteSpace($VpcId)) {
    Write-Host "==> Finding default VPC"
    $VpcId = Get-AwsText @(
        "ec2", "describe-vpcs",
        "--filters", "Name=isDefault,Values=true",
        "--region", $Region,
        "--query", "Vpcs[0].VpcId",
        "--output", "text"
    )
    if ([string]::IsNullOrWhiteSpace($VpcId) -or $VpcId -eq "None") {
        throw "No default VPC found. Run with -VpcId and -SubnetIds."
    }
}

if ($SubnetIds.Count -eq 0) {
    Write-Host "==> Finding VPC subnets"
    $subnetOutput = Get-AwsText @(
        "ec2", "describe-subnets",
        "--filters", "Name=vpc-id,Values=$VpcId",
        "--region", $Region,
        "--query", "Subnets[].SubnetId",
        "--output", "text"
    )
    $SubnetIds = @($subnetOutput -split "\s+" | Where-Object { $_ })
    if ($SubnetIds.Count -lt 2) {
        throw "RDS needs at least 2 subnets in different AZs. Run with -SubnetIds."
    }
}

Write-Host "==> Ensuring DB subnet group"
$previousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"
try {
    & aws rds describe-db-subnet-groups --db-subnet-group-name $DbSubnetGroupName --region $Region 1>$null 2>$null
    $subnetGroupExists = $LASTEXITCODE -eq 0
} finally {
    $ErrorActionPreference = $previousErrorActionPreference
}
if (-not $subnetGroupExists) {
    $subnetGroupArgs = @(
        "rds", "create-db-subnet-group",
        "--db-subnet-group-name", $DbSubnetGroupName,
        "--db-subnet-group-description", "Workly RDS subnet group",
        "--subnet-ids"
    ) + $SubnetIds + @("--region", $Region)
    Invoke-Aws $subnetGroupArgs | Out-Null
}

Write-Host "==> Ensuring security group"
$SecurityGroupId = Get-AwsText @(
    "ec2", "describe-security-groups",
    "--filters", "Name=group-name,Values=$SecurityGroupName", "Name=vpc-id,Values=$VpcId",
    "--region", $Region,
    "--query", "SecurityGroups[0].GroupId",
    "--output", "text"
)
if ([string]::IsNullOrWhiteSpace($SecurityGroupId) -or $SecurityGroupId -eq "None") {
    $SecurityGroupId = Get-AwsText @(
        "ec2", "create-security-group",
        "--group-name", $SecurityGroupName,
        "--description", "Workly public RDS access",
        "--vpc-id", $VpcId,
        "--region", $Region,
        "--query", "GroupId",
        "--output", "text"
    )
}

foreach ($port in @(3306, 1433)) {
    Write-Host "==> Authorizing inbound TCP $port from $AllowedCidr"
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & aws ec2 authorize-security-group-ingress `
            --group-id $SecurityGroupId `
            --protocol tcp `
            --port $port `
            --cidr $AllowedCidr `
            --region $Region 1>$null 2>$null
        $authorizeExitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    if ($authorizeExitCode -ne 0) {
        Write-Host "Ingress rule for $port may already exist or be restricted by the lab; continuing."
    }
}

$mysqlInstanceExists = Test-RdsInstanceExists $MysqlInstanceId
$sqlServerInstanceExists = Test-RdsInstanceExists $SqlServerInstanceId

if (-not $SkipDatabaseSetup -and ($SkipCreateInstances -or $mysqlInstanceExists) -and -not $mysqlMasterPasswordProvided) {
    throw "MySQL RDS instance already exists or creation is skipped. Pass -MysqlMasterPassword with the existing master password, or pass -SkipDatabaseSetup if databases and users already exist."
}

if (-not $SkipDatabaseSetup -and ($SkipCreateInstances -or $sqlServerInstanceExists) -and -not $sqlServerMasterPasswordProvided) {
    throw "SQL Server RDS instance already exists or creation is skipped. Pass -SqlServerMasterPassword with the existing master password, or pass -SkipDatabaseSetup if databases and users already exist."
}

if (-not $SkipCreateInstances) {
    if (-not $mysqlInstanceExists) {
        Write-Host "==> Creating MySQL RDS instance '$MysqlInstanceId'"
        Invoke-Aws @(
            "rds", "create-db-instance",
            "--db-instance-identifier", $MysqlInstanceId,
            "--db-instance-class", $MysqlInstanceClass,
            "--engine", "mysql",
            "--allocated-storage", "$MysqlAllocatedStorage",
            "--master-username", $MysqlMasterUsername,
            "--master-user-password", $MysqlMasterPassword,
            "--db-subnet-group-name", $DbSubnetGroupName,
            "--vpc-security-group-ids", $SecurityGroupId,
            "--backup-retention-period", "0",
            "--publicly-accessible",
            "--no-multi-az",
            "--no-deletion-protection",
            "--region", $Region
        )
    }

    if (-not $sqlServerInstanceExists) {
        Write-Host "==> Creating SQL Server Express RDS instance '$SqlServerInstanceId'"
        Invoke-Aws @(
            "rds", "create-db-instance",
            "--db-instance-identifier", $SqlServerInstanceId,
            "--db-instance-class", $SqlServerInstanceClass,
            "--engine", "sqlserver-ex",
            "--license-model", "license-included",
            "--allocated-storage", "$SqlServerAllocatedStorage",
            "--master-username", $SqlServerMasterUsername,
            "--master-user-password", $SqlServerMasterPassword,
            "--db-subnet-group-name", $DbSubnetGroupName,
            "--vpc-security-group-ids", $SecurityGroupId,
            "--backup-retention-period", "0",
            "--publicly-accessible",
            "--no-multi-az",
            "--no-deletion-protection",
            "--region", $Region
        )
    }
}

if ($ResetMasterPasswords) {
    if (-not $mysqlMasterPasswordProvided -or -not $sqlServerMasterPasswordProvided) {
        throw "When using -ResetMasterPasswords, pass both -MysqlMasterPassword and -SqlServerMasterPassword."
    }

    Reset-RdsMasterPassword $MysqlInstanceId $MysqlMasterPassword
    Reset-RdsMasterPassword $SqlServerInstanceId $SqlServerMasterPassword
}

if (-not $SkipCreateInstances) {
    Wait-RdsAvailable $MysqlInstanceId
    Wait-RdsAvailable $SqlServerInstanceId
}

if ($ResetMasterPasswords) {
    Wait-RdsAvailable $MysqlInstanceId
    Wait-RdsAvailable $SqlServerInstanceId
}

$MysqlEndpoint = Get-RdsEndpoint $MysqlInstanceId
$SqlServerEndpoint = Get-RdsEndpoint $SqlServerInstanceId

if (-not $SkipDatabaseSetup) {
    $mysqlSql = @"
CREATE DATABASE IF NOT EXISTS $CompanyDatabase CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS $IdentityDatabase CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS $ReservationDatabase CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS $RoomsDatabase CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '$CompanyDbUsername'@'%' IDENTIFIED BY '$(Escape-MySqlString $CompanyDbPassword)';
CREATE USER IF NOT EXISTS '$IdentityDbUsername'@'%' IDENTIFIED BY '$(Escape-MySqlString $IdentityDbPassword)';
CREATE USER IF NOT EXISTS '$ReservationDbUsername'@'%' IDENTIFIED BY '$(Escape-MySqlString $ReservationDbPassword)';
CREATE USER IF NOT EXISTS '$RoomsDbUsername'@'%' IDENTIFIED BY '$(Escape-MySqlString $RoomsDbPassword)';
ALTER USER '$CompanyDbUsername'@'%' IDENTIFIED BY '$(Escape-MySqlString $CompanyDbPassword)';
ALTER USER '$IdentityDbUsername'@'%' IDENTIFIED BY '$(Escape-MySqlString $IdentityDbPassword)';
ALTER USER '$ReservationDbUsername'@'%' IDENTIFIED BY '$(Escape-MySqlString $ReservationDbPassword)';
ALTER USER '$RoomsDbUsername'@'%' IDENTIFIED BY '$(Escape-MySqlString $RoomsDbPassword)';
GRANT ALL PRIVILEGES ON $CompanyDatabase.* TO '$CompanyDbUsername'@'%';
GRANT ALL PRIVILEGES ON $IdentityDatabase.* TO '$IdentityDbUsername'@'%';
GRANT ALL PRIVILEGES ON $ReservationDatabase.* TO '$ReservationDbUsername'@'%';
GRANT ALL PRIVILEGES ON $RoomsDatabase.* TO '$RoomsDbUsername'@'%';
FLUSH PRIVILEGES;
"@

    Invoke-MySql $MysqlEndpoint $MysqlMasterUsername $MysqlMasterPassword $mysqlSql "creating MySQL databases and users"
}

$companyConn = "server=$MysqlEndpoint;port=3306;database=$CompanyDatabase;user=$CompanyDbUsername;password=$CompanyDbPassword"
$identityConn = "server=$MysqlEndpoint;port=3306;database=$IdentityDatabase;user=$IdentityDbUsername;password=$IdentityDbPassword"
$reservationConn = "server=$MysqlEndpoint;port=3306;database=$ReservationDatabase;user=$ReservationDbUsername;password=$ReservationDbPassword"
$roomsConn = "server=$MysqlEndpoint;port=3306;database=$RoomsDatabase;user=$RoomsDbUsername;password=$RoomsDbPassword"
$auditMigrationConn = "Server=$SqlServerEndpoint,1433;Database=$AuditDatabase;User Id=$SqlServerMasterUsername;Password=$SqlServerMasterPassword;TrustServerCertificate=True;"
$auditRuntimeConn = "Server=$SqlServerEndpoint,1433;Database=$AuditDatabase;User Id=$AuditDbUsername;Password=$AuditDbPassword;TrustServerCertificate=True;"

$generated = @(
    "AWS_REGION=$Region",
    "WORKLY_RDS_SECURITY_GROUP_ID=$SecurityGroupId",
    "WORKLY_RDS_ALLOWED_CIDR=$AllowedCidr",
    "WORKLY_MYSQL_ENDPOINT=$MysqlEndpoint",
    "WORKLY_SQLSERVER_ENDPOINT=$SqlServerEndpoint",
    "COMPANY_DB_CONNECTION=$companyConn",
    "IDENTITY_DB_CONNECTION=$identityConn",
    "RESERVATION_DB_CONNECTION=$reservationConn",
    "ROOMS_DB_CONNECTION=$roomsConn",
    "AUDIT_MIGRATION_CONNECTION=$auditMigrationConn",
    "AUDIT_RUNTIME_CONNECTION=$auditRuntimeConn",
    "AUDIT_DB_APP_USERNAME=$AuditDbUsername",
    "AUDIT_DB_APP_PASSWORD=$AuditDbPassword"
)
$generated | Set-Content -Path $GeneratedEnvPath -Encoding UTF8

if (-not $SkipMigrations) {
    Write-Host "==> Running EF migrations against AWS RDS"

    if (-not $KeepLocalProcesses) {
        $stopLocalScript = Join-Path $Root "stop-local.ps1"
        if (Test-Path $stopLocalScript) {
            Invoke-Checked "powershell" @("-ExecutionPolicy", "Bypass", "-File", $stopLocalScript) $Root "stop local Workly processes"
        }
    }

    Set-EnvMap @{
        ASPNETCORE_ENVIRONMENT = "Development"
        ConnectionStrings__DefaultConnection = $companyConn
        DEFAULT_CONNECTION_STRING = $companyConn
        DB_PASSWORD = $CompanyDbPassword
        SKIP_AUDIT_QUEUE = "true"
    }
    Invoke-Checked "dotnet" @("run", "--project", "workly-api/workly-api.csproj", "--", "migrate") (Join-Path $Root "company-service\backend") "company migrate"

    Set-EnvMap @{
        ASPNETCORE_ENVIRONMENT = "Development"
        ConnectionStrings__DefaultConnection = $identityConn
        DEFAULT_CONNECTION_STRING = $identityConn
        DB_PASSWORD = $IdentityDbPassword
        SKIP_AUDIT_QUEUE = "true"
        SKIP_INVITATION_NOTIFICATION = "true"
        OrganizationService__BaseUrl = "http://localhost:5000"
    }
    Invoke-Checked "dotnet" @("run", "--project", "IdentityService.Api/IdentityService.Api.csproj", "--", "migrate") (Join-Path $Root "identity-service\backend") "identity migrate"

    Set-EnvMap @{
        ASPNETCORE_ENVIRONMENT = "Development"
        ConnectionStrings__DefaultConnection = $reservationConn
        CONNECTION_STRING = $reservationConn
        DEFAULT_CONNECTION_STRING = $reservationConn
        DB_PASSWORD = $ReservationDbPassword
        SKIP_RESERVATION_NOTIFICATION = "true"
        MainService__BaseUrl = "http://localhost:5000"
        IdentityService__BaseUrl = "http://localhost:5100"
        RoomsService__BaseUrl = "http://localhost:5300"
    }
    Invoke-Checked "dotnet" @("run", "--project", "WorklyReservationService.WebApi/WorklyReservationService.WebApi.csproj", "--", "migrate") (Join-Path $Root "reservation-service") "reservation migrate"

    Set-EnvMap @{
        ASPNETCORE_ENVIRONMENT = "Development"
        ConnectionStrings__DefaultConnection = $roomsConn
        DEFAULT_CONNECTION_STRING = $roomsConn
        DB_PASSWORD = $RoomsDbPassword
        SKIP_AUDIT_QUEUE = "true"
        IdentityService__BaseUrl = "http://localhost:5100"
        ReservationService__BaseUrl = "http://localhost:5200"
    }
    Invoke-Checked "dotnet" @("run", "--project", "RoomsService.Api/RoomsService.Api.csproj", "--", "migrate") (Join-Path $Root "rooms-service\backend") "rooms migrate"

    Set-EnvMap @{
        ASPNETCORE_ENVIRONMENT = "Development"
        ConnectionStrings__AuditMigrationDatabase = $auditMigrationConn
        ConnectionStrings__AuditDatabase = $auditRuntimeConn
        AUDIT_DB_APP_USERNAME = $AuditDbUsername
        AUDIT_DB_APP_PASSWORD = $AuditDbPassword
        SKIP_AUDIT_QUEUE = "true"
    }
    Invoke-Checked "dotnet" @("run", "--project", "src/AuditService.Api/AuditService.Api.csproj", "--", "migrate") (Join-Path $Root "audit-service") "audit migrate"
}

if ($WriteServiceEnvFiles) {
    Write-Host "==> Writing service .env files"
    Update-EnvFile (Join-Path $Root "company-service\backend\.env") @{
        ConnectionStrings__DefaultConnection = $companyConn
        DEFAULT_CONNECTION_STRING = $companyConn
        DB_PASSWORD = $CompanyDbPassword
    }
    Update-EnvFile (Join-Path $Root "identity-service\backend\.env") @{
        ConnectionStrings__DefaultConnection = $identityConn
        DEFAULT_CONNECTION_STRING = $identityConn
        DB_PASSWORD = $IdentityDbPassword
    }
    Update-EnvFile (Join-Path $Root "reservation-service\.env") @{
        ConnectionStrings__DefaultConnection = $reservationConn
        DEFAULT_CONNECTION_STRING = $reservationConn
        CONNECTION_STRING = $reservationConn
        DB_PASSWORD = $ReservationDbPassword
    }
    Update-EnvFile (Join-Path $Root "rooms-service\backend\.env") @{
        ConnectionStrings__DefaultConnection = $roomsConn
        DEFAULT_CONNECTION_STRING = $roomsConn
        DB_PASSWORD = $RoomsDbPassword
    }
    Update-EnvFile (Join-Path $Root "audit-service\.env") @{
        ConnectionStrings__AuditMigrationDatabase = $auditMigrationConn
        ConnectionStrings__AuditDatabase = $auditRuntimeConn
        AUDIT_DB_APP_USERNAME = $AuditDbUsername
        AUDIT_DB_APP_PASSWORD = $AuditDbPassword
    }
}

Write-Host ""
Write-Host "AWS RDS setup complete."
Write-Host "Generated env file: $GeneratedEnvPath"
Write-Host "MySQL endpoint:      $MysqlEndpoint"
Write-Host "SQL Server endpoint: $SqlServerEndpoint"
Write-Host "Security group:      $SecurityGroupId"
Write-Host "Allowed CIDR:        $AllowedCidr"
