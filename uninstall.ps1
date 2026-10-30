#Requires -RunAsAdministrator
# Убирает пункт из контекстного меню и удаляет ClaudeHere.exe.
foreach ($class in 'Directory', 'Directory\Background', 'Drive') {
    $key = "HKCU:\Software\Classes\$class\shell\OpenWithClaude"
    if (Test-Path -LiteralPath $key) { Remove-Item -LiteralPath $key -Recurse -Force }
}

$installDir = Join-Path $env:ProgramFiles 'ClaudeHere'
if (Test-Path -LiteralPath $installDir) { Remove-Item -LiteralPath $installDir -Recurse -Force }

Write-Host 'Удалено'
