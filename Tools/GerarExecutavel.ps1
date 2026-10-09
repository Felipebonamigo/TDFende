# Gera Builds\Windows\TDFende.exe sem abrir o Unity (o editor precisa estar FECHADO neste projeto).
#   powershell -ExecutionPolicy Bypass -File Tools\GerarExecutavel.ps1 [-Diagnostico]
# -Diagnostico: Builds\Diagnostico\TDFende.exe com variantes de shader estritas (variante que
# falta vira erro no Player.log). Teste com Tools\Captura.ps1 -Diagnostico.
# -PularLicencas: só em emergência. Por padrão o build confere o disco inteiro contra
# docs\licencas\manifesto.json (inclusive o que o .gitignore esconde: Mixamo, pacotes pagos) e
# não sai com arquivo de arte sem entrada.
param([switch]$Diagnostico, [switch]$PularLicencas)
# Leva alguns minutos (na primeira vez, bem mais: compila os shaders). O log fica em Builds\build.log.
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
# um Unity por vez nesta pasta (MANUAL, seção 2): build em batch e editor aberto brigam
# pela Library e regravam os mesmos .asset
$trava = "$raiz\Builds\.trava"
if (Test-Path $trava) {
    Write-Host "Outro build em andamento (trava $trava, de $((Get-Item $trava).LastWriteTime)). Se não houver, apague a trava."
    exit 3
}
if (Get-Process Unity -ErrorAction SilentlyContinue) {
    Write-Host "O Unity está aberto: feche o editor antes de gerar o executável."
    exit 3
}
if (-not $PularLicencas) {
    Write-Host "Conferindo as licenças da arte no disco..."
    & dotnet run --project "$raiz\Tools\AuditaLicencas" -v quiet --nologo -- --disco --raiz "$raiz"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build recusado: arte sem licença registrada. Acrescente em docs\licencas\manifesto.json (MANUAL, seção 7)."
        exit 4
    }
}
$versao = ((Get-Content "$raiz\ProjectSettings\ProjectVersion.txt" | Select-String 'm_EditorVersion: (\S+)').Matches[0].Groups[1].Value)
$unity = "C:\Program Files\Unity\Hub\Editor\$versao\Editor\Unity.exe"
if (-not (Test-Path $unity)) { throw "Unity $versao não instalado em $unity (instale pelo Hub)" }
New-Item -ItemType Directory -Force "$raiz\Builds" | Out-Null
$log = "$raiz\Builds\build.log"
Write-Host "Gerando o executável com Unity $versao... (log: $log)"
Set-Content -Path $trava -Value "$PID $(Get-Date -Format s)"
try {
$p = Start-Process -FilePath $unity -Wait -PassThru -NoNewWindow -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', "`"$raiz`"",
    '-executeMethod', $(if ($Diagnostico) { 'TDFende.EditorTools.BuildJogo.Diagnostico' } else { 'TDFende.EditorTools.BuildJogo.Windows' }),
    '-logFile', "`"$log`"")
} finally { Remove-Item $trava -ErrorAction SilentlyContinue }
Select-String -Path $log -Pattern '\[TDFende\] build|error CS|Build Finished|BuildFailedException' | ForEach-Object { $_.Line }
if ($p.ExitCode -ne 0) { Write-Host "FALHOU (código $($p.ExitCode)). Veja $log"; exit $p.ExitCode }
Write-Host "Pronto: $raiz\Builds\$(if ($Diagnostico) { 'Diagnostico' } else { 'Windows' })\TDFende.exe"
