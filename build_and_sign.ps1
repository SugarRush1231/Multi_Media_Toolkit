[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# ============================================
# Multi Media Toolkit - 빌드 & 서명 & 설치파일 생성
# ============================================

$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot
$PublishDir = "$ProjectDir\bin\Release\net10.0-windows\win-x64\publish"
$ExePath = "$PublishDir\Multi Media Toolkit.exe"
$CertPath = "$ProjectDir\MMT_Cert.pfx"
$CertPassword = "1234"
$SignToolPath = "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"
$DistDir = "$ProjectDir\dist"
$DenoPath = "$ProjectDir\deno.exe"
$InstalledDenoPath = Join-Path $env:LOCALAPPDATA "YoutubeDownloader\deno.exe"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Multi Media Toolkit Build Pipeline" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# --- 1/3: Build ---
Write-Host "`n[1/3] dotnet publish..." -ForegroundColor Yellow
Set-Location $ProjectDir
dotnet publish -c Release -r win-x64 --self-contained true
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build FAILED!" -ForegroundColor Red
    exit 1
}
Write-Host "  Build OK!" -ForegroundColor Green

# --- 2/3: Sign EXE ---
Write-Host "`n[2/3] Signing EXE..." -ForegroundColor Yellow
$CanSign = (Test-Path $CertPath) -and (Test-Path $SignToolPath)
if ($CanSign) {
    & $SignToolPath sign /f $CertPath /p $CertPassword /fd SHA256 $ExePath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Signing FAILED!" -ForegroundColor Red
        exit 1
    }
    Write-Host "  EXE Signed!" -ForegroundColor Green
} else {
    Write-Host "  Certificate not found. Continuing without signing." -ForegroundColor DarkYellow
}

if (!(Test-Path $DenoPath) -and (Test-Path $InstalledDenoPath)) {
    Write-Host "  Preparing bundled Deno..." -ForegroundColor Yellow
    Copy-Item -LiteralPath $InstalledDenoPath -Destination $DenoPath
}

# --- 3/3: Inno Setup ---
Write-Host "`n[3/3] Creating installer..." -ForegroundColor Yellow
$InnoCompiler = $null
$InnoPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
)
foreach ($p in $InnoPaths) {
    if (Test-Path $p) {
        $InnoCompiler = $p
        break
    }
}

if ($InnoCompiler) {
    & $InnoCompiler "$ProjectDir\installer_script.iss"

    $SetupExe = Get-ChildItem "$DistDir\MMT_Setup_*.exe" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($SetupExe -and $CanSign) {
        Write-Host "  Signing installer: $($SetupExe.Name)" -ForegroundColor Yellow
        & $SignToolPath sign /f $CertPath /p $CertPassword /fd SHA256 $SetupExe.FullName
        Write-Host "  Installer Signed!" -ForegroundColor Green
    } elseif ($SetupExe) {
        Write-Host "  Installer created without signing." -ForegroundColor DarkYellow
    }
} else {
    Write-Host "  Inno Setup not found. Skipping installer." -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  DONE!" -ForegroundColor Green
Write-Host "  EXE: $ExePath" -ForegroundColor White
if (Test-Path $DistDir) {
    Get-ChildItem "$DistDir\MMT_Setup_*.exe" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "  Installer: $($_.FullName)" -ForegroundColor White
    }
}
Write-Host "========================================" -ForegroundColor Cyan
