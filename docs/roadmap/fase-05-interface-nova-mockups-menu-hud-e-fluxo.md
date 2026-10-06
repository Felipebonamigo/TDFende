# Fase 5 — Interface nova: mockups, menu, HUD e fluxo

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 6-8 sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Sai o OnGUI, primeiro aprovado em mockup HTML no celular. Entram, em UI Toolkit e sobre o mundo já bonito: menu, HUD das duas lanes, fluxo menu → partida → menu e fim de partida.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Exe abre no menu em UI Toolkit sobre o mundo novo.
- Partida com HUD novo, Tab e fim de partida.
- 5 ciclos sem vazamento; nenhum OnGUI fora do overlay de desenvolvedor.
- Prints em 16:9, 16:10 e 21:9 aprovados.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Aprovar os mockups e os tokens.
- [ ] Nome e logo provisórios.
- [ ] Idiomas do EA (sugestão: PT-BR, EN e ZH-Hans).

## Tarefas, em ordem

- [ ] UX-29 — Mockups em HTML/CSS antes do UI Toolkit
- [ ] UX-01 — Fundação de UI em UI Toolkit
- [ ] UX-02 — Sistema visual da interface
- [ ] META-14 — Textos por chave
- [ ] UX-04 — Fluxo menu → partida → menu
- [ ] UX-06 — HUD das duas lanes
- [ ] DES-01 — Morte súbita anunciada
- [ ] UX-10 — Troca suave de lane
- [ ] UX-19 — Fim de partida (essencial)
- [ ] META-02 — Tela de créditos

---

### UX-29 — Mockups em HTML/CSS antes do UI Toolkit

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

USS é quase CSS: iterar em HTML sai dez vezes mais barato, e o Felipe aprova no celular.

**Pronto quando:**

Artifact com menu, HUD, loja, mercado (abas 1 e 2) e painel da torre, usando os tokens da bíblia e com os prints do mundo novo de fundo. Aprovado pelo Felipe.

---

### UX-01 — Fundação de UI em UI Toolkit

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4,5 · custo 3 · risco 2,5 · prioridade 5
- **Depende de:** TEC-01

**Por que, e nesta fase:**

É a base de tudo que é bonito, e acaba com o clique vazando para o mundo.

**O que é:**

UXML/USS em texto, escala por PanelSettings, transições e Painter2D. O OnGUI fica só no overlay de desenvolvedor. PanelSettings e tema são gerados por script de Editor. O PointerOverUi usa panel.Pick. Um Runtime/UiModel puro é testado no FlowSim. Primeiro, migrar o HUD atual 1:1.

**Valor para o jogador:**

É a base de menu, loja e HUD bonitos em qualquer resolução, e acaba o clique vazando para o mundo.

**Pronto quando:**

- PanelSettings, .tss e FontAsset gerados por script e incluídos no build.
- UIElementsModule no CompileCheck e PointerOverUi via panel.Pick.
- UiModel puro testado; HUD migrado 1:1 numa sessão.
- Prints em 1280×800, 1080p, 2560×1080 e 4K, sem texto sumido.

**Como verificar:**

CompileCheck verde, prints em 720p, 1080p e 4K sem quadrado nem rosa, e FPS igual.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 5, custo 3, risco 2) É o alicerce de tudo que o Felipe pediu com nome: menu, loja, seleção de torre e envio de bicho. Sem isso, cada tela bonita vira retrabalho em OnGUI de pixel fixo, que não escala para 4K nem para mobile. O módulo de UI Toolkit já está ligado. O risco fica no PanelSettings e no tema gerados por script e na fonte, porque não existe nenhuma no projeto e a fonte padrão denuncia protótipo: escolher uma fonte livre (OFL) já nesta etapa. A migração 1:1 do HUD primeiro é a ordem certa. Só depende do TEC-01, o pre-commit verde.
- (agora; valor 4, custo 3, risco 3) É a base do menu e da loja que o Felipe pediu, e acaba com o clique vazando para o mundo. Também não depende do tema nem da arte, então dá para fazer em paralelo à decisão de direção. Riscos reais: no runtime, o UI Toolkit exige PanelSettings e um ThemeStyleSheet incluídos no build (sem o tema o texto some); o CompileCheck precisa da referência à UIElementsModule; e falta fonte (OFL do Google Fonts). A migração 1:1 sozinha não melhora nada que se veja. Ela tem que ser curta, com menu e loja vindo logo em cima. Vem depois de TEC-01 e de destravar o pre-commit.

</details>

---

### UX-02 — Sistema visual da interface

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 2 · prioridade 4
- **Depende de:** UX-01, VIS-01

**Por que, e nesta fase:**

Aparece em todo print da Steam.

**O que é:**

