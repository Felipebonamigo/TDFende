# Fase 7 — Torres, efeitos e fortaleza no tema

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 7-10 sessões
- **Créditos Meshy estimados:** 0-150. Só peças que faltarem no kit, com piloto aprovado por critério medido. As torres inteiras pelo Meshy (~540) estão descartadas. _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Torres, projéteis, explosões e fortaleza com material e peso de jogo atual, numa fonte coerente. Construir e evoluir viram recompensa visível nos 6 níveis.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Desfile, -captura vfx e sequência do nível 1 ao 6 aprovados.
- AuditaAssets e p95 verdes com 6 torres disparando.
- Livro-caixa batendo.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Kit das torres e cada lote de créditos, sempre com o número antes.
- [ ] Confirmar o enquadramento implementado.

## Tarefas, em ordem

- [ ] VIS-17 — Torres por kitbash de fonte única
- [ ] VIS-15 — Efeitos por torre com flipbook e luz
- [ ] VIS-18 — Cerimônia de construção e evolução, com marca por nível
- [ ] VIS-19 — Bichos com peso (conforme o enquadramento)
- [ ] VIS-14 — Marcas no chão
- [ ] VIS-21 — Fortaleza com estados de dano

---

### VIS-17 — Torres por kitbash de fonte única

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 5 · risco 4,5 · prioridade 2
- **Depende de:** VIS-01, TEC-09

**Por que, e nesta fase:**

As torres atuais têm luz assada e 18 de 21 não têm normal map. Dezoito downloads soltos repetem a colagem.

**O que é:**

No converte.py: tirar a luz assada da cor e gerar normal e rugosidade, ou retexturizar por projeção triplanar de materiais PBR fotográficos com máscara por cor. Torres, fortaleza e peças procedurais usam o mesmo conjunto de materiais.

**Valor para o jogador:**

As torres parecem pedra e madeira reais, num estilo só.

**Pronto quando:**

- Um modelo-base por tipo a partir de um kit coerente (pronto ou pacote).
- Estágios 2 e 3 por peças e ornamentos do mesmo kit; Meshy só para peças.
- Normal map presente, albedo sem sombra assada (medido no desfile), triângulos no orçamento.
- Desfile aprovado.

**Como verificar:**

Folha de contato antes e depois, e albedo médio na faixa física pelo TEC-25.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 4, custo 5, risco 5) O problema existe: luz assada nas torres da comunidade Meshy brigando com o sol do URP mata o realismo. Mas 'des-assar' albedo e gerar normal e rugosidade por algoritmo costuma ficar lamacento, e a triplanar com máscara por cor em 18 modelos é um projeto de pesquisa. Antes, decidir o tema (VIS-01) e procurar torres com PBR de verdade, já com LOD. Só vale se o tema mantiver estas torres e não houver substituto pronto.
- (talvez; valor 3, custo 5, risco 4) É pesquisa com resultado incerto. Tirar a luz assada de modelos gerados por IA usando script e reprojetar triplanar com máscara de cor em 18 modelos de 10k–95k tris pode deixar tudo pior, e a validação é subjetiva. Se o VIS-01 trocar o tema, essas torres saem de qualquer jeito. É mais barato e mais seguro conseguir torres que já venham com PBR (Sketchfab ou Poly Haven com licença conferida, ou Meshy com textura PBR, perguntando antes sobre os créditos). Só vale se as torres Karrades sobreviverem à decisão de tema.

</details>

---

### VIS-15 — Efeitos por torre com flipbook e luz

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4,5 · custo 4,5 · risco 3,5 · prioridade 3,5
- **Depende de:** VIS-06, TEC-06

**Por que, e nesta fase:**

Bolha sem textura grita protótipo.

**O que é:**

Flipbooks próprios, procedurais ou simulados no Blender. Soft particles de verdade, fumaça iluminada pelo sol, pool de luzes curtas e distorção para o Ar. Uma receita por torre e por estado.

**Valor para o jogador:**

Cada tiro se reconhece e tem peso.

**Pronto quando:**

Flipbooks prontos (CC0 ou pacote), soft particles e pool de luzes dentro do Forward. -captura vfx, e p95 com 6 torres disparando no perfil mínimo.

**Como verificar:**

