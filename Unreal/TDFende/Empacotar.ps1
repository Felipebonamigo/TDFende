# U4: empacota o TDFende do Unreal para Windows (cozinha o Content, compila o jogo sem editor, junta a DLL da Sim) em
# Unreal\TDFende\Pacote\Windows, grava o selo de build (git describe) e roda o autoteste NO JOGO EMPACOTADO.
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\Empacotar.ps1 [-Config Development|Shipping] [-SemTeste]
# Development: tem log e o autoteste (-TdfSelfTest). Shipping: o da loja (sem console nem log; o teste roda no Development).
# Sai 0 se empacotou (e, sem -SemTeste, o autoteste passou). Subir para a Steam é outra etapa (skill publicar-na-steam: só o Felipe loga).
param([ValidateSet('Development', 'Shipping')][string]$Config = 'Development', [switch]$SemTeste)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$uproj = Join-Path $PSScriptRoot 'TDFende.uproject'
$uat = 'C:\Program Files\Epic Games\UE_5.8\Engine\Build\BatchFiles\RunUAT.bat'
$saida = Join-Path $PSScriptRoot 'Pacote'
if (-not (Test-Path (Join-Path $PSScriptRoot 'Binaries\Win64\tdfende_sim.dll'))) { Write-Host 'falta a DLL: rode Tools\SimBridge\PublicarParaUnreal.ps1'; exit 1 }

# selo: o mesmo do executável do Unity (git describe --always --dirty); "-dirty" = empacotado com mudança sem commit
$selo = (& git -C $repo describe --always --dirty).Trim()
Write-Host "selo: build $selo"

# outra sessão compilando outro jogo (Detonauto, Age of Earth) segura o UBT e o UAT falha na hora ("ConflictingInstance"; o
# -ubtargs=-WaitMutex não chega ao passo que compila o editor): espera, até 60 min, nenhum UBT rodando
$limite = (Get-Date).AddMinutes(60)
while (Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | Where-Object { $_.CommandLine -match 'UnrealBuildTool' }) {
    if ((Get-Date) -gt $limite) { Write-Host 'outro build do Unreal não terminou em 60 min'; exit 1 }
    Write-Host 'outro build do Unreal rodando; esperando...'
    Start-Sleep -Seconds 30
}
$log = Join-Path $PSScriptRoot 'Saved\Empacotar.log'
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
& $uat BuildCookRun "-project=$uproj" -noP4 -platform=Win64 "-clientconfig=$Config" -build -cook -stage -pak -archive `
    "-archivedirectory=$saida" -unattended -utf8output '-ubtargs=-WaitMutex' 2>&1 | Tee-Object -FilePath $log | Select-String 'BUILD SUCCESSFUL|BUILD FAILED|Error:|ERROR:' | Select-Object -First 20
if ($LASTEXITCODE -ne 0) { Write-Host "empacotamento falhou ($LASTEXITCODE): veja $log"; exit 1 }

$jogo = Join-Path $saida 'Windows'
$exe = Join-Path $jogo 'TDFende.exe'
if (-not (Test-Path $exe)) { Write-Host "não achei $exe"; exit 1 }
# o jogo lê o selo de <projeto empacotado>\Selo.txt (TdfBuildStamp.cpp)
Set-Content -Path (Join-Path $jogo 'TDFende\Selo.txt') -Value $selo -Encoding ascii -NoNewline
$mb = [math]::Round(((Get-ChildItem $jogo -Recurse | Measure-Object Length -Sum).Sum) / 1MB)
Write-Host "empacotado em $jogo ($mb MB)"
if ($SemTeste -or $Config -eq 'Shipping') { exit 0 }

# autoteste no jogo empacotado (o que o Felipe vai jogar): mesma lista de verificações do RodarU0.ps1 -AutoTeste
$tlog = Join-Path $PSScriptRoot 'Saved\Empacotado-autoteste.log'
# A placa de vídeo é dividida com as outras sessões (Detonauto, Age of Earth): o driver já caiu duas vezes ("D3D Device Removed",
# 15 e 16/10/2026) e a repetição passou. Só a QUEDA DE GPU repete (até 3 vezes); falha de verdade do teste nunca é repetida.
for ($i = 1; $i -le 3; $i++) {
    $p = Start-Process $exe -ArgumentList @('-windowed', '-ResX=1600', '-ResY=900', '-nosplash', '-unattended', '-NoSound', "-abslog=`"$tlog`"", '-TdfSelfTest') -PassThru
    if (-not $p.WaitForExit(180 * 1000)) { $p.Kill(); Write-Host "TRAVOU (veja $tlog)"; exit 1 }
    Select-String -Path $tlog -Pattern 'AUTOTESTE (FALHOU|PASSOU|REPROVOU)|\[TDFende\] build' | ForEach-Object { $_.Line -replace '^\[[^\]]+\]\[[^\]]+\]', '' }
    # 4 = inconclusivo (o mouse real mexeu durante o teste de controle): também repete
    if ($p.ExitCode -eq 0 -or -not ($p.ExitCode -eq 4 -or (Select-String -Path $tlog -Pattern 'GPU Crashed or D3D Device Removed' -Quiet))) { exit $p.ExitCode }
    Write-Host "autoteste sem resultado (queda do driver de vídeo ou mouse real mexendo); tentativa $i de 3, repetindo"
}
exit 1