Tokens num tokens.uss, painéis de vidro escuro, fontes OFL (Barlow Condensed + Inter), ícones de linha, times em ciano × laranja (seguro para daltônico), textura sutil do tema e micro-animações. Trocar de tema = trocar os tokens.

**Valor para o jogador:**

Produto acabado, coerente com o mundo realista.

**Pronto quando:**

- tokens.uss traduzido dos mockups, com times seguros para daltônico.
- Token de alvo mínimo de toque e safe area.
- Fonte OFL com fallback CJK (Noto).
- Ícones no manifesto, contraste AA conferido por script.

**Como verificar:**

Script confere contraste AA, e prints da loja e do menu aprovados.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 2) A UI aparece em todo print da Steam, e hoje é OnGUI cru. Ter tokens desde o início evita que cada tela saia com uma cara diferente, e o contraste AA com ciano × laranja é acerto barato. O valor só aparece nas telas que usam o sistema, então ele vai junto com o UX-01 e a primeira tela, não sozinho. Cuidado com o 'vidro escuro': é o visual genérico de sci-fi e pode brigar com um mundo realista de natureza ou medieval. Fechar os tokens só depois do VIS-01 (tema). Ícones e fontes precisam de licença conferida, como os modelos.
- (logo; valor 4, custo 3, risco 2) Tem que nascer junto com o UX-01; refazer o estilo depois sai mais caro que fazer tokens agora. As fontes OFL (Barlow, Inter) são seguras para a Steam. Ressalvas: o UI Toolkit não tem desfoque de fundo, então o 'vidro' vira painel translúcido escuro. Fonte no UI Toolkit precisa de FontAsset gerado por script de Editor. Ícone em SVG pode exigir pacote ou rasterização, então usar PNG ou fonte de ícones (Lucide é ISC). Depende do VIS-01 só nas cores e na textura; os tokens deixam trocar de tema sem retrabalho, o que combina com o tema ainda indefinido.

</details>

---

### META-14 — Textos por chave

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2,5 · risco 1,5 · prioridade 4
- **Depende de:** UX-01

**Por que, e nesta fase:**

Nascer com chaves é barato; adaptar depois é caro.

**O que é:**

Textos numa tabela CSV de chave → idioma (PT-BR e EN primeiro). O Name do catálogo nunca é traduzido. Pseudo-idioma com +40% de largura e fontes Noto OFL recortadas.

**Valor para o jogador:**

Inglês e outros idiomas multiplicam o público.

**Pronto quando:**

CSV de PT-BR com coluna EN e nenhuma string literal na UI nova. O teste de chaves completas, o pseudo-idioma e a tradução EN entram antes do Portão 2 (Fase 10).

**Como verificar:**

Teste de chaves completas no pre-commit, e prints em pseudo-idioma sem corte.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 1) Sem inglês, o público na Steam fica minúsculo. O momento certo é junto com a UI nova (UX-01): nascer com uma tabela de chaves sai barato, e adaptar depois todas as strings fixas sai caro. O pseudo-idioma com +40% pega corte de texto cedo. Começar só com PT-BR e EN, e deixar mais idiomas para quando o jogo estiver perto de lançar.
- (logo; valor 4, custo 3, risco 2) O inglês é praticamente obrigatório para vender na Steam. Montar a tabela de chaves junto com a UI nova (UX-01) é barato. Retrofitar depois, com strings espalhadas por OnGUI e UXML, é caro e cheio de lacuna. Escopo realista agora: CSV com PT-BR e EN, teste de chave completa no pre-commit e pseudo-idioma para checar largura. Fontes Noto recortadas e outros idiomas ficam para depois, já que uma fonte OFL latina cobre PT e EN. O custo extra de verdade é a disciplina de não escrever texto literal na UI.

</details>

---

### UX-04 — Fluxo menu → partida → menu

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3,5 · risco 3 · prioridade 4
- **Depende de:** UX-03

**Por que, e nesta fase:**

Várias partidas sem fechar o jogo, e menu sobre o mundo 3D.

**O que é:**

Uma máquina de estados GameFlow (Boot → Menu → Partida → Pausa → Fim → Menu) sem recarregar a cena e mantendo o mundo montado. Desmontagem limpa da partida e dos estáticos. Revanche com semente nova ou a mesma.

**Valor para o jogador:**

Várias partidas seguidas sem fechar o jogo.

**Pronto quando:**

- GameFlow medido antes de escolher entre recarregar a cena e manter o mundo.
- Menu com câmera orbitando.
- TD clássico fora do menu.
- 5 ciclos sem erro e com memória estável.

**Como verificar:**

