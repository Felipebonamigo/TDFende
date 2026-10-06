# Fila de ideias para depois e ideias rejeitadas

[← cronograma](../../ROADMAP.md)

Ideias que o planejamento de 06/10/2026 não pôs em nenhuma fase. Reavalie quando uma fase terminar ou quando o Felipe pedir. Cada uma tem o cartão completo.

## Para depois

- **TEC-05 — Galeria dourada com diff entre builds:** Com a arte mudando, o 'dourado' seria refeito o tempo todo. Reavaliar depois da Fase 7.
- **TEC-08 — Forward+ e GPU Resident Drawer:** Nenhuma medição aponta gargalo de draws. Só se o profiler apontar depois da Fase 4.
- **TEC-13 — Cartão de conteúdo único por unidade:** O gargalo é arte, não C#. Reavaliar acima de ~25 unidades ou com mods.
- **VIS-07 — Céu acompanha a partida (versão completa):** Caro e piora a leitura. A versão barata (luz e névoa na morte súbita) já entrou na Fase 4.
- **VIS-22 — Portão das bestas com currais:** O portão cabe no cenário do tema. Os currais custam desempenho, e a informação fica melhor no HUD.
- **VIS-23 — Água em volta das lanes:** O relevo entrou na Fase 4; a água é polimento de vitrine, se sobrar fôlego.
- **UX-07 — Trilho de chegada:** Redundante com o UX-08 e o Tab, e o progresso não é monotônico.
- **UX-09 — Feed dos seus envios:** Precisa de id de lote que sobreviva ao LeakRouter. Reavaliar depois do Portão 2.
- **UX-11 — Janela da lane inimiga (PiP):** Segunda câmera cara; o Tab entrega quase tudo.
- **UX-14 — Relatório do adversário e selo 'eficaz agora':** Arrisca o replay e as metas. Reavaliar como ajuda do Fácil.
- **UX-20 — Treinador pós-partida:** Depende de conteúdo estável. Depois da Fase 14.
- **UX-25 — Bestiário e arsenal:** Só se o tema for natureza/expedição.
- **UX-26 — Toque e gestos:** Steam primeiro. As intenções, o token de toque e a safe area já preservam o caminho.
- **TORRE-09 — Armadilha no caminho:** Entidade nova fora do flow field, num papel que o Gelo cobre.
- **TORRE-11 — Posto de troca:** Compete com 'envio = renda'.
- **BICHO-04 — Saltador (Canguru):** Mexe no pathing, e o modelo animado é raro.
- **BICHO-07 — Camuflagem:** Shader de dither é a mesma classe do BUG-01. Só depois dos dois portões.
- **BICHO-08 — Gnu em disparada:** Regra sutil. Candidato se o tema for savana.
- **MERC-03 — Estoque de elite compartilhado:** Só tem graça entre humanos.
- **MERC-06 — Comando Reparar:** Decidir na Fase 12, depois de jogar com o derrubador.
- **MERC-09 — Bichos de suporte com aura:** A Matriarca mexe na tese, e precisa de modelos novos. Depois da Fase 12.
- **MERC-10 — Hiena que apaga a fronteira:** Só se a fronteira for a estrela nos portões.
- **MERC-13 — Babuínos saqueadores:** Responde a um prédio que não existe.
- **DES-02 — Morte súbita com identidade:** Primeiro ver se o DES-01 basta.
- **DES-06 — Sobreposição de fronteiras:** Só se o Portão 1 pedir mais profundidade.
- **DES-07 — Leva na corneta:** Mexe em todas as metas. Depois do Portão 2.
- **DES-08 — Estouro: segurar a manada:** Junto com o multiplayer.
- **DES-09 — Captura: defesa vira munição:** Diferencial pós-EA, começando pela variante A.
- **DES-11 — Clima e condições:** Seis BalanceLabs. Só a parte visual barata.
- **DES-12 — Manada neutra:** Vira bola de neve e pede modelos.
- **DES-13 — Mapas com terreno que joga:** Pós-EA, começando por 2 layouts no bioma atual.
- **DES-14 — Draft ou feira:** Quando o elenco passar de 12 torres ou 15 bichos (ver a decisão de seleção pré-partida).
- **DES-17 — Caça premiada:** Medir viradas primeiro.
- **DES-18 — Último reduto:** Reavaliar com os dados do Portão 2.
- **DES-20 — Cenários-quebra-cabeça:** 3 a 5 podem nascer do tutorial.
- **DES-22 — Arena de 4 a 8 lanes:** Pós-EA, com multiplayer.
- **META-06 — Desafio diário:** A semente local pela data pode entrar depois da Fase 15.
- **META-08 — Rival espelho:** Depois da liga.
- **META-09 — Campanha Expedição:** Pós-EA, com 3 a 5 missões.
- **META-10 — Teatro de replays:** O -replay do TEC-16 cobre a depuração.
- **META-15 — Online 1x1 por lockstep:** Pós-EA. O PlayerId e o lockstep virtual já entraram no TEC-27.
- **META-16 — Versus local, Remote Play e controle completo:** Pós-EA. O mínimo do Deck (1280×800 legível) já está no UX-01.
- **META-17 — Chat manda os bichos (Twitch):** Perto do lançamento, com a música já sem Content ID.
- **VIS-02 — Enquadramento não letal: o bicho foge:** ficou sem fase no planejamento; reavaliar (nota de prioridade 3).
- **MERC-02 — Estoque e recarga dos envios de elite:** ficou sem fase no planejamento; reavaliar (nota de prioridade 3).
- **META-03 — Página da Steam, demo e Next Fest:** ficou sem fase no planejamento; reavaliar (nota de prioridade 3).

## Rejeitadas

- **UX-27 — Oráculo de envio:** GG: fork da Sim em menos de 150 ms a cada hover, e tira a aposta da escolha. A ficha com 'bom contra' ensina o mesmo.
- **TORRE-10 — Barricada:** Leva ao labirinto degenerado do gênero e não projeta fronteira.
- **BICHO-03 — Escavador:** Bicho invisível num jogo que vende pelo visual: contra-duro frustrante e sem animação realista.
- **MERC-12 — Leilão às cegas:** Blefe não existe contra a IA, e empilha UI numa partida curta.
- **DES-15 — Juros sobre ouro:** Compete com 'envio = renda' e premia ficar parado.
- **DES-16 — Empréstimo:** Como foi especificado, é dinheiro grátis, e não acrescenta nada à tese.
- **META-11 — Melhores momentos automáticos:** Custo GG para uma câmera genérica; a Steam e o OBS já gravam.
- **META-18 — Editor com Oficina:** Custo GG; uma Oficina sem comunidade fica vazia, e o CatalogJson cobre o editor de regras para desenvolvedor.

---

## Cartões

### TEC-05 — Galeria dourada com diff entre builds

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 4 · risco 3 · prioridade 2
- **Depende de:** TEC-04, TEC-16

**O que é:**

Um replay fixo dirige planos nomeados, com captureDeltaTime fixo e Vfx semeado. O compara.py calcula SSIM e um mapa de calor contra o 'dourado' aprovado e gera uma galeria HTML que o Felipe aprova pelo celular. Um subagente com visão anota cada plano contra o guia de direção de arte.

**Valor para o jogador:**

Toda mudança visual é vista no executável real no mesmo dia.

**Como verificar:**

Dois builds sem mudança dão diff ≈ 0, e esconder um modelo de propósito acende exatamente os planos certos.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 4, risco 3) Ideia bonita no papel, mas agora a arte vai mudar radicalmente (tema, chão, céu, UI). O 'dourado' seria invalidado toda semana e o SSIM só acusaria a mudança que foi feita de propósito. Animação, partículas e TAA trazem ruído e alarme falso. A folha de contato do TEC-04 mandada ao celular cobre 80% disso por uma fração do custo. Só reconsiderar quando o visual estabilizar.
- (talvez; valor 2, custo 4, risco 3) Uma galeria dourada no meio de uma troca de direção de arte é desperdício: cada build muda quase tudo, e o 'dourado' seria refeito toda semana. Render não é bit-exato (SSAO, TAA, partículas e animação Legacy variam), então o SSIM vai precisar de limiar e calibração, com falso positivo constante. O subagente com visão é custo recorrente para julgar o que a folha de contato do TEC-04 no celular do Felipe já mostra. Reavaliar só quando o visual estabilizar.

</details>

---

### TEC-08 — Forward+ e GPU Resident Drawer

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 3 · risco 3,5 · prioridade 2
- **Depende de:** TEC-07, TEC-11

**O que é:**

Forward+ tira o limite de luzes por objeto que as explosões precisam. GPU Resident Drawer e oclusão por GPU servem às centenas de árvores e props, e as variantes BRG entram no build.

**Valor para o jogador:**

Explosões com luz e mata densa sem derrubar o FPS.

**Como verificar:**

Prints antes e depois sem diferença indesejada, e draws e p95 no métricas.json.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 3, risco 3) É solução esperando problema. Ninguém mediu que o jogo está limitado por draw calls, e o gargalo conhecido é a grama (triângulos). O GPU Resident Drawer não ajuda os bichos com Animation legado, exige shaders compatíveis e aumenta variantes e tempo de build. Explosão com luz é um detalhe que o jogador mal nota diante de um VFX bom. Só fazer se a captura provar que é limitado por CPU ou draws.
- (talvez; valor 2, custo 3, risco 4) O ganho é especulativo. O gargalo medido é a grama, que é desenhada com Graphics.RenderMeshInstanced e não passa pelo GPU Resident Drawer. O GRD só atende MeshRenderer, não os bichos skinned, e convive mal com static batching, que acabou de ser ligado no ProjectSettings. Também exige manter as variantes BRG no build, o que repete o risco de 'some no executável', e não roda em GLES no Android. Luz por explosão não tem uso enquanto os efeitos forem bolhas sem textura. Só faz sentido se o profiler mostrar draw calls ou CPU de render como gargalo depois das árvores realistas.

</details>

---

### TEC-13 — Cartão de conteúdo único por unidade

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 4 · risco 3 · prioridade 2
- **Depende de:** TEC-12, BUG-05

**O que é:**

Um arquivo Conteudo/<tipo>/<chave>.txt por unidade junta estatística, modelo, altura real, ícone, VFX, sons, descrição, papel e licença. Bicho novo = cartão + FBX, sem C#. O replay v2 grava chaves e continua lendo o v1. É architectural.

**Valor para o jogador:**

Mais bichos e torres por sessão, e a loja alimentada pelo mesmo cartão.

**Como verificar:**

