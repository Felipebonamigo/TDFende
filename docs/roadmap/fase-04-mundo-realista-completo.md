# Fase 4 — Mundo realista completo

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 6-8 sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Os ~80% da tela passam a parecer fotografados em todo o mapa, e os bichos param de denunciar o falso. Dentro do orçamento e no perfil mínimo.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Folha do exe (início, meio, close) aprovada contra a bíblia e os jogos-régua.
- Nenhuma árvore procedural.
- Teste cego da fronteira repetido sobre o chão novo (8 de 10).
- AuditaAssets reprovando arte nova fora do orçamento.
- p95 no perfil mínimo.
- ~~Relatório Android.~~ (cancelado em 09/10/2026)

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Pacote de vegetação e terreno na camada privada ou só CC0.
- [ ] Escolher a luz entre 3 prints.

## Tarefas, em ordem

- [ ] TEC-09 — Arte nova comprimida na GPU
- [ ] TEC-10 — LOD nativo e auditoria reprovando
- [ ] VIS-03 — Chão realista completo
- [ ] VIS-11 — Grama definitiva
- [ ] VIS-13 — Trilha no chão (variante A)
- [ ] VIS-06 — Luz e grading finais, e GI
- [ ] VIS-12 — Mata com LOD
- [ ] VIS-23r — Relevo e horizonte (sem água)
- [ ] VIS-24 — Vida ambiente e morte súbita barata
- [ ] BICHO-11 — Elenco coerente
- [ ] VIS-26 — Animal crível
- [x] ~~TEC-36 — Build Android de fumaça nº 1 (relatório)~~ — cancelada: celular fora da meta (Felipe, 09/10/2026)

---

### TEC-09 — Arte nova comprimida na GPU

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2 · prioridade 3,5
- **Depende de:** TEC-04

**Por que, e nesta fase:**

Decodificar JPG em RGBA32 escala mal e engasga quando um estágio de torre aparece.

**O que é:**

Trocar o JPG em .bytes, decodificado em runtime como RGBA32, por Texture2D importada: BC7/BC5 no PC e ASTC no Android. O ajuste do GradeToPalette passa para o conversor, offline. O .bytes fica só de fallback.

**Valor para o jogador:**

Boot mais rápido, nenhum engasgo quando um estágio de torre aparece e ~4x menos VRAM.

**Pronto quando:**

Toda textura nova como Texture2D BC7/BC5 (ASTC preparado para Android); as antigas migram quando forem trocadas. Boot e VRAM menores, sem mudança de cor.

**Como verificar:**

Métricas mostram boot e VRAM menores e o p99 do upgrade de torre menor, sem mudança de cor no print.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 3, risco 2) A virada para o realista vai multiplicar texturas (PBR 2k e 4k, chão, props). Manter JPG decodificado em runtime como RGBA32 escala mal em VRAM e em boot e causa engasgo quando a torre sobe de estágio, coisa que o jogador sente. Melhor mudar o pipeline antes da leva grande de arte do que converter tudo depois. Risco de diferença de cor ao mover o GradeToPalette para offline: conferir no print.
- (depois; valor 3, custo 3, risco 2) Ganho real. São 48 JPG em .bytes (16 MB) decodificados para RGBA32 com LoadImage na thread principal, incluindo as texturas das torres. Isso gera VRAM alta e engasgo quando um estágio de torre aparece. O momento certo, porém, é a troca de arte: o tema realista provavelmente substitui boa parte dessas texturas, e o GradeToPalette (que puxa a foto para a paleta) talvez nem sobreviva. Regra prática: arte nova já entra como Texture2D importada e comprimida (BC7/BC5, ASTC), e as antigas migram quando forem trocadas. Converter agora seria refazer o trabalho depois.

</details>

---

### TEC-10 — LOD nativo e auditoria reprovando

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 4 · risco 3 · prioridade 3
- **Depende de:** TEC-06

**Por que, e nesta fase:**

Arte realista sem LOD derruba o FPS.

**O que é:**

