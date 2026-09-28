# Arte de verdade, grátis — passo a passo

O jogo funciona sem nada disto (tudo tem versão procedural). Cada item abaixo que você
fizer troca uma parte do procedural por arte real; o código já sabe usar.

## 1. Poly Haven — pedras, tocos, troncos, barris, caixas, telhado (5 min, automático)

Grátis e CC0 (domínio público: pode vender o jogo, sem crédito obrigatório).

No PowerShell, na pasta do projeto:

```
powershell -ExecutionPolicy Bypass -File Tools\BaixarArte.ps1
```

Depois abra o Unity (ele importa sozinho) e faça commit — estes arquivos PODEM ir para o GitHub.

## 2. Mixamo — soldados realistas animados (15–20 min, manual)

Grátis, uso comercial liberado, mas exige conta Adobe (grátis) e **o arquivo não pode ir
para o GitHub** — o `.gitignore` já barra. Ficam só no seu PC.

1. Entre em https://www.mixamo.com com uma conta Adobe (crie se não tiver).
2. Aba **Characters**, escolha o personagem (sugestões abaixo; pode trocar por outro parecido).
3. Com o personagem selecionado, clique **Download**: Format **FBX for Unity (.fbx)**,
   Pose **T-pose**. Salve com o nome da tabela.
4. Aba **Animations**, com o MESMO personagem selecionado, baixe cada animação:
   - marque **In Place** quando a opção aparecer (senão o boneco sai andando sozinho)
   - Download: Format **FBX for Unity**, Skin **Without Skin**, 30 fps
5. Coloque todos os arquivos em `Assets/Resources/TDFende/Personagens/`.

| Inimigo do jogo | Personagem sugerido (busca no Mixamo) | Arquivos |
|---|---|---|
| Recruta (lanceiro) | "Castle Guard" ou "Knight D Pelegrini" | `Inimigo_Recruta.fbx` |
| Couraçado (armadura) | "Paladin J Nordstrom" | `Inimigo_Couracado.fbx` |
| Enxame (leve) | "Erika Archer" ou outro de roupa leve | `Inimigo_Enxame.fbx` |

Animações, para CADA um (troque `Inimigo_Recruta` pelo nome da linha):

| Busca no Mixamo | Nome do arquivo |
|---|---|
| "Walking" | `Inimigo_Recruta@Walk.fbx` |
| "Running" | `Inimigo_Recruta@Run.fbx` |
| "Idle" (qualquer de pé) | `Inimigo_Recruta@Idle.fbx` |

Só o modelo + `@Walk` já basta para funcionar; `@Run` e `@Idle` melhoram. O jogo acerta
o tamanho, põe um anel com a cor do time no pé e sincroniza a passada com a velocidade.
Cavaleiro, planador e torre de cerco continuam procedurais (Mixamo só tem gente).

## 3. Unity Asset Store — pacotes gratuitos (opcional)

Em https://assetstore.unity.com filtre por **Free** e busque "medieval", "castle",
"siege", "horse". Ao importar um pacote, me diga o nome e o que veio dentro: eu ligo os
modelos às torres, à fortaleza e ao cavalo (o jogo troca qualquer peça por um prefab em
`Assets/Resources/TDFende/<nome do modelo>`, ex.: `Torre_Canhao`, `Fortaleza`).
Leia a licença de cada pacote — a maioria permite uso em jogo, não redistribuição, então
também não devem ir para o GitHub público.