Um 10º bicho só por cartão aparece no -captura, e o replay v1 reproduz byte a byte.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 4, risco 3) Parece ótimo no papel, mas o jogo terá umas 20-30 unidades, não 300. Um catálogo em C# com campos de loja (descrição, papel, ícone) resolve com uma fração do custo. Replay v2 + formato de arquivo novo é architectural e mexe no núcleo testado sem ganho visível para o jogador. Só vale se a produção de conteúdo virar gargalo comprovado depois do TEC-12.
- (talvez; valor 2, custo 4, risco 3) Ataca o gargalo errado. Trazer uma unidade nova custa caro por causa da arte (modelo realista animado, licença, escala), não pelas três linhas de C#, que o Claude escreve em minutos. Além disso, 'bicho novo sem C#' é otimista: os bichos novos pedidos (que atacam torre, voador de verdade) exigem lógica nova na Sim. O replay v2 com chaves é migração de formato num núcleo determinístico, e o CatalogJson já cobre parte disso. Reavaliar se o catálogo passar de cerca de 20 unidades ou se houver suporte a mod. Para a loja, bastam campos de metadado no catálogo atual.

</details>

---

### VIS-07 — Céu acompanha a partida

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 4,5 · risco 4 · prioridade 2,5
- **Depende de:** VIS-06, VIS-11, DES-01

**O que é:**

Um DayCycle puro lê o MatchTime: manhã, sol alto, fim de tarde e, na morte súbita, tempestade com vento, chuva, relâmpagos e chão molhado. Os bichos mantêm uma luminância mínima.

**Valor para o jogador:**

Cada partida ganha um arco dramático visível.

**Como verificar:**

Teste da curva no FlowSim, print com '-hora 0.95' e p95 com chuva dentro do orçamento.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 4, risco 4) Fica lindo no trailer, mas parece melhor no papel do que no jogo. Um ciclo de dia inteiro em partidas de 7-10 min fica estranho, e cada hora do dia desfaz o look calibrado na VIS-06. Chuva e cena escura prejudicam a leitura dos bichos, e chuva com chão molhado pesa no mobile. A versão que vale a pena é barata: só uma virada de clima na morte súbita (luz, névoa, grading e chuva leve), como marcador dramático de 'a partida mudou'. Só depois que o look base estiver aprovado.
- (talvez; valor 3, custo 5, risco 4) É caro em quase todas as frentes. O chão molhado exige shader próprio no Terrain, porque o TerrainLit não tem umidade. Chuva, relâmpago e vento são VFX novos e pesam no mobile. Depende de três itens ainda não feitos (VIS-06, VIS-11, DES-01). Tempestade na morte súbita também piora a leitura justo no momento mais tenso de um TD. Se entrar, que seja só a curva do sol pelo MatchTime (barata e testável) e a tempestade bem no fim do polimento.

</details>

---

### VIS-22 — Portão das bestas com currais

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 3 · prioridade 2,5
- **Depende de:** VIS-04, BUG-01

**O que é:**

Um portão ou paliçada substitui as tendas e se abre quando chega um envio. Currais mostram os bichos que o rival já comprou (SendsByType).

**Valor para o jogador:**

Lê-se no mundo o que o rival está investindo.

**Como verificar:**

Print depois de 10 envios com os currais mostrando os tipos certos.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 3) Trocar as tendas de brinquedo por um portão que se abre na chegada de um envio é ganho real, um telegrafo bonito de 'lá vem'. Os currais com os bichos do rival são legais no papel, mas custam mais bichos animados na tela, e o jogador lê melhor a mesma informação num painel do rival no HUD com os retratos do VIS-25. Fazer o portão junto com o cenário. Currais ficam para talvez, depois do BUG-01.
- (talvez; valor 3, custo 3, risco 3) Trocar as tendas de brinquedo por um portão que abre quando chega envio tem valor, mas isso cabe dentro do VIS-04 (cenário). Os currais com bichos vivos são a parte cara e especulativa: são mais malhas animadas na tela (desempenho no mobile), dependem do BUG-01, e a informação 'o que o rival comprou' fica mais legível e barata como um painel no HUD.

</details>

---

### VIS-23 — Água e relevo em volta das lanes

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 4 · risco 3 · prioridade 3
- **Depende de:** VIS-03, TEC-06

**O que é:**

Um rio com vau atrás do acampamento inimigo, ou lanes em platôs com um desfiladeiro e um rio entre elas. Shader de água por profundidade com espuma, e margens com as pedras e troncos que já existem. O topo da lane continua plano exato.

**Valor para o jogador:**

Composição de diorama e água viva.

**Como verificar:**

SampleHeight nas células da lane = 0 ± 0,001, print do rio e água ≤ 0,5 ms.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 4, risco 3) Com cerca de 80% da tela sendo chão, a composição em volta das lanes é o que separa 'diorama bonito' de 'tabuleiro'. Um rio entre as lanes dá a imagem de capa. Mas é caro (terreno, shader de água, margens, perf no mobile) e só faz sentido depois que o chão base (VIS-03) e o tema estiverem resolvidos. Antes disso é maquiar a moldura de um quadro feio. Para começar, um rio simples com shader por profundidade, sem platô nem desfiladeiro.
- (depois; valor 3, custo 4, risco 3) Relevo e água mudariam a composição, já que hoje cerca de 80% da tela é chão. Só que o problema número um é o material do chão em si (VIS-03), não a falta de rio. Shader de água por profundidade no URP é HLSL próprio e custa no mobile. O Terrain precisa garantir a lane plana exata, o que é testável e bom. Funciona com qualquer tema natural, mas é G de verdade. Só depois do chão e dos tiers de qualidade.

</details>

---

### UX-07 — Trilho de chegada

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 3 · risco 2 · prioridade 2
- **Depende de:** UX-01, VIS-25

**O que é:**

Faixas nas bordas com mini-retratos de cada bicho na sua lane (e dos seus na lane da IA), posicionados pelo progresso até a fortaleza, pulsando acima de 80%.

**Valor para o jogador:**

Acompanhar as duas lanes sem mexer a câmera.

**Como verificar:**

Teste do progresso (0 na entrada, 1 na base, monotônico), e print com 9 retratos.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 3, risco 2) Repete o que o UX-08 (alerta), o UX-09 (feed) e o UX-10 (troca de lane) já fazem, e soma mais uma faixa piscando na tela de um jogo que quer parecer realista. Com 9 retratos e escala crescente de envios, vira um trem ilegível no fim da partida. Parece legal no papel. Só vale se um playtest mostrar que o jogador é pego de surpresa por vazamentos mesmo com o alerta.
- (talvez; valor 2, custo 3, risco 2) É redundante com o UX-08 (alerta), o UX-10 (troca de lane) e o UX-09 (feed), e entulha a tela, o que pesa ainda mais no mobile. Também depende dos retratos do VIS-25. A verificação 'progresso monotônico' é falsa nesta simulação: construir torre muda o labirinto e aumenta a distância restante, e o bicho que vaza volta para outra lane. Com o envio escalando (ratos ×4 e mais), o trilho vira ruído.

</details>

---

### UX-09 — Feed dos seus envios

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 3 · risco 2 · prioridade 3
- **Depende de:** TEC-17

**O que é:**

Cada compra vira um cartão que acompanha o bando na outra lane ('4 Ratos: 3 mortos pelo atrito, 1 vazou'). Clicar foca a lane. No máximo 4 cartões.

**Valor para o jogador:**

Atacar ganha recompensa visível, o laço que vicia em Line TW.

**Como verificar:**

O despawn de um bicho enviado reporta o SenderId certo, e o Felipe sabe se o último envio vazou.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 2) A recompensa de atacar é real e o '3 mortos pelo atrito' reforça a tese. Mas o grosso desse prazer sai mais barato: um toast 'vazou! −1 vida da IA' e o HUD da IA piscando, que já cabem no UX-08/06. Cartões que acompanham cada bando e somem com morte e vazamento custam rastreio por remetente e espaço de tela. Fazer depois das lojas, e só se o toast simples não bastar.
- (depois; valor 4, custo 3, risco 2) O laço de recompensa do ataque é valioso, mas está subestimado. O SenderId é a lane de quem comprou, não o lote da compra. Para '4 Ratos: 3 mortos, 1 vazou' falta um id de lote no SimEnemy, e ele precisa sobreviver ao LeakRouter, quando o bicho vaza e volta a correr. Então a verificação proposta (SenderId certo) não prova o cartão. Vem depois da loja, do HUD e do alerta. O UX-10 já deixa olhar o estrago a custo quase zero.

</details>

---

### UX-11 — Janela da lane inimiga (PiP)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 3,5 · risco 3 · prioridade 2
- **Depende de:** UX-01, TEC-06

**O que é:**

Uma segunda câmera barata (sem pós, grama ou sombra, a 15-20 qps) mostra a lane da IA num monitor no canto. Clicar troca de lane. Quando um envio seu chega a 90% do caminho, o monitor dá zoom nele.

**Valor para o jogador:**

Ver o próprio ataque funcionar.

**Como verificar:**

Print com as duas lanes, e custo de no máximo +1,5 ms no p95.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 3, risco 3) Ver o próprio ataque é legal, mas é uma segunda câmera num cenário que já caiu para 54 FPS, e isso é ruim no mobile que é meta. O UX-10 (Tab em 0,4 s) entrega quase o mesmo prazer sem custo de render. O zoom automático a 90% parece ótimo no papel e tira o olho da própria defesa no momento errado. Reavaliar só depois que desempenho e LOD estiverem resolvidos e se o playtest pedir.
- (talvez; valor 3, custo 4, risco 3) O custo de +1,5 ms é otimista. Uma segunda câmera URP com render texture precisa excluir de verdade a grama: se ela for desenhada por instancing sem câmera definida, os 4M tris vão para as duas câmeras. Também tem que excluir sombra e SSAO, e há os bichos com animação Legacy. É ruim para o mobile e depende do TEC-06 (tiers de qualidade). O UX-10 entrega 80% do valor por quase nada. Só reavaliar depois de a grama e o LOD estarem resolvidos.

</details>

---

### UX-14 — Relatório do adversário e selo 'eficaz agora'

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 3 · prioridade 3

**O que é:**

Painel da lane inimiga: torres por tipo e nível, DPS total, antiaéreo e de área, e quanto do caminho está coberto pela fronteira. Os 1-2 envios que exploram essa defesa ganham um selo com o porquê. A nota vem de uma função SendAdvice pura, compartilhada com a IA. Desligável.

**Valor para o jogador:**

Cada compra vira jogada, e o jogador aprende os contras jogando.

**Como verificar:**