O Blender gera LOD0/1/2 e um proxy de sombra para torres, fortaleza, bichos e árvores, e o Unity monta o LODGroup pelo sufixo. Bake de normal map (18 de 21 torres não têm). Um AuditaAssets reprova malha ou textura fora do orçamento.

**Valor para o jogador:**

Mais torres e bichos na tela sem perder FPS, e torres menos chapadas.

**Pronto quando:**

Mesh LOD nativo do importador testado na 6000.3 antes de qualquer pipeline no Blender. O AuditaAssets reprova arte nova fora do orçamento.

**Como verificar:**

O AuditaAssets reprova hoje a Torre_Gelo_3 (95k) e a Fortaleza (92,6k) e passa depois, sem 'pop' visível na galeria.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 4, risco 3) O FPS caiu por causa da grama (4M tris), não das torres. 18 torres a 10-95k tris cabem numa 4070 Ti. O normal map 'bakeado' sem uma malha high-poly de origem pouco muda o visual. Fazer o pipeline Blender agora é arriscado porque o tema/arte ainda pode mudar e os assets seriam trocados. O Unity 6.2+ já gera Mesh LOD nativo na importação, então esse é o primeiro passo antes do Blender. Agora só vale o AuditaAssets (orçamento de tris/textura), que é barato e útil quando chegar a leva de assets realistas.
- (depois; valor 3, custo 4, risco 3) O gargalo de FPS hoje é a grama (4,0M tris) e a falta de níveis de qualidade, não as torres. Numa câmera de TD, quase tudo fica à mesma distância, então o LOD rende pouco no PC. O bake de normal map pelo bpy headless em malha do Meshy (UV bagunçada, cage) é frágil e caro. Se o tema mudar, as 21 torres Karrades podem ser trocadas e o trabalho se perde. Pacotes pagos geralmente já trazem LODGroup. Separar a ideia: o AuditaAssets de orçamento é barato e pode entrar já no pre-commit do TEC-22. A geração de LOD fica para quando a arte estabilizar, porque o mobile vai exigi-la. Depende do TEC-06.

</details>

---

### VIS-03 — Chão realista completo

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 3 · risco 2 · prioridade 4,5
- **Depende de:** VIS-01, TEC-04

**Por que, e nesta fase:**

É o maior ganho de beleza por hora gasta.

**O que é:**

As fotos atuais são marrons (leafy_grass dá RGB 151,131,89). Proposta:
- 4 camadas: grama viva, grama roçada na lane, terra batida e cascalho;
- mask map e mistura por altura;
- ladrilho em escala real e com tamanhos que não coincidem;
- macro-variação de 30-60 u;
- borda natural da lane no lugar da mureta de caixas.

**Valor para o jogador:**

O que ocupa ~80% da tela deixa de ser amarelo-seco e com ladrilho visível.

**Pronto quando:**

- 4 camadas com mask map e mistura por altura, macro-variação e tamanhos de ladrilho que não coincidem.
- Matiz na faixa da bíblia de arte (G>R só se o bioma for verde).
- Lane plana em y = 0.

**Como verificar:**

Métrica de matiz e G>R nos recortes do chão: hoje falha, depois passa.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 5, custo 3, risco 2) O chão ocupa ~80% da tela, então é o maior ganho de beleza por hora gasta. Ladrilho visível, ocre chapado e falta de macro-variação são o que mais grita 'amador'. Camadas, mistura por altura e borda natural da lane são o caminho certo. Ressalva: a métrica 'G>R' assume chão verde. Se o tema escolhido for savana ou planalto seco, chão dourado é legítimo, e o defeito real é a falta de variação e o ladrilho, não a cor. Fazer logo depois da VIS-01 e com a régua do tema.
- (logo; valor 5, custo 3, risco 2) É o maior ganho visual por esforço, porque o chão ocupa ~80% da tela. Grande parte é nativa: o TerrainLit do URP faz mistura por altura com até 4 camadas e mask map. O código novo é empacotar os mapas da Poly Haven no mask map (ferramenta de Editor) e pintar o splat. A macrovariação não é nativa no Terrain e vai pedir camada extra ou shader. Vem logo depois do VIS-01, porque um tema de savana inverte a meta 'G>R': a métrica tem que sair da bíblia de arte, não ser fixa. Risco: a borda natural da lane não pode piorar a leitura de onde o labirinto termina, e o plano y=0 tem que ser mantido.

