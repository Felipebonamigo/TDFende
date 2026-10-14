# A/B de desempenho do Unreal: roda a partida de demonstração em cada configuração e mostra o FPS médio depois do aquecimento.
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\MedirFps.ps1
# Cada linha de $casos é um nome e os argumentos extras (flags do TDFende ou -ExecCmds com variáveis de console).
param([int]$Tiques = 1500)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$uproj = Join-Path $PSScriptRoot 'TDFende.uproject'
$ue = 'C:\Program Files\Epic Games\UE_5.8\Engine\Binaries\Win64\UnrealEditor.exe'
$replay = Join-Path $repo 'Tools\SimBridge\smoke\demo_replay.txt'
$out = Join-Path $PSScriptRoot 'Saved\Fps'
New-Item -ItemType Directory -Force $out | Out-Null
$casos = [ordered]@{
    'completo'          = @()
    'sem grama'         = @('-TdfSemGrama')
    'sem nevoa volum.'  = @('-ExecCmds="r.VolumetricFog 0"')
    'sem nuvens'        = @('-ExecCmds="r.VolumetricCloud 0"')
    'sem Lumen'         = @('-ExecCmds="r.DynamicGlobalIlluminationMethod 0, r.ReflectionMethod 2"')
    'sem sombra virtual'= @('-ExecCmds="r.Shadow.Virtual.Enable 0"')
}
foreach ($nome in $casos.Keys) {
    $log = Join-Path $out (($nome -replace '[^a-z]', '_') + '.log')
    $a = @("`"$uproj`"", '-game', '-windowed', '-ResX=1600', '-ResY=900', '-nosplash', '-unattended', '-NoSound', "-abslog=`"$log`"",
        "-TdfReplay=`"$replay`"", "-TdfQuitTick=$Tiques") + $casos[$nome]
    $p = Start-Process $ue -ArgumentList $a -PassThru
    if (-not $p.WaitForExit(600000)) { $p.Kill(); Write-Host "$nome : TRAVOU"; continue }
    # FPS das janelas de 5 s depois do primeiro terço (aquecimento: compilação de shader, carga)
    $fps = Select-String -Path $log -Pattern 'LogTdf: Display: FPS ([0-9.]+)' | ForEach-Object { [double]$_.Matches[0].Groups[1].Value }
    $uteis = $fps | Select-Object -Skip ([math]::Floor($fps.Count / 3))
    $media = if ($uteis) { ($uteis | Measure-Object -Average).Average } else { 0 }
    Write-Host ("{0,-20} {1,6:N1} fps" -f $nome, $media)
}