Contra 6 Canhões sem Sentinela, a Águia fica em 1º, e o replay continua idêntico depois da extração.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 3) Ensinar contras é bom, mas um selo 'eficaz agora' resolve pelo jogador a decisão que É o jogo (o que enviar), e vira piloto automático. Extrair SendAdvice da nota da IA mexe em código coberto por replay e balanceamento: risco desproporcional para uma dica. Melhor como ajuda da dificuldade Fácil ou de tutorial, desligada por padrão. O painel de torres da IA, sem o selo, pode entrar barato no UX-06.
- (depois; valor 3, custo 3, risco 3) Ajuda a aprender os contras, mas só rende de verdade com mais bichos e o segundo mercado. Com 9 envios o jogador descobre sozinho. O risco técnico é real: o ChooseSend da IA mistura nota e _rng. Extrair o SendAdvice tem que manter exatamente a ordem das operações de float e o consumo do RNG, senão o replay e as metas do laboratório de balanceamento quebram (o teste de replay idêntico está certo). Além disso, o exemplo 'Águia em 1º contra Canhões sem Sentinela' pressupõe um papel antiaéreo que o catálogo não declara.

</details>

---

### UX-20 — Treinador pós-partida

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2,5 · prioridade 3
- **Depende de:** UX-19, TEC-14

**O que é:**

Dois ou três conselhos gerados por regras puras sobre as estatísticas, com números da matriz. Exemplo: '8 vazamentos foram Águias; a Sentinela mata 27% mais rápido'.

**Valor para o jogador:**

Ensina contra-jogo e a tese usando a própria partida.

**Como verificar:**

Partidas montadas disparam a regra certa, e uma partida equilibrada não dispara nenhuma.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 3) A ideia é boa para ensinar contra-jogo, mas um conselho errado ou óbvio ('você perdeu porque vazou') deixa o jogo com cara de burro. Depende da matriz medida e da tela de fim. Só vale quando as regras forem calibradas em partidas reais e a regra 'partida equilibrada não dispara nada' estiver garantida. Não é o que vende o jogo.
- (depois; valor 3, custo 3, risco 2) A ideia é boa e as regras puras são testáveis, mas dependem da tela de fim (UX-19) e de uma matriz medida (TEC-14) que muda a cada rebalanceamento e a cada torre ou bicho novo. Um conselho errado com número ('mata 27% mais rápido') tira a confiança. Só depois que o conteúdo estabilizar.

</details>

---

### UX-25 — Bestiário e arsenal

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 3 · risco 1,5 · prioridade 2
- **Depende de:** UX-16, VIS-25, TEC-22

**O que é:**

Enciclopédia em tom de guia de campo: modelo girando, nome popular e científico, fatos reais com fonte, estatísticas, papel, 'responde com' e crédito do modelo. Torres nos 6 níveis. Marcos de coleção.

**Valor para o jogador:**

Reforça o realismo e ensina os contras.

**Como verificar:**

Teste de completude das fichas, e print do bestiário.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 3, risco 2) Em TD, bestiário quase nunca é o motivo de alguém comprar ou continuar jogando. Fatos reais com fonte dão trabalho editorial e combinam com um tema safári/natureza, mas não com fantasia. O crédito dos modelos CC BY é obrigatório e deve ficar numa tela de Créditos já, sem esperar o bestiário. Reavalie depois da decisão de tema.
- (talvez; valor 2, custo 3, risco 1) Combina com o realismo e ficaria ótimo num tema safári ou natureza, mas é conteúdo escrito (fatos reais com fonte para 9 ou mais bichos) mais um visualizador de modelo e as torres nos 6 níveis. O valor de jogo é baixo comparado a menu, loja e som. O crédito do modelo não substitui a tela de créditos obrigatória pela CC BY, que tem que vir antes e é barata. Reavaliar depois de escolhido o tema.

</details>

---

### UX-26 — Toque e gestos

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 4 · risco 3 · prioridade 3
- **Depende de:** TEC-18, UX-12, UX-15

**O que é:**

TouchInput com arrastar o cartão para construir, menu radial na torre, pressão longa abrindo a ficha, pinça e pan, alvos ≥ 48 dp e safe area. O reconhecedor de gestos é puro, e a flag -toque simula com o mouse.

**Valor para o jogador:**

Caminho real para o mobile e controles mais rápidos no PC.

**Como verificar:**

Testes do reconhecedor (Tap, LongPress, Drag, Pinch), e construção por arrasto no log.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 2, custo 4, risco 3) O alvo agora é a Steam. Para o jogador de PC, o ganho real é só o menu radial na torre e o arrastar para construir, e os dois podem nascer no painel de seleção (UX-12) sem o sistema de gestos inteiro. Pinça, pan, safe area e alvos de 48 dp são trabalho de porte mobile. Por ora, basta que as telas em UI Toolkit já nasçam escaláveis.
- (depois; valor 3, custo 4, risco 3) O alvo é a Steam primeiro, e o mobile ainda tem bloqueios maiores do que o toque: tiers de qualidade, compressão de texturas, 4M triângulos de grama e falta de LOD. IGameInput/DesktopInput já existem, mas o controlador lê Input direto, e a refatoração mexe em construir, vender e enviar, que hoje funcionam. Um reconhecedor puro testado é barato, mas arrastar para construir, menu radial e safe area exigem testar em aparelho de verdade. 'G' é otimista.

</details>

---

### TORRE-09 — Armadilha no caminho

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 3,5 · risco 3 · prioridade 2,5

**O que é:**

Fica no caminho sem bloquear e sem mudar o flow field. Prende por 1,5-2 s, com recarga ou cargas, e pega escavador. Usa um RootLeft separado do congelamento, com guarda contra prisão eterna. Na morte súbita só dá dano.

**Valor para o jogador:**

Uma ferramenta de colocação que hoje é impossível.

**Como verificar:**

O custo do flow não muda, o Lobo fica preso 1,5 s e a guarda funciona.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 3) É uma mecânica de colocação nova, satisfatória e realista (armadilha de urso, estacas). Dá ao jogador uma decisão que hoje não existe. O custo real está na regra de colocar em célula de caminho, na UI que mostra onde pode e na guarda contra prisão eterna, somada ao congelamento. 'Pega escavador' depende de um bicho que não existe. Fica bem como a primeira torre nova depois do visual e da UI.
- (talvez; valor 2, custo 4, risco 3) É uma entidade nova, não uma torre. Fica numa célula de caminho, que a colocação hoje recusa, e fica fora do flow field. Precisa de status de prisão próprio, guarda contra prisão eterna e regra de morte súbita. A IA também teria de aprender a escolher uma célula de caminho. O papel se sobrepõe ao congelamento do Gelo, e 'pega escavador' depende de um bicho que não existe. Custa muito para pouca decisão nova.

</details>

---

### TORRE-11 — Posto de troca (economia)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 2 · risco 3 · prioridade 2,5

**O que é:**

Não atira e soma +5 de ouro por pingo de renda (+3 por nível), com teto de 3 por lane. Dá renda que expõe em vez de pressionar o adversário.

**Valor para o jogador:**

Abre o estilo 'fazendeiro ganancioso'.

**Como verificar:**

+5 por pingo, teto respeitado e metas da IA verdes.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 2, risco 3) No Tower Wars, a renda já vem dos envios, e essa é a decisão central: atacar é investir. Uma fazenda que dá renda sem pressionar dilui essa tensão e incentiva a jogar parado, que é o oposto do gênero. Também ocupa célula de labirinto e exige ensinar a IA. Só faz sentido no TD clássico, onde não existe envio. Arte realista de posto de troca é fácil, mas isso não salva o design.
- (depois; valor 3, custo 2, risco 3) Na Sim é pouco: torre de dano 0, somar à renda no TickIncome e um teto por lane. A arte é um prédio estático, fácil de achar pronto (sem animação). O risco é na economia, que foi calibrada por medição: um segundo caminho de renda compete com a renda dos envios, que é a decisão central do Tower Wars. Abre espaço para jogar na defensiva acumulando ouro, a IA precisa avaliar o investimento, e há exploit de vender e construir perto do pingo. Também é a 7ª torre numa barra feita para 6 com Q/E.

</details>

---

### BICHO-04 — Saltador (Canguru)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BICHO · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3,5 · risco 4 · prioridade 2
- **Depende de:** BICHO-01

**O que é:**

Pula uma célula bloqueada a cada N s (Dijkstra com arestas de salto). Pune parede fina.

**Valor para o jogador:**

Labirinto de parede grossa passa a valer.

**Como verificar:**

Atravessa parede de 1 célula e não a de 2.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 3, risco 4) Um canguru pulando torre rende clipe, mas pune o padrão normal de labirinto (torre de 1 célula) e obriga a dobrar paredes, um imposto pesado. Também torna o caminho imprevisível para quem defende, o que exige mostrar onde ele vai saltar. Dijkstra com aresta de salto mexe na validação de 'não bloquear caminho' e na IA. Canguru realista com animação de salto é raro, e o Meshy quadrúpede só dá 'Andando'.
- (talvez; valor 3, custo 4, risco 4) O FlowField de hoje é um campo por lane rumo à base. O salto exige um segundo campo com arestas de salto, temporizador, célula de pouso livre e convívio com PlacementBlocksPath, CanBuild sob inimigo e empurrão. Isso é muita superfície de bug no pathing que já funciona. O ganho, punir a parede fina, é sutil para o jogador. O canguru animado com pulo realista também é difícil de achar.

</details>

---

### BICHO-07 — Camuflagem

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BICHO · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 3 · risco 3 · prioridade 2,5
- **Depende de:** BUG-01

**O que é:**

As torres só miram o bicho dentro do território (ou no raio de uma vigia). Fora dele, aparece esmaecido via MaterialPropertyBlock. Projétil já em voo continua valendo. Portador: Tigre ou Leopardo.

**Valor para o jogador:**

A fronteira ganha um segundo sentido, o de visão.

**Como verificar:**

Sentinela com fronteira 1,5 não acerta a 3 células, e o Gelo acerta.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 3, risco 3) É elegante no papel (fronteira = visão) e o Tigre já existe. Mas camuflagem em TD costuma frustrar, e aqui o contra é só 'ter fronteira', que todo mundo já tem, então o efeito prático pode ser pequeno. No URP, esmaecer uma malha opaca com skin não se resolve só com MaterialPropertyBlock: precisa de shader com dither ou troca de material. Só vale testar depois de confirmar que a fronteira é divertida.
- (depois; valor 4, custo 3, risco 3) É a ideia que mais ensaia o RTS: a fronteira vira visão. O filtro de alvo é barato. O esmaecer não é: os modelos baixados (Sketchfab legado, Meshy) usam materiais diferentes, então o fade por MaterialPropertyBlock exige um shader com dithering ou variantes transparentes, justo no caminho de render quebrado do BUG-01. Sem a UI explicar por que a torre não atira, parece bug. Mudar o Tigre também mexe em balanceamento calibrado.