</details>

---

### VIS-11 — Grama definitiva

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 2,5 · prioridade 4
- **Depende de:** TEC-04, TEC-06, VIS-03

**Por que, e nesta fase:**

De longe, a grama vira fiapos escuros: é aliasing e cor, não só triângulos.

**O que é:**

Hoje são 4 M de triângulos sem corte, e o FPS caiu para 54.

Primeiro, um spike com detalhes nativos do Terrain (instancing, distância, densidade, cor do chão).

Plano B:
- cartões de 6-24 triângulos com atlas assado das touceiras;
- pedaços com bounds próprios;
- densidade pela distância;
- fora do DepthNormals;
- flores e vento.

Teto de 400k triângulos.

**Valor para o jogador:**

Campo verde e denso visto de cima, e o FPS de volta acima de 100.

**Pronto quando:**

- Spike do detalhe nativo do Terrain (adotado ou descartado).
- No máximo 400k triângulos, corte por pedaço e distância.
- Alpha-to-coverage ou a AA decidida na VIS-28, cor acertada sobre o chão novo.
- Prints de perto e de longe; p95 no orçamento.

**Como verificar:**

Triângulos da grama ≤ 400k, p95 no orçamento e prints de perto e de longe.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 3) FPS de 104 para 54 numa 4070 Ti significa que numa placa média da Steam fica injogável, e o ganho visual é quase nulo: de 36-58° a grama 3D vira 'fiapos escuros', e quem dá a cor verde é o chão. O passo imediato é de uma linha: voltar a densidade, tirar a grama da lane e da passada DepthNormals do SSAO. O spike com detalhes nativos do Terrain é o degrau certo antes de cartões feitos à mão. Risco de jogo: grama alta na lane esconde rato e cachorro. Manter a lane roçada, como a própria VIS-03 propõe.
- (logo; valor 4, custo 3, risco 2) A regressão de 104 para 54 FPS tem mitigação de uma linha que vale já: voltar o TriangleBudget/MaxInstances e tirar a grama do DepthNormals do SSAO. O plano completo é M de verdade. O spike com detalhes nativos do Terrain é a ordem certa (instancing e distância já existem), mas dele não sai culling por pedaço nem cartões com atlas. Os cartões são trabalho de shader e de bake. Vale perguntar se a grama 3D se paga com câmera a 36-58°: um chão bom (VIS-03) pode resolver quase tudo, e aí o teto de 400k sobra.

</details>

---

### VIS-13 — Trilha no chão (variante A)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2 · prioridade 3
- **Depende de:** VIS-03

**Por que, e nesta fase:**

O labirinto aparece no próprio chão.

**O que é:**

Variante A: traça o caminho atual do flow field, com crossfade quando muda.
Variante B: um mapa de desgaste acumula a passagem (o elefante pisa mais) e volta a verdejar devagar.
Só vista, sem afetar a simulação.

**Valor para o jogador:**

O labirinto aparece no chão sem setas.

**Pronto quando:**

Caminho do flow field desenhado como terra batida, com crossfade. Teste de traço contíguo.

**Como verificar:**

