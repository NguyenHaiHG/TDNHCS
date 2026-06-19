param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Notes = "",
    [switch]$SkipBuild,
    [switch]$SkipGh
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "TDNHCS\TDNHCS.csproj"
$setupFile = Join-Path $root "dist\TDNHCS_Setup.exe"

function Set-ProjectVersion {
    param([string]$ProjectPath, [string]$NewVersion)

    $content = Get-Content $ProjectPath -Raw
    $content = $content -replace '<Version>[^<]+</Version>', "<Version>$NewVersion</Version>"
    $content = $content -replace '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$NewVersion.0</AssemblyVersion>"
    $content = $content -replace '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$NewVersion.0</FileVersion>"
    Set-Content -Path $ProjectPath -Value $content -Encoding UTF8
}

Write-Host "==> Cập nhật version trong csproj -> $Version"
Set-ProjectVersion -ProjectPath $project -NewVersion $Version

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot "BuildInstaller.ps1")
}

if (-not (Test-Path $setupFile)) {
    throw "Không tìm thấy $setupFile. Hãy build installer trước."
}

if ($SkipGh) {
    Write-Host "Bỏ qua tạo GitHub Release."
    exit 0
}

$gh = Get-Command "gh" -ErrorAction SilentlyContinue
if (-not $gh) {
    Write-Host "Chưa cài GitHub CLI (gh). Upload thủ công file:"
    Write-Host "  $setupFile"
    Write-Host "  Tag: v$Version"
    exit 0
}

$tag = "v$Version"
$releaseNotes = if ([string]::IsNullOrWhiteSpace($Notes)) {
    Get-Content (Join-Path $root "dist\RELEASE_NOTES.md") -Raw
} else {
    $Notes
}

Write-Host "==> Tạo GitHub Release $tag"
gh release create $tag $setupFile `
    --title "QLVBNHCS $tag" `
    --notes $releaseNotes

Write-Host "Hoàn tất release: $tag"
