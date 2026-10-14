# Baixa as texturas de chão da Poly Haven (CC0, fotografadas, sem login) para Unreal\TDFende\Fontes\PolyHaven\<id>\ (fora do git:
# arte pesada fica no PC, com backup no D: pelo Tools\Backup.ps1). Depois: ImportarArte.ps1 -Script importa_ambiente.py.
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\BaixarTexturas.ps1 [-Resolucao 4k]
# Rodar de novo não baixa o que já existe. Licença registrada em docs/licencas/manifesto.json (entrada unreal-chao-polyhaven-4k).
param([string]$Resolucao = '4k')
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # a barra de progresso do PowerShell 5.1 deixa o download 10x mais lento
$destino = Join-Path $PSScriptRoot 'Fontes\PolyHaven'
$headers = @{ 'User-Agent' = 'TDFende-BaixarArte/1.0 (jogo indie, uso CC0)' }
# para a câmera de cima, as texturas "aéreas" (fotografadas do alto, cobrindo vários metros) são as que leem melhor
$conjuntos = @(
    'aerial_grass_rock',   # grama com pedras, 15 m: a camada principal
    'dirt_aerial_02',      # terra, 20 m: manchas de chão batido
    'forest_ground_04',    # folhas e galhos, 3,2 m: detalhe perto das árvores
    'rocky_terrain_02'     # terreno rochoso, 90 m: variação em escala grande (quebra a repetição)
)
$mapas = @('Diffuse', 'nor_gl', 'arm', 'Displacement')
$total = 0
foreach ($id in $conjuntos) {
    $files = Invoke-RestMethod "https://api.polyhaven.com/files/$id" -Headers $headers
    $dir = Join-Path $destino $id
    New-Item -ItemType Directory -Force $dir | Out-Null
    foreach ($m in $mapas) {
        $res = $files.$m.$Resolucao
        if (-not $res) { Write-Host "  $id sem $m em $Resolucao"; continue }
        $f = if ($res.jpg) { $res.jpg } else { $res.png }
        $nome = Split-Path $f.url -Leaf
        $alvo = Join-Path $dir $nome
        if (-not (Test-Path $alvo)) {
            Invoke-WebRequest -Uri $f.url -Headers $headers -OutFile $alvo -UseBasicParsing
            $total += $f.size
        }
        Write-Host ("  {0}/{1} ({2:N1} MB)" -f $id, $nome, ((Get-Item $alvo).Length / 1MB))
    }
}
Write-Host ("baixados agora: {0:N1} MB em {1}" -f ($total / 1MB), $destino)
