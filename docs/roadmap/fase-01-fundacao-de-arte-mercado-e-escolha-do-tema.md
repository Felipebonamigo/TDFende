# Fase 1 — Fundação de arte, mercado e escolha do tema

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 2-3 (sem Unity, em paralelo à Fase 0) sessões
- **Créditos Meshy estimados:** 0 (só o plano de gasto) _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Antes de qualquer download ou crédito: licença, camada privada, auditoria e inventário real de arte por tema (bichos inclusive), elenco por bioma e posição de mercado. Sai daqui o tema favorito para a fatia de beleza.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Manifesto cobrindo 100% de Resources, com auditoria no build.
- Camada privada funcionando com e sem a pasta.
- AuditaAssets em aviso.
- Matriz de temas, bíblia com jogos-régua, rascunho do DES-23 e relatório de mercado no MANUAL.
- O Felipe escolhe o tema favorito para a fatia.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Tema e bioma, decididos pela matriz e confirmados na fatia de beleza (Fase 2).
- Natureza/expedição. Prós: arte mais farta (Poly Haven; o HDRI do jogo já é sul-africano), elenco natural, enquadramento não letal. Contras: troca as torres e pede cuidado com o tom de 'canhão contra bicho'.
- Fantasia realista 'baixa'. Prós: torres elementais coerentes, espaço para alfas e chefes. Contras: torres PBR de fantasia escassas, risco de cair no estilizado.
- Medieval realista com kit pago. Prós: kits modulares abundantes no Fab e na Asset Store. Contras: tigre, rinoceronte e elefante destoam; Gelo, Fogo e Ar puxam para fantasia.
- Fora: moderno militar (tom de caçada), cidade retomada (arquitetura sob medida, quase só pago) e Índia mogol (só referência).
- [ ] Elenco coerente com o bioma: no temperado, cervo, alce, bisão, raposa e corvo; na savana, gnu, búfalo, hiena, zebra e leão. Manter os 9 atuais ou migrar para uma fonte única de bichos.
- [ ] Verba para pacotes pagos (camada privada): sim ou não, e com que teto.
- [ ] Os 2 ou 3 jogos-régua da Steam.
- [ ] Conferir no site do Meshy o plano da conta em 03/10, quando foram gerados o javali e o tigre.
- [ ] EA sem multiplayer? (a partir do MKT-01)

## Tarefas, em ordem

- [x] TEC-22 — Manifesto de licenças com auditoria no commit e no build — 09/10/2026 (e1bc185)
- [ ] TEC-23 — Camada privada de arte e som (código em 2c9e38d; falta provar no exe)
- [x] TEC-10a — AuditaAssets em modo aviso (adiantado) — 09/10/2026
- [ ] VIS-01 — Inventário por tema e bíblia de arte (inventário pronto; falta decisão do Felipe)
- [x] DES-23 — Meta de elenco do Early Access (rascunho) — 09/10/2026
- [x] MKT-01 — Spike de mercado do gênero — 09/10/2026
- [x] TEC-24 — Plano de gasto dos créditos Meshy — 09/10/2026

---

### TEC-22 — Manifesto de licenças com auditoria no commit e no build

- [x] **Status:** feito em 09/10/2026, commit e1bc185. 41 entradas cobrem os 201 arquivos de Resources; ferramenta em `Tools/AuditaLicencas`; 3 entradas pendentes (plano do Meshy em 03-04/10) só avisam. O Felipe fecha o pendente conferindo o plano no site do Meshy.  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 2,5 · risco 1,5 · prioridade 4

**Por que, e nesta fase:**

A troca de tema vai trazer dezenas de assets. CC BY exige crédito em todo build, e o pre-commit só vê arquivo rastreado: Mixamo, extras em Bichos/ e a futura _Privado entram no exe sem aviso.

**O que é:**

Um manifesto legível por máquina para tudo em Resources: origem, autor, licença, cadeia de derivação e plano do Meshy na data. O pre-commit reprova arquivo sem entrada ou com licença proibida (NC, ND). Do manifesto saem o THIRD_PARTY.md e o creditos.txt.