Ciclo menu → partida → menu 5 vezes sem erro no Player.log e com a memória estável.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 3) Voltar ao menu e pedir revanche é o mínimo de um produto, e hoje só existe o R. Manter o mundo montado ainda deixa o menu bonito de graça: a interface sobre o cenário 3D vivo em vez do Default-Skybox. O risco está na desmontagem dos estáticos (Juice, caches do ModelLib/ArtFactory, Time.timeScale global do GameController), que vaza em silêncio. Medir a memória em 5 ciclos é a verificação certa. Fazer junto com o menu novo, não antes.
- (logo; valor 4, custo 4, risco 3) Sem ele, 'menu bonito' e 'voltar ao menu' não existem, então é pré-requisito do que o Felipe pediu. O esforço M é otimista. Hoje o ModeSelect cria o controlador de dentro do OnGUI, e há estáticos e caches (ModelLib, Vfx, SimplePool, AnimalLoader, Juice, Time.timeScale global) que precisam ser zerados sem vazar. O degrau mais baixo da escada é recarregar a cena, já que tudo é montado em código. Manter o mundo montado só se justifica se reconstruir Terrain e grama for lento; vale medir antes de escolher. O teste de 5 ciclos com memória estável é o certo.

</details>

---

### UX-06 — HUD das duas lanes

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 2 · prioridade 4
- **Depende de:** UX-01

**Por que, e nesta fase:**

Num olhar: quem ganha e quando cai o próximo ouro.

**O que é:**

Barra simétrica:
- vidas em segmentos;
- ouro com contador animado;
- renda com anel até o próximo pagamento (TimeToIncome);
- relógio, escala dos envios e contagem da morte súbita;
- do lado da IA, torres e últimos envios.
A ajuda vira F1.

**Valor para o jogador:**

Num olhar: quem está ganhando e quando cai o próximo ouro.

**Pronto quando:**

Vidas em segmentos com '-N' do porte, ouro animado, anel de renda, relógio e escala. Lado da IA enxuto, ajuda no F1. Atualiza só quando muda.

**Como verificar:**

Testes de UiModel (mm:ss, fração do anel), prints em 2 resoluções e alocação estável.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 2) Responde num olhar a quem está ganhando e quando cai o ouro. O anel até a próxima renda é o ritmo central de um Line TW e vale mais que qualquer número. É o elemento mais visível em gameplay e trailer. Manter enxuto: o lado da IA com torres e últimos envios pode virar poluição, então começar com vidas, ouro, renda e relógio de cada lado. Ajuda no F1 está certo.
- (logo; valor 4, custo 3, risco 2) Os dados já existem (TimeToIncome, Lives, Gold, Income, SendScale, os dois LaneSim), então o custo é só de interface, em cima do UX-01. Contador animado e anel são polimento barato depois que a base existe. O risco é alocação por quadro na UI Toolkit (strings a cada frame): atualizar só quando o valor muda. Tirar o texto de ajuda da tela para o F1 é ganho imediato.

</details>

---

### DES-01 — Morte súbita anunciada

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 2 · risco 1 · prioridade 4

**Por que, e nesta fase:**

Aos 7:00 o Gelo para de congelar sem aviso, e parece bug.

**O que é:**

A partir de 5:00, um contador. Aos 7:00, um banner com o texto exato do que muda (o Gelo não congela, o vento não empurra), lido das constantes. A escala fica sempre à vista e a vida atual aparece nos cartões. Gelo e Ar ganham ícone de efeito desligado.

**Valor para o jogador:**

A virada deixa de parecer bug e vira clímax.

**Pronto quando:**

Contador desde 5:00 e banner com o texto lido das constantes. Ícones de efeito desligado. Print com MatchTime > 7 min.

**Como verificar:**

Função de contagem testada, e print forçando MatchTime > 7 min.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 1) É barato e transforma uma regra invisível num clímax que dá para ler. Hoje o Gelo para de congelar aos 7:00 sem avisar, e o jogador acha que é bug. Deve entrar junto com o HUD novo em UI Toolkit, não no OnGUI provisório. O texto do banner vem das constantes, então não desatualiza.
- (logo; valor 3, custo 2, risco 1) É barato e a Sim já expõe InSuddenDeath e a escala. Contador, banner e ícone de efeito desligado acabam com o 'Gelo parou de funcionar = bug'. Não é 'agora' porque o banner e os cartões devem nascer no HUD novo (UI Toolkit). Feito em OnGUI, seria reescrito. A função de contagem e o texto lido das constantes podem ser testados no FlowSim já.

</details>

---

### UX-10 — Troca suave de lane

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3 · custo 1 · risco 1 · prioridade 4

**Por que, e nesta fase:**

Olhar a lane da IA e voltar com uma tecla.

**O que é:**

Tab alterna entre as lanes com transição de 0,4 s, Home volta à fortaleza e duplo clique centraliza.

**Valor para o jogador:**

Olhar o estrago na IA sem perder a própria defesa.

**Pronto quando:**

Tab em 0,4 s com tempo sem escala; Home volta à fortaleza.

**Como verificar:**