</details>

---

### BICHO-08 — Gnu em disparada

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BICHO · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 3 · prioridade 2,5
- **Depende de:** BUG-01

**O que é:**

Bando de 3 que corre +40% enquanto fica junto. A resposta é alvo único de longo alcance que quebra a manada cedo.

**Valor para o jogador:**

Matar o primeiro cedo vira decisão.

**Como verificar:**

Matar 1 cedo aumenta o tempo de travessia dos outros.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 3, risco 3) Uma manada correndo com poeira é bonita, mas 'só corre junto' é uma regra sutil que o jogador não percebe sem UI dedicada. Além disso, o empurrão do Ar e a área do Morteiro já separam ou matam o grupo, o que confunde a lição pretendida. A mesma pressão sai de um bicho rápido comum. Gnu realista animado existe, mas é mais um modelo para caçar.
- (depois; valor 3, custo 3, risco 3) A lógica de manada é moderada: identificador de bando, proximidade e bônus de velocidade. O empurrão separa a manada, o que é bom e precisa de teste. O custo pesado é o gnu realista animado, porque achar um com licença CC BY e boa qualidade não é garantido, e o Meshy só dá a animação 'Andando'. Combina com um tema safári, se esse for o escolhido. Precisa da loja nova (10º slot).

</details>

---

### MERC-03 — Estoque de elite compartilhado

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 2 · risco 2,5 · prioridade 2
- **Depende de:** MERC-02

**O que é:**

Uma reserva comum de 2 alfas por janela de 60 s: o que um jogador compra, o outro não pode comprar.

**Valor para o jogador:**

Negação de mercado e corrida pela elite.

**Como verificar:**

A compra de um lado esgota o estoque do outro.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 2, risco 3) Contra a IA, 'esgotado porque o adversário comprou' parece arbitrário e frustra, não vira corrida. Só faz sentido em PvP de verdade, e o MatchRunner hoje é fixo em jogador contra IA. Guarde para quando houver multiplayer.
- (talvez; valor 2, custo 2, risco 2) A negação de mercado é mecânica de jogo entre pessoas. Contra a IA, ver o 'seu' alfa esgotado parece arbitrário. A implementação é simples (contador no MatchSim), mas exige regra determinística para compra simultânea no mesmo tique e depende do MERC-02. Guardar para quando houver multiplayer.

</details>

---

### MERC-06 — Comando Reparar

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 3 · risco 2 · prioridade 2
- **Depende de:** MERC-05, UX-15

**O que é:**

Repair cobra ouro proporcional à vida que falta, e o upgrade restaura tudo. A IA ganha regras de reparar e proteger a torre-chave.

**Valor para o jogador:**

Uma decisão tensa no meio da onda.

**Como verificar:**

Reparo cobra o valor certo, e o replay reproduz.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 3, risco 2) Num duelo rápido, em que você já constrói, sobe nível e envia bichos, mais um clique de manutenção tende a virar tarefa chata e não decisão tensa. Só existe se MERC-05 existir. Se existir, o mais simples é o upgrade restaurar a vida, sem um comando próprio de reparo.
- (talvez; valor 3, custo 3, risco 2) Segue o caminho conhecido de comando (CommandKind → Replay → Apply), mas também precisa de botão no painel de seleção (UX-15), regra de IA e teste de replay. O upgrade restaurando tudo e a venda já dão uma saída para torre danificada. Decidir só depois de jogar o MERC-08 e ver se a destruição parece injusta sem reparo.

</details>

---

### MERC-09 — Bichos de suporte com aura

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 4 · risco 3 · prioridade 2,5
- **Depende de:** MERC-01, TORRE-01, TORRE-08

**O que é:**

- Matriarca: reduz o atrito dos aliados (x0,35) ou cura 4%/s, cortada pela queima.
- Búfalo-guarda: reduz o dano de projétil dos que vêm atrás dele.
- Leão: acelera o grupo em +25%.
Anel no chão. Respostas: mira 'suporte primeiro', Arpão e Fogo.

**Valor para o jogador:**

Envios que se combinam e o quebra-cabeça de quem matar primeiro.

**Como verificar:**

Com a Matriarca, um Urso perde 35% do atrito normal; morta, volta ao normal.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 4, risco 3) É uma mecânica comprovada no gênero: o quebra-cabeça de quem matar primeiro dá profundidade de verdade às compras. A Matriarca reaproveita o elefante. O Búfalo e o Leão precisam de arte nova, CC BY ou gerada no Meshy (perguntar antes dos créditos). Depende da mira configurável (TORRE-01), sem a qual não há resposta. O anel no chão destoa um pouco do realismo, então use um efeito discreto em vez de neon.
- (talvez; valor 3, custo 4, risco 3) O quebra-cabeça de quem matar primeiro é bom, mas depende de mira configurável (TORRE-01) e de uma torre nova, o Arpão (TORRE-08). A Matriarca que corta o atrito vai contra a tese no momento em que ela ainda não foi validada. Leão e Búfalo exigem modelos animados novos. A Matriarca pode reaproveitar o elefante. Aura com acúmulo é fonte clássica de desbalanceamento, e o anel no chão compete com a leitura da fronteira. Só depois do DES-03 e do MERC-08.

</details>

---

### MERC-10 — Hiena que apaga a fronteira

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 4 · risco 4 · prioridade 2
- **Depende de:** MERC-01, TORRE-05

**O que é:**

O bando de hienas marca as células onde pisa, que deixam de ser território por 10 s. Abre um corredor sem atrito para o bicho gordo que vem atrás. A vigia impede a marca.

**Valor para o jogador:**

O ataque disputa o território, que é o coração do jogo.

**Como verificar:**

Um Cachorro enviado logo depois das hienas chega mais fundo, e o atrito continua ≥ 12% das mortes.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 4, risco 4) É a ideia mais ligada à tese e a que mais parece genial no papel. Na tela, o jogador vê células piscando e o bicho gordo passando, e talvez nem entenda por quê. Também pode corroer o próprio atrito que dá identidade ao jogo. Só vale a pena se o DES-03 confirmar que a fronteira é divertida e se houver uma visualização muito clara do território apagado.
- (talvez; valor 4, custo 4, risco 4) É a ideia mais alinhada à tese (o ataque disputa o território), e por isso a mais perigosa. Pede máscara temporal por célula no TerritoryField, rebuild frequente, uma torre que ainda não existe (Vigia, TORRE-05), modelo de hiena animado e um visual de 'território apagado' que precisa ser muito legível. Só vale se o DES-03 confirmar que a fronteira é divertida. Se não for, a hiena reforça um núcleo errado. Combina com tema savana.

</details>

---

### MERC-13 — Babuínos saqueadores

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 3 · risco 3 · prioridade 2
- **Depende de:** TORRE-11, MERC-05, MERC-01

**O que é:**

Bando de elite que rouba 4 de ouro por golpe num Posto de troca e manda o ouro para quem os enviou. A transferência acontece na fronteira do tique.

**Valor para o jogador:**

Pune a ganância de quem enche a lane de Postos.

**Como verificar:**

A soma de ouro entre as lanes se conserva, e o replay é determinístico.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 3, risco 3) É um contra de nicho para um prédio de economia (TORRE-11) que ainda nem existe. Ainda puxa a vida da torre e um bicho novo com animação de saque. Roubar ouro é gostoso para quem envia, mas a cadeia de dependências é longa demais para o retorno. Reavalie só se os Postos de troca entrarem e dominarem o metajogo.
- (talvez; valor 2, custo 3, risco 3) É a resposta a um problema que ainda não existe: o Posto de troca (TORRE-11) não foi feito, e ninguém sabe se será dominante. Exige transferência de ouro entre lanes (MatchSim, conservação, replay), vida de torre (MERC-05) e modelo de babuíno com animação de ataque. Só se o Posto virar estratégia dominante no BalanceLab.

</details>

---

### DES-02 — Morte súbita com identidade

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 3,5 · prioridade 2
- **Depende de:** DES-01, DES-03

**O que é:**

Final em atos. 'Fim da estação': a fronteira encolhe 10% a cada 30 s (variante: fica mais forte e mais cara). 'Estouro final': manadas espelhadas a partir de 9:00.

**Valor para o jogador:**

Um clímax legível no lugar de um moedor lento.

**Como verificar:**

O território aos 8:00 tem menos células que aos 6:59, e mediana de 7-10 min.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 3, risco 3) Encolher a fronteira no fim tira do jogador a ferramenta que define o jogo justo no clímax, e pode deixar a reta final sem identidade em vez de dar uma. As manadas espelhadas são mais promissoras. Primeiro faça o DES-01 e jogue. Só refaça o final se os testes mostrarem que ele é um moedor chato.
- (talvez; valor 3, custo 3, risco 4) Mexe no fim de partida que já foi calibrado por medição (morte súbita quadrática, escalada de 1,80/min). A fronteira encolhendo muda o atrito e quebra metas estatísticas do FlowSim. As 'manadas espelhadas' custam arte e desempenho. Só faz sentido se o DES-01 (anúncio) não resolver a sensação de moedor e se o DES-03 mostrar que o final é chato.

</details>

---

### DES-06 — Sobreposição de fronteiras

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 3 · prioridade 3

**O que é:**

O território conta quantas torres cobrem cada célula. O atrito multiplica por camada (ex.: +25-35%, teto 1,5-2x), com a base recalibrada. A vista pinta mais forte onde há sobreposição. As metas ganham um teto de atrito.

**Valor para o jogador:**

Decisão espacial entre espalhar e concentrar.

**Como verificar:**

Duas camadas tiram mais vida que uma, e o atrito fica entre 12% e 60% das mortes.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 3, risco 3) No papel parece uma decisão espacial, mas o raio de fronteira é 2,75 e num labirinto as torres ficam a 1-2 células umas das outras. Na prática, quase toda célula do caminho já teria várias camadas, e o efeito vira só 'mais torre, mais atrito', o que o dano já faz. Ainda obriga a recalibrar a base do atrito. Primeiro é preciso responder à pergunta central em aberto (a fronteira simples tem graça?) e só depois aprofundá-la.
- (logo; valor 4, custo 3, risco 3) Reforça a tese central, a fronteira com atrito, em vez de somar sistema paralelo. O TerritoryField é um bool[] e precisa virar contagem. O Rebuild por raio já existe. O caro é recalibrar: o 0,19 foi escolhido por varredura, então tudo tem de ser varrido de novo. A IA precisa de um termo de posicionamento para sobreposição, e a vista tem de pintar camadas, o que também é trabalho visual. Fazer só depois de uma rodada de playtest que mostre que a fronteira tem graça, porque essa pergunta nunca foi respondida.

