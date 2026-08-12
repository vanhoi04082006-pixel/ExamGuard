param(
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $true
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$app = Join-Path $root "src\ExamGuard.App\ExamGuard.App.csproj"
$out = Join-Path $root "artifacts\ExamGuard-$Runtime"

Write-Host "Publishing single-file binary to $out ..."
$selfFlag = if ($SelfContained) { "--self-contained" } else { "--self-contained=false" }

dotnet publish $app -c Release -r $Runtime $selfFlag `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $out

if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

$exe = Join-Path $out "ExamGuard.exe"
if (-not (Test-Path $exe)) { throw "Expected single-file exe not found: $exe" }

$sizeMB = [Math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "OK - single file: $exe ($sizeMB MB)"
