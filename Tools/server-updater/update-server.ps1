param(
    [string]$Repo = "vaciliykotov-sketch/space-front-erp-marines",
    [string]$Tag = "latest",
    [string]$Asset = "SS14.Server_win-x64.zip",
    [string]$ServerDir = "C:\SS14\server",
    [string]$ServiceName = "SS14Server",
    [string[]]$Keep = @("data", "server_config.toml"),
    [string]$Token = $env:GITHUB_TOKEN
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$stateDir = Join-Path $ServerDir "..\updater"
New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
$stateFile = Join-Path $stateDir "last-sha.txt"
$logFile = Join-Path $stateDir "updater.log"

function Log($msg) {
    Add-Content -Path $logFile -Value ("{0} {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $msg)
}

$mutex = New-Object System.Threading.Mutex($false, "Global\SS14ServerUpdater")
if (-not $mutex.WaitOne(0)) { exit 0 }

try {
    $headers = @{ "User-Agent" = "ss14-updater"; "Accept" = "application/vnd.github+json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    $release = Invoke-RestMethod -Headers $headers -Uri "https://api.github.com/repos/$Repo/releases/tags/$Tag"
    $sha = $release.body.Trim()
    $last = if (Test-Path $stateFile) { (Get-Content $stateFile -Raw).Trim() } else { "" }
    if ($sha -eq $last) { exit 0 }

    $asset = $release.assets | Where-Object { $_.name -eq $Asset } | Select-Object -First 1
    if (-not $asset) { throw "Asset $Asset not found in release $Tag" }

    $tmp = Join-Path $env:TEMP "ss14-update"
    if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
    New-Item -ItemType Directory -Path $tmp | Out-Null
    $zip = Join-Path $tmp $Asset
    $dlHeaders = $headers.Clone()
    $dlHeaders["Accept"] = "application/octet-stream"
    Invoke-WebRequest -Headers $dlHeaders -Uri $asset.url -OutFile $zip -UseBasicParsing
    $extract = Join-Path $tmp "new"
    Expand-Archive -Path $zip -DestinationPath $extract -Force

    Log "Updating to $sha"
    Stop-Service -Name $ServiceName -Force
    try {
        $xd = $Keep | ForEach-Object { Join-Path $ServerDir $_ }
        robocopy $extract $ServerDir /MIR /XD ($xd | Where-Object { Test-Path $_ -PathType Container }) /XF ($xd | Where-Object { Test-Path $_ -PathType Leaf }) /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy failed: $LASTEXITCODE" }
        Set-Content -Path $stateFile -Value $sha
        Log "Updated to $sha"
    }
    finally {
        Start-Service -Name $ServiceName
    }
    Remove-Item $tmp -Recurse -Force
}
catch {
    Log "ERROR: $_"
    exit 1
}
finally {
    $mutex.ReleaseMutex()
}
