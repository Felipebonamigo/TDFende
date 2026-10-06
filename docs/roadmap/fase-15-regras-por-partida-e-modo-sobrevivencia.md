# Fase 15 — Regras por partida e modo Sobrevivência

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 4-5 sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Um modo solo sem fim, sobre a mesma simulação, com as regras de cada partida virando dado.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Replays iguais no exe e no FlowSim.
- Sobrevivência jogável.
- Legado removido, com pre-commit verde.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Aprovar o MatchRules.
- [ ] Nome do modo.

## Tarefas, em ordem

- [ ] TEC-15 — MatchRules
- [ ] TEC-16 — Determinismo provado no exe
- [ ] DES-21 — Sobrevivência e faxina do legado
- [ ] DES-19 — Partida personalizada e mutadores

---

### TEC-15 — MatchRules

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 4 · risco 3 · prioridade 3
- **Depende de:** BUG-05

**Por que, e nesta fase:**

Modo novo vira dado.

**O que é:**

Um MatchRules imutável é passado a MatchRunner, LaneSim e IA, com catálogos, atrito, escalada, morte súbita, ouro inicial, vidas, renda, ids liberados e IA ativa ou não. Os statics viram Padrao, o LeakRouter.Buffer vira instância, e o replay ganha a linha opcional 'rules'. É architectural.

**Valor para o jogador:**

Cada modo novo (desafio, campanha, mutador, tutorial) vira dado em vez de código.

**Pronto quando:**

Design aprovado. MatchRules imutável, statics viram Padrao e LeakRouter por instância. Replays antigos reproduzem, e regras não vazam entre partidas.

**Como verificar:**

Replays antigos reproduzem com Padrao, e duas partidas com regras diferentes no mesmo processo não se contaminam.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 4, risco 3) Tutorial e desafios importam para a Steam, mas hoje não existe nenhum modo novo pronto para consumir isso. Refatorar statics do núcleo determinístico 'por garantia' é complexidade especulativa. Fazer quando o tutorial ou a campanha for de fato implementado, junto com ele.
- (depois; valor 3, custo 4, risco 3) Abre modos novos (tutorial, desafio, mutador) como dado. Mas nenhum desses modos foi pedido agora, e é architectural: trocar os statics de TowerWarsConfig por instância passa por LaneSim, IA e LeakRouter, que são o coração determinístico. Os 183 Check() reduzem o risco, sem zerá-lo. O segundo mercado não precisa disso: é conteúdo de catálogo e comando novo. Fazer só quando o primeiro modo novo, provavelmente o tutorial, estiver aprovado. Depende do BUG-05.

</details>

---

### TEC-16 — Determinismo provado no exe

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 3 · risco 2,5 · prioridade 3
- **Depende de:** TEC-01

**Por que, e nesta fase:**

Bugs reproduzíveis e base de placar.

**O que é:**

O executável toca replay (-replay e -fingerprint) e compara com o FlowSim, Mono contra CoreCLR. O StateFingerprint ganha um hash quantizado de posições, vidas, torres e rng. Math.Pow vira tabela. O tique divergente é achado por busca binária. Replay vira relatório de bug.

**Valor para o jogador:**

Bugs reproduzíveis na hora, e a base de replays compartilháveis e multiplayer.

**Pronto quando:**

O exe toca -replay e -fingerprint. 20 replays iguais no exe e no FlowSim, com o backend escolhido.

**Como verificar:**

O fingerprint muda com 0,01 de deslocamento (falha hoje), e 20 replays dão o mesmo resultado no exe e no FlowSim.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 2, custo 3, risco 3) Só paga com multiplayer, que não está no horizonte, e a simulação em float entre Mono e CoreCLR pode virar um buraco sem fundo. Nada disso deixa o jogo mais bonito ou divertido hoje. O pedaço útil e barato (o exe tocar replay para reproduzir bug) pode entrar junto com o TEC-19.
- (depois; valor 2, custo 3, risco 2) Sem multiplayer no horizonte, o valor para o jogador é baixo. A parte barata e útil já: trocar o StateFingerprint grosso (só contadores) por um hash quantizado, que também pega regressão nos refactors TEC-12 e TEC-15. Diferença entre Mono e CoreCLR (e IL2CPP no mobile) só importa para lockstep e para reproduzir no FlowSim um replay vindo do executável. Há só um Math.Pow na Sim (LaneSim.cs:773), então essa parte é pequena. Depende do TEC-01.

