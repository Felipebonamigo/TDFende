# Teste automático da U0 (sem abrir o editor): compila TDFendeEditor, roda o replay da ponte no motor e confere o fingerprint.
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\RodarU0.ps1 [-SemCompilar] [-ShotTick 2500]
# Sai com 0 se o fingerprint for IGUAL ao do FlowSim e 1 se não for (ou se o jogo travar). Demora ~3,5 min (200 s de jogo em tempo real).
# Saída em Unreal\TDFende\Saved\U0 (log, print do viewport): fora do git.
param([switch]$SemCompilar, [int]$ShotTick = 2500, [int]$Tempo = 420)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$uproj = Join-Path $PSScriptRoot 'TDFende.uproject'
$ue = 'C:\Program Files\Epic Games\UE_5.8\Engine'
$replay = Join-Path $repo 'Tools\SimBridge\smoke\demo_replay.txt'
$esperado = 't6000 pl20/775/100/56/66 fo22/372/206/9/36 k61/16/1/40/21/0 #835d1db265f91d80'
$out = Join-Path $PSScriptRoot 'Saved\U0'
New-Item -ItemType Directory -Force $out | Out-Null

if (-not $SemCompilar) {
    & "$ue\Build\BatchFiles\Build.bat" TDFendeEditor Win64 Development "-Project=$uproj" -WaitMutex -NoHotReloadFromIDE
    if ($LASTEXITCODE -ne 0) { Write-Host "compilação falhou ($LASTEXITCODE)"; exit 1 }
}
if (-not (Test-Path (Join-Path $PSScriptRoot 'Binaries\Win64\tdfende_sim.dll'))) { Write-Host 'falta a DLL: rode Tools\SimBridge\PublicarParaUnreal.ps1'; exit 1 }

$log = Join-Path $out ("u0-{0:HHmmss}.log" -f (Get-Date))
$jogo = @("`"$uproj`"", '/Engine/Maps/Entry?game=/Script/TDFende.TdfDemoGameMode', '-game', '-windowed', '-ResX=1600', '-ResY=900',
    '-nosplash', '-unattended', '-NoSound', "-abslog=`"$log`"", "-TdfReplay=`"$replay`"",
    "-TdfShot=`"$out\viewport.png`"", "-TdfShotTick=$ShotTick", '-TdfQuit')
$p = Start-Process "$ue\Binaries\Win64\UnrealEditor.exe" -ArgumentList $jogo -PassThru
if (-not $p.WaitForExit($Tempo * 1000)) { $p.Kill(); Write-Host "TRAVOU: não fechou em $Tempo s (veja $log)"; exit 1 }

$linhas = Select-String -Path $log -Pattern 'LogTdf' | ForEach-Object { $_.Line }
$linhas
$fp = ($linhas | Where-Object { $_ -match 'FINGERPRINT' } | Select-Object -First 1)
if ($fp -and $fp.EndsWith($esperado)) { Write-Host 'U0: fingerprint IGUAL ao do FlowSim'; exit 0 }
Write-Host "U0: fingerprint DIFERENTE (esperado: $esperado)"; exit 1
