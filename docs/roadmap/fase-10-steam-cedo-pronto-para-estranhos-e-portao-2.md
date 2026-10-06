# Fase 10 — Steam cedo, pronto para estranhos e Portão 2

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 6-8 sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Página 'Em breve' juntando wishlists, Steam Playtest como canal, e um build que aguenta estranhos: qualidade, configurações, save, relato de crash e uma primeira partida guiada mínima. O Portão 2 mede comportamento.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Página 'Em breve' no ar.
- Relatórios tabulados e critério de comportamento avaliado.
- Tabela dos 3 presets no hardware mínimo.
- Save e crash testados.
- Relatório Android.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Pagar o Steam Direct (ação do Felipe), fazer a verificação de identidade e escolher o nome em inglês.
- [ ] Nome do estúdio e quem são os testadores (5 ou 10 ou mais).
- [ ] Repositório público ou privado antes da página; só Windows com Proton ou Linux nativo; Android primeiro (iOS só com Mac).
- [ ] Se o Portão 2 falhar: o que corrigir e em que ordem. O conteúdo pedido continua.

## Tarefas, em ordem

- [ ] META-03a — Steam Direct, AppID e página 'Em breve'
- [ ] UX-28 — Modo foto (mínimo)
- [ ] TEC-02 — Nome da empresa definitivo
- [ ] TEC-28 — SaveStore versionado
- [ ] TEC-29 — Crash e relato
- [ ] TEC-32 — Backend de script decidido
- [ ] TEC-07 — Três níveis de qualidade
- [ ] UX-22 — Configurações
- [ ] UX-23m — Primeira partida guiada mínima
- [ ] UX-24 — Cartões de primeiro encontro
- [ ] META-14b — Inglês e pseudo-idioma
- [ ] TEC-36b — Build Android de fumaça nº 2 (relatório)
- [ ] META-01 — Playtest pelo Steam Playtest e Portão 2

---

### META-03a — Steam Direct, AppID e página 'Em breve'

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

Wishlist se acumula com o tempo. São 30 dias entre pagar e lançar e pelo menos 2 semanas de 'Em breve', e a verificação de identidade leva dias.

**Pronto quando:**

Página aprovada pela Valve com screenshots do modo foto, frase de venda e divulgação de IA (terceiro e própria). As datas vão para o ROADMAP.

---

### UX-28 — Modo foto (mínimo)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 2 · risco 1 · prioridade 3
- **Depende de:** UX-01, VIS-06

**Por que, e nesta fase:**

Screenshots para a página.

**O que é:**

F10 pausa a vista e abre câmera livre, DOF, controle de hora do dia, HUD escondido e PNG em 4K.

**Valor para o jogador:**

Jogadores produzem material para a Steam.

**Pronto quando:**

F10 pausa a vista, abre câmera livre, esconde o HUD e salva PNG em 4K.

**Como verificar:**

PNG 4K gerado sem HUD.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 2, custo 2, risco 1) Para o jogador o valor é baixo. Para a página da Steam (cápsulas, screenshots, trailer), uma versão de desenvolvedor com câmera livre, HUD escondido e PNG 4K via ScreenCapture superSize é barata e útil, mas só quando o jogo estiver bonito. O controle de hora do dia depende de iluminação dinâmica (VIS-06) e pode ficar de fora.
- (depois; valor 2, custo 2, risco 1) A versão mínima é barata: F10, câmera livre, esconder o HUD e ScreenCapture com superSize para 4K. Serve também como ferramenta nossa para a página da Steam. Mas modo foto de um jogo que ainda não está bonito não produz material que preste. O controle de hora do dia depende do VIS-06. Entra quando o visual estiver pronto.

</details>

---

### TEC-02 — Nome da empresa definitivo

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 1 · risco 1,5 · prioridade 3

**Por que, e nesta fase:**

O primeiro build que sai do PC fixa o caminho dos saves.

**O que é:**

Trocar 'DefaultCompany' antes de existirem saves de jogadores, com uma cópia única de Balanceamento/ e replays/ do persistentDataPath antigo para o novo. README e MANUAL são atualizados no mesmo commit. Precisa do OK do Felipe e do nome do estúdio.

**Valor para o jogador:**

Trocar depois do lançamento apagaria as configurações e os replays dos jogadores.

**Pronto quando:**

companyName trocado; MANUAL e README atualizados; arquivos copiados à mão.

**Como verificar:**

