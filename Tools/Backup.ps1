# Copia para o D: o que NÃO está no git (TEC-26): os GLBs originais (Meshy e downloads), a
# quarentena e o último executável. O D: é outro disco físico (o C: é o disco 1, o D: o 0).
#   powershell -ExecutionPolicy Bypass -File Tools\Backup.ps1 [-Destino D:\TDFende-backup]
# robocopy /E sem /PURGE: o que for apagado no PC continua no D:. Sai com 0 (ok) ou 1 (erro).
# Restaurar: copie de volta a pasta de D:\TDFende-backup\<item> para o lugar listado abaixo
# (camada-privada -> Assets\_Privado; o Unity refaz os .meta ao abrir).
param([string]$Destino = 'D:\TDFende-backup')
$raiz = Split-Path -Parent $PSScriptRoot
$itens = @(
    @{ de = "$raiz\Tools\ConverterBichos\glb"; para = 'ConverterBichos-glb' },
    @{ de = "$raiz\Tools\ConverterTorres\glb"; para = 'ConverterTorres-glb' },
    @{ de = "$raiz\Assets\_Privado"; para = 'camada-privada' },
    @{ de = "$env:USERPROFILE\TDFende-quarentena"; para = 'quarentena' },
    @{ de = "$raiz\Builds\Windows"; para = 'executavel' }
)
New-Item -ItemType Directory -Force $Destino | Out-Null
$falhou = $false
foreach ($i in $itens) {
    if (-not (Test-Path $i.de)) { Write-Host "pulado (não existe): $($i.de)"; continue }
    $alvo = Join-Path $Destino $i.para
    robocopy $i.de $alvo /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    # robocopy: 0 a 7 = ok (0 nada novo, 1 copiou...); 8 ou mais = erro
    if ($LASTEXITCODE -ge 8) { Write-Host "ERRO: $($i.de) (robocopy $LASTEXITCODE)"; $falhou = $true }
    else { Write-Host "ok: $($i.de) -> $alvo" }
}
$estado = if ($falhou) { 'com erro' } else { 'ok' }
Add-Content -Path (Join-Path $Destino 'backup.log') -Value ("{0:s}  backup {1}" -f (Get-Date), $estado)
if ($falhou) { exit 1 } else { exit 0 }