</details>

---

### DES-07 — Leva na corneta

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2,5 · prioridade 3
- **Depende de:** UX-08

**O que é:**

O envio comprado espera reunido no acampamento e sai junto no próximo pingo de 10 s. O defensor vê a leva se formar.

**Valor para o jogador:**

Ritmo legível e jogo de tempo na hora de comprar.

**Como verificar:**

Zero inimigos ativos até o múltiplo de 10 s, e o replay é determinístico.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 2) Juntar os envios no pingo de 10 s, que já é o tique de renda, deixa o ritmo legível. Ver a leva se formando no acampamento é um bom aviso para o defensor e combina com o aviso de 'bichos chegando', que hoje não existe. O custo é a resposta ao clique, que fica atrasada, e por isso só vale com o bicho aparecendo na hora no acampamento (UX-08). Sobrepõe-se ao DES-08: escolher um dos dois.
- (depois; valor 3, custo 3, risco 3) A ideia é a fila no acampamento + soltar no pingo de 10 s. Ela concentra a pressão em picos, e isso mexe em todas as metas de balanceamento e no tempo da IA. Precisa do aviso de chegada (UX-08) e de uma leva visível esperando, e isso significa muitos bichos animados parados na tela, justo quando o render dos bichos está quebrado. Conflita com o DES-08: escolher no máximo um, e só depois de jogar o ritmo atual com UI decente.

</details>

---

### DES-08 — Estouro: segurar a manada

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3,5 · risco 3,5 · prioridade 2
- **Depende de:** UX-13

**O que é:**

O jogador segura os envios num curral visível, com a contagem no HUD do adversário, e solta de uma vez. Oito ou mais soltos juntos ganham +25% de velocidade e imunidade a empurrão por 4 s. O curral solta sozinho em 30 s.

**Valor para o jogador:**

Jogo mental e momentos enormes.

**Como verificar:**

O segurado não nasce até a soltura, e segurar vence no máximo 60%.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 3, risco 3) O jogo mental só rende de verdade contra um humano, e hoje só existe jogador contra IA. A IA precisaria ler e responder ao curral, e a imunidade a empurrão anula a torre de Ar justo no momento em que ela importa. É uma mecânica de timing que concorre com o DES-07, que é mais simples. Vale reavaliar quando houver multiplayer.
- (talvez; valor 3, custo 4, risco 4) O momento é bom, mas é a versão mais cara do mesmo eixo do DES-07. Pede curral visível com dezenas de bichos skinned, contagem no HUD do adversário, decisão de soltura na IA e um bônus de velocidade com imunidade a empurrão que mexe na torre de Ar recém-ajustada. A meta 'segurar vence ≤ 60%' mostra que o risco de estratégia dominante já é conhecido. Não fazer antes de validar o ritmo básico.

</details>

---

### DES-09 — Captura: defesa vira munição

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 4 · risco 4 · prioridade 2,5
- **Depende de:** UX-13, VIS-02

**O que é:**

Variante A: morte por atrito vira captura (metade da recompensa e uma ficha no Recinto para reenviar por 40% do custo, sem renda).
Variante B: uma torre de Captura converte bicho comum quase morto e o reenvia com 50% da vida.
Elite é imune, não há recaptura e tudo desliga na morte súbita. Visual não letal (dardo ou rede).

**Valor para o jogador:**

Mecânica única no gênero, que funde ataque e defesa.

**Como verificar:**

Captura conta só na morte certa, e capturas ficam entre 5% e 20% dos envios.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 4, risco 4) Pode virar um bom gancho de página da Steam ('capture os bichos dele e devolva'), e casa com um tema safári (dardo, rede). Mas é grande: Recinto, fichas, UI, visual não letal, regras de exceção (elite, morte súbita) e a IA. Também mistura ataque e defesa antes de a tese do atrito estar validada. Fica guardada como diferencial para depois que o núcleo provar que diverte.
- (depois; valor 4, custo 4, risco 4) É a ideia mais original da lista e casa com a tese (variante A: morte por atrito vira captura). Mas é sistema novo inteiro: estoque, comando de reenvio, regra contra recaptura, exceção para elite e morte súbita, UI do Recinto e visual não letal. A variante B ainda pede uma torre nova com arte. O equilíbrio entre 5% e 20% vai pedir várias iterações. Se for feita, começar pela variante A, sem torre nova, e só com loja e UI prontas.

</details>

---

### DES-11 — Clima e condições de partida

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 4,5 · risco 3,5 · prioridade 2,5
- **Depende de:** TEC-15, VIS-07

**O que é:**

Uma condição sorteada no início, ou um calendário a cada ~90 s, sempre simétrico e anunciado. Opções: Seca/Calor, Chuva, Neblina, Vento forte, Noite e Calmaria, cada uma com efeito de regra e visual. Rng próprio, para não mudar replays. Desligável.

**Valor para o jogador:**

Partidas diferentes entre si e uma vitrine do realismo.

**Como verificar:**

O calendário é igual para a mesma semente, e o BalanceLab fica verde por condição.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 4, risco 3) O valor real está no visual: chuva, neblina e noite são o que mais vende o 'realista' em screenshot e trailer. Já os efeitos de regra multiplicam a superfície de balanceamento (BalanceLab por condição). Sugestão: entregar primeiro o clima só visual, junto com VIS-07, e ligar as regras depois, uma condição por vez. Antes disso o chão, a vegetação e a iluminação precisam estar bonitos.
- (talvez; valor 3, custo 5, risco 4) Estimativa otimista. São 6 condições, cada uma com regra + visual (chuva, neblina e noite realistas em URP custam caro e precisam de tier mobile, que não existe), e o BalanceLab tem de passar por condição, ou seja, ×6 nas metas. Quebra fácil o balanceamento medido. Se entrar, começar só visual (hora do dia e neblina leve como vitrine) e sem efeito de regra.

</details>

---

### DES-12 — Evento de manada neutra

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 3 · risco 2 · prioridade 2
- **Depende de:** TEC-15

**O que é:**

Num momento fixo (ex.: minuto 4), uma manada neutra de cervos, zebras ou gnus atravessa as duas lanes ao mesmo tempo, pagando recompensa dobrada. Determinístico.

**Valor para o jogador:**

Marca o 'ato 2' com uma chance de virada.

**Como verificar:**

Nasce no minuto 4 nas duas lanes, com replay byte a byte.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 3, risco 2) Não é virada, é bola de neve: como a manada é simétrica e paga em dobro, quem tem a defesa mais forte mata mais e ganha mais. O custo escondido é arte: cervo, zebra e gnu precisam de modelo, rig e animação. É um momento isolado que pouca gente lembra depois da 3ª partida.
- (talvez; valor 2, custo 3, risco 2) Truque de valor baixo para o custo. Pede modelos animados novos (cervo, zebra, gnu): arte cara, que é preciso buscar com licença ou pagar créditos do Meshy. Na Sim, inimigo sem dono não encaixa no SenderId usado pelo LeakRouter e pela recompensa. Dá para reaproveitar os bichos atuais, mas aí perde o sabor. Só depois de existir conteúdo e visual sólidos.

</details>

---

### DES-13 — Mapas com terreno que joga

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 5 · risco 3,5 · prioridade 3
- **Depende de:** TEC-15, VIS-03

**O que é:**

Formato de mapa em texto com tipos de célula: rocha, vau, lama, mata alta, colina e ponte. Layouts espelhados (Pedreira, Riacho, Dois portões em que o atacante escolhe a porta) e 4-6 biomas realistas. O Gelo pode congelar o vau. Linha 'map' no replay e BalanceLab por mapa.

**Valor para o jogador:**

Rejogabilidade, e um motivo de jogo para o realismo.

**Como verificar:**

Caminho válido em cada mapa, e ≥ 75% das partidas decididas por mapa.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 5, risco 3) Um único mapa é pouco para a Steam, e terreno que muda a jogada (vau, rocha, ponte, o Gelo congelando o vau) dá um motivo de jogo ao realismo. O caro são os 4-6 biomas realistas, que multiplicam o trabalho de arte. Começar com o formato de mapa + 2-3 layouts no mesmo bioma + 2 tipos de célula, e só abrir bioma novo depois que o primeiro estiver bonito.
- (depois; valor 4, custo 5, risco 4) Rejogabilidade real, mas é o item mais caro do bloco. Precisa de: formato de mapa, custo por célula no pathfinding, regras de construção por tipo de célula, IA ciente do mapa, BalanceLab por mapa e 4-6 biomas realistas. Isso é arte enorme, e hoje nem um bioma está bonito. Caminho honesto: primeiro só o formato de mapa com rocha e obstáculo e 2 layouts espelhados no bioma atual, e biomas muito depois.

</details>

---

### DES-14 — Elenco limitado: draft ou feira

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 3 · risco 2,5 · prioridade 2,5
- **Depende de:** TEC-15, UX-16

**O que é:**

Cada partida usa um subconjunto. Draft: 6 bichos e 4-5 torres, com o elenco do adversário visível para contra-escolher. Feira: rotação pela semente. Máscara de permitidos e um modo 'tudo liberado'.

**Valor para o jogador:**

Variedade e identidade de 'deck' sem a loja virar parede.

**Como verificar:**

Envio fora do conjunto é recusado, e o BalanceLab roda com conjuntos sorteados.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 3, risco 3) Com 6 torres e 9 bichos o elenco ainda não pesa, e cortar parte dele só tira opção do jogador. Contra-escolher contra a IA é raso. Faz sentido quando o conteúdo crescer (12+ torres, 15+ bichos, segundo mercado) e a loja começar a ficar cheia demais.
- (depois; valor 3, custo 3, risco 2) A máscara de permitidos é barata e segura na Sim (o envio fora do conjunto é recusado). Mas com 6 torres e 9 bichos, um subconjunto fica raso: só faz sentido quando o elenco crescer, como o Felipe pediu. Depende de uma loja bonita (UX-16) para o draft ser legível. Entrar junto com a expansão de conteúdo, não antes.

</details>

---

### DES-17 — Caça premiada contra o líder

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 1,5 · risco 2 · prioridade 2,5
- **Depende de:** DES-03

**O que é:**

Matar um bicho mandado por quem tem mais renda paga até 1,5x, multiplicador calculado na hora do envio. O HUD mostra 'caça premiada x1,3'.

**Valor para o jogador:**

Amortece a bola de neve e mantém partidas vivas.

