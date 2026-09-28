# Instalação única: deixa este PC sincronizando sozinho com o GitHub.
#
#   powershell -ExecutionPolicy Bypass -File Tools\InstalarSincronizacao.ps1
#
# Faz, uma vez: tira as caixas de plástico da primeira leva, baixa o que faltar da Poly
# Haven, sincroniza (a primeira vez pode pedir login do GitHub numa janela — é para
# guardar a senha; depois nunca mais) e cria a tarefa "TDFende - Sincronizar", que
# repete a sincronização a cada 2 minutos, escondida.
#
# Para desligar:  Unregister-ScheduledTask -TaskName 'TDFende - Sincronizar' -Confirm:$false

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

Write-Host '1/4 atualizando o projeto...'
git pull --rebase --autostash origin main

Write-Host '2/4 tirando as caixas de plástico...'
foreach ($p in 'plastic_crate_01', 'plastic_crate_02') {
    $dir = Join-Path $repo "Assets/Resources/TDFende/Cenario/Caixas/$p"
    if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Remove-Item -Force "$dir.meta" -ErrorAction SilentlyContinue }
}

Write-Host '3/4 baixando o que faltar da Poly Haven...'
& (Join-Path $PSScriptRoot 'BaixarArte.ps1')

Write-Host '4/4 primeira sincronização e tarefa agendada...'
& (Join-Path $PSScriptRoot 'Sincronizar.ps1')

$script = Join-Path $PSScriptRoot 'Sincronizar.ps1'
$action = New-ScheduledTaskAction -Execute 'powershell.exe' `
    -Argument "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$script`"" -WorkingDirectory $repo
$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 2)
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -DontStopIfGoingOnBatteries -AllowStartIfOnBatteries `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 5) -MultipleInstances IgnoreNew
Register-ScheduledTask -TaskName 'TDFende - Sincronizar' -Action $action -Trigger $trigger -Settings $settings `
    -Description 'Puxa o que o Claude manda para a main do TDFende e envia a arte baixada.' -Force | Out-Null

Write-Host ''
Write-Host 'Pronto. A cada 2 minutos este PC puxa o que o Claude mandar para o GitHub.'
Write-Host 'Com o Unity aberto, basta clicar na janela dele para ele recarregar.'
Write-Host 'Registro: Tools\sincronizar.log'
