# TDFende - abre no navegador a busca de cada bicho, já filtrada por "baixável" e "animado".
# Rode na pasta do projeto:  powershell -ExecutionPolicy Bypass -File Tools\BaixarBichos.ps1
#
# Em cada aba:
#  1. escolha um bicho REALISTA que tenha animação de andar (veja o ícone de play na miniatura);
#  2. na página do modelo, confira a licença: "CC Attribution" ou "CC0" (evite "NoDerivs"/"NonCommercial");
#  3. Download -> prefira "FBX" (formato original); se só houver glTF/GLB, veja o ASSETS.md;
#  4. descompacte e coloque o .fbx (e a pasta de texturas, se vier) em
#     Assets\Resources\TDFende\Bichos\  — o nome do arquivo precisa ter o bicho (ex.: elefante.fbx);
#  5. anote o autor e o link: licença CC Attribution pede crédito (mande para eu pôr no THIRD_PARTY.md).

$bichos = @(
  @{ pt = 'Rato';        en = 'rat' },
  @{ pt = 'Cachorro';    en = 'dog' },
  @{ pt = 'Lobo';        en = 'wolf' },
  @{ pt = 'Javali';      en = 'wild boar' },
  @{ pt = 'Águia';       en = 'eagle' },
  @{ pt = 'Urso';        en = 'bear' },
  @{ pt = 'Tigre';       en = 'tiger' },
  @{ pt = 'Rinoceronte'; en = 'rhino' },
  @{ pt = 'Elefante';    en = 'elephant' }
)

$destino = Join-Path (Get-Location) 'Assets\Resources\TDFende\Bichos'
New-Item -ItemType Directory -Force -Path $destino | Out-Null

foreach ($b in $bichos) {
  $q = [uri]::EscapeDataString("$($b.en) realistic animated")
  $url = "https://sketchfab.com/search?features=downloadable&features=animated&q=$q&sort_by=-likeCount&type=models"
  Write-Host "Abrindo busca: $($b.pt)  ($($b.en))"
  Start-Process $url
  Start-Sleep -Milliseconds 700
}

Write-Host ""
Write-Host "Coloque os arquivos baixados em: $destino"
Write-Host "Depois abra o Unity: o jogo troca o bicho feito em código pelo de verdade sozinho."
Start-Process explorer.exe $destino
