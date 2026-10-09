# Roda o teste de fumaça do executável (-captura) e devolve o código de saída do jogo:
#   0 = passou, 1 = reprovou (o motivo sai na tela), 2 = travou (passou do tempo e foi fechado).
#   powershell -ExecutionPolicy Bypass -File Tools\Captura.ps1 [-Saida C:\caminho\print.png] [-Estresse]
# -Estresse: fim de partida (6 torres no máximo por lane e 4 rodadas de todos os bichos nas duas).
# -Extra: argumentos a mais para o jogo, ex.: -Extra -sem-grama
# Gera print.png, print_grama.png, print_bicho_<Nome>.png (e _simples), print_metricas.json.
param(
    [string]$Saida = "$env:TEMP\tdf_captura.png",
    [int]$Tempo = 150,
    [switch]$Estresse,
    [string[]]$Extra = @()
)
$raiz = Split-Path -Parent $PSScriptRoot
$exe = "$raiz\Builds\Windows\TDFende.exe"
if (-not (Test-Path $exe)) { Write-Host "Sem executável: rode Tools\GerarExecutavel.ps1"; exit 2 }
$log = "$env:USERPROFILE\AppData\LocalLow\DefaultCompany\TDFende\Player.log"
$argumentos = @('-screen-fullscreen', '0', '-screen-width', '1600', '-screen-height', '900', '-captura', "`"$Saida`"")
if ($Estresse) { $argumentos += '-estresse' }
$argumentos += $Extra
$p = Start-Process -FilePath $exe -PassThru -ArgumentList $argumentos
if (-not $p.WaitForExit($Tempo * 1000)) {
    $p.Kill()
    Write-Host "TRAVOU: o jogo não fechou em $Tempo s (veja $log)"
    exit 2
}
Select-String -Path $log -Pattern '\[TDFende\] captura(, cobertura|, desempenho|, REPROVA|: PASSOU|: REPROVOU)' |
    ForEach-Object { $_.Line }
Write-Host "Código de saída: $($p.ExitCode)  (prints e métricas em $(Split-Path -Parent $Saida))"
exit $p.ExitCode
