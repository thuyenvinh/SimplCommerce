#Requires -Version 7.2
<#
.SYNOPSIS
  Apply the consolidated SimplCommerce.Migrations bundle against an
  existing SQL Server database, with a mandatory backup-before-apply
  and row-count verification afterwards.

.DESCRIPTION
  Phase 6 in-place upgrade helper. The script is deliberately verbose —
  every destructive step pauses for confirmation unless -Force is given.

  Flow:
    1. Capture a schema + row-count snapshot of critical tables.
    2. `BACKUP DATABASE` to a local `.bak`.
    3. `dotnet ef database update` via the Migrations project.
    4. Re-capture row counts; if any critical table shrank, restore
       from the backup and exit non-zero.

.EXAMPLE
  pwsh tools/migrate-data.ps1 -Server sql-prod -Database SimplCommerce -SignedOffBy ops-lead@example
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Server,
    [Parameter(Mandatory)][string] $Database,
    [Parameter(Mandatory)][string] $SignedOffBy,
    [string] $BackupDir = (Join-Path $PSScriptRoot '../artifacts/backups'),
    [string] $MigrationsProject = 'src/Migrations/SimplCommerce.Migrations',
    [string] $StartupProject    = 'src/Apps/SimplCommerce.ApiService',
    [string] $SqlUser,
    [string] $SqlPassword,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
$criticalTables = @(
    'Core_User', 'Catalog_Product', 'Orders_Order',
    'ShoppingCart_Cart', 'Reviews_Review'
)

function Invoke-SqlCmd {
    param([string] $Query)
    $args = @('-S', $Server, '-d', $Database, '-Q', $Query, '-h', '-1', '-W', '-s', ',')
    if ($SqlUser) { $args += @('-U', $SqlUser, '-P', $SqlPassword) }
    else { $args += @('-E') }
    & sqlcmd @args
}

function Get-RowCounts {
    $union = $criticalTables | ForEach-Object {
        "SELECT '$_' AS t, COUNT_BIG(*) AS c FROM $_"
    } | Join-String -Separator " UNION ALL "
    Invoke-SqlCmd -Query $union
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet CLI not on PATH.'
}
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd not on PATH (install SQL Server command-line tools).'
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupFile = Join-Path $BackupDir "${Database}-pre-aspire-${timestamp}.bak"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

Write-Host "=== pre-flight ==="
Write-Host "  server    : $Server"
Write-Host "  database  : $Database"
Write-Host "  signed-off: $SignedOffBy"
Write-Host "  backup    : $backupFile"
Write-Host ""

if (-not $Force) {
    $answer = Read-Host "This will BACKUP then MIGRATE $Database on $Server. Continue? (yes/no)"
    if ($answer -ne 'yes') { Write-Host 'Aborted.'; exit 2 }
}

Write-Host "=== 1. capturing baseline row counts ==="
$before = Get-RowCounts
$before | ForEach-Object { Write-Host "  $_" }

Write-Host "=== 2. backing up database ==="
Invoke-SqlCmd -Query "BACKUP DATABASE [$Database] TO DISK = N'$backupFile' WITH INIT, FORMAT, COMPRESSION, STATS = 10"

Write-Host "=== 3. applying migrations ==="
$connection = if ($SqlUser) {
    "Server=$Server;Database=$Database;User ID=$SqlUser;Password=$SqlPassword;TrustServerCertificate=true;"
} else {
    "Server=$Server;Database=$Database;Trusted_Connection=true;TrustServerCertificate=true;"
}
& dotnet ef database update `
    --project $MigrationsProject `
    --startup-project $StartupProject `
    --connection $connection
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet ef failed — consider restoring from $backupFile"
    exit $LASTEXITCODE
}

Write-Host "=== 4. re-checking row counts ==="
$after = Get-RowCounts
$after | ForEach-Object { Write-Host "  $_" }

# Parse "table,count" lines and compare. A shrunk critical table is a rollback trigger.
$beforeMap = @{}; $before | ForEach-Object {
    $parts = $_ -split ','
    if ($parts.Count -eq 2) { $beforeMap[$parts[0].Trim()] = [long]$parts[1].Trim() }
}
$afterMap = @{}; $after | ForEach-Object {
    $parts = $_ -split ','
    if ($parts.Count -eq 2) { $afterMap[$parts[0].Trim()] = [long]$parts[1].Trim() }
}

$regressed = @()
foreach ($t in $criticalTables) {
    if ($afterMap[$t] -lt $beforeMap[$t]) {
        $regressed += "$t: was $($beforeMap[$t]), now $($afterMap[$t])"
    }
}

if ($regressed.Count -gt 0) {
    Write-Error "Row-count regression detected; restoring from backup:"
    $regressed | ForEach-Object { Write-Error "  $_" }
    Invoke-SqlCmd -Query "USE master; ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$Database] FROM DISK = N'$backupFile' WITH REPLACE; ALTER DATABASE [$Database] SET MULTI_USER;"
    exit 3
}

Write-Host "=== done ==="
Write-Host "  backup kept at $backupFile — remove after you've verified the cutover"
exit 0