Modo '-captura vfx' com close-ups e p95 com 6 torres disparando.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 5, custo 5, risco 4) Tiro com peso é metade do apelo de um TD na página da Steam. Bolha sem textura grita protótipo, então o valor é máximo. Mas 'simular no Blender' é o tipo de coisa que parece bom no papel e devora semanas. Primeiro procurar flipbooks prontos de qualidade: os pacotes gratuitos de partículas da Unity e do Fab ficam só no PC, como manda a regra, e há fumaça e explosão CC0. Procedural fica só para o que faltar. Soft particles, pool de luzes curtas e distorção do Ar são baratos. Medir o p95 com o tier de qualidade (TEC-06), senão o mobile morre.
- (depois; valor 4, custo 4, risco 3) O combate com peso é central, mas a estimativa é otimista. Não existe pipeline do Blender no projeto, e flipbook bom é trabalho de arte que eu não consigo validar sozinho. Cada torre tem 6 níveis e estados. A distorção do Ar exige Opaque Texture, que custa caro no mobile. O pool de luzes esbarra no limite de luzes por objeto do Forward, a menos que se use Forward+. Também precisa antes dos tiers de qualidade (TEC-06). Começar por flipbooks CC0 prontos e soft particles, e deixar a simulação própria como talvez.

</details>

---

### VIS-18 — Cerimônia de construção e evolução, com marca por nível

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 2 · prioridade 3,5
- **Depende de:** VIS-15

**Por que, e nesta fase:**

Com 6 níveis e 3 modelos, metade das evoluções não aparece.

**O que é:**

Na construção, andaime e poeira por ~0,8 s. Na troca de estágio: poeira, a torre crescendo, luz acesa e bandeira subindo no estágio 3, com silhueta elemental no topo.

**Valor para o jogador:**

Subir de nível vira uma recompensa visível.

**Pronto quando:**

Poeira, salto de escala e flash; andaime de até 0,8 s só na vista. Marca própria em cada nível (insígnia, peça, brilho). O -captura do nível 1 ao 6 dá 6 prints diferentes.

**Como verificar:**

O -captura sobe uma torre do 1 ao 6 em 6 prints.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 2) Evoluir é a decisão mais repetida da partida, e a troca de estágio (3 modelos por torre) já existe e merece um momento. Uma versão curta já entrega a recompensa: poeira, crescer de 0,9 para 1,0 e um som. Andaime, bandeira e silhueta elemental podem vir depois. Cuidado no Tower Wars: a cerimônia é só vista, não pode atrasar a torre na simulação nem esconder o alcance. 0,8 s de andaime é aceitável, mais que isso irrita quem constrói em rajada.
- (depois; valor 4, custo 3, risco 2) Subir de nível precisa de recompensa visível, ainda mais com mais evoluções no plano. Mas a versão completa (andaime, bandeira, silhueta elemental) depende dos estágios definitivos das torres, que podem mudar com o VIS-01 e o VIS-17, e bandeira é coisa medieval. Dá para dividir: o essencial (escala que pula, poeira do Vfx atual, flash na troca de estágio) leva horas, não depende de tema e pode ir junto com o VIS-19. O resto espera a arte final das torres.

</details>

---

### VIS-19 — Bichos com peso (conforme o enquadramento)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4,5 · custo 3 · risco 2,5 · prioridade 4
- **Depende de:** BUG-01, VIS-08

**Por que, e nesta fase:**

O bicho tem que sentir o golpe.

**O que é:**

- Flash e tranco ao levar golpe.
- Morte procedural para quem não tem clipe; morte por atrito distinta.
- Poeira nos passos dos pesados.
- Congelado trava a pose; quem queima leva chama presa ao osso.
- Corrida acelerada e chegada pelo portão.

**Valor para o jogador:**

O bicho sente o golpe, e cada tiro satisfaz.

**Pronto quando:**

Flash e tranco só na vista. Morte, fuga ou queda conforme a decisão da Fase 2, com versão diferente para o atrito. Poeira nos pesados e pose congelada. Folha de contato e teste cego tiro × atrito.

**Como verificar:**