Boot com uma pasta de fixture no caminho antigo migra os arquivos (conferido no log).

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 2, custo 1, risco 1) Ainda não há jogador nem save de terceiros, então a migração automática é cerimônia para os arquivos do próprio Felipe, que dá para copiar à mão. Basta trocar o nome antes do primeiro build público (demo, playtest, página da Steam). Depende de o Felipe decidir o nome do estúdio.
- (depois; valor 2, custo 1, risco 2) Não existe jogador externo, então o código de migração protege só arquivos que o próprio Felipe tem. É especulativo (degrau 1 da escada). Basta trocar o companyName antes do primeiro build que sair do PC (playtest ou Steam), quando o Felipe decidir o nome do estúdio. No mesmo commit, corrigir o docs/MANUAL.md e o README.md, que citam o caminho LocalLow/DefaultCompany. Trocar agora quebraria só os roteiros e hábitos que leem o Player.log nesse caminho.

</details>

---

### TEC-28 — SaveStore versionado

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

Configurações, diário, progresso e conquistas não podem se perder.

**Pronto quando:**

Escrita atômica (temporário + rename), versão de esquema e migração, pasta pronta para o Steam Cloud. Teste de arquivo corrompido.

---

### TEC-29 — Crash e relato

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

Um crash no meio da partida não gera nada hoje.

**Pronto quando:**

- Diário que sobrevive ao crash, e pacote oferecido na abertura seguinte.
- Botão 'Reportar problema' na pausa.
- Player.log anonimizado (sem o nome de usuário).
- Build e símbolos arquivados por hash, com backup.

---

### TEC-32 — Backend de script decidido

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

Afeta desempenho, determinismo, símbolos e o porte.

**Pronto quando:**

Mono ou IL2CPP no PC, decidido com os dados do TEC-27, e -captura verde no backend escolhido.

---

### TEC-07 — Três níveis de qualidade

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 3 · risco 2,5 · prioridade 3
- **Depende de:** TEC-06

**Por que, e nesta fase:**

Notebook e Steam Deck precisam rodar.

**O que é:**

Três URP assets gerados por script (Alto, Médio, Baixo/mobile) e ligados ao QualitySettings; hoje os 6 níveis usam o asset de PC. Cada nível define cascatas e distância de sombra, SSAO, densidade de grama, renderScale e HDR. Também liga o gpuSkinning e aceita o argumento -qualidade.

**Valor para o jogador:**

Roda em notebook, Deck e celular, e no PC bom entrega o mesmo visual gastando menos.

**Pronto quando:**

Alto, Médio e Baixo gerados por script. O Baixo é calibrado no hardware mínimo. -captura -qualidade com a tabela dos 3.

**Como verificar:**

Captura nos 3 níveis com folhas de contato e métricas: o Baixo dentro do orçamento mobile.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 3, risco 2) Para a Steam é obrigatório: Deck, notebooks e menu de gráficos esperado, e review negativa por desempenho mata o jogo. Mas ajustar 3 níveis em cima de uma arte que ainda vai ser trocada é retrabalho. Primeiro consertar a grama e definir o visual, depois criar os níveis junto do menu de Configurações. O mobile está longe: o nível Baixo com ≤ 300k não deve guiar decisão agora.
- (depois; valor 3, custo 3, risco 3) Mobile e Deck ainda são intenção, e hoje o Felipe joga numa 4070 Ti. O único ajuste que importa agora é a densidade da grama, que já resolve a queda de FPS e pertence ao trabalho de grama (VIS-11). Mais URP assets multiplicam variantes, stripping por asset e tempo de build. Isso é exatamente a classe de bug do BUG-01 (variante faltando no executável). Ligar o gpuSkinning mexe justo nos skinned meshes que hoje somem. Fazer depois que a arte e o tema assentarem, com o TEC-04 para medir.

</details>

---

### UX-22 — Configurações

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 3 · risco 2 · prioridade 3,5
- **Depende de:** UX-01, TEC-07, SOM-01, TEC-02

**Por que, e nesta fase:**

Mínimo esperado na Steam.

**O que é:**

- Vídeo: resolução, janela, vsync e preset de qualidade.
- Áudio: 4 volumes.
- Interface: escala 80-150%, números flutuantes, tremor, modo daltônico com forma redundante, texto maior.
- Controles.
Persistência com padrão quando o arquivo quebra.

**Valor para o jogador:**

Mínimo esperado na Steam, e o controle do FPS fica com o jogador.

**Pronto quando:**

Vídeo, áudio, escala de UI, daltônico e tremor, persistidos no SaveStore. Remapeamento de teclas fica fora.

**Como verificar:**

Teste de ida e volta da serialização, e tabela de FPS dos 3 presets.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 2) É o mínimo para a Steam: resolução, janela, vsync, preset de qualidade e volumes. Ficou mais urgente porque a grama derruba o FPS para 54 numa 4070 Ti e só existe um URP para tudo. A acessibilidade completa (daltônico com forma, escala 80-150%) pode vir numa segunda leva, mas a escala de UI tem que nascer junto com o UI Toolkit. Persistência com padrão para arquivo quebrado é correto.
- (depois; valor 3, custo 3, risco 2) É obrigatório antes de ir para a Steam, mas não deixa o jogo mais bonito agora. Resolução, janela, vsync e volumes são baratos com a API do Unity. O preset de qualidade depende do TEC-07, porque hoje há um único URP_Asset. Remapear controles no Input Manager legado é caro e frágil: cortar ou adiar até o Input System. O modo daltônico com forma redundante sai quase de graça se o UX-18 já usar formas. Persistência com fallback é simples.

