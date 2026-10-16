# Caça à queda do driver de vídeo ("D3D Device Removed", sempre no desenho das sombras virtuais do Nanite): roda o autoteste várias
# vezes em cada configuração, ALTERNANDO entre elas (a carga das outras sessões e do Windows pesa igual em todas) e conta as quedas.
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\CacarQueda.ps1 [-Rodadas 20]
# Configurações: padrão, sem tesselação do Nanite, sem sombras virtuais (controle: se o problema é lá, aqui tem que zerar).
# Uma queda é contada pelo texto "GPU Crashed or D3D Device Removed" no log; falha de teste comum não conta.
param([int]$Rodadas = 20)
$ErrorActionPreference = 'Stop'
$uproj = Join-Path $PSScriptRoot 'TDFende.uproject'
$ue = 'C:\Program Files\Epic Games\UE_5.8\Engine\Binaries\Win64\UnrealEditor.exe'
$out = Join-Path $PSScriptRoot 'Saved\Queda'
New-Item -ItemType Directory -Force $out | Out-Null
$config = [ordered]@{
    'padrao'            = @()
    'sem tesselacao'    = @('-ExecCmds="r.Nanite.Tessellation 0"')
    'sem sombra virtual'= @('-ExecCmds="r.Shadow.Virtual.Enable 0"')
}
$queda = @{}; $ok = @{}; $outro = @{}
foreach ($k in $config.Keys) { $queda[$k] = 0; $ok[$k] = 0; $outro[$k] = 0 }
for ($r = 1; $r -le $Rodadas; $r++) {
    foreach ($k in $config.Keys) {
        $log = Join-Path $out (("{0:00}_{1}" -f $r, ($k -replace '[^a-z]', '_')) + '.log')
        $a = @("`"$uproj`"", '/Engine/Maps/Entry?game=/Script/TDFende.TdfDemoGameMode', '-game', '-windowed', '-ResX=1600', '-ResY=900',
            '-nosplash', '-unattended', '-NoSound', "-abslog=`"$log`"", '-TdfSelfTest') + $config[$k]
        $p = Start-Process $ue -ArgumentList $a -PassThru
        if (-not $p.WaitForExit(150 * 1000)) { $p.Kill() }
        if (Select-String -Path $log -Pattern 'GPU Crashed or D3D Device Removed' -Quiet) { $queda[$k]++ }
        elseif (Select-String -Path $log -Pattern 'AUTOTESTE PASSOU' -Quiet) { $ok[$k]++ }
        else { $outro[$k]++ }
    }
    Write-Host ("rodada {0}/{1}: " -f $r, $Rodadas) -NoNewline
    Write-Host (($config.Keys | ForEach-Object { "{0}: {1} queda(s)" -f $_, $queda[$_] }) -join ' | ')
}
Write-Host ''
foreach ($k in $config.Keys) { Write-Host ("{0,-20} passou {1,2}   queda de GPU {2,2}   outro resultado {3,2}   (de {4})" -f $k, $ok[$k], $queda[$k], $outro[$k], $Rodadas) }