Teste puro (traço contíguo ou desgaste com decaimento), e pelo menos 8 das 10 células mais gastas em cima do caminho.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 2) Ler o labirinto importa num maze TD, mas quem decide é a PRÉVIA do caminho ao posicionar a torre (UX), não o rastro depois. A variante A tem valor funcional. A B (mapa de desgaste que volta a verdejar) é bonita no papel e quase ninguém repara durante a partida, então fica como polimento. Fazer a A depois do chão novo, como trilha de terra batida coerente com o realismo, e não como linha de debug.
- (depois; valor 3, custo 3, risco 2) A variante A tem valor real de jogabilidade num TD de labirinto, porque o FlowField já tem o caminho. O mais útil seria mostrar o caminho NOVO no fantasma de construção, antes de construir. A variante B (mapa de desgaste) é cosmética e exige um chão com máscara ou splat, que depende do VIS-03. Risco de poluir o chão: território, grade e trilha somam três camadas. Recomendo só a A, ligada ao fantasma. A B fica como talvez.

</details>

---

### VIS-06 — Luz e grading finais, e GI

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2,5 · risco 2 · prioridade 4
- **Depende de:** VIS-03

**Por que, e nesta fase:**

Fecha o que a fatia começou.

**O que é:**

- Sol quente a 28-38° e HDRI de fim de tarde alinhado com ele.
- Grading nativo (WhiteBalance, split toning, LUT) no lugar do contraste 6 / saturação 4.
- Névoa exponencial ao quadrado.
- Sombra de nuvens por cookie.
- Bloom só no que brilha.
- Presets de hora do dia pela semente.

**Valor para o jogador:**

Volume e profundidade: a cena passa a parecer fotografada.

**Pronto quando:**

- Sombra de nuvem por cookie e bloom só no que brilha.
- GI conforme a VIS-28.
- Menos de 1% de pixels estourados e bicho legível na sombra longa.
- O Felipe escolhe entre 3 prints.

**Como verificar:**

Menos de 1% de pixels estourados, matiz do chão na faixa e três prints para o Felipe escolher.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 2) Luz e grading são o caminho mais barato para 'parece fotografado': contraste 6 e saturação 4 mais luz chapada matam o realismo, mesmo com bons assets. Sombra de nuvem por cookie e névoa são baratas e têm muito efeito. Tem que ser ajustado junto com o chão. Cuidado: sol baixo (28-38°) faz sombra longa sobre a lane e pode esconder bicho pequeno; validar a leitura, não só a beleza. Presets de hora pela semente são extra: só depois de existir um look aprovado.
- (logo; valor 4, custo 3, risco 2) Tudo é nativo do URP: WhiteBalance, split toning, LUT, névoa exponencial ao quadrado e cookie na luz principal. O custo está na iteração, porque é ajuste subjetivo com rodadas de aprovação do Felipe por print, e não em código. Cortar os 'presets de hora do dia pela semente', que multiplicam o que precisa ser aprovado e testado. A meta de pixels estourados é boa e barata. Depende do chão (VIS-03), senão a correção de cor vira remendo do amarelo.

</details>

---

### VIS-12 — Mata com LOD

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4,5 · custo 4,5 · risco 3,5 · prioridade 3,5
- **Depende de:** VIS-01, VIS-03, TEC-06

**Por que, e nesta fase:**

Transforma o tabuleiro em lugar.

**O que é:**

Clareira emoldurada por mata densa, de 3 a 6 espécies. Um Tools/ConverterArvores gera LOD0, LOD1 e um impostor hemi-octaédrico, que basta porque a câmera nunca desce de 36°. Crossfade entre níveis e ~0,35 M de triângulos no total.

**Valor para o jogador:**

O maior salto para 'mundo de verdade', e a borda do terreno fica escondida.

**Pronto quando:**

3 a 6 espécies no manifesto, LODGroup e billboard nativos, ~0,35 M triângulos e nenhuma árvore procedural no quadro.

**Como verificar:**