Playtest da pergunta 3 do TESTE.md.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 1, risco 1) É barato e responde à pergunta 3 do TESTE.md (acompanhar as duas lanes). Olhar o estrago na IA e voltar com uma tecla é exatamente o que um jogador de Line TW faz o tempo todo. Nada a desconfiar aqui. Só confirmar que o Tab não briga com outro atalho e que a transição usa tempo sem escala, para funcionar na pausa.
- (logo; valor 3, custo 1, risco 1) É barato e isolado: o CameraRigDriver tem 83 linhas e as duas lanes têm posição fixa. Resolve quase tudo que o UX-07 e o UX-11 querem resolver, por uma fração do custo. Cuidado com o duplo clique para centralizar, que pode brigar com clicar para construir ou selecionar. Melhor um atalho só (Tab, Home) e deixar o duplo clique de fora se der conflito.

</details>

---

### UX-19 — Fim de partida (essencial)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 2 · prioridade 4
- **Depende de:** UX-01, UX-04, TEC-17

**Por que, e nesta fase:**

'R para jogar de novo' mata o 'só mais uma'.

**O que é:**

- Câmera lenta na fortaleza que cai.
- Estatísticas você × IA: torre × atrito, vazamentos, ouro, envio favorito.
- Linha do tempo com marcos: primeiro vazamento, maior envio, morte súbita, virada.
- Mapa de calor dos abates.
- Botões: Revanche, Mesma semente, Outra dificuldade, Salvar replay, Menu. Recorde local.

**Valor para o jogador:**

Entender por que perdeu e querer jogar de novo.

**Pronto quando:**

Câmera lenta na fortaleza, estatísticas você × IA e botões Revanche, Mesma semente, Outra dificuldade, Salvar replay e Menu. A nota do diário passa para cá. -captura -fim.

**Como verificar:**

Teste do detector de virada e da % de atrito, e print com '-captura-fim'.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 2) Hoje o fim é 'VITÓRIA' e 'R para jogar de novo', o que mata o 'só mais uma'. Revanche, Menu, as estatísticas você × IA (atrito, torre e vazamentos já existem no LaneSim) e a câmera lenta na fortaleza dão a maior parte do valor com custo M. Mapa de calor e detector de virada parecem ótimos no papel e quase ninguém olha: deixe para uma segunda passada.
- (logo; valor 4, custo 3, risco 2) O núcleo é barato porque LaneSim já guarda TotalLeaked, KilledByTower, KilledByAttrition e GoldSpentOnTowers/Sends: estatísticas você × IA, Revanche e Menu cabem em pouco. Ainda é preciso registrar o envio favorito. 'M' para o pacote inteiro é otimista. Câmera lenta, detector de virada, linha do tempo, mapa de calor, recorde local e -captura-fim somam bem mais. Fazer em duas etapas: o essencial junto com a migração para UI Toolkit (UX-01), e o resto depois. O mapa de calor exige registrar a posição de cada abate na Sim.

</details>

---

### META-02 — Tela de créditos

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 1 · risco 1 · prioridade 4
- **Depende de:** TEC-22, UX-01

**Por que, e nesta fase:**

Obrigatória pela CC BY em qualquer build distribuído.

**O que é:**

Tela que lê o creditos.txt gerado pelo manifesto: os 7 CC BY do Sketchfab, Karrades, Poly Haven, Meshy e o aviso MIT do O3DE. O BuildJogo copia THIRD_PARTY e LICENSE-MIT para o lado do exe.

**Valor para o jogador:**

Obrigatória para publicar com os bichos CC BY.

**Pronto quando:**

Lê o creditos.txt; o BuildJogo copia THIRD_PARTY e LICENSE-MIT. Teste de autores CC BY completos.

**Como verificar:**

Teste confere que todo autor CC BY está no texto, e print da tela.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 2, custo 1, risco 1) Não diverte ninguém, mas é obrigatória: a CC BY exige atribuição em qualquer distribuição, inclusive em build de playtest. Copiar THIRD_PARTY e LICENSE-MIT para o lado do exe é trivial e devia entrar já no BuildJogo. A tela bonita vem junto com a UI nova. O teste que confere todo autor CC BY é barato e impede esquecer um bicho novo.
- (logo; valor 2, custo 1, risco 1) Para o jogador vale pouco, mas é obrigatória pela CC BY antes de qualquer distribuição, playtest com terceiros incluso. O texto pronto já está no THIRD_PARTY.md. Copiar o THIRD_PARTY e o LICENSE-MIT para o lado do exe no BuildJogo é uma linha e cobre a obrigação já. Gerar a tela a partir do manifesto evita que ela fique velha quando o tema ou os modelos mudarem. Falta conferir o plano Meshy do javali e do tigre (CC BY com crédito ao Meshy se foi no plano grátis). A tela em si espera o UX-01.

</details>

---
