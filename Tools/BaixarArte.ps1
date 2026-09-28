# Baixa arte CC0 (domínio público) da Poly Haven para dentro do projeto.
#
#   Uso, no PowerShell, na pasta do projeto:
#     powershell -ExecutionPolicy Bypass -File Tools\BaixarArte.ps1
#
# O que baixa (tudo CC0, pode usar em jogo comercial sem crédito):
#   Assets/Resources/TDFende/Cenario/Pedras|Tocos|Troncos|Barris|Caixas/<id>/  modelos FBX 1k + texturas
#   Assets/Resources/TDFende/Textures/RoofTile_*.bytes, Slate_*.bytes           telha e ardósia
#
# O jogo usa o que encontrar e segue procedural no que faltar. Rodar de novo não
# baixa o que já existe. Depois de rodar: abra o Unity (ele importa), e faça commit.

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # a barra de progresso do PS 5.1 deixa o download 10x mais lento
$repo = Split-Path -Parent $PSScriptRoot
$api = 'https://api.polyhaven.com'
# a Poly Haven pede um User-Agent que identifique quem usa a API
$headers = @{ 'User-Agent' = 'TDFende-BaixarArte/1.0 (jogo indie, uso CC0)' }

function Get-Json($url) { Invoke-RestMethod -Uri $url -Headers $headers }

function Save-Url($url, $dest) {
    if (Test-Path $dest) { return }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Invoke-WebRequest -Uri $url -Headers $headers -OutFile $dest -UseBasicParsing
}

Write-Host "Consultando a Poly Haven..."
$models = Get-Json "$api/assets?t=models"
$ids = @($models.PSObject.Properties.Name)
Write-Host ("  {0} modelos disponíveis" -f $ids.Count)

# categoria do jogo -> expressão que escolhe pelo id (o nome do arquivo na Poly Haven)
$plan = [ordered]@{
    'Pedras'  = @{ Pattern = '^(rock|boulder|stone)'; Max = 6 }
    'Tocos'   = @{ Pattern = 'stump';                 Max = 3 }
    'Troncos' = @{ Pattern = '(log|trunk)';           Max = 3 }
    'Barris'  = @{ Pattern = 'barrel';                Max = 3 }
    'Caixas'  = @{ Pattern = 'crate';                 Max = 3 }
}

foreach ($cat in $plan.Keys) {
    $rule = $plan[$cat]
    $pick = @($ids | Where-Object { $_ -match $rule.Pattern } | Sort-Object | Select-Object -First $rule.Max)
    Write-Host ("{0}: {1}" -f $cat, ($pick -join ', '))
    foreach ($id in $pick) {
        try {
            $files = Get-Json "$api/files/$id"
            $fbx = $files.fbx.'1k'.fbx
            if (-not $fbx) { Write-Host "  $id sem FBX 1k, pulando"; continue }
            $dir = Join-Path $repo "Assets/Resources/TDFende/Cenario/$cat/$id"
            Save-Url $fbx.url (Join-Path $dir "$id.fbx")
            if ($fbx.include) {
                foreach ($inc in $fbx.include.PSObject.Properties) {
                    Save-Url $inc.Value.url (Join-Path $dir $inc.Name)
                }
            }
            Write-Host "  ok  $id"
        } catch {
            Write-Host "  falhou $id : $($_.Exception.Message)"
        }
    }
}

# texturas de telhado: o jogo lê JPG guardado como .bytes (ver MatSpec.External)
$texDir = Join-Path $repo 'Assets/Resources/TDFende/Textures'
$roofing = Get-Json "$api/assets?t=textures&c=roofing"
$roofIds = @($roofing.PSObject.Properties.Name | Sort-Object)
$wanted = [ordered]@{
    'RoofTile' = '(clay|terracotta|tile)'
    'Slate'    = 'slate'
}
foreach ($mat in $wanted.Keys) {
    $id = $roofIds | Where-Object { $_ -match $wanted[$mat] } | Select-Object -First 1
    if (-not $id) { Write-Host "$mat : nenhuma textura encontrada"; continue }
    try {
        $f = Get-Json "$api/files/$id"
        Save-Url $f.Diffuse.'1k'.jpg.url (Join-Path $texDir "$($mat)_albedo.bytes")
        Save-Url $f.nor_gl.'1k'.jpg.url (Join-Path $texDir "$($mat)_normal.bytes")
        Write-Host "$mat : $id"
    } catch {
        Write-Host "$mat : falhou ($($_.Exception.Message))"
    }
}

Write-Host ""
Write-Host "Pronto. Abra o Unity para importar, depois faça commit do que entrou em Assets/Resources/TDFende."