**Como verificar:**

Abate de quem tem o dobro de renda paga 1,5x, e Difícil > Fácil se mantém.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 1, risco 2) Contra a IA, o líder costuma ser o jogador, e ser punido por jogar bem incomoda mesmo com o x1,3 aparecendo no HUD. A morte súbita já garante que a partida termina. Só vale ligar se o BalanceLab mostrar bola de neve, com taxa de viradas baixa, e usado como ajuste do Difícil, não como regra geral.
- (depois; valor 3, custo 2, risco 2) Barato: um multiplicador gravado no SimEnemy na hora do envio e um rótulo no HUD. Mas é borracha contra a bola de neve, e não há medição mostrando que a bola de neve é um problema. Primeiro medir a taxa de viradas no BalanceLab. Contra a IA, compensação automática pode parecer injusta se não ficar bem visível. Depende do DES-03.

</details>

---

### DES-18 — Último reduto

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 2 · risco 2,5 · prioridade 3
- **Depende de:** DES-04

**O que é:**

Uma vez por partida, com 25% ou menos de vida, a fronteira ganha +0,5 de raio até o fim, com anúncio na tela.

**Valor para o jogador:**

O 'quase virei' em vez de esperar a derrota.

**Como verificar:**

O território aumenta uma única vez.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 2, custo 2, risco 3) Mecânica de virada é barata, mas no Tower Wars um bônus de quem está perdendo costuma parecer injusto para quem está ganhando e pode esticar partidas já decididas. Ainda não sabemos se cercar o inimigo com fronteira tem graça (pergunta 1 do TESTE.md nunca respondida), então mexer no raio agora é ajustar algo não validado. Só faz sentido depois do playtest, e com som e anúncio bonito. Sem isso, é um número que muda em silêncio.
- (depois; valor 3, custo 2, risco 2) Barato no LaneSim: flag de uso único, +0,5 no raio, teste FlowSim e um aviso na tela. Mas é ajuste fino de virada em cima de uma tese que ninguém testou ainda (a pergunta 1 do TESTE.md continua sem resposta). Também mexe nas metas estatísticas do BalanceLab, porque o lado que está perdendo passa a ganhar mais vezes, e depende do DES-04. Só vale depois que o playtest mostrar que a fronteira diverte e que as partidas terminam cedo demais.

</details>

---

### DES-20 — Cenários-quebra-cabeça

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 4 · risco 2,5 · prioridade 2,5
- **Depende de:** TEC-15, DES-13

**O que é:**

Vinte cenários feitos à mão, com estado inicial e envios roteirizados e objetivos como 'mate 30 lobos só com atrito'. O FlowSim garante que existe solução e que não fazer nada perde.

**Valor para o jogador:**

Ensina cada mecânica sem tutorial em texto.

**Como verificar:**

O replay-solução vence e o replay vazio perde, em cada cenário.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 4, risco 3) Ensinar a mecânica jogando é muito melhor que tutorial em texto, e a garantia do FlowSim (a solução vence, não fazer nada perde) é ótima. Mas 20 cenários feitos à mão são muito trabalho de design para um público de puzzle-TD que é nicho, e o rótulo M subestima isso. Melhor começar com 4-5 cenários que funcionem como tutorial do atrito e da fronteira, e só expandir se os testadores gostarem.
- (talvez; valor 3, custo 4, risco 2) Garantir pelo FlowSim que tem solução e que ficar parado perde é elegante. Mas o custo de verdade é design à mão, não código: 20 cenários com estado inicial, envios roteirizados, objetivos, condição de vitória por cenário e replay-solução para cada um. Ainda depende do TEC-15 e do DES-13. Se houver tutorial, 3 a 5 cenários resolvem; vinte só se o jogo pegar.

</details>

---

### DES-22 — Arena de 4 a 8 lanes

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** GG (mais de 5 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 5 · risco 4,5 · prioridade 2,5
- **Depende de:** TEC-15, TEC-06, UX-01

**O que é:**

2x2 (o vazamento vai para uma lane inimiga, com doação de ouro entre aliados) e todos contra todos em anel, com troca de alvo. Primeiro contra IAs. Câmera e HUD de várias lanes, e MatchSim e IA generalizados.

**Valor para o jogador:**

O formato raiz do Line TW: caos, alianças e traição.

**Como verificar:**

Partida de 4 lanes termina, o vazamento vai para a lane certa e nenhum assento tem viés de vitória.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 5, risco 5) É o formato raiz do Line TW, mas a graça dele é o caos entre humanos, e contra IAs perde muito. Pede MatchSim, IA, câmera e HUD generalizados, além de 4-8 lanes de arte realista na tela. Isso agrava o desempenho que já é problema: a grama sozinha derrubou o FPS para 54. Sem multiplayer online e com a tese de uma lane ainda não validada, é escopo de pós-lançamento.
- (depois; valor 4, custo 5, risco 4) O LeakRouter já é em anel para N lanes, então a regra do todos-contra-todos existe em parte. Todo o resto é novo: MatchRunner fixo em jogador x IA, TowerWarsAi com um único adversário (me/foe), troca de alvo, doação no 2x2, câmera e HUD de várias lanes e viés de assento. O desempenho preocupa: uma lane já cai para 54 FPS com a grama. Com 4 a 8 lanes realistas, sem LOD nem tiers de qualidade, não roda. Sem rede, o 'caos e traição' vira contra IAs, o que tira boa parte da graça. Só depois de multiplayer e otimização.

</details>

---

### META-06 — Desafio diário

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 4 · risco 4 · prioridade 2,5
- **Depende de:** TEC-15, DES-19, META-04, TEC-16

**O que é:**

Semente pela data UTC, com mutadores e rival do dia. Os candidatos são certificados offline pelo BalanceLab (o proxy vence entre 35% e 65%). Melhor resultado local e, depois, placar da Steam com replay anexado e verificável.

**Valor para o jogador:**

Motivo para voltar todo dia e comparar com o mundo.

**Como verificar:**

Mesma data → mesmas regras, e um replay adulterado é pego pelo verificador.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 4, risco 4) Desafio diário segura quem já gosta do jogo, mas sem comunidade é uma tela vazia. A versão completa empilha mutadores, Steamworks, certificação no BalanceLab e verificação de replay. Essa verificação esbarra no float sem garantia entre runtimes: um replay legítimo marcado como adulterado seria pior que não ter placar. A semente local pela data UTC é barata e pode vir antes, sozinha.
- (talvez; valor 3, custo 4, risco 4) Encadeia dependências que ainda não existem: mutadores, Steam, TEC-15 e TEC-16. A promessa de que 'um replay adulterado é pego pelo verificador' é otimista: sem servidor, o verificador roda no cliente de quem trapaceia, e o float não tem garantia entre runtimes. A semente pela data e o desafio local são baratos. O placar mundial confiável não é. Só vale com uma base de jogadores que justifique.

</details>

---

### META-08 — Rival espelho

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 4 · risco 3,5 · prioridade 2
- **Depende de:** META-07, TEC-19

**O que é:**

Ajusta uma Personality a partir dos seus replays e cria o rival 'Você, semana passada'. Um código curto deixa um amigo jogar contra o seu estilo.

**Valor para o jogador:**

PvP assíncrono sem rede.

**Como verificar:**

Ida e volta a partir de uma Personality conhecida recupera a distribuição de envios.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 4, risco 4) No papel é genial ('Você, semana passada'), mas ajustar pesos a partir de replays raramente gera uma IA que o jogador reconheça como o próprio estilo. Se não reconhecer, a promessa vira decepção. É PvP assíncrono para um público que ainda não existe. Só vale reconsiderar depois dos rivais nomeados, e se houver dados de jogadores reais.
- (talvez; valor 2, custo 4, risco 3) Ajustar uma Personality de 5 parâmetros contínuos a partir de replays é um problema inverso que dificilmente imita o estilo de um humano. A IA nem tem preferência de envio para recuperar. O teste de ida e volta prova o ajuste, não que pareça 'você'. Chamar de PvP assíncrono é exagero, e o valor percebido é de curiosidade. Depende do META-07 e do TEC-19. Só se a liga existir e der certo.

</details>

---

### META-09 — Campanha Expedição

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 4,5 · risco 3 · prioridade 3
- **Depende de:** TEC-15, META-07, DES-13, META-05

**O que é:**

De 12 a 15 missões em dados, por bioma, com mutador natural, rivais e 3 estrelas (uma delas por atrito). Cada vitória libera 1 torre, bicho ou ramo. Cada missão é certificada por bot, e o modo livre tem 'liberar tudo'.

**Valor para o jogador:**

Começo, meio e fim, e um tutorial progressivo.

**Como verificar:**

Cada missão é vencível pelo bot e não fica mais fácil que a anterior.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 4, risco 3) Quem compra um TD single-player na Steam espera uma campanha, e ela também resolve o tutorial. Certificar cada missão com o bot aproveita bem o FlowSim. Só que depende de 4 itens e da pergunta central, se cercar com a fronteira tem graça, que ainda não foi respondida. Também não adianta antes de arte, UI e som. Começar com 5 missões, não 15: missão em dados é barata, mas missão que diverte é cara de balancear.
- (depois; valor 4, custo 5, risco 3) Dá começo, meio e fim e serve de tutorial, mas depende de 4 itens que ainda não existem. 'Por bioma' com arte realista multiplica o item mais caro do projeto: cenário de qualidade para cada bioma. A pergunta central do README (cercar com fronteira tem graça?) também segue sem resposta, e não faz sentido fazer 15 missões em cima de uma tese não validada. As missões em dados e a certificação por bot são baratas porque a Sim é determinística. O que pesa é o conteúdo, os mutadores novos na Sim (cada um com campo, Replay e teste FlowSim) e o balanceamento. Plano honesto: primeiro 3 a 5 missões-tutorial num bioma só, depois que o visual e a UI estiverem resolvidos.

</details>

---

### META-10 — Teatro de replays

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 4 · risco 2,5 · prioridade 2,5
- **Depende de:** TEC-16, TEC-19, UX-01

**O que é:**

Lista de replays com play, pausa e velocidade de 0,5x a 8x, pulo para um minuto por re-simulação, câmera livre e linha do tempo com marcas. Um código de compartilhamento abre o replay, com aviso de versão.

**Valor para o jogador:**

Rever derrotas e compartilhar partidas.

**Como verificar:**