**Valor para o jogador:**

Publicável na Steam sem risco de licença, e créditos nunca desatualizados.

**Pronto quando:**

- Manifesto cobre 100% de Resources: origem, autor, licença, derivação, plano Meshy e campo 'IA: própria/terceiro/não'.
- Pre-commit recusa entrada faltando e licença NC ou ND.
- BuildJogo audita o disco real e reprova arquivo desconhecido.
- Estado 'pendente' só avisa.
- Caminho Mixamo inventariado.
- THIRD_PARTY.md e creditos.txt gerados.

**Como verificar:**

Hoje reprova (Meshy com plano desconhecido, plastic_crate), e um arquivo sem entrada é recusado.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 2, risco 1) O jogador não vê, mas é condição para vender. CC BY exige créditos, e o Meshy com plano desconhecido e o plastic_crate são riscos reais de takedown na Steam. A tela de créditos é obrigatória de qualquer jeito e sai do mesmo manifesto. É barato. Precisa estar pronto antes da página da Steam ou de qualquer build público, não antes de consertar o bicho invisível.
- (logo; valor 4, custo 3, risco 2) A Steam e a CC BY exigem créditos corretos, e a troca de direção de arte vai trazer dezenas de assets novos. Criar o manifesto antes evita uma dívida que depois ninguém reconstrói. O custo não é M otimista: preencher a origem e a cadeia de derivação de tudo o que está em Resources dá trabalho. Além disso, o plano do Meshy na data do javali e do tigre (23:06, talvez plano grátis) não se resolve com código, só o Felipe pode conferir. Se o gate entrar reprovando, como a própria verificação prevê, ele trava o commit de todas as sessões. Precisa de um estado 'pendente' que só avisa até o Felipe decidir. O AuditaAssets de orçamento do TEC-10 cabe neste mesmo hook.

</details>

---

### TEC-23 — Camada privada de arte e som

- [ ] **Status:** código feito em 09/10/2026 (commit 2c9e38d: `ArtLayerPaths`, `ArtLayers`, regras de importação `ArtRoots`, `.gitignore`, `Backup.ps1`, `-captura` com "com privado"/"sem privado"; 15 testes no FlowSim, Runtime compila). **Falta, na sessão do PC:** gerar o exe com um FBX de bicho em `Assets/_Privado/Resources/TDFende/Privado/Bichos/` e sem ele, e ver no log `camada privada` e `-captura` passando nos dois (MANUAL, seção 6). O código de editor (`ArtRoots` e as regras) não compila aqui sem o UnityEditor: o `CompileCheckUrp` do PC é quem confere.  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2,5 · risco 3 · prioridade 4
- **Depende de:** TEC-22

**Por que, e nesta fase:**

A verba de pacote pago é decidida aqui, e o inventário pode depender de pacote. Sem a camada, o primeiro pacote comprado vai para o Git público ou fica sem caminho de carga.

**O que é:**

Uma pasta Assets/_Privado/Resources/TDFende/Privado fora do Git. ArtFactory, AnimalLoader e áudio procuram primeiro em Privado, depois no público, depois no procedural. Pacotes pagos ou sem permissão de redistribuição só entram no build do Felipe.

**Valor para o jogador:**

Arte e som de nível comercial no executável, sem quebrar o repositório público.

**Pronto quando:**

- Assets/_Privado fora do Git e coberta pelo TEC-26.
- ArtFactory, AnimalLoader e áudio procuram em Privado → público → procedural.
- Builds com e sem a pasta rodam, o log diz a camada, e o -captura registra 'com privado'.

**Como verificar:**

