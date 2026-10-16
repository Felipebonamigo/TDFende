# TDFende no Unreal 5.8 — U0 (ponte Sim → Unreal)

Design: [`docs/designs/migracao-unreal.md`](../docs/designs/migracao-unreal.md). Aqui só **código e configuração** (D4): `Content/`,
`Binaries/`, `Intermediate/` e `Saved/` ficam fora do git, no PC, com backup no D:.

**Estado:** o lado da Sim está pronto e provado na nuvem (a Sim em NativeAOT, chamada pelo C, dá o fingerprint do C# gerenciado).
O lado do Unreal (`Source/TDFende`) foi **escrito mas ainda não compilado**: a nuvem não tem o Unreal. Ele é o que a sessão do PC prova.

## Roteiro da U0 no PC (go / no-go)

0. **Antes de tudo:** abra a sessão do Age of Earth (`AgeOfEarth\unreal\AgeOfEarthUE`, `AoEBridgeActor`) e veja como ela liga a
   simulação ao Unreal. Se houver um padrão melhor que o `TdfSim` (carregar DLL, thread, formato do snapshot, armadilhas de include
   como `SkeletalMeshActor.h`), adapte o `TdfSim`/`TdfSimActor` a ele e me diga o que mudou.
1. **Publicar a DLL** (precisa do Visual Studio Build Tools com "Desktop development with C++"):
   `powershell -ExecutionPolicy Bypass -File Tools\SimBridge\PublicarParaUnreal.ps1`
   → copia `tdfende_sim.dll` para `Unreal\TDFende\Binaries\Win64\`.
2. **Gerar e compilar o projeto:** clique direito em `Unreal\TDFende\TDFende.uproject` → *Generate Visual Studio project files*, abra
   e compile `TDFendeEditor | Win64 | Development` (ou `Build.bat TDFendeEditor Win64 Development -Project=...`). Erros de
   compilação do C++ são esperados na primeira vez: corrija e me mande o diff.
3. **Abrir o projeto**, criar um nível vazio, arrastar um **`TdfSimActor`** (Place Actors → procure por TdfSim), deixar na origem.
4. **Teste de paridade (o critério):** no ator, preencha `ReplayPath` com o caminho absoluto de
   `Tools\SimBridge\smoke\demo_replay.txt`, dê **Play** e olhe o Output Log. A linha
   `FINGERPRINT (compare com o FlowSim): ...` tem que ser exatamente
   `t6000 pl20/775/100/56/66 fo22/372/206/9/36 k61/16/1/40/21/0 #835d1db265f91d80`
   (o mesmo de `dotnet run --project Tools/FlowSim -v quiet -- replay Tools/SimBridge/smoke/demo_replay.txt --forcar`).
5. **Custo da ponte:** com `stat unit`/Insights, a chamada de `Redraw` + `step` por quadro deve ficar abaixo de 0,5 ms.
6. Sem `ReplayPath`, a IA joga sozinha e você vê cubos (bichos), cilindros (torres) e esferas (tiros) nas duas lanes.

**Go:** fingerprint igual e custo < 0,5 ms → segue para a U1 (fatia de beleza). **No-go:** reabrimos o caminho B do design.

## Coordenadas
Sim (x direita, y cima, z frente; unidades de célula) → Unreal `X = z`, `Y = x`, `Z = y` em cm (`CellSizeCm`, padrão 100). A lane 1 é
deslocada em X. A vista de verdade (U1+) troca estes cubos por malhas por **chave** (`SendUnit.Key` / `TowerType.Key`, TEC-12).

## Resultado da U0 (14/10/2026) — **GO**

Rodado no PC (UE 5.8.3, Visual Studio 2026 com C++, .NET SDK 10.0.302, RTX 4070 Ti), em jogo (`-game`) com o replay `demo_replay.txt`
(6000 tiques = 200 s de jogo em tempo real, 1600×900).

- **Fingerprint obtido:** `t6000 pl20/775/100/56/66 fo22/372/206/9/36 k61/16/1/40/21/0 #835d1db265f91d80` — **idêntico** ao esperado (FlowSim),
  em 3 execuções seguidas. A assinatura dos catálogos foi `d610b62201892c46 7cb8cd3f7c5ce6cd 0d8715b99f2d0452`.
- **Custo da ponte por quadro** (tudo que o `Tick` faz com a Sim: passos, snapshot, cópia, instâncias, eventos; ~65 mil quadros a ~320 fps):
  média **0,036 ms**, p50 0,032, p95 0,055, p99 0,096, máx 3,0 ms (um quadro isolado). Meta < 0,5 ms: cumprida com folga.
  Numa 4ª execução, pelo `RodarU0.ps1` (com o PC mais ocupado, ~165 fps): média 0,041, p99 0,211, máx **15,3 ms** em um único quadro
  (não investigado: parece soluço de carga/GC; a média e o p99 seguem muito abaixo da meta).
  Só os passos da Sim: média 0,002 ms (um tique custa uns 31 µs; cada quadro roda 0 ou 1). `Redraw` + eventos: média 0,034 ms.
- **Ressalva da medida:** o replay de demonstração tem no máximo **10 inimigos vivos** ao mesmo tempo. O fim de partida real e o
  `-Estresse` do Unity (60 bichos, 12 torres no máximo) ainda não foram medidos no Unreal; repetir na U1.
- **Print do viewport (tique 2500):** `Unreal\TDFende\Saved\U0\viewport_t2500.png` (o `viewport_t4000.png` é a primeira tentativa, com
  enquadramento errado). Duas lanes com piso, torres e tiros sobre o piso; só formas básicas, como previsto para a U0.

### O que foi corrigido / mudou

1. **Passo 0 (Age of Earth):** o `AoEBridgeActor` liga a simulação por **WebSocket** a um servidor TypeScript (`ws://127.0.0.1:8790`, JSON,
   `state` a ~10 Hz com interpolação no cliente). É outro desenho (processo separado), então **não havia padrão melhor para a nossa ponte em
   processo** (DLL NativeAOT por ponteiros, snapshot plano, 30 Hz fixos): o `TdfSim`/`TdfSimActor` ficaram como estão. Do AoE aproveitei só a
   ideia do medidor de desempenho no log e o aviso de que o include certo é `Animation/SkeletalMeshActor.h` (vale para a U1).
2. **`PublicarParaUnreal.ps1`:** o NativeAOT falhava no link (`'vswhere.exe' não é reconhecido`): o ILCompiler 10.0.10 chama o `vswhere` pelo
   PATH e o Visual Studio 2026 não o põe lá. O script agora acrescenta `...\Microsoft Visual Studio\Installer` ao PATH. Nada para instalar:
   o Visual Studio Community 2026 já tinha "Desktop development with C++" (MSVC 14.51).
3. **Compilação C++:** compilou na primeira tentativa, sem erro. Só o aviso C4005 (`TDF_API` redefinida: o cabeçalho C declara `dllimport`),
   silenciado com `#pragma warning` ao redor do `#include` em `TdfSim.h`. O cabeçalho `tdfende_sim.h` **não foi tocado**.
4. **Coordenadas:** a grade da Sim é **centrada na origem** (`GridMap`: x em [-W/2, W/2], z em [-H/2, H/2]), não no canto. A lane 0 fica em
   X = 0 e a lane 1 em X = `(GridHeight+4) × CellSizeCm`. A nota "Coordenadas" acima continua valendo, com esse detalhe.
5. **Novo, para teste sem abrir o editor:** `TdfDemoGameMode` (em qualquer nível, inclusive o vazio `/Engine/Maps/Entry`, monta sozinho o
   `TdfSimActor`, pisos, câmera de cima e luz) e opções de linha de comando no ator: `-TdfReplay=`, `-TdfShot=` + `-TdfShotTick=`, `-TdfQuit`.
   No fim do replay o log traz o `FINGERPRINT` e o `CUSTO` (média, p50, p95, p99, máx). Tudo roda por
   `powershell -ExecutionPolicy Bypass -File Unreal\TDFende\RodarU0.ps1` (compila, roda, confere o fingerprint; sai com 0 ou 1; ~3,5 min).
6. O roteiro manual (abrir o projeto, nível vazio, arrastar o `TdfSimActor`, Play) não foi feito à mão: o `TdfDemoGameMode` faz o mesmo
   pelo mesmo ator, e o resultado é o do log. Arrastar o ator num nível continua funcionando.

## U2, parte 1: jogo jogável com formas simples (14/10/2026)

O `TdfSimActor` virou a vista jogável da Sim (formas simples; as malhas reais são a U3). O que existe, tudo por **código C++** no git:

- **Ponte, ABI 2** (só acréscimos, o resto do contrato não mudou): `tdf_catalog_text` (catálogo de torres e de envios em texto,
  `chave;nome;custo;...`), `tdf_can_build(x,y,tipo)` (0 = dá; senão o `RejectReason`) e `tdf_upgrade_cost(x,y)`. Testados no FlowSim
  (9 verificações novas, vistas falhando antes de existir o código). A vista não conhece mais números de balanceamento: tudo vem do catálogo.
- **Controle** (`TdfPlayerController`): botão esquerdo constrói a torre selecionada na célula sob o mouse (ou sobe, se já há torre),
  botão direito vende, **Q/E** troca a torre, **1 a 9** envia o bicho. O mouse vira célula por raio contra o chão.
- **HUD provisório** (`TdfHUD`, Canvas): vidas, ouro, renda, torres, tempo (você e a IA), paleta de torres com custo, lista de envios,
  contorno da célula sob o mouse (verde = dá, vermelho = não, amarelo = subir) com o custo, e aviso curto quando um comando é recusado.
- **Território** desenhado (placas por célula, só reconstruídas quando muda), cor por tipo de torre e por estado do inimigo (normal,
  lento/congelado, queimando, voador).
- **Partida gravada** em `Saved/Tdf/partida.txt` (formato do replay), pronta para o F9 da U2 parte 2.
- **Autoteste** `-TdfSelfTest` (ou `RodarU0.ps1 -AutoTeste`, ~10 s, sai com 0 ou 1): 19 verificações — catálogo pela ponte, `CanBuild`,
  conversão célula↔mundo em todas as células, construir, subir, enviar, vender, comandos gravados. Passou.
- Opções novas de linha de comando: `-TdfQuitTick=<n>`; `-TdfSelfTest`.

**Limites conhecidos da U2 parte 1:** o HUD é de teste (a loja de verdade é a fase de UI); as formas ainda são cubos e cilindros (as cores
da engine saíram lavadas pela exposição automática); não há som, Niagara, câmera livre, nem replay F9 de leitura; nenhum teste com mouse
real, só com as mesmas chamadas (a conversão do raio do mouse foi testada pela volta célula↔mundo, não pela projeção da câmera).

## U2 parte 2 + U1 primeira passada: arte real, ambiente e revisão (14/10/2026)

**Jogar:** `powershell -ExecutionPolicy Bypass -File Unreal\TDFende\Jogar.ps1 [-Dificuldade 0|1|2] [-TelaCheia]`. Mouse: esquerdo
constrói/sobe, direito vende; Q/E troca a torre; 1-9 envia; **WASD/setas movem a câmera, a roda aproxima**.

**Receitas do Content (fora do git, D4):** `Unreal\TDFende\ImportarArte.ps1` (torres e bichos, `Scripts/importa_arte.py`) e
`ImportarArte.ps1 -Script importa_ambiente.py` (chão, cenário, grama). Rodam sem abrir o editor (UnrealEditor-Cmd + Python) e refazem
`/Game/TDFende` do zero a partir de `Assets/Resources` (a mesma arte do Unity). Backup do Content no D: (`Tools\Backup.ps1`).

### O que entrou
- **Arte real:** as 18 torres (6 tipos × 3 estágios) e os 9 bichos com esqueleto e animação de andar, texturizados. Torre por estágio do
  nível (1-2, 3-4, 5-6), instanciada; bicho em componente esquelético com pool, escala medida pelos **ossos** na pose de andar (a caixa
  guardada mente, como no BUG-01 do Unity), águia pela envergadura e voando a 1,1 m. Frente da malha conferida no `-TdfPreview`.
- **Ambiente (U1):** `ATdfEnvironment`: céu físico (atmosfera + nuvens volumétricas), névoa de altura volumétrica, sol e luz do céu
  dinâmicos (Lumen), pós-processo; chão de 600 m com material de UV pelo mundo (duas texturas Poly Haven misturadas por um padrão
  grande); 64 peças de cenário (paredões, rochas, tocos, troncos) fora das lanes; 549 pedras contornando as lanes; 2.600 touceiras de grama
  3D (22 variantes, normalizadas a ~42 cm). Nanite nas malhas. Render como o do Age of Earth (`Config/DefaultEngine.ini`: Lumen, sombras
  virtuais, TSR, DX12 SM6). Território virou véu translúcido por lane (azul você, vermelho a IA).
- **Câmera jogável** (WASD/setas, roda) e câmera inicial três-quartos de trás da base. **Dificuldade** por `-TdfDificuldade=`.
- **Ferramentas:** `-TdfPreview` (vitrine de toda a arte), `-TdfCamX/Y/Z/Pitch/Yaw/Fov`, `-TdfSemArte`, `-TdfSemAmbiente`, `-TdfSemGrama`,
  `-TdfQuitSeconds`, FPS no log a cada 5 s, `MedirFps.ps1` (A/B de desempenho).

### Corrigido (revisão independente + testes que falharam primeiro)
1. **Crash a ~60 s de partida:** o GC coletava a animação do bicho (ponteiro cru fora de `UPROPERTY`). Teste no autoteste (força um GC e
   confere componente, malha, animação e torre por `TWeakObjectPtr`): falhou, depois passou com `KeepAlive`.
2. **Bichos flutuando 50 cm:** a Sim guarda o centro do corpo (y = 0,5); a vista agora põe o pé no chão (teste: falhou, passou).
3. **A gravação do Unreal não reproduzia no FlowSim** (sem assinaturas; dificuldade sem acento): agora é igual ao `Replay.Serialize`
   (com `final`), em UTF-8 sem BOM. O `RodarU0.ps1 -AutoTeste` joga no Unreal, grava e **reproduz no FlowSim exigindo o mesmo estado
   final** (falhou com "não traz as assinaturas", depois passou).
4. **DLL NativeAOT descarregada** (não suportado): carrega uma vez por processo e nunca libera.
5. Bichos guardados no pool continuavam animando; movimento a 30 Hz sem interpolação (agora desliza entre tiques, sem virar no empurrão);
   torres refeitas todo quadro (agora só quando mudam); buffers fixos (agora crescem); alocação por quadro no território e no `Snapshot`
   do C#; fingerprint só no tique exato (agora também no fim da partida); soluço de ~100-145 ms quando o primeiro bicho de um tipo aparecia
   (agora a arte é pré-carregada: o pior quadro caiu para 2,7 ms).
6. Importação: animações recusadas por não caírem em quadro inteiro (lobo, urso, elefante ficavam sem andar) — opção de encaixe ligada;
   materiais sem a flag de malha esquelética/instanciada (cinza padrão) — corrigido.
7. `--nologo` no `dotnet run` engolia os argumentos do FlowSim (o replay rodava a suíte de testes no lugar).

### Verificado (14/10/2026)
- Autoteste no motor: 24 verificações, PASSOU; e a gravação do Unreal reproduz no FlowSim com o mesmo estado final.
- Replay de 200 s: fingerprint **idêntico** (`#835d1db265f91d80`); custo médio da ponte 0,056 ms/quadro, pior quadro 2,7 ms.
- Desempenho (1600×900, RTX 4070 Ti, medida com ruído — cada execução recompila shaders): ~100 fps com tudo; sem grama ~147, sem nuvens
  ~143, sem sombras virtuais ~144. Prints em `Unreal\TDFende\Saved\U0\` (`ambiente_3.png` é o atual).

### Pendente — depende do Felipe
- **Tema e bioma** (decisão da Fase 1): o ambiente atual é um campo seco com rochas, neutro. Para o "realista bonito" de verdade falta
  escolher o bioma; com ele, a paleta, a vegetação e o céu seguem.
- ~~Texturas 4K de chão~~ **feito com o ok do Felipe (14/10/2026):** ver "Chão 4K" abaixo. Mais rochas/plantas da Poly Haven continuam
  pedindo autorização antes de baixar (dizer quais arquivos e o tamanho).
- ~~Árvores realistas~~ **resolvido no mesmo dia:** o acervo comum (`Felipebonamigo/acervo-3d`, regra nova do CLAUDE.md) tem carvalho,
  oliveira, dois ciprestes, arbusto e rochas da galeria do Meshy (CC0), já neste PC na pasta do Age of Earth. Importados em
  `/Game/TDFende/Ambiente/Natureza` (material M_TdfGltf, Nanite) e espalhados em volta das lanes (81 peças), sem árvore alta entre a
  câmera e a sua lane. Licença: 8 entradas `unreal-natureza-*` no `docs/licencas/manifesto.json` (raiz nova só para essa pasta) e os
  créditos regenerados. Ainda faltam árvores de outros biomas quando o tema for escolhido.
- ~~Aprovar o visual da U1~~ **aprovado pelo Felipe em 14/10/2026** (print com o chão 4K). Falta: **testar o controle** (mouse e
  teclado reais; aqui só foi testado pelas mesmas chamadas), sobretudo clicar nos cartões do HUD sem construir torre embaixo deles;
  sons (o Claude não ouve); hardware mínimo alvo; conferir os royalties da Epic (5% acima de US$ 1 mi brutos).

### Chão 4K (14/10/2026)
Fotografias aéreas 4K da Poly Haven (CC0): `rocky_terrain_02` (campo verde com pedras, 90 m), `aerial_grass_rock` (grama amarelada com
cascalho, 15 m), `dirt_aerial_02` (terra pisada, 20 m), `forest_ground_04` (cascalho e folhas, 3,2 m); cor, normal e ARM de cada.
- **Baixar:** `powershell -ExecutionPolicy Bypass -File Unreal\TDFende\BaixarTexturas.ps1` (135 MB em `Unreal\TDFende\Fontes\PolyHaven`,
  fora do git, no backup do D: como `unreal-fontes`). Depois `ImportarArte.ps1 -Script importa_ambiente.py`.
- **Material `M_TdfChao4K`** (`/Game/TDFende/Ambiente/Chao4K`): cada foto no tamanho real; fora das lanes, campo verde com manchas de
  grama amarela; dentro, terra pisada com manchas de grama e de cascalho; borda irregular. As manchas e a máscara das lanes saem de um
  nó HLSL (ruído fbm pela posição no mundo); o `ATdfEnvironment` passa onde ficam as lanes (`LaneX0/X1/HX/HY`, `Borda`). Sem o
  material, o chão volta ao `M_TdfChao` antigo.
- Antes e depois: `Saved\U0\ambiente_4.png` (campo seco) e `ambiente_8.png` (atual). 100-145 fps no mesmo replay (1600×900).
- **Atenção:** a primeira execução depois de mudar o material derrubou o driver de vídeo uma vez ("GPU Crashed or D3D Device Removed",
  no quadro 3, compilando shaders); a segunda rodou normal. Se repetir, investigar (dump em `Saved\Logs\*.nv-gpudmp`).
- Licença: entrada `unreal-chao-polyhaven-4k` no manifesto (raiz nova `Content/TDFende/Ambiente/Chao4K`), créditos regenerados.

### HUD em UMG (U2, 14/10/2026)
`UTdfHudWidget` (`Source/TDFende/TdfHudWidget.*`), montado em C++ sem asset de Blueprint (a receita continua sem abrir o editor):
- **Placar:** você (vidas, ouro, renda, torres) à esquerda, a IA (vidas, renda, torres, bichos na sua lane) à direita, relógio no meio
  (e "MORTE SÚBITA"); aviso curto (comando recusado, fim da partida) abaixo do relógio.
- **Loja embaixo:** um cartão por torre (clique seleciona; Q/E também; contorno dourado na selecionada) e um por envio (clique envia;
  1-9 também). Cada cartão tem a **miniatura 3D do próprio modelo**, nome, preço e dica com os números do catálogo; fica apagado (preço
  em vermelho) quando falta ouro, mas continua clicável (dá para escolher a torre antes de juntar o ouro).
- **Miniaturas:** fotografadas uma vez, no 12º quadro: um "estúdio" 1,5 km acima do mapa, captura de cena só com o modelo (HDR; o alfa
  dá o recorte), enquadramento em duas fotos pelo que a câmera viu, luz de preenchimento própria, exposição de compromisso entre a do
  modelo e a mediana de todos, curva de filme e sRGB no processador. `-TdfMiniaturas=<pasta>` grava os PNG para conferir. Rodando pelo
  editor (`-game`), espera texturas e shaders compilarem antes (no pacote isso não existe).
- O clique sobre o HUD não vai para o campo (`IsPointerOverUi`); o contorno da célula e o custo junto ao mouse continuam no `ATdfHUD`
  (Canvas), porque acompanham o mundo. Os prints do `-TdfShot` agora saem com a interface.
- Autoteste (5 verificações novas, vistas falhando antes): HUD na tela; 6 + 9 cartões; cartão de torre seleciona; miniatura em todos;
  cartão aceso só com ouro; cartão de envio envia. Replay de 200 s com o HUD: fingerprint igual, ~114 fps. Print: `Saved\U0\hud_5.png`.

### Efeitos de combate (U2, 14/10/2026)
Sem Niagara (montar sistema do Niagara sem abrir o editor não dá): tudo instanciado, barato e determinístico.
- **Tiros** (`UTdfFxComponent`): cada torre com a sua cara (bala de ferro com fumaça no canhão e no morteiro; cristal azul no gelo;
  virote dourado na sentinela; bola de fogo; lufada clara no ar), interpolados entre os tiques (cada projétil casa com o do tique
  anterior: mesma lane e tipo, o mais perto) e com rastro na direção do voo. **Clarão** na boca do cano (evento de disparo) e no
  impacto (na cor da torre, do tamanho do dano); **poeira** no tiro pesado e quando o bicho cai.
- **Véu de efeito** no bicho (material de sobreposição `M_TdfStatus`): gelo azul, lentidão azul fraco, fogo laranja pulsando.
- **Barras de vida** (Canvas, só bicho ferido): cor de verde a vermelho, fundo azulado com gelo e alaranjado com fogo, mais larga
  para bicho mais forte.
- **Morte:** o bicho que sai do snapshot sem ter vazado não some: toca a animação `Death` se tem (elefante), senão tomba de lado
  (a águia despenca), espera e afunda; só então volta ao pool. Quem vazou pela base some na hora.
- Materiais: `Scripts/importa_efeitos.py` (`ImportarArte.ps1 -Script importa_efeitos.py`): `M_TdfBrilho` (aditivo, cor e brilho
  por instância), `M_TdfFumaca` (translúcido), `M_TdfStatus`. Brilhos calibrados no print (acima de ~10 a cor estoura para branco).
- **Ferramentas novas:** `-TdfShotTicks=a,b,c` (um print por tique, com o número de inimigos e tiros no log), `-TdfExec=tique:comando;...`
  (comando de console num tique, ex.: `ProfileGPU`), e o log de FPS agora diz jogo/render/GPU em ms.
- Autoteste: 7 verificações novas (vistas falhando antes). Replay de 200 s: fingerprint igual, 140-153 fps, GPU 5,5-6,8 ms.
  Prints: `Saved\U0\fx6_5850.png` (perto) e `fx4_6000.png` (câmera normal).
- **Medição:** duas execuções tiveram 15-20 s a ~13 fps em tiques diferentes cada vez; a terceira, com o tempo de GPU no log, não teve
  nenhuma (GPU sempre ~6 ms). Era algo fora do jogo (janela em segundo plano ou outra carga de vídeo). Se aparecer de novo, o log
  diz qual dos três segura o quadro.

### R, F9 e fim de partida (U2, 15/10/2026)
- **R:** partida nova com outra semente (como o Unity; a semente vai no replay, então continua reprodutível). A anterior fica em
  `Saved\Tdf\partida.txt`; bichos, mortos, tiros e clarões somem; o HUD continua (o catálogo é o mesmo). Não vale reproduzindo replay.
- **F9:** grava o replay da partida em andamento em `Saved\Tdf\replays\tdfende-AAAAMMDD-HHMMSS.txt` (o nome do Unity), com aviso no
  HUD. Reproduz no FlowSim com o mesmo estado final (`dotnet run --project Tools/FlowSim -v quiet -- replay <arquivo>`).
- **Fim:** a tela escurece com VITÓRIA ou DERROTA e "R para jogar de novo"; o log diz o tique do fim, e com `-TdfShot` sai um print
  `<nome>_fim.png`. Dica "R partida nova · F9 salva o replay" embaixo do relógio.
- Autoteste: F9 (cabeçalho, estado final e comandos no arquivo) e R (tique do começo, outra semente, sem torres, sem bicho nem tiro
  da anterior, ouro do começo, partida anterior gravada) — vistos falhando antes.
- **Torreta que mira: adiada.** As torres reais são uma malha só (o Unity mirava só nos modelos procedurais, que tinham torreta
  separada). Girar o prédio inteiro fica estranho; precisa de arte com a parte de cima separada (ou um cano por cima) — decisão de arte.

### Empurrão e contorno do território (U2, 15/10/2026)
- **Empurrão** (efeito 3 da Sim): onda de ar (disco claro rente ao chão) e poeira onde o bicho está.
- **Território:** véu contínuo e mais leve (sem a grade de quadradinhos) e uma linha fina que brilha na cor do dono, só nos lados de
  célula que dão para fora (`TerritoryEdges`, função pura testada). Print: `Saved\U0\terr_1500.png`.

### U4: empacotar, selo e orçamento (15/10/2026)
- **Empacotar:** `powershell -ExecutionPolicy Bypass -File Unreal\TDFende\Empacotar.ps1 [-Config Development|Shipping] [-SemTeste]`.
  Cozinha o Content, compila o jogo sem editor, junta a DLL da Sim, grava o selo e (Development) roda o **autoteste no jogo
  empacotado**. Sai em `Unreal\TDFende\Pacote\Windows\TDFende.exe` (fora do git). Subir para a Steam: skill `publicar-na-steam`
  (login e Steam Guard só o Felipe; o app da Steam ainda não existe).
- **Selo:** "build <hash>" (`git describe --always --dirty`) no canto inferior direito do HUD e `[TDFende] build <hash>` no log.
  Empacotado, vem de `Pacote\Windows\TDFende\Selo.txt`; pelo editor, do git na hora. Sem selo, o autoteste reprova.
- **Orçamento:** `MedirFps.ps1` mede FPS e GPU por caso (completo, qualidade média, leve e os A/B de grama, névoa, nuvens, Lumen,
  sombra virtual) e sai 1 se a GPU média do caso completo passar de 8 ms (1600×900, este PC).

### Configurações (Esc, 15/10/2026)
Painel no HUD: qualidade gráfica Baixa/Média/Alta/Épica (os grupos sg.* do Unreal de uma vez), resolução de render 70/85/100%
(o TSR completa) e janela/tela cheia; salvo no `GameUserSettings` para a próxima vez. Com o painel aberto o clique não vai para o
campo. `-TdfConfiguracoes` abre o painel ao começar (para o print). Medidas: Média ~7,9 ms de GPU, Leve (Baixa + 70%) ~4,8 ms.
DLSS/FSR/XeSS como opção: precisam dos plugins dos fabricantes (baixar = autorização do Felipe).

### Teste de controle com mouse e teclas de verdade (16/10/2026)
O autoteste agora injeta mouse pelo Slate (o caminho do mouse físico: hover, botões do UMG, viewport) e teclas pelo controle:
clicar num cartão seleciona a torre sem construir por baixo; clicar no campo constrói na célula do mouse; Esc abre e fecha as
configurações; clicar em "Baixa" muda a qualidade de verdade; com o painel aberto o campo não recebe clique; R começa outra partida.
A câmera é deslocada para a lane ficar sob a loja, e o teste só vale se achar uma célula livre embaixo do cartão ou da borda
(senão reprova: não passa vazio). **Teste de mutação:** sem a proteção do clique sobre o HUD, dois casos reprovam.
- **Defeito real achado e corrigido:** ao apertar o botão do mouse o viewport captura o mouse e o Slate deixa de achar a loja
  "sob o mouse" (`IsHovered` vira falso), então clicar no vão da placa da loja (ou no placar) construía uma torre por baixo.
  `IsPointerOverUi` agora compara a posição do cursor com a geometria dos painéis.
- O que continua só com mouse e teclado físicos: sensação (tamanho dos botões, cursor), foco da janela e tela cheia.

### Próximo
Dependem do Felipe: sons
(baixar pede autorização), partículas de gelo/fogo (Niagara: ligar o Unreal MCP), torreta que mira (arte com a parte de cima
separada), Steam (app, login), tema/bioma (U3).

### Recursos do Unreal a usar (proposta de 14/10/2026, aguardando o Felipe)

- **Unreal MCP:** ligar uma vez com a skill `ligar-unreal-mcp` (porta 8030); depois a sessão do PC monta HUD, materiais e
  efeitos pelo editor aberto, com a skill `unreal-mcp`.
- **Upscalers:** TSR já é o padrão; DLSS, FSR e XeSS como opção do jogador entram na U4, medidos no `MedirFps.ps1`.
- **PCG:** espalhar a natureza do acervo em volta das lanes por regra (densidade, distância da câmera), em vez de peça a peça.
- **Niagara e MetaSounds:** os efeitos de gelo, fogo e empurrão, e os sons da Fase 8 sem um arquivo por situação.