Pular até o tique T dá o mesmo fingerprint que rodar direto, e o código faz ida e volta byte a byte.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 4, risco 3) Em TD, pouca gente revê replay. Esse público só existe com PvP e com comunidade. Pular de minuto por re-simulação e usar código de compartilhamento entre versões tem um risco grande: a simulação usa float e o replay quebra a cada mudança de balanceamento. Um player simples de replay serve de ferramenta interna, para o META-12 e para depuração, mas não de recurso para vender o jogo.
- (depois; valor 2, custo 4, risco 2) O que dá para reaproveitar já existe: Replay com assinatura de catálogo, e o pulo por re-simulação é trivial porque a Sim é pura. Mesmo assim, para o jogador de TD, rever replay é nicho. O custo real está na UI (lista, linha do tempo, marcas), na câmera livre e no código de compartilhamento entre versões. Vale fazer só a versão mínima (abrir arquivo, play, pausa e velocidade), porque ela destrava o META-12 e ajuda a depurar. O teatro completo fica para depois do lançamento.

</details>

---

### META-15 — Online 1x1 por lockstep

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** GG (mais de 5 sessões)
- **Notas dos avaliadores (1-5):** valor 4,5 · custo 5 · risco 5 · prioridade 3
- **Depende de:** TEC-15, TEC-16, META-04, META-16

**O que é:**

O MatchCommand ganha PlayerId e é agendado para o tique atual + um atraso. Hash de estado a cada 30 tiques, rng separado por função, Steam Networking com convite. Primeiro, um 'lockstep virtual' no FlowSim. É architectural.

**Valor para o jogador:**

PvP de verdade, o coração do gênero.

**Como verificar:**

500 partidas de lockstep virtual com hash igual, e uma dessincronia plantada é detectada.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 5, risco 5) PvP é o coração do Line Tower Wars, mas é a ideia mais cara e arriscada da lista. Float sem teste entre máquinas dá dessincronia, e as filas de matchmaking de um indie ficam vazias. Só depois de o jogo ser bonito e divertido contra a IA. Dá para preparar desde já, com pouco custo, o PlayerId no MatchCommand e o hash de estado. O lockstep virtual no FlowSim é o passo certo antes de qualquer rede.
- (depois; valor 5, custo 5, risco 5) PvP é o coração do gênero e ótimo treino para o RTS, mas é o item mais arriscado da lista. A Sim usa float sem teste entre máquinas ou runtimes, e ARM (mobile) contra x64 provavelmente diverge. A assinatura de estado é grossa demais para pegar dessincronia, e o MatchRunner é fixo em jogador contra IA. Por cima disso vêm Steam Networking (ainda inexistente), convite, reconexão e latência. É architectural: começar pelo 'lockstep virtual' no FlowSim é o caminho certo, mas só depois de o jogo single-player estar bonito e a tese validada. Estimativa honesta: semanas, não dias.

</details>

---

### META-16 — Versus local, Remote Play Together e Steam Deck

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 4 · risco 3 · prioridade 3
- **Depende de:** TEC-18, UX-01, META-04

**O que é:**

Dois humanos no mesmo PC (mouse × controle com cursor virtual). Isso habilita o Remote Play Together e o suporte a controle exigido pelo selo do Deck. Foe humano e PlayerId no replay.

**Valor para o jogador:**

Jogar com amigo antes do online próprio.

**Como verificar:**

Replay com PlayerId reproduz, e uma sessão real de Remote Play.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 4, risco 3) É o caminho barato para o PvP: o Remote Play Together dá online com amigos sem netcode próprio. O suporte a controle também ajuda a vender no Steam Deck. Deve vir antes do META-15. O custo real está na UI para dois jogadores numa tela e no cursor virtual. Só depois da UI nova.
- (depois; valor 3, custo 4, risco 3) É um caminho mais barato que o online para PvP e reaproveita o MatchCommand. Mesmo assim, exige uma câmera que mostre as duas lanes ou tela dividida, cursor virtual por controle no Input Manager legado e a UI inteira navegável por controle. O selo do Deck pede isso de ponta a ponta, e a UI hoje é OnGUI. Remote Play Together também depende da integração com a Steam. Faz sentido depois do UX-01 e da troca para o Input System, e serve como degrau antes do META-15.

</details>

---

### META-17 — O chat manda os bichos (Twitch)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 2,5 · risco 2 · prioridade 2
- **Depende de:** UX-01

**O que é:**

O chat vota o envio a cada 15 s (!1/!2/!3) por IRC anônimo, só com comandos fixos. A IA continua construindo o lado do chat e reassume se a rede cair.

**Valor para o jogador:**

Recurso que faz streamer escolher o jogo.

**Como verificar:**

Um log de chat gravado gera sempre os mesmos envios.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 3, risco 2) O recurso combina com o modo de envios, e o determinismo por log de chat é elegante. Mas streamer só escolhe um jogo que já é bonito e tem algum público, então hoje o recurso não traz ninguém. Fica para perto do lançamento, se sobrar tempo, como gancho de divulgação.
- (talvez; valor 2, custo 2, risco 2) A técnica é simples e isolada: IRC anônimo por TcpClient, comandos fixos, e os votos viram MatchCommand do lado da IA, então dá para reproduzir pelo log. Mas só tem valor se algum streamer escolher o jogo, e isso exige primeiro visual e UI de qualidade. A rede fica fora da Sim e o risco é baixo, mas é recurso de marketing para um jogo que ainda não está pronto para ser mostrado.

</details>

---

### VIS-02 — Enquadramento não letal: o bicho foge

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 3 · prioridade 3
- **Depende de:** VIS-01

**O que é:**

O bicho derrotado foge correndo ou cai exausto, em vez de morrer. O atrito vira medo ou cansaço do território. Num tema moderno, as torres viram dissuasores. Só vista: a Sim não muda.

**Valor para o jogador:**

Identidade única na loja, classificação etária baixa e nada de matar animal fotorrealista.

**Como verificar:**

Print de um bicho fugindo, sem nenhum Check do FlowSim alterado.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 3) Tem um mérito real: matar cachorro e elefante fotorrealistas com canhão pode pegar mal em review e em trailer, e 'o bicho foge' dá identidade. Mas o ganho de classificação etária é fraco (sem sangue já fica baixa), e fugir tira o 'estalo' de abate, que é metade da graça de um TD. Também fica incoerente com Canhão, Morteiro e Fogo, a menos que o tema vire 'dissuasão'. Custo escondido: os quadrúpedes do Meshy só têm 'Andando', não há animação de fugir nem de cair. Decidir junto com a VIS-01; implementar só depois que o feedback de abate estiver bom.
- (depois; valor 3, custo 3, risco 3) A ideia é boa para identidade e classificação, e a Sim realmente não muda, mas o custo de arte está subestimado. Os quadrúpedes do Meshy só têm 'Andando': não há correr nem deitar, e os do Sketchfab variam. Fugir pede animação nova ou um blend improvisado. Além disso, a vista diverge da Sim (bicho já removido continua andando na tela), o que pede cuidado com seleção e contagem. E só fecha se as torres forem retemadas: canhão e fogo atirando em bicho 'que foge' não convence. Decidir no VIS-01 e implementar depois.

</details>

---

### MERC-02 — Estoque e recarga dos envios de elite

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 2 · risco 2 · prioridade 3
- **Depende de:** MERC-01

**O que é:**

Cada envio de elite tem estoque 1 e recarga própria por jogador, de 25 a 90 s. O CanAfford checa, e a IA filtra sozinha.

**Valor para o jogador:**

A elite vira momento, não spam.

**Como verificar:**

Uma segunda compra dentro da recarga é recusada.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 2, risco 2) É barato e necessário assim que a elite existir, porque sem recarga a elite vira spam e quebra o balanceamento. Só faz sentido junto com MERC-01 e com uma UI que mostre a recarga (anel radial no cartão do bicho). Em OnGUI ficaria ilegível, então depende da loja nova.
- (depois; valor 3, custo 2, risco 2) É barato e determinístico: um array de recarga por jogador, checado no CanAfford e filtrado na IA. Deve sair junto com o MERC-01, porque sem ele não existe. Tem custo de UI: a contagem da recarga precisa aparecer no botão da loja nova. Recarga mais pouca renda por ouro são duas travas no mesmo eixo: medir se as duas são necessárias.

</details>

---

### META-03 — Página da Steam, demo e Next Fest

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 3,5 · risco 3 · prioridade 3
- **Depende de:** VIS-01, META-02, META-12, UX-23

**O que é:**

- Página 'Em breve' só depois do salto visual.
- Cápsulas e ícone feitos a partir de render do jogo; 5+ screenshots e trailer.
- Questionário de conteúdo com divulgação de IA.
- Frase de venda pela tese.
- Demo para um Next Fest.
- Preço premium ~US$ 10-15 com preço regional, sem microtransação.
- Nome que se encontre em inglês.
Pagamento e postagens são ações do Felipe.

**Valor para o jogador:**

É assim que os jogadores chegam.

**Como verificar:**

Página aprovada pela Valve, e wishlists acompanhadas no painel.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 5, custo 3, risco 3) É assim que o jogo chega a alguém, mas uma página 'Em breve' com o visual de hoje (chão ocre, árvores facetadas, OnGUI) queimaria a primeira impressão e as wishlists. A regra 'só depois do salto visual' está certa. Duas coisas baratas podem andar antes: decidir um nome encontrável em inglês ('TDFende' não é) e registrar o uso de IA (modelos do Meshy) para a divulgação exigida pela Valve. Taxa e postagens são ações do Felipe.
- (depois; valor 5, custo 4, risco 3) É o caminho até o jogador, mas o momento importa: abrir a página com a arte de hoje (chão ocre, árvores de cones, OnGUI) queima a primeira impressão e as wishlists. Cápsula e trailer feitos a partir do render só funcionam depois do VIS-01. Um trailer bom é caro e chamar de M é otimista. O preço de US$ 10-15 é otimista para o elenco atual. Taxa do Steam Direct, divulgação de IA e postagens são ações do Felipe. Next Fest exige demo polida e página no ar com semanas de antecedência.

</details>

---