Builds com e sem a pasta rodam, e o log diz qual camada carregou.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 3) É o que destrava o 'realista de última geração' de verdade. Arte realista coerente (animais com rig e animações, cenário, VFX, áudio) só com CC0/CC BY é colcha de retalhos, e pacotes da Asset Store/Fab resolvem por dezenas de dólares. O encanamento é pequeno porque o ArtFactory já resolve por nome em Resources. Riscos: o build de outras sessões e do pre-commit fica diferente do build do Felipe, e Resources embute tudo no build. Fazer logo depois da decisão de tema, porque ela depende de saber quais pacotes existem.
- (logo; valor 4, custo 3, risco 3) Com a direção 'realista de última geração', a arte boa de verdade (bichos realistas animados, vegetação no nível Megascans, bibliotecas de som) quase sempre vem de pacote pago ou sem permissão de redistribuição. Pela regra do próprio Felipe, esse material fica fora do GitHub. Isso faz desta camada um habilitador direto da meta visual, e a busca em camadas do ArtFactory e o .gitignore de Bichos e Personagens já existem. Riscos reais: o executável e o -captura passam a depender da máquina, e sessões em clones (TEC-20) não enxergam a pasta, a não ser com junction. Pacotes da Asset Store costumam vir em Built-in ou HDRP e pedem conversão para URP no editor. Tudo em Resources incha o build. Cada compra exige autorização do Felipe. Vem logo depois da decisão de tema e do TEC-22.

</details>

---

### TEC-10a — AuditaAssets em modo aviso (adiantado)

- [x] **Status:** feito em 09/10/2026 (`Tools/AuditaAssets`, MANUAL seção 4). Mede o FBX sem o Unity e bate com o `-captura` (fortaleza 92,6 k, rato 25,5 k, cachorro 17,6 k, rinoceronte 16 k). Hoje 21 de 30 modelos acima do teto, o pior a Torre_Gelo_3 (94,7 k). Texturas: avisa de >2048 px, sem compressão e dos `.bytes` (48 hoje). O teto de textura é provisório.  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

O princípio diz que toda arte nova nasce com orçamento. Por isso a auditoria precisa existir antes da fatia de beleza, e não na antiga Fase 7.

**Pronto quando:**

O pre-commit e o BuildJogo avisam malha ou textura fora do orçamento do TEC-06, e textura nova não importada como BC7/BC5. Hoje aponta a Torre_Gelo_3 (95k) e a Fortaleza (92,6k).

---

### VIS-01 — Inventário por tema e bíblia de arte

- [ ] **Status:** inventário e análise prontos em 09/10/2026 ([`docs/arte/tema.md`](../arte/tema.md), três relatórios e dados brutos). **Falta o Felipe:** escolher o tema (recomendação: fantasia realista baixa), 2 ou 3 jogos-régua e a verba. Depois disso, registrar a decisão no MANUAL e no README e marcar [x].  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 2,5 · risco 2,5 · prioridade 5
- **Depende de:** BUG-01

**Por que, e nesta fase:**

O maior problema visual é a colagem, nas torres e também nos bichos (7 autores do Sketchfab e 2 do Meshy, de biomas misturados). Greybox não mede realismo.

**O que é:**

Decidir uma vez, antes de baixar ou gerar mais arte. Candidatos:
- fantasia realista 'baixa' num planalto: reaproveita as torres;
- natureza/expedição ou savana: casa com os 9 bichos, e o céu atual já é sul-africano;
- cidade retomada pelo mato: explica o elenco, e o asfalto plano casa com a grade;
- fronteira selvagem moderna;
- medieval;
- Índia mogol, só como referência.

Método: um MoodPreset por '-clima' fotografa o mesmo tique em cada tema, e subagentes medem a cobertura do kit mínimo de arte e o risco de licença. Sai uma bíblia de arte (paleta, hora, materiais, lista do proibido) e uma frase de venda.

**Valor para o jogador:**

Mundo coerente no lugar da colagem atual, atendendo o pedido de realismo sem prender ao medieval.

**Pronto quando:**

Subagentes inventariam por tema, com licença, autor, triângulos e animações:
- torres, fortaleza, acampamento, props e vegetação;
- bichos, com fidelidade e clipes (parado, andar, correr, golpe, morte, deitar);
- pacotes pagos de animais completos como opção.

Matriz pontuada pelos critérios do Felipe: beleza alcançável, coerência, fonte única. Reuso só desempata. Saem a bíblia de arte (paleta, bioma, hora, materiais, lista do proibido, frase de venda) e 2-3 jogos-régua que o Felipe escolhe.

