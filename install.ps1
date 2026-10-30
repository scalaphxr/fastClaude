#Requires -RunAsAdministrator
# Собирает ClaudeHere.exe и добавляет пункт в контекстное меню папок.
$ErrorActionPreference = 'Stop'

$label = 'Открыть с помощью Claude'
$installDir = Join-Path $env:ProgramFiles 'ClaudeHere'
$exe = Join-Path $installDir 'ClaudeHere.exe'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$claude = (Get-Command claude).Source

New-Item -ItemType Directory -Force $installDir | Out-Null

& $csc /nologo /target:winexe /codepage:65001 "/out:$exe" "/win32manifest:$PSScriptRoot\app.manifest" "$PSScriptRoot\ClaudeHere.cs"
if ($LASTEXITCODE -ne 0) { throw 'Сборка не удалась' }

# Directory - клик по папке, Directory\Background - по пустому месту внутри папки, Drive - по диску.
foreach ($class in 'Directory', 'Directory\Background', 'Drive') {
    $key = "HKCU:\Software\Classes\$class\shell\OpenWithClaude"
    New-Item -Path $key -Force -Value $label | Out-Null
    New-ItemProperty -Path $key -Name 'Icon' -Value $claude -Force | Out-Null
    New-Item -Path "$key\command" -Force -Value "`"$exe`" `"%V`"" | Out-Null
}

Write-Host "Готово: $exe"