### UX-27 — Oráculo de envio

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** GG (mais de 5 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 5 · risco 3,5 · prioridade 1
- **Depende de:** TEC-16, META-10

**O que é:**

Só no Fácil, no Treino e em replay: parar o mouse num cartão faz um fork da partida e simula 40 s, mostrando onde o bicho morreria ou se vaza.

**Valor para o jogador:**

Experimentar contras sem gastar ouro.

**Como verificar:**

O fork + envio dá o mesmo fingerprint de uma partida real, com fork em menos de 150 ms.

<details><summary>Parecer dos avaliadores</summary>

- (nao; valor 2, custo 5, risco 4) Só parece legal no papel. Custa GG (fork da simulação em menos de 150 ms a cada hover, clonagem de estado) e serve apenas ao Fácil, ao Treino e ao replay. Ainda tira a aposta e a leitura, que são a graça de escolher o envio. A ficha com 'bom contra' (UX-16) e o treinador ensinam o mesmo por uma fração do custo.
- (nao; valor 2, custo 5, risco 3) A Sim não tem fork nem clone: LaneSim e MatchSim não copiam estado, e o System.Random não se clona. Seria preciso um snapshot profundo ou reexecutar o replay até o tique, o que fica mais lento quanto mais longa a partida, contra uma meta de 150 ms. Serve só no Fácil, no Treino e em replay. A ficha com 'bom contra' cobre a maior parte do benefício por uma fração do custo. Reabrir só se o multiplayer exigir snapshot ou rollback, porque aí o fork vem quase de graça.

</details>

---

### TORRE-10 — Barricada

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 2,5 · risco 4 · prioridade 1,5

**O que é:**

~8 de ouro: bloqueia a célula, sem tiro e sem fronteira. Alonga o labirinto e serve de isca para derrubadores. Teto por lane ou custo crescente.

**Valor para o jogador:**

Separa desenhar o labirinto de causar dano.

**Como verificar:**

Aumenta o custo do caminho sem criar território, e o 'match 60' é medido.

<details><summary>Parecer dos avaliadores</summary>

- (nao; valor 2, custo 2, risco 4) Uma parede de 8 de ouro transforma o Line Tower Wars em puro concurso de labirinto. É o meta degenerado conhecido do gênero, e o Canhão a 25 já serve para fazer labirinto. Também dilui a tese, porque bloqueia sem território. O valor de isca só existe com bichos que atacam torres. Se o segundo mercado tiver derrubadores e o labirinto ficar barato demais, reavalie com um teto por lane. Hoje não.
- (talvez; valor 2, custo 3, risco 4) Na Sim é quase uma torre com dano e fronteira zero. Mas parede a 8 de ouro num Line Tower Wars é o caminho clássico para o labirinto degenerado e partidas de 60+ min, como a própria verificação admite. A IA não sabe montar labirinto, e a fronteira, que é a tese do jogo, fica de fora. 'Isca para derrubadores' só faz sentido se existir inimigo que ataca torre. Reavaliar só se o segundo mercado trouxer derrubadores.

</details>

---

### BICHO-03 — Escavador

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BICHO · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 4,5 · risco 4 · prioridade 1,5
- **Depende de:** BICHO-01, TORRE-09

**O que é:**

Tatu ou texugo-do-mel.
Variante A: alterna enterrado (sem alvo e sem atrito) e superfície em ciclo.
Variante B: vai em linha reta por baixo até uma saída fixa perto da fortaleza.
Na superfície, um rastro de terra visível. Contras: armadilha, área e vigia.

**Valor para o jogador:**

Uma segunda zona de defesa a planejar.

**Como verificar:**

DamageDealt = 0 enquanto enterrado, e a armadilha o pega.

<details><summary>Parecer dos avaliadores</summary>

- (nao; valor 2, custo 4, risco 4) Parece legal no papel e é ruim na tela. Num jogo que aposta no visual realista, o bicho passa a maior parte do tempo invisível. Num jogo de envio PvP, é um contra-duro: se você não construiu a armadilha (TORRE-09, que ainda nem existe), não tem resposta, e isso frustra. Animação realista de cavar e emergir é quase impossível de achar pronta, e o Meshy só gera 'Andando'. É esforço G com duas dependências. Melhor gastar em BICHO-05, 06 e 10.
- (talvez; valor 3, custo 5, risco 4) O design ainda não foi decidido (variante A ou B). Depende de uma torre nova (TORRE-09) e do BICHO-01, e precisa de animação de cavar que nenhum modelo nem o Meshy oferece, além de rastro de terra como efeito. Uma fase sem alvo e sem atrito é a terceira mecânica que escapa da tese, frustra e é ruim de ler em tela pequena. É esforço grande para um nicho.

</details>

---

### MERC-12 — Leilão às cegas de lendários

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 3 · risco 3,5 · prioridade 1
- **Depende de:** MERC-01

**O que é:**

A cada 2:00, um lendário vai a leilão fechado de segundo preço. O vencedor envia o bicho ou o adota como mascote, com bônus passivo. No empate vence quem tem menos renda.

**Valor para o jogador:**

Blefe e leitura da economia rival, coisa rara em TD.

**Como verificar:**

O maior lance vence e paga o segundo + 1, e quem vence o leilão ganha a partida em no máximo 70%.

<details><summary>Parecer dos avaliadores</summary>

- (nao; valor 2, custo 3, risco 4) O blefe e a leitura da economia rival não existem contra a IA, porque lance fechado vira sorteio. Mesmo no PvP, um leilão a cada 2 min, mais o mascote com bônus passivo, empilha UI e sistema numa partida de 7-10 min que já é corrida. É uma ideia ótima no papel e cara de deixar legível.
- (nao; valor 2, custo 3, risco 3) Leilão às cegas é blefe entre humanos. Contra uma IA que lê o estado não há leitura de economia nenhuma. Exige UI de lance (mais uma tela num projeto sem UI pronta), sistema de mascote com bônus passivo e um lendário com modelo próprio. A meta '≤70% de vitória para quem vence' é frouxa e admite bola de neve. Fora do cronograma enquanto não houver multiplayer.

</details>

---

### DES-15 — Juros sobre ouro guardado

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 1 · risco 2 · prioridade 1,5

**O que é:**

A cada pingo, o ouro guardado rende 2%, com teto de 20.

**Valor para o jogador:**

A tensão 'gasto agora ou junto?'.

**Como verificar:**

500 de ouro rende +10 e 2.000 rende +20, com as metas verdes.

<details><summary>Parecer dos avaliadores</summary>

- (nao; valor 2, custo 1, risco 2) O jogo já tem uma mecânica de investimento, que é o próprio coração da economia: envio vira renda permanente. Juros criam uma segunda forma de investir, que compete com a primeira e premia ficar parado, justo num jogo contra IA que precisa de ação. Funciona em autobattler porque lá as rodadas são discretas, mas aqui só dilui a decisão central.
- (talvez; valor 2, custo 1, risco 2) Custa quase nada no código, mas acrescenta um terceiro eixo econômico que puxa contra o motor do jogo (envio = renda). O teto de 20 é o dobro da renda-base de 10, o que é significativo no começo. A IA precisa de um termo novo, e o efeito ainda tem de aparecer no HUD. Testar no BalanceLab só se o DES-05 não criar tensão suficiente.

</details>

---

### DES-16 — Empréstimo

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 1,5 · risco 2,5 · prioridade 1

**O que é:**

Uma vez por partida: 150 de ouro na hora, pagos com 20% da renda por 2 min, à vista do adversário.

**Valor para o jogador:**

A jogada arriscada passa a existir.

**Como verificar:**

Desconto de 20% por 12 tiques.

<details><summary>Parecer dos avaliadores</summary>

- (nao; valor 2, custo 1, risco 2) É uma ideia que só parece boa no papel: um botão usado uma vez por partida, contra a IA, que pouca gente notaria. 'Gastar tudo em envios' já é a jogada arriscada que existe. Se o empréstimo fizer falta, ele pode virar uma das cartas do DES-10 em vez de ser um sistema próprio.
- (nao; valor 2, custo 2, risco 3) Como está especificado, é dinheiro grátis. A devolução é 20% da renda por 12 tiques. Com renda de 10 a 30 no começo, isso paga de volta 24 a 72 por 150 de ouro, então pegar cedo vira jogada obrigatória e não aposta. Ainda pede comando novo, parse no replay, decisão na IA e HUD. Se for repensado com dívida fixa maior que o valor, dá para reavaliar, mas não acrescenta nada à tese da fronteira.

</details>

---

### META-11 — Melhores momentos automáticos

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** GG (mais de 5 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 5 · risco 3,5 · prioridade 1,5
- **Depende de:** META-10, TEC-17, TEC-15

**O que é:**

No fim, uma re-simulação headless detecta 3 lances: maior multiabate, vazamento decisivo, 'quase' e cadeia de atrito. Eles são tocados com câmera cinematográfica e câmera lenta só de vista.

**Valor para o jogador:**

O momento 'olha isso!' que vira clipe.

**Como verificar:**

Um replay fixo acha o vazamento decisivo no tique exato.

<details><summary>Parecer dos avaliadores</summary>

- (nao; valor 2, custo 5, risco 4) É o caso típico de ideia que só parece legal no papel. Custa muito (GG), depende de três sistemas, e a câmera cinematográfica automática costuma sair genérica. O momento 'olha isso!' que vira clipe nasce de jogo bonito e de lance real, e o jogador grava esse clipe com o OBS ou com a gravação da Steam. Não compensa.
- (talvez; valor 2, custo 5, risco 3) Achar o lance em re-simulação headless é barato, porque os eventos já saem do LaneSim. O caro é a câmera cinematográfica com câmera lenta só de vista, que exige separar o tempo de render do tempo da Sim nas views, Legacy Animation dos bichos inclusive. Depende do META-10 inteiro. Enquanto o jogo não estiver bonito, o clipe não vende nada. É polimento de jogo já lançado.

</details>

---

### META-18 — Editor de regras e mapas com Oficina

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** GG (mais de 5 sessões)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 5 · risco 4 · prioridade 1
- **Depende de:** TEC-15, DES-13, META-04, META-10

**O que é:**

Editor de regras e de mapa no grid 24×16, com validação automática por 20 partidas IA × IA. Publicação na Oficina da Steam, com o hash da variante no replay.

**Valor para o jogador:**

Vida longa pela comunidade, como no berço do gênero.

**Como verificar:**

Mapa sem caminho é recusado, e uma variante publicada reproduz em outra conta.

<details><summary>Parecer dos avaliadores</summary>

- (nao; valor 2, custo 5, risco 4) Oficina sem comunidade fica vazia, e o custo é GG. No Tower Wars, o labirinto já é construído pelo jogador dentro da lane, então um editor de mapa muda pouco a experiência. Seria uma aposta pós-lançamento, só se o jogo pegar. Hoje não.
- (nao; valor 3, custo 5, risco 4) Fica fora do escopo deste cronograma. Mapa customizado exige generalizar a Sim e as views, hoje montadas para lanes fixas, além de validação de caminho, UI de editor completa e Steam UGC, que ainda não existe. Também depende de 4 itens não feitos. O que já existe (catálogos em JSON via CatalogJson) cobre um 'editor de regras' para desenvolvedor sem custo extra. Oficina é coisa de pós-lançamento, com comunidade formada. Rever só depois que o jogo vender.

</details>

---
