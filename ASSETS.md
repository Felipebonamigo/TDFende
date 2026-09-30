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

## 2. Bichos (as tropas que você envia)

As tropas agora são animais, do rato ao elefante — todos feitos em código
(`Art/ModelLibAnimals.cs`), sem arquivo para baixar. O passo do Mixamo não vale mais
(o Mixamo só tem gente).

Para trocar um bicho por um modelo de verdade (ex.: um pacote grátis de animais da Asset
Store), crie um prefab em `Assets/Resources/TDFende/` com o nome do modelo — ele substitui
o procedural sozinho:

`Inimigo_Rato`, `Inimigo_Cachorro`, `Inimigo_Lobo`, `Inimigo_Javali`, `Inimigo_Aguia`,
`Inimigo_Urso`, `Inimigo_Tigre`, `Inimigo_Rinoceronte`, `Inimigo_Elefante`.

## 3. Unity Asset Store — pacotes gratuitos (opcional)

Em https://assetstore.unity.com filtre por **Free** e busque "medieval", "castle",
"siege", "horse". Ao importar um pacote, me diga o nome e o que veio dentro: eu ligo os
modelos às torres, à fortaleza e ao cavalo (o jogo troca qualquer peça por um prefab em
`Assets/Resources/TDFende/<nome do modelo>`, ex.: `Torre_Canhao`, `Fortaleza`).
Leia a licença de cada pacote — a maioria permite uso em jogo, não redistribuição, então
também não devem ir para o GitHub público.
