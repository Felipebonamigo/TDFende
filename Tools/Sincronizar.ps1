# Sincroniza este PC com o GitHub, sem ninguém digitar nada.
# Roda sozinho a cada 2 minutos (tarefa agendada criada pelo InstalarSincronizacao.ps1).
#
#  1. puxa o que o Claude mandou para a main (guarda e devolve alterações locais: --autostash)
#  2. manda para o GitHub SÓ a arte baixada (Cenario e Textures) — nunca código ou
#     configuração mexidos à mão, para não publicar nada que você não quis
#
# Registro de cada rodada: Tools\sincronizar.log

$ErrorActionPreference = 'Continue'
$repo = Split-Path -Parent $PSScriptRoot
$log = Join-Path $PSScriptRoot 'sincronizar.log'
function Log($msg) { Add-Content -Path $log -Value ("{0:yyyy-MM-dd HH:mm:ss}  {1}" -f (Get-Date), $msg) -Encoding UTF8 }

Set-Location $repo
if ((git rev-parse --abbrev-ref HEAD) -ne 'main') { Log 'fora da main: nada feito'; exit 0 }

$before = git rev-parse HEAD
$out = git pull --rebase --autostash origin main 2>&1
if ($LASTEXITCODE -ne 0) {
    git rebase --abort 2>$null
    Log "pull falhou, nada mudado: $out"
    exit 1
}
$after = git rev-parse HEAD
if ($before -ne $after) { Log "atualizado: $($before.Substring(0,7)) -> $($after.Substring(0,7))" }

# o Claude mudou a lista de arte a baixar? roda o download de novo (só baixa o que falta)
$dl = Join-Path $PSScriptRoot 'BaixarArte.ps1'
$stamp = Join-Path $PSScriptRoot '.baixararte.hash'
if (Test-Path $dl) {
    $hash = (Get-FileHash $dl -Algorithm SHA256).Hash
    $last = if (Test-Path $stamp) { Get-Content $stamp -Raw } else { '' }
    if ($hash -ne $last.Trim()) {
        try {
            & $dl *>> $log
            Set-Content -Path $stamp -Value $hash
            Log 'BaixarArte mudou: download rodado de novo'
        } catch { Log "BaixarArte falhou: $($_.Exception.Message)" }
    }
}

$art = @('Assets/Resources/TDFende/Cenario', 'Assets/Resources/TDFende/Textures')
$art = @($art | Where-Object { Test-Path (Join-Path $repo $_) })
if ($art.Count -gt 0) {
    git add -- $art 2>$null
    git diff --cached --quiet
    if ($LASTEXITCODE -ne 0) {
        git commit -q -m 'Arte baixada da Poly Haven (sincronização automática)' 2>&1 | Out-Null
        $push = git push -q origin main 2>&1
        if ($LASTEXITCODE -eq 0) { Log 'arte enviada ao GitHub' } else { Log "push falhou: $push" }
    }
}