</details>

---

### DES-21 — Sobrevivência e faxina do legado

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3,5 · risco 2,5 · prioridade 4
- **Depende de:** TEC-15

**Por que, e nesta fase:**

Modo solo com todo o conteúdo, sem código paralelo.

**O que é:**

Um gerador de ondas determinístico usa o SendCatalog com orçamento crescente, e o placar é de ondas. O TD clássico (GameController, Units/, Towers/, DebugHud) é aposentado; no curto prazo, só sai do menu.

**Valor para o jogador:**

Modo solo com todo o conteúdo, sem código paralelo.

**Pronto quando:**

Ondas determinísticas e mediana do bot Difícil entre 15 e 30. Apagados GameController, Units/, Towers/, DebugHud, TowerPlacer e o caminho Mixamo, se não houver uso (CharacterLoader, CharacterImportRules, AnimKind.Walker), com o manifesto atualizado.

**Como verificar:**

Ondas iguais para a mesma semente, e mediana do bot Difícil entre 15 e 30 ondas.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 4, risco 3) A maioria de quem compra TD na Steam joga sozinho e quer um modo sem fim. Hoje o menu oferece um 'TD clássico' paralelo, mais feio e que não enxerga bicho nem torre nova, e isso é pior que não ter nada. Tirar o modo do menu é imediato e quase grátis. O gerador de ondas sobre o LaneSim reaproveita todo o conteúdo e todos os testes. O risco está em calibrar um orçamento de ondas que fique divertido, não só determinístico.
- (logo; valor 4, custo 3, risco 2) O TD clássico é código paralelo (GameController, Units/, Towers/, GameConfig) que não enxerga nenhuma torre ou bicho novo. Cada conteúdo acrescentado aumenta essa dívida. Tirar o modo do menu custa quase nada e deve ir junto com o menu novo. O gerador de ondas sobre o LaneSim reaproveita o SendCatalog e o replay. O risco está na calibração (mediana de 15 a 30 ondas do bot Difícil, que pede várias rodadas do laboratório) e em achar o que ainda referencia o código antigo antes de apagar. Um modo solo com todo o conteúdo é o esperado num TD da Steam.

</details>

---

### DES-19 — Partida personalizada e mutadores

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2 · prioridade 3
- **Depende de:** TEC-15

**Por que, e nesta fase:**

Variedade e A/B permanente da tese.

**O que é:**

Tela com dificuldade, ouro, vidas e escalada, e mutadores: Sem fronteira, Fronteira dobrada, Só voadores, Só gigantes, Sem upgrades, Ouro em dobro, Morte súbita aos 4 min e Herança. A combinação vira um código curto para compartilhar.

**Valor para o jogador:**

Variedade sem conteúdo novo, e um A/B direto da tese.

**Pronto quando:**

Tela de partida personalizada. Mutadores como MatchRules, cada um terminando em menos de 20 min.

**Como verificar:**

Cada mutador termina em menos de 20 min e mostra o efeito esperado no FlowSim.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 2) Mutadores dão variedade quase de graça porque a Sim é orientada a dados, e o 'Sem fronteira' é o melhor A/B da tese. Por isso esse mutador vale ligar já como flag de teste interna, sem tela. A tela para o jogador depende da UI nova (UI Toolkit) e de haver conteúdo e visual que façam alguém querer jogar várias vezes. O código para compartilhar é um extra que só rende com comunidade.
- (depois; valor 3, custo 3, risco 2) O A/B Sem fronteira x Fronteira dobrada vale ouro para a tese, mas isso já sai como opção de config no BalanceLab, sem tela. Os 8 mutadores são subestimados como M: 'Só voadores' e 'Só gigantes' pedem filtro no SendCatalog e na IA, que hoje escolhe envio por nota e não por lista. 'Herança' nem está definido. O código de compartilhar exige serializar a config de forma versionada. A tela depende do UX-01 (UI Toolkit), que ainda não existe. Fazer já só o flag de A/B no laboratório; a tela fica para depois do salto visual.

</details>

---