**Como verificar:**

Folhas de contato por tema enviadas ao Felipe, e a decisão registrada no MANUAL e no README.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 5, custo 3, risco 3) É a decisão que mais rende: o maior problema visual hoje é a colagem (torre de um lugar, bicho de outro, tenda de brinquedo), não a falta de polígono. Tem que vir logo depois do BUG-01 e antes de qualquer download ou crédito gasto. Duas ressalvas. (1) Um MoodPreset por '-clima' só muda luz e cor, não os assets, então fotografar o mesmo tique 'em cada tema' engana. O que decide de verdade é um inventário de arte que dá para baixar de fato por tema (licença e qualidade), mais um greybox rápido dos 2 finalistas. (2) Os 9 bichos misturam continentes (lobo e urso do norte, tigre, rinoceronte e elefante da Ásia/África), então nenhum bioma real fecha sozinho. 'Natureza/expedição' ou 'fantasia realista baixa' cobrem isso melhor, e natureza realista é o tema com mais arte boa e gratuita (Poly Haven, Fab no PC). Cidade retomada e Índia mogol pedem arquitetura sob medida: cortar. O maior risco é virar a quarta troca de direção em dois meses. A decisão tem que sair como definitiva e registrada.
- (agora; valor 5, custo 2, risco 2) Define todo o gasto de arte que vem depois, e cada semana sem decidir alimenta a colagem atual (três trocas de direção em dois meses). O método proposto é otimista: um MoodPreset por '-clima' só troca luz e céu, não os modelos, então 'fotografar o mesmo tique em cada tema' não mostra tema nenhum. A versão barata e honesta: inventário de arte CC0/CC BY disponível por tema (subagentes buscando no Sketchfab, na Poly Haven e na galeria Meshy, com licença conferida), prancha de referências e uma decisão registrada. A bíblia de arte entra junto. Não depende de BUG-01 para decidir. Incluir aqui a decisão do enquadramento não letal (VIS-02), que muda que torres procurar.

</details>

---

### DES-23 — Meta de elenco do Early Access (rascunho)

- [x] **Status:** rascunho entregue em 09/10/2026 ([`docs/arte/elenco.md`](../arte/elenco.md)), condicionado ao tema e ao pacote de animais; recalibra na Fase 11.  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 3 · risco 3 · prioridade 3,5
- **Depende de:** TEC-14

**Por que, e nesta fase:**

O 'mais bichos, mais torres' do pedido vira lista aprovada desde o começo. Fixar o número antes do tema seria planilha otimista.

**O que é:**

Bichos de 9 para ~15-16 comuns, mais 8 de elite e 2 chefes. Torres de 6 para 10-11, com 2 ramos por torre. As métricas de elenco são recalibradas, por exemplo ≥ 6 tipos com 5% ou mais das compras.

**Valor para o jogador:**

Conteúdo suficiente para justificar a compra, sem planilha.

**Pronto quando:**

Lista com números e espécies, tirada do inventário. Proposta: 10-12 torres, 14-16 bichos comuns, 4 elites, 2 chefes e ramos nas 6 torres.

Regra: no máximo um papel especial por espécie.
- Rinoceronte: armadura. Urso: regeneração. Javali: ninhada.
- Elefante: só o chefe ancestral.
- Fúria que desliga torre: búfalo ou bisão.
- Imune a controle: outra espécie do bioma.
- Derrubador: espécie com clipe de ataque.

Recalibrada na Fase 11.

**Como verificar:**

