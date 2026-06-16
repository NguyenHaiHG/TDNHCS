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
    "C:\Program Files\Inno Setup 7\ISCC.exe",
    "D:\Inno Setup 6\ISCC.exe"
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    throw "Chưa tìm thấy ISCC.exe. Nếu đã cài Inno Setup, hãy thêm thư mục chứa ISCC.exe vào PATH hoặc sửa biến `$isccCandidates trong script này."
}

Write-Host "==> Xóa bản build cũ..."
Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $distDir -Recurse -Force -ErrorAction SilentlyContinue
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

Write-Host "==> Tạo installer..."
& $iscc $installerScript

$setupFile = Join-Path $distDir "TDNHCS_Setup.exe"
if (Test-Path $setupFile) {
    Write-Host ""
    Write-Host "Hoàn tất: $setupFile"
    Write-Host "Gửi file này sang máy khác, chạy để cài vào D:\QLVB và tạo shortcut ngoài Desktop."
} else {
    throw "Không tìm thấy file installer sau khi build."
}
