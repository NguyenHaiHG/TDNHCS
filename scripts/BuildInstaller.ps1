param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$InnoCompilerPath = ""
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "TDNHCS\TDNHCS.csproj"
$publishDir = Join-Path $root "publish\win-x64-single"
$distDir = Join-Path $root "dist"
$installerScript = Join-Path $root "installer\TDNHCS.iss"

function Get-ProjectVersion {
    param([string]$ProjectPath)
    [xml]$xml = Get-Content $ProjectPath
    $version = $xml.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($version)) { return "1.0.0" }
    return $version.Trim()
}

$version = Get-ProjectVersion $project
Write-Host "==> Phiên bản: $version"

$isccFromPath = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
$isccFromPathValue = if ($isccFromPath) { $isccFromPath.Source } else { $null }
$isccCandidates = @(
    $InnoCompilerPath,
    $isccFromPathValue,
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 7\ISCC.exe",
    "C:\Program Files\Inno Setup 7\ISCC.exe"
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    throw "Chưa tìm thấy ISCC.exe. Cài Inno Setup 7 hoặc truyền -InnoCompilerPath."
}

Write-Host "==> Xóa bản build cũ..."
Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $publishDir, $distDir | Out-Null

Write-Host "==> Publish ứng dụng self-contained single-file..."
dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

$publishedExe = Join-Path $publishDir "TDNHCS.exe"
if (-not (Test-Path $publishedExe)) {
    throw "Publish không tạo ra $publishedExe. Dừng đóng gói để tránh tạo installer không mở được app."
}

Write-Host "==> Tạo installer (version $version)..."
& $iscc "/DMyAppVersion=$version" $installerScript

$setupFile = Join-Path $distDir "TDNHCS_Setup.exe"
if (-not (Test-Path $setupFile)) {
    throw "Không tìm thấy file installer sau khi build."
}

$releaseNotes = @"
# QLVBNHCS v$version

## Cài đặt
- Chạy TDNHCS_Setup.exe
- Chương trình: D:\QLVB
- Dữ liệu: D:\SysCache_QLVB (tự tạo khi thêm văn bản đầu tiên)
"@

$releaseNotes | Out-File -FilePath (Join-Path $distDir "RELEASE_NOTES.md") -Encoding UTF8

Write-Host ""
Write-Host "Hoàn tất:"
Write-Host "  Installer : $setupFile"
Write-Host "  Version   : $version"
Write-Host "  Tag GitHub: v$version"
Write-Host ""
Write-Host "Bước tiếp theo:"
Write-Host "  1. Upload TDNHCS_Setup.exe lên GitHub Release v$version"
Write-Host "  2. Hoặc chạy: .\scripts\PublishRelease.ps1 -Version $version"
