# Confere que a grama balança no vento: dois prints da mesma cena, 2 s de jogo de distância (instantes 700 e 760, com a exposição já
# assentada), com a câmera rente à grama; conta os pixels que mudaram bastante na faixa do capim. Sem vento são ~20 pixels (só ruído);
# com o vento normal, ~2.800 (medido em 16/10/2026). Reprova (sai 1) se mudarem menos de $Minimo.
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\TestarVento.ps1 [-Minimo 500]
param([int]$Minimo = 500)
$ErrorActionPreference = 'Stop'
$uproj = Join-Path $PSScriptRoot 'TDFende.uproject'
$ue = 'C:\Program Files\Epic Games\UE_5.8\Engine\Binaries\Win64\UnrealEditor.exe'
$out = Join-Path $PSScriptRoot 'Saved\U0'
New-Item -ItemType Directory -Force $out | Out-Null
$jogo = @("`"$uproj`"", '/Engine/Maps/Entry?game=/Script/TDFende.TdfDemoGameMode', '-game', '-windowed', '-ResX=1600', '-ResY=900',
    '-nosplash', '-unattended', '-NoSound', "-abslog=`"$out\vento.log`"", "-TdfShot=`"$out\vento.png`"", '-TdfShotTicks=700,760', '-TdfQuitTick=780',
    '-TdfCamX=-3200', '-TdfCamY=-1500', '-TdfCamZ=120', '-TdfCamPitch=-8', '-TdfCamYaw=0', '-TdfCamFov=60')
$p = Start-Process $ue -ArgumentList $jogo -PassThru
if (-not $p.WaitForExit(400 * 1000)) { $p.Kill(); Write-Host 'TRAVOU'; exit 1 }
Add-Type -AssemblyName System.Drawing
$a = New-Object System.Drawing.Bitmap (Join-Path $out 'vento_700.png')
$b = New-Object System.Drawing.Bitmap (Join-Path $out 'vento_760.png')
$mudou = 0
foreach ($y in 330..700) { foreach ($x in 900..1560) {
    $pa = $a.GetPixel($x, $y); $pb = $b.GetPixel($x, $y)
    if ([math]::Abs($pa.R - $pb.R) + [math]::Abs($pa.G - $pb.G) + [math]::Abs($pa.B - $pb.B) -gt 60) { $mudou++ } } }
$a.Dispose(); $b.Dispose()
Write-Host "pixels que mudaram na faixa do capim: $mudou (mínimo $Minimo)"
if ($mudou -lt $Minimo) { Write-Host 'VENTO: a grama não se mexe'; exit 1 }
Write-Host 'VENTO: a grama balança'
exit 0