Log de árvores por LOD e de triângulos, com 0 procedurais e p95 no orçamento.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 4, risco 3) A moldura de mata é o que mais transforma 'tabuleiro' em 'lugar', e responde à queixa 5 do Felipe. Mas o impostor hemi-octaédrico feito em casa é engenharia demais: antes, LODGroup e billboard nativos, ou o sistema de árvores do próprio Terrain, com névoa na distância. O risco maior é a arte: árvore realista CC0 de qualidade é rara (a Poly Haven tem poucas), e a do Fab só fica no PC. Depende do tema: savana pede acácia esparsa, não mata densa. Fazer depois de chão, luz e câmera.
- (depois; valor 5, custo 5, risco 4) É o maior salto para 'mundo de verdade', mas a estimativa G é otimista. Impostor hemi-octaédrico com crossfade é um sistema próprio (bake mais shader escritos em código), e a arte de árvore realista com licença limpa é escassa fora do Fab/Megascans, que não pode ir para o GitHub. Caminho mais barato: poucas espécies CC0, o Mesh LOD automático do importador do Unity 6.2+ (conferir na 6000.3) via ArtImportRules, e névoa para esconder a borda. Com a câmera teleobjetiva (VIS-09), aparece menos horizonte e a demanda cai. Depende do tema.

</details>

---

### VIS-23r — Relevo e horizonte (sem água)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

Sem colinas fechando o quadro, o tabuleiro plano continua maquete.

**Pronto quando:**

Colinas e fundo fecham o horizonte. SampleHeight na lane = 0 ± 0,001. p95 no orçamento. A água continua no estacionamento.

---

### VIS-24 — Vida ambiente e morte súbita barata

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 2,5 · risco 2 · prioridade 3
- **Depende de:** VIS-11, VIS-12

**Por que, e nesta fase:**

Vegetação parada não parece real, e a morte súbita pede virada visual.

**O que é:**

Vento global compartilhado por grama, árvores, bandeiras e fumaça. Pássaros distantes, pólen na luz, folhas caindo e borboletas, tudo fora das lanes.

**Valor para o jogador:**

O mundo parece vivo mesmo parado.

**Pronto quando:**

Vento compartilhado e pássaros distantes. Virada de luz e névoa na morte súbita (versão barata do VIS-07). Diferença de p95 de no máximo 0,2 ms.

**Como verificar:**

p95 com diferença ≤ 0,2 ms.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 2, risco 2) Vento em grama, árvores e bandeiras faz parte de vegetação parecer real. Isso vale e deveria entrar junto com a vegetação nova (VIS-11/12). Pássaros, pólen e borboletas são enfeite que o jogador olhando a lane mal vê, ou seja, polimento final para trailer e menu. Barato, desde que não toque a grama atual de 4M triângulos, que precisa ser resolvida antes.
- (depois; valor 2, custo 3, risco 2) O esforço P é otimista. Um vento global na grama e nas árvores exige shader com vento no vértice para toda a vegetação, e o URP Lit não tem isso. E a grama já é o maior problema de desempenho (4M tris). Com a câmera a 36–58° e longe, quase não se veem borboletas e pólen. Os pássaros distantes são baratos. Esperar a vegetação ficar resolvida (VIS-11 e VIS-12).

</details>

---

### BICHO-11 — Elenco coerente

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** G (3-5 sessões)

**Por que, e nesta fase:**

A colagem dos bichos pesa tanto quanto a das torres.

**Pronto quando:**

Executa a decisão da Fase 1: troca para a fonte única, ou ajusta materiais e escala dos 9. Migração Legacy→Animator se a fonte exigir. Desfile aprovado.

---

### VIS-26 — Animal crível

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

Pata patinando, clones marchando em sincronia e curvas secas são o que mais denuncia o falso.

**Pronto quando:**

- Velocidade do clipe ligada à da Sim, com passada calibrada por bicho e por estado.
- Deslize do pé medido no -captura.
- Fase, escala e tom variados em todo envio; curva suave; culling e LOD de animação.

---

### TEC-36 — Build Android de fumaça nº 1 (relatório)

- [x] **Status:** cancelada em 09/10/2026: o Felipe tirou o celular da meta (MANUAL, seção 9)  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

'Mobile-ready' sem nenhum build é declaração vazia.

**Pronto quando:**

IL2CPP, preset Baixo e ASTC; FPS medido num aparelho real. Só como relatório, e os bloqueios vão para o ROADMAP.

---
