# Arte de verdade, grátis — passo a passo

O jogo funciona sem nada disto (tudo tem versão procedural). Cada item abaixo que você
fizer troca uma parte do procedural por arte real; o código já sabe usar.

## 1. Poly Haven — árvores, pedras, tocos, troncos, barris, caixas, telhado (automático)

Grátis e CC0 (domínio público: pode vender o jogo, sem crédito obrigatório).

No PowerShell, na pasta do projeto:

```
powershell -ExecutionPolicy Bypass -File Tools\BaixarArte.ps1
```

Depois abra o Unity (ele importa sozinho) e faça commit — estes arquivos PODEM ir para o GitHub.
Com a sincronização automática instalada (`Tools\InstalarSincronizacao.ps1`), nada disso é
preciso: quando o script de download muda, o PC roda de novo sozinho e envia a arte nova.

## 2. Bichos de verdade (as tropas que você envia) — 10–20 min, manual

Os bichos feitos em código servem de reserva, mas **não parecem animais reais**. Para ficarem
reais, o jogo usa modelos 3D baixados, com esqueleto e animação. Daqui (nuvem) eu não alcanço
os sites de modelos; no seu PC eles abrem normalmente.

1. Na pasta do projeto, rode:
   `powershell -ExecutionPolicy Bypass -File Tools\BaixarBichos.ps1`
   Ele abre no navegador uma busca para cada um dos 9 bichos no **Sketchfab**, já filtrada por
   "baixável" e "animado" (precisa de conta grátis para baixar).
2. Escolha um modelo **realista com animação de andar**. Confira a licença na página:
   **CC Attribution** ou **CC0** (evite "NonCommercial" e "NoDerivs").
3. **Download → FBX** (às vezes aparece como "Original format"). Descompacte.
4. Coloque o `.fbx` (e a pasta de texturas, se vier) em `Assets/Resources/TDFende/Bichos/`.
   O nome do arquivo só precisa conter o bicho: `elefante.fbx`, `Tiger_Animated.fbx`...
5. Abra o Unity: o jogo troca o bicho de código pelo de verdade sozinho, acerta o tamanho,
   põe um anel com a cor do time no chão e usa as animações de andar/correr/morrer do arquivo.
6. Me mande o autor e o link de cada um (licença CC Attribution pede crédito).

Detalhes:
- Bicho andando de lado ou de costas: renomeie com `_giro90`, `_giro-90` ou `_giro180`.
- Só tem **GLB/glTF**? No Unity, *Window → Package Manager → + → Add package by name*,
  `com.unity.cloud.gltfast`. Depois arraste o `.glb` para a mesma pasta; se a animação não
  tocar, selecione o arquivo e ponha *Animation Method = Legacy* no Inspector.
- Outras fontes grátis que funcionam igual: **Unity Asset Store** e **Fab** (filtre por Free,
  busque "realistic animal animated"); **Quaternius** (CC0, mas estilo low-poly, menos real).
- Os arquivos dessa pasta **não vão para o GitHub** (cada licença é diferente) — o `.gitignore`
  já barra; ficam só no seu PC.

## 3. Unity Asset Store — pacotes gratuitos (opcional)

Em https://assetstore.unity.com filtre por **Free** e busque "medieval", "castle",
"siege", "horse". Ao importar um pacote, me diga o nome e o que veio dentro: eu ligo os
modelos às torres, à fortaleza e ao cavalo (o jogo troca qualquer peça por um prefab em
`Assets/Resources/TDFende/<nome do modelo>`, ex.: `Torre_Canhao`, `Fortaleza`).
Leia a licença de cada pacote — a maioria permite uso em jogo, não redistribuição, então
também não devem ir para o GitHub público.
