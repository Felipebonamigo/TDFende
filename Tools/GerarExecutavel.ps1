# Gera Builds\Windows\TDFende.exe sem abrir o Unity (o editor precisa estar FECHADO neste projeto).
#   powershell -ExecutionPolicy Bypass -File Tools\GerarExecutavel.ps1
# Leva alguns minutos (na primeira vez, bem mais: compila os shaders). O log fica em Builds\build.log.
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$versao = ((Get-Content "$raiz\ProjectSettings\ProjectVersion.txt" | Select-String 'm_EditorVersion: (\S+)').Matches[0].Groups[1].Value)
$unity = "C:\Program Files\Unity\Hub\Editor\$versao\Editor\Unity.exe"
if (-not (Test-Path $unity)) { throw "Unity $versao não instalado em $unity (instale pelo Hub)" }
New-Item -ItemType Directory -Force "$raiz\Builds" | Out-Null
$log = "$raiz\Builds\build.log"
Write-Host "Gerando o executável com Unity $versao... (log: $log)"
$p = Start-Process -FilePath $unity -Wait -PassThru -NoNewWindow -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', "`"$raiz`"",
    '-executeMethod', 'TDFende.EditorTools.BuildJogo.Windows', '-logFile', "`"$log`"")
Select-String -Path $log -Pattern '\[TDFende\] build|error CS|Build Finished|BuildFailedException' | ForEach-Object { $_.Line }
if ($p.ExitCode -ne 0) { Write-Host "FALHOU (código $($p.ExitCode)). Veja $log"; exit $p.ExitCode }
Write-Host "Pronto: $raiz\Builds\Windows\TDFende.exe"
