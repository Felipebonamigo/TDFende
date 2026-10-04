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

## 2. Bichos de verdade (as tropas que você envia) — já vêm no projeto

Sete bichos já estão em `Assets/Resources/TDFende/Bichos/`: modelos realistas do Sketchfab,
todos **CC BY** (uso comercial liberado, crédito obrigatório — lista pronta no `THIRD_PARTY.md`),
cada um com animação de andar (e, quando o modelo tinha, correr, parado e morrer). A águia é um
gavião-de-cauda-vermelha batendo asas: não achei águia realista animada com licença livre.
**Javali e tigre** ficam com o modelo feito em código: não achei versão realista, animada e com
autoria comprovada (detalhes no `THIRD_PARTY.md`). Achando uma, ponha no `bichos.json` do conversor.

Abra o Unity e dê Play: o jogo troca o bicho feito em código pelo de verdade sozinho, acerta o
tamanho, põe o anel com a cor do time no chão e toca o andar.

Para refazer ou trocar um bicho: `Tools/ConverterBichos` (Python + Blender sem janela,
`pip install bpy`). Em `bichos.json` fica o id do modelo no Sketchfab, as animações e o limite de
polígonos; `baixa.py` baixa (precisa de token da API), `converte.py` gera o FBX e `verifica.py`
desenha o bicho andando para conferir antes de copiar.

Trocar à mão continua valendo: qualquer `.fbx` com o nome do bicho na pasta (`Tiger_Animated.fbx`,
`elefante2.fbx`...) serve — mas **tire o nosso do mesmo bicho**, senão o jogo pega um dos dois
sem critério. Bicho andando de lado: `_giro90`, `_giro-90` ou `_giro180` no nome. Arquivo seu
nessa pasta **não vai para o GitHub** (o `.gitignore` só deixa passar os nossos).

## 3. Unity Asset Store — pacotes gratuitos (opcional)

Em https://assetstore.unity.com filtre por **Free** e busque "medieval", "castle",
"siege", "horse". Ao importar um pacote, me diga o nome e o que veio dentro: eu ligo os
modelos às torres, à fortaleza e ao cavalo (o jogo troca qualquer peça por um prefab em
`Assets/Resources/TDFende/<nome do modelo>`, ex.: `Torre_Canhao`, `Fortaleza`).
Leia a licença de cada pacote — a maioria permite uso em jogo, não redistribuição, então
também não devem ir para o GitHub público.
