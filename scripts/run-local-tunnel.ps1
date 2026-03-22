$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $repoRoot '.env'

if (-not (Test-Path $envFile)) {
    throw "Khong tim thay file .env tai: $envFile"
}

$cloudflared = Get-Command cloudflared -ErrorAction SilentlyContinue
if (-not $cloudflared) {
    $fallbackPaths = @(
        'C:\Program Files\cloudflared\cloudflared.exe',
        'C:\Program Files (x86)\cloudflared\cloudflared.exe',
        (Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps\cloudflared.exe'),
        (Join-Path $env:USERPROFILE 'scoop\shims\cloudflared.exe')
    )

    $fallbackPath = $fallbackPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($fallbackPath) {
        $cloudflared = @{ Source = $fallbackPath }
    } else {
        throw "Khong tim thay cloudflared trong PATH hoac cac thu muc cai dat pho bien."
    }
}

$envLines = Get-Content $envFile
$localTunnelTokenLine = $envLines | Where-Object { $_ -match '^LOCAL_TUNNEL_TOKEN=' } | Select-Object -First 1
$defaultTunnelTokenLine = $envLines | Where-Object { $_ -match '^TUNNEL_TOKEN=' } | Select-Object -First 1

$tokenName = $null
$tunnelToken = $null

if ($localTunnelTokenLine) {
    $tokenName = 'LOCAL_TUNNEL_TOKEN'
    $tunnelToken = $localTunnelTokenLine.Substring('LOCAL_TUNNEL_TOKEN='.Length).Trim()
} elseif ($defaultTunnelTokenLine) {
    $tokenName = 'TUNNEL_TOKEN'
    $tunnelToken = $defaultTunnelTokenLine.Substring('TUNNEL_TOKEN='.Length).Trim()
} else {
    throw "Khong tim thay bien LOCAL_TUNNEL_TOKEN hoac TUNNEL_TOKEN trong file .env"
}

if ([string]::IsNullOrWhiteSpace($tunnelToken)) {
    throw "Gia tri $tokenName trong .env dang rong"
}

Write-Host "Dang chay Cloudflare Tunnel bang $tokenName trong .env..." -ForegroundColor Cyan
Write-Host "Repo root: $repoRoot" -ForegroundColor DarkGray

& $cloudflared.Source tunnel --no-autoupdate run --token $tunnelToken
