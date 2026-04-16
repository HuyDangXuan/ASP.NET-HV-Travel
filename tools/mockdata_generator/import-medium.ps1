param(
    [string]$DataDir = ".\output\medium",
    [string]$Database = "HV-Travel",
    [string]$MongoUri = "mongodb://localhost:27017",
    [switch]$DropCollections
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$resolvedDataDir = Resolve-Path (Join-Path $scriptRoot $DataDir) -ErrorAction Stop
$manifestPath = Join-Path $resolvedDataDir "manifest.json"

if (-not (Test-Path $manifestPath)) {
    throw "manifest.json not found in $resolvedDataDir"
}

$mongoImport = Get-Command mongoimport -ErrorAction SilentlyContinue
if (-not $mongoImport) {
    throw "mongoimport was not found in PATH. Install MongoDB Database Tools first."
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

foreach ($file in $manifest.files) {
    if ($file -eq "manifest.json") {
        continue
    }

    $collection = [System.IO.Path]::GetFileNameWithoutExtension($file)
    $filePath = Join-Path $resolvedDataDir $file

    if (-not (Test-Path $filePath)) {
        throw "Missing data file: $filePath"
    }

    $arguments = @(
        "--uri=$MongoUri",
        "--db=$Database",
        "--collection=$collection",
        "--file=$filePath",
        "--jsonArray"
    )

    if ($DropCollections) {
        $arguments += "--drop"
    }

    Write-Host "Importing $collection from $filePath"
    & $mongoImport.Source @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "mongoimport failed for collection $collection with exit code $LASTEXITCODE"
    }
}

Write-Host "Import completed successfully into database '$Database'."
