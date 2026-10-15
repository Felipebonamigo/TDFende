# A/B de desempenho do Unreal: roda a partida de demonstração em cada configuração e mostra FPS e tempo de GPU médios depois do
# aquecimento, e confere o orçamento (GPU média do caso "completo" até $OrcamentoGpuMs; sai 1 se passar).
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\MedirFps.ps1 [-Tiques 1500] [-OrcamentoGpuMs 8] [-SoCompleto]
# Cada linha de $casos é um nome e os argumentos extras (flags do TDFende ou -ExecCmds com variáveis de console).
# "media" e "leve" usam os grupos de escalabilidade do Unreal (sg.*): é quanto sobra num PC mais fraco que este.
# Cada caso roda $Repeticoes vezes e vale a de MENOR GPU: outra carga de vídeo no PC (navegador, janela em primeiro plano) já
# dobrou a medida de uma rodada (15/10/2026: 10,8 ms numa, 5,0 ms nas duas seguintes).
param([int]$Tiques = 1500, [double]$OrcamentoGpuMs = 8.0, [switch]$SoCompleto, [int]$Repeticoes = 2)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$uproj = Join-Path $PSScriptRoot 'TDFende.uproject'
$ue = 'C:\Program Files\Epic Games\UE_5.8\Engine\Binaries\Win64\UnrealEditor.exe'
$replay = Join-Path $repo 'Tools\SimBridge\smoke\demo_replay.txt'
$out = Join-Path $PSScriptRoot 'Saved\Fps'
New-Item -ItemType Directory -Force $out | Out-Null
$casos = [ordered]@{
    'completo'          = @()
    'qualidade media'   = @('-ExecCmds="sg.ShadowQuality 2, sg.GlobalIlluminationQuality 2, sg.ReflectionQuality 2, sg.EffectsQuality 2, sg.FoliageQuality 2, sg.PostProcessQuality 2"')
    'leve'              = @('-ExecCmds="sg.ShadowQuality 1, sg.GlobalIlluminationQuality 1, sg.ReflectionQuality 1, sg.EffectsQuality 1, sg.FoliageQuality 1, sg.PostProcessQuality 1, r.ScreenPercentage 70"')
    'sem grama'         = @('-TdfSemGrama')
    'sem nevoa volum.'  = @('-ExecCmds="r.VolumetricFog 0"')
    'sem nuvens'        = @('-ExecCmds="r.VolumetricCloud 0"')
    'sem Lumen'         = @('-ExecCmds="r.DynamicGlobalIlluminationMethod 0, r.ReflectionMethod 2"')
    'sem sombra virtual'= @('-ExecCmds="r.Shadow.Virtual.Enable 0"')
}
$gpuCompleto = $null
foreach ($nome in $casos.Keys) {
    if ($SoCompleto -and $nome -ne 'completo') { continue }
    $fps = 0; $gpu = [double]::MaxValue
    for ($r = 1; $r -le $Repeticoes; $r++) {
        $log = Join-Path $out (($nome -replace '[^a-z]', '_') + "_$r.log")
        $a = @("`"$uproj`"", '-game', '-windowed', '-ResX=1600', '-ResY=900', '-nosplash', '-unattended', '-NoSound', "-abslog=`"$log`"",
            "-TdfReplay=`"$replay`"", "-TdfQuitTick=$Tiques") + $casos[$nome]
        $p = Start-Process $ue -ArgumentList $a -PassThru
        if (-not $p.WaitForExit(600000)) { $p.Kill(); Write-Host "$nome : TRAVOU"; continue }
        # janelas de 5 s depois do primeiro terço (aquecimento: compilação de shader, carga); o log traz FPS e GPU (ms) de cada uma
        $linhas = Select-String -Path $log -Pattern 'LogTdf: Display: FPS ([0-9.]+).*GPU ([0-9.]+) ms'
        $uteis = $linhas | Select-Object -Skip ([math]::Floor($linhas.Count / 3))
        $f = ($uteis | ForEach-Object { [double]$_.Matches[0].Groups[1].Value } | Measure-Object -Average).Average
        $g = ($uteis | ForEach-Object { [double]$_.Matches[0].Groups[2].Value } | Measure-Object -Average).Average
        if ($g -lt $gpu) { $gpu = $g; $fps = $f }
    }
    if ($nome -eq 'completo') { $gpuCompleto = $gpu }
    Write-Host ("{0,-20} {1,6:N1} fps   GPU {2,5:N2} ms" -f $nome, $fps, $gpu)
}
if ($null -eq $gpuCompleto) { Write-Host 'sem medida do caso completo'; exit 1 }
if ($gpuCompleto -gt $OrcamentoGpuMs) { Write-Host ("ORÇAMENTO ESTOURADO: GPU {0:N2} ms > {1} ms" -f $gpuCompleto, $OrcamentoGpuMs); exit 1 }
Write-Host ("orçamento ok: GPU {0:N2} ms <= {1} ms (1600x900, este PC)" -f $gpuCompleto, $OrcamentoGpuMs)
exit 0