</details>

---

### UX-23m — Primeira partida guiada mínima

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

Sem ensino, nota baixa mede 'não entendi', não 'não diverte'.

**Pronto quando:**

3-4 passos esperando condição: construir, ver o atrito, enviar e ver a renda. Dica com Pular. Roda headless no FlowSim.

---

### UX-24 — Cartões de primeiro encontro

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3 · custo 2 · risco 1 · prioridade 3
- **Depende de:** UX-16, VIS-25

**Por que, e nesta fase:**

Ensina cada bicho sem parede de texto.

**O que é:**

Na primeira vez que cada bicho entra na sua lane, desliza um cartão com retrato, uma linha e a torre que responde. Salvo no perfil e desligável.

**Valor para o jogador:**

Ensina 9 ou mais bichos sem parede de texto.

**Pronto quando:**

Uma vez por tipo, desligável e salva no SaveStore.

**Como verificar:**

Aparece uma vez por tipo, com print.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 2, risco 1) É um jeito barato e eficaz de ensinar 9 bichos ou mais sem parede de texto, e cresce junto com o segundo mercado. Depende das fichas (UX-16) e dos retratos (VIS-25), então naturalmente vem depois deles. Se for desligável, não incomoda o veterano.
- (depois; valor 3, custo 2, risco 1) Ensina muito por pouco e escala sozinho com os bichos novos. É P de verdade, mas só depois de existirem a ficha (UX-16) e os retratos (VIS-25), que hoje não existem. Também precisa de um perfil salvo, que ainda não há (é pequeno). É um bom substituto barato para boa parte do tutorial enquanto o design muda.

</details>

---

### META-14b — Inglês e pseudo-idioma

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

Testadores e página em inglês.

**Pronto quando:**

Tradução EN, teste de chaves no pre-commit e print em pseudo-idioma sem corte.

---

### TEC-36b — Build Android de fumaça nº 2 (relatório)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

Confirma que a arte final não fechou o caminho mobile.

**Pronto quando:**

IL2CPP, preset Baixo e ASTC, com FPS no aparelho real, no ROADMAP.

---

### META-01 — Playtest pelo Steam Playtest e Portão 2

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 2,5 · risco 1,5 · prioridade 4
- **Depende de:** TEC-03, TEC-19

**Por que, e nesta fase:**

O jogo final é construído sobre o que diverte quem não é o Felipe.

**O que é:**

No fim da partida, um zip local com replay, versão, estatísticas, log e as 3 notas do TESTE.md; depois, o app Steam Playtest. O FlowSim tabula os relatórios. Portão: só escalar conteúdo pesado com nota média ≥ 3,5 em 10 ou mais testadores.

**Valor para o jogador:**

O jogo final é construído sobre o que diverte, e nasce uma comunidade.

**Pronto quando:**

- 5 a 10 ou mais testadores pelo Steam Playtest, sem SmartScreen.
- Tabulação no FlowSim.
- Critério principal: pelo menos 50% jogam a 2ª partida por conta própria e terminam a 1ª.
- Secundário: nota de pelo menos 3,5 e o que citam espontaneamente.

**Como verificar:**

Um zip de fixture reproduz o fingerprint, e a decisão fica registrada no MANUAL.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 5, custo 2, risco 2) É o item que mais protege o jogador final. A pergunta central do projeto (cercar tem graça?) nunca foi testada com gente, e o portão de nota ≥ 3,5 antes de escalar conteúdo pesado evita gastar meses de arte num núcleo chato. Vem depois dos bichos visíveis e de uma UI minimamente usável, senão a nota mede os bugs. A versão mínima é exe e formulário para 5-10 pessoas. O zip automático, a tabulação no FlowSim e o Steam Playtest (que precisa do AppID pago pelo Felipe) vêm depois.
- (logo; valor 5, custo 3, risco 1) É a única forma de responder à pergunta central, que nunca teve resposta: cercar com a fronteira tem graça? O portão (nota média ≥ 3,5 com 10 ou mais testadores antes de escalar conteúdo pesado) é a melhor defesa contra gastar meses de arte no lugar errado. O zip local (replay, versão, estatísticas, log, notas) é barato porque o replay e as estatísticas já existem. O Steam Playtest depende de app pago e de ação do Felipe, e fica para a segunda fase. Precisa antes dos bichos visíveis no executável e de UI mínima. Não precisa esperar o salto visual para testar a tese com amigos.

</details>

---