Metas de elenco no BalanceLab verdes com o conteúdo novo.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 3) Fixar a meta cedo guia o resto: 15-16 bichos comuns, 8 de elite, 2 chefes e 10-11 torres com ramos são quantidade que justifica a compra. Só que a meta em si é barata e o que ela compromete não é. Cada bicho realista precisa de modelo animado com licença, e cada ramo de torre, de estágios visuais. Na direção realista isso domina o cronograma. Por isso o elenco deve sair da arte que existe de fato com qualidade e licença (o tema pode virar safári/natureza se isso facilitar), não de planilha. As métricas de diversidade no BalanceLab são o portão certo.
- (depois; valor 3, custo 4, risco 3) Escrever a meta é P, mas o que ela compromete é o maior custo de arte do projeto: de 9 para cerca de 26 bichos animados e realistas (+17), mais 4 a 5 torres novas com 3 estágios e 2 ramos cada. A comunidade de CC BY animado é limitada, e o quadrúpede do Meshy só tem 'Andando'. Chefe e elite que atacam pedem animação de ataque e de morte. O elenco também depende do tema, que está em aberto (safári e natureza muda a lista inteira). Fixar o número só depois de escolher o tema e passar pelo portão do playtest (META-01). Antes disso é planilha otimista.

</details>

---

### MKT-01 — Spike de mercado do gênero

- [x] **Status:** feito em 09/10/2026. Relatório em `docs/mercado.md` (19 jogos, com fontes), resumo no MANUAL, seção 12. Achado que muda o plano: já existe um "Line Tower Wars" grátis na Steam (Mithryl Labs, 30/07/2026).  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

O gênero vive de multiplayer. É preciso saber se um Line Tower Wars só contra a IA vende, a que preço e com que tags.

**Pronto quando:**

Relatório de subagente sobre Legion TD 2, Element TD 2 e outros Line TD da Steam: preço, tags, avaliações e público solo. Alimenta a frase de venda e a decisão 'EA sem multiplayer?'.

---

### TEC-24 — Plano de gasto dos créditos Meshy

- [x] **Status:** feito em 09/10/2026 (MANUAL, seção 14, e [`docs/meshy-livro-caixa.md`](../meshy-livro-caixa.md)). Saldo 842; 150 créditos entre 04 e 09/10 sem registro, a conferir no site.  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 1 · risco 1 · prioridade 4
- **Depende de:** VIS-01

**Por que, e nesta fase:**

842 créditos dão ~28 modelos. Trocar 18 estágios de torre pelo Meshy custaria ~540, o que é inviável, e o Meshy é a mesma fonte do problema de luz assada.

**O que é:**

842 créditos ≈ 28 modelos. Regras:
- Meshy só onde não há CC0/CC BY de qualidade.
- Piloto aprovado por print antes de lote.
- Livro-caixa de cada operação.
- Reserva para o segundo mercado.
- Preferir rig e remesh baratos sobre modelos da comunidade.
- Bípedes têm ataque e morte a ~3 créditos.
- Um pedido de aprovação ao Felipe por lote.

**Valor para o jogador:**

Mais modelos bons com o mesmo saldo e sem surpresa de licença.

**Pronto quando:**

Seção no MANUAL:
- Meshy só para peças e lacunas, não para torres inteiras;
- piloto aprovado por critério medido: normal map presente, albedo sem sombra assada e triângulos no orçamento;
- reservas explícitas para bichos e para o tier 2;
- um pedido por lote e livro-caixa que bate com o site.

**Como verificar:**

O livro-caixa bate com o saldo no Meshy, e cada lote tem aprovação no chat.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 2, custo 1, risco 1) O jogador não vê, mas evita queimar 842 créditos em arte que depois não combina com o tema. Por isso vem depois da VIS-01 e antes de qualquer geração. Já é quase regra do Felipe (perguntar antes). Basta uma seção curta no MANUAL e um livro-caixa simples, sem burocracia de subagente. Alegações como 'ataque e morte a ~3 créditos' e 'rig gratuito' precisam ser conferidas antes de entrar no plano. Reservar créditos para o segundo mercado é sensato.
- (logo; valor 2, custo 1, risco 1) É barato e evita surpresa. A regra central (perguntar antes de gastar) já está no CLAUDE.md, então o ganho real é o livro-caixa e a aprovação por lote. Só faz sentido no primeiro lote, depois do VIS-01: sem tema decidido, não há o que gastar. Cuidado com números não verificados ('rig pareceu gratuito', 'ataque/morte a ~3 créditos'). O piloto tem que confirmar o débito real no saldo antes de o plano assumir esses valores.

</details>

---
