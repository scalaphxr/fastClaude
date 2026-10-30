# Собирает dist\fastClaude-Setup.exe. Нужен только .NET Framework 4, он уже есть в Windows.
$ErrorActionPreference = 'Stop'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null

& $csc /nologo /optimize+ /target:winexe /codepage:65001 "/out:$dist\fastClaude-Setup.exe" "/win32manifest:$PSScriptRoot\app.manifest" "/win32icon:$PSScriptRoot\assets\icon.ico" "$PSScriptRoot\fastClaude.cs"
if ($LASTEXITCODE -ne 0) { throw 'Сборка не удалась' }

(Get-FileHash "$dist\fastClaude-Setup.exe" -Algorithm SHA256).Hash