Folha de contato com 4 quadros de cada morte, e teste cego tiro × atrito.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 5, custo 3, risco 3) Feedback de golpe é o que faz um TD ser gostoso. Hoje o tiro some num bicho que nem aparece. Flash ou tranco e uma morte com peso pagam mais que qualquer cenário. O risco é real: só urso e elefante têm clipe de morte no bichos.json, os outros 7 precisam de morte procedural, e prender chama a osso varia em 9 esqueletos diferentes. Começar por tranco, flash sutil, morte procedural (tomba e afunda) e poeira dos pesados. Pose congelada e chama no osso ficam para depois. Para o tom realista, sem sangue: bichos realistas morrendo com gore complicam a classificação na Steam. Só depois do BUG-01.
- (logo; valor 4, custo 3, risco 2) É a maior fatia de 'sensação de jogo' pelo custo, e tudo fica só na view. Flash, morte procedural (tomba e afunda) e congelar com Animation.speed=0 são baratos. Riscos: os materiais dos modelos Sketchfab/Meshy usam shaders diferentes, e o flash por emissão pode não funcionar em todos (testar um multiplicador de cor). O tranco não pode mexer na posição da Sim. A chama presa ao osso exige mapa de ossos por modelo, que é o mesmo trabalho do VIS-20. Fica totalmente bloqueado pelo BUG-01: sem bicho visível, nada disso aparece.

</details>

---

### VIS-14 — Marcas no chão

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3 · custo 2 · risco 1,5 · prioridade 3

**Por que, e nesta fase:**

O campo guarda a história da partida.

**O que é:**

Pool de 64 a 128 decalques planos: cratera, chamuscado, geada, grama deitada e poeira, cada um sumindo em 20-40 s.

**Valor para o jogador:**

O campo guarda a história da partida.

**Pronto quando:**

Pool de 64 a 128 decalques PBR com fade, sem z-fighting. Print depois de 60 s com FPS estável.

**Como verificar:**

Print depois de 60 s de batalha, com o pool dentro do limite e o FPS estável.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 2, risco 1) É barato e deixa o print de meio de partida com cara de batalha de verdade. Só que decalque em chão ocre e ladrilhado não salva nada, e sem VFX de tiro decente uma cratera vira mancha solta. Depois do chão (VIS-03) e do VIS-15, rende bem por pouco código. Os decalques precisam de textura PBR boa (há CC0), senão viram quadrado escuro. Com o chão plano, quad plano basta, sem URP Decal Projector.
- (depois; valor 3, custo 2, risco 2) O esforço P é crível: a lane é plana, então quads num pool fixo resolvem sem o Decal Projector do URP, e as texturas podem sair do ProcTex. Mas decalque em cima do chão ocre ladrilhado atual não salva nada. Precisa do VIS-03 (chão) e de um VFX melhor antes. Cuidados: z-fighting com o mesh de território (Y=0,02) e a grade, e overdraw de alfa no mobile.

</details>

---

### VIS-21 — Fortaleza com estados de dano

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 3 · risco 2 · prioridade 3
- **Depende de:** VIS-08, VIS-15

**Por que, e nesta fase:**

Perder vida hoje é só um número.

**O que é:**

Fortaleza escalada pela régua, com estandartes. Estágios de dano por vidas (fumaça, fogo, bandeira a meio-mastro), impacto a cada vazamento e desabamento no fim da partida.

**Valor para o jogador:**

Perder vida pesa, e o fim da partida tem catarse.

**Pronto quando:**

Fortaleza na régua, VFX em 3 limiares e desabamento. Três prints forçados (vidas 20, 10 e 3).

**Como verificar:**

O -captura força vidas 20, 10 e 3 e tira 3 prints.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 3, risco 2) Perder vida hoje é um número caindo. Fumaça e fogo por faixa de vida e tremor no vazamento tornam a pressão legível, e o desabamento dá o clímax do trailer. A versão simples (VFX em 3 limiares, tremor e afundar com poeira no fim) é barata e reaproveita o VIS-15. Estados de dano modelados na malha de 92,6k são caros e ficam fora. Vem depois dos básicos (bug, UI, VFX), mas é uma boa candidata para fechar a fatia vertical.
- (depois; valor 3, custo 3, risco 2) O vazamento precisa pesar, mas o grosso do efeito sai mais barato com tremor de câmera, flash vermelho, som e animação das vidas no HUD. Os estados de dano numa fortaleza baixada de malha única (92,6k tris) exigem pontos de fixação posicionados à mão. Estandarte e meio-mastro amarram o tema medieval, que pode mudar no VIS-01. Esperar o tema e o VIS-15.

</details>

---
