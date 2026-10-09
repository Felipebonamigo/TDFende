# Cronograma do TDFende

Montado em 06/10/2026 por um planejamento com 47 agentes: 5 leitores do código, 12 especialistas (6 lentes × pé-no-chão/ousado), consolidação de 164 ideias distintas (de 232), dois avaliadores independentes por ideia, cronograma e revisão por 3 críticos.

## Como usar (vale para qualquer sessão, inclusive de um modelo mais barato)

1. Leia o [`docs/MANUAL.md`](docs/MANUAL.md) (como trabalhar) e o `CLAUDE.md`.
2. Ache aqui a **primeira fase com tarefa aberta** e, nela, a primeira tarefa `[ ]` cujas dependências estão feitas. A Trilha S (simulação) pode andar em paralelo, em outra sessão.
3. Abra o arquivo da fase e siga o **cartão** da tarefa: "Por que", "O que é", "Pronto quando", "Como verificar". O cartão é a especificação; na dúvida, pergunte ao Felipe em vez de inventar.
4. Se o cartão depende de uma **decisão do Felipe** (lista no arquivo da fase) ou de **créditos Meshy**, pergunte antes e registre a resposta no arquivo da fase.
5. Ao terminar: verificação completa (FlowSim + CompileCheck + `-captura` se for visível), commit e push, e marque `[x]` com data e hash do commit **aqui e no arquivo da fase**, no mesmo commit.
6. A fase só fecha quando o **critério de saída** dela estiver verificado. Anote na tabela de acompanhamento as sessões reais × estimadas.
7. Uma coisa de cada vez: não abra a próxima tarefa com a atual pela metade.

## Visão

TDFende vira um Line Tower Wars realista, com cara de jogo atual da Steam. A assinatura continua a fronteira viva: cada torre projeta território, e a fera que entra nele se esgota à vista do jogador.

O que mudou na revisão: a ordem passa a seguir a prioridade do Felipe (bonito primeiro), cada risco grande ganha um portão, e o pedido explícito (mais bichos, mais torres, mais evoluções, segundo mercado, inimigo que ataca torre) deixa de ser condicional.

O caminho:
1. Bicho visível já. A Fase 0 começa pelo BUG-01, com os dois suspeitos baratos conferidos hoje no repositório: o diff não commitado do URP_Asset (prefiltering de variantes de 1/4 para 3, mais ~25 flags m_Prefilter* ligadas) e o AnimalLoader com cullingType BasedOnRenderers e updateWhenOffscreen = false.
2. Fundação de arte antes de qualquer download (licença, camada privada, auditoria, LFS). Depois, uma fatia de beleza com arte real no tema, medida contra jogos-régua. É o Portão Visual.
3. A tese é provada cedo, antes da UI e com A/B cego (Portão 1). Ao mesmo tempo, uma trilha paralela de simulação, headless e em worktree, afia o núcleo e adianta as regras do conteúdo pedido.
4. Mundo realista completo, depois interface e loja já sobre o mundo bonito, depois torres e efeitos no tema, depois som.
5. Uma fatia do conteúdo pedido entra antes do Portão 2: Mercado 2, um bicho novo e uma torre nova. A Steam entra cedo, com página 'Em breve' e Steam Playtest.
6. Segundo mercado completo, cerco com torres que caem, ramos, torres e bichos novos. Por fim, Sobrevivência, ensino, liga, vitrine e lançamento em Early Access.

Estimativa honesta, a recalibrar no fim da Fase 0 e no Portão 1:
- ~55-70 sessões até a página 'Em breve' e o Portão 2;
- ~110-140 sessões até o Early Access, já com 15-25% de folga para a fila de aprovações;
- a trilha paralela de simulação (~12-16 sessões) não entra no caminho crítico.

Sobre o tema: ~80% da tela é chão, vegetação e luz, que é natureza em qualquer tema. O tema decide torres, fortaleza, acampamento, interface, sons e o elenco de bichos. O critério de escolha deixa de ser 'reaproveita torres', porque o VIS-17 troca todas em qualquer tema. Passa a ser:
- existe uma fonte coerente única (kit modular ou pacote) para torres, fortaleza e props, e outra para bichos com animação completa;
- dá para chegar na beleza dos jogos-régua;
- o elenco fecha com o bioma.
O reuso só desempata.

Candidatos ao inventário: natureza/expedição, fantasia realista 'baixa' e medieval realista com kit pago. O mais bem pontuado vira fatia de beleza. Se reprovar, o segundo vira fatia, nunca greybox. Moderno militar, cidade retomada e Índia mogol ficam de fora, pelos motivos da Fase 1.

## Princípios

- O executável é a verdade. Nada é 'pronto' sem o -captura do exe gerado da main (print + métricas.json com código de saída), mais FlowSim e CompileCheck verdes. O GitHub Actions roda o FlowSim a cada push, como rede de segurança.
- Bonito primeiro, sem fingir. A beleza é provada cedo, com arte real (Portão Visual), e tudo que é aprovado por print (menu, loja) é aprovado sobre o mundo já bonito.
- Duas trilhas. A trilha Unity é única, com trava em Builds/.trava, e nela roda o Tools/Sincronizar.ps1 compatível ou aposentado. A trilha de simulação, Python e pesquisa roda headless em worktree e adianta regras e balanceamento, para que cada fase de conteúdo só precise da vista.
- Portões reordenam e ajustam, não cancelam pedido explícito. Portão Visual (Fase 2), Portão 1 da tese (Fase 3, A/B cego) e Portão 2 de comportamento (Fase 10). Se um portão falha, o conteúdo pedido é replanejado em parâmetros e ordem, nunca apagado.
- Tema decidido uma vez, pela fatia de beleza. O critério é fonte coerente única para torres, props e bichos, mais jogos-régua escritos na bíblia de arte.
- Arte vem nesta ordem: pronta com licença conferida, depois pacote pago na camada privada (fora do Git, com backup), por último Meshy, sempre perguntando antes, com piloto aprovado por critério medido e livro-caixa. Toda arte nova entra auditada (triângulos, LOD, textura comprimida) desde o primeiro download.
- Elenco coerente com o bioma, e no máximo um papel especial por espécie. Reaproveitar modelo é bom, mas não pode sobrecarregar Rinoceronte e Elefante.
- A simulação é pura e determinística. Ids só são acrescentados no fim e o conteúdo é referenciado por chave estável. Toda regra nova vem com teste visto falhar, campo na assinatura do replay e BalanceLab verde. O fingerprint com hash quantizado de posições vem antes de ser usado como prova. A decisão float × ponto fixo sai antes do conteúdo caro.
- A interface nunca muda regra: tudo passa por MatchCommand e IGameInput. Loja e mercado são montados a partir de Count, com abas e mais de 9 envios previstos. Os mockups em HTML são aprovados antes do UI Toolkit.
- Desempenho medido no pior caso: -captura -estresse de fim de partida, num perfil de hardware mínimo além da 4070 Ti. O orçamento começa como aviso e vira reprovação quando a arte assentar. URP é o teto; o HDRP está descartado por causa do mobile.
- O Felipe é recurso escasso. As aprovações vão para uma caixa semanal (prints, sons, lotes Meshy, cada um com sim ou não), e o que não depende de aprovação segue em paralelo. O ROADMAP.md registra por fase as sessões reais × estimadas, o custo de Opus e o que a fase ensinou para o RTS.
- Git: commit e push na main, sempre com pull --rebase --autostash. Assets regravados pelo Unity só entram com OK do Felipe. Cada bloco fecha com DONE, DONE_WITH_CONCERNS, NEEDS_CONTEXT ou BLOCKED.
- Steam primeiro, e cedo: página 'Em breve' e Steam Playtest antes do Portão 2. O caminho mobile é preservado e verificado por builds Android de fumaça (só relatório), mas o porte fica para depois.

## Fases

| # | Fase | Sessões | Itens | Créditos Meshy | Arquivo |
|---|---|---|---|---|---|
| 0 | Fase 0 — Bichos visíveis e executável confiável | 5-6 | 14 | 0 | [abrir](docs/roadmap/fase-00-bichos-visiveis-e-executavel-confiavel.md) |
| 1 | Trilha S — Simulação em paralelo (headless, worktree, sem Unity) | 12-16, em paralelo (fora do caminho crítico) | 16 | 0 | [abrir](docs/roadmap/trilha-s-simulacao-em-paralelo-headless-worktree-sem-unity.md) |
| 2 | Fase 1 — Fundação de arte, mercado e escolha do tema | 2-3 (sem Unity, em paralelo à Fase 0) | 7 | 0 (só o plano de gasto) | [abrir](docs/roadmap/fase-01-fundacao-de-arte-mercado-e-escolha-do-tema.md) |
| 3 | Fase 2 — Fatia de beleza e Portão Visual | 5-7 | 11 | 0 (piloto só com aprovação, e só se falt | [abrir](docs/roadmap/fase-02-fatia-de-beleza-e-portao-visual.md) |
| 4 | Fase 3 — Tese à vista e Portão 1 (antes da UI) | 3-4 | 8 | 0 | [abrir](docs/roadmap/fase-03-tese-a-vista-e-portao-1-antes-da-ui.md) |
| 5 | Fase 4 — Mundo realista completo | 6-8 | 12 | 0 | [abrir](docs/roadmap/fase-04-mundo-realista-completo.md) |
| 6 | Fase 5 — Interface nova: mockups, menu, HUD e fluxo | 6-8 | 10 | 0 | [abrir](docs/roadmap/fase-05-interface-nova-mockups-menu-hud-e-fluxo.md) |
| 7 | Fase 6 — Loja e seleção de torres e bichos | 5-7 | 5 | 0 | [abrir](docs/roadmap/fase-06-loja-e-selecao-de-torres-e-bichos.md) |
| 8 | Fase 7 — Torres, efeitos e fortaleza no tema | 7-10 | 6 | 0-150. Só peças que faltarem no kit, com | [abrir](docs/roadmap/fase-07-torres-efeitos-e-fortaleza-no-tema.md) |
| 9 | Fase 8 — Som e sensação | 4-5 | 8 | 0 | [abrir](docs/roadmap/fase-08-som-e-sensacao.md) |
| 10 | Fase 9 — Fatia do conteúdo pedido | 3-4 | 3 | 0-60 (só se o bicho novo não tiver model | [abrir](docs/roadmap/fase-09-fatia-do-conteudo-pedido.md) |
| 11 | Fase 10 — Steam cedo, pronto para estranhos e Portão 2 | 6-8 | 13 | 0 | [abrir](docs/roadmap/fase-10-steam-cedo-pronto-para-estranhos-e-portao-2.md) |
| 12 | Fase 11 — Segundo mercado completo: elites e chefe | 4-6 | 4 | 0-90 | [abrir](docs/roadmap/fase-11-segundo-mercado-completo-elites-e-chefe.md) |
| 13 | Fase 12 — Cerco: torres com vida e derrubadores | 4-6 | 4 | 0-60 | [abrir](docs/roadmap/fase-12-cerco-torres-com-vida-e-derrubadores.md) |
| 14 | Fase 13 — Mais evoluções, torres novas e trunfos | 5-7 | 7 | 0-90 | [abrir](docs/roadmap/fase-13-mais-evolucoes-torres-novas-e-trunfos.md) |
| 15 | Fase 14 — Bichos novos e voo de verdade | 4-6 | 6 | 0-90 | [abrir](docs/roadmap/fase-14-bichos-novos-e-voo-de-verdade.md) |
| 16 | Fase 15 — Regras por partida e modo Sobrevivência | 4-5 | 4 | 0 | [abrir](docs/roadmap/fase-15-regras-por-partida-e-modo-sobrevivencia.md) |
| 17 | Fase 16 — Ensino completo e liga de rivais | 7-9 | 2 | 0 | [abrir](docs/roadmap/fase-16-ensino-completo-e-liga-de-rivais.md) |
| 18 | Fase 17 — Vitrine | 5-6 | 6 | 0 | [abrir](docs/roadmap/fase-17-vitrine.md) |
| 19 | Fase 18 — Lançamento em Early Access e pós-lançamento | 3-5 | 4 | 0 | [abrir](docs/roadmap/fase-18-lancamento-em-early-access-e-pos-lancamento.md) |

## Tarefas por fase

### [Fase 0 — Bichos visíveis e executável confiável](docs/roadmap/fase-00-bichos-visiveis-e-executavel-confiavel.md)

_O Felipe abre o TDFende.exe gerado da main e vê os 9 bichos com o FPS de volta. Um -captura com métricas e cenário de estresse reprova qualquer regressão. Infraestrutura de sessões, backup e repositório resolvida antes do primeiro asset novo._

- [x] **BUG-01** — Bichos invisíveis no executável (primeiro item) — 09/10/2026
- [x] **TEC-04** — Captura v2 (núcleo) com código de saída e estresse — 09/10/2026
- [x] **BUG-02** — Clipe padrão dos bichos — 09/10/2026
- [x] **VIS-11a** — Grama: rarear ou desligar por flag até o chão novo — 09/10/2026
- [x] **TEC-11** — Shaders sem surpresa no build (escopo dado pela causa do BUG-01) — 09/10/2026
- [x] **TEC-01** — Base limpa: o executável sai da main — 09/10/2026
- [ ] **TEC-34** — Repositório: LFS, histórico e público × privado
- [ ] **TEC-20** — Sessões paralelas seguras (mínimo)
- [ ] **TEC-30** — FlowSim no GitHub Actions
- [ ] **TEC-03** — Selo de build no print e no log
- [ ] **TEC-06** — Orçamento de desempenho escrito (modo aviso)
- [ ] **TEC-26** — Backup do que não está no Git
- [ ] **TEC-35** — Tetos e políticas no MANUAL
- [ ] **PROC-01** — Caixa de aprovações e acompanhamento

### [Trilha S — Simulação em paralelo (headless, worktree, sem Unity)](docs/roadmap/trilha-s-simulacao-em-paralelo-headless-worktree-sem-unity.md)

_Adiantar tudo que é regra pura, para que o Portão 1 rode com o núcleo afiado e cada fase de conteúdo só precise da vista. Roda desde a Fase 0 até a Fase 12, em sessões próprias que não pisam no executável._

- [ ] **TEC-31** — Fingerprint com hash quantizado de posições
- [ ] **BUG-04** — SendCatalog completa por nome e valida id
- [ ] **BUG-05** — Assinatura do replay cobre todos os campos
- [ ] **TEC-12** — Conteúdo por chave estável
- [ ] **TEC-17** — Eventos só de vista
- [ ] **DES-05** — Envios com papel econômico
- [ ] **DES-04** — Vazamento pesa pelo porte
- [ ] **TORRE-03** — Upgrade amplia a fronteira
- [ ] **TORRE-01** — Mira inteligente (fase 1)
- [ ] **BICHO-05** — Armadura no Rinoceronte (Sim)
- [ ] **BICHO-06** — Urso regenera fora da fronteira (Sim)
- [ ] **TEC-14** — Matriz de contras medida (relatório)
- [ ] **DES-03s** — Chaves do laboratório da tese
- [ ] **TEC-27** — Spike de determinismo: float × ponto fixo, e lockstep virtual
- [ ] **MERC-01s** — Regras do segundo mercado e do primeiro sabotador
- [ ] **CONT-S** — Regras do conteúdo seguinte (em ordem)

### [Fase 1 — Fundação de arte, mercado e escolha do tema](docs/roadmap/fase-01-fundacao-de-arte-mercado-e-escolha-do-tema.md)

_Antes de qualquer download ou crédito: licença, camada privada, auditoria e inventário real de arte por tema (bichos inclusive), elenco por bioma e posição de mercado. Sai daqui o tema favorito para a fatia de beleza._

- [ ] **TEC-22** — Manifesto de licenças com auditoria no commit e no build
- [ ] **TEC-23** — Camada privada de arte e som
- [ ] **TEC-10a** — AuditaAssets em modo aviso (adiantado)
- [ ] **VIS-01** — Inventário por tema e bíblia de arte
- [ ] **DES-23** — Meta de elenco do Early Access (rascunho)
- [ ] **MKT-01** — Spike de mercado do gênero
- [ ] **TEC-24** — Plano de gasto dos créditos Meshy

### [Fase 2 — Fatia de beleza e Portão Visual](docs/roadmap/fase-02-fatia-de-beleza-e-portao-visual.md)

_Uma lane que já parece jogo, com arte real no tema favorito, medida contra os jogos-régua e dentro do orçamento. Resolve já os problemas 4 e 5 do Felipe (chão e cenário) na versão 1._

- [ ] **VIS-04** — Faxina de coerência
- [ ] **VIS-05** — Grade só ao construir
- [ ] **VIS-09** — Câmera teleobjetiva
- [ ] **VIS-08** — Régua de escala única
- [ ] **VIS-10** — Sombra de contato no lugar do anel
- [ ] **TEC-25** — Desfile de torres e bichos
- [ ] **VIS-27** — Fatia de beleza no tema favorito
- [ ] **VIS-28** — Spike de estabilidade temporal e luz indireta
- [ ] **VIS-02p** — Protótipo de enquadramento: morre, foge ou cai exausto
- [ ] **TEC-33** — Perfil de hardware mínimo (aviso)
- [ ] **MKT-02** — Hábito de captura para divulgação

### [Fase 3 — Tese à vista e Portão 1 (antes da UI)](docs/roadmap/fase-03-tese-a-vista-e-portao-1-antes-da-ui.md)

_A fronteira com atrito fica impossível de não ver, com render no mundo e sem UI Toolkit. A pergunta 'cercar com fronteira tem graça?' é respondida com A/B cego e com gente além do Felipe, sobre o núcleo já afiado pela Trilha S._

- [ ] **DOC-01** — Reescrever o TESTE.md
- [ ] **TEC-18** — Entrada só por intenções
- [ ] **UX-03** — Pausa e velocidade
- [ ] **UX-05** — Fantasma de construção tático (enxuto)
- [ ] **VIS-16** — Fronteira viva (versão legível)
- [ ] **UX-18** — Leitura dos bichos
- [ ] **TEC-19** — Diário automático de partidas
- [ ] **DES-03** — Portão 1: A/B cego

### [Fase 4 — Mundo realista completo](docs/roadmap/fase-04-mundo-realista-completo.md)

_Os ~80% da tela passam a parecer fotografados em todo o mapa, e os bichos param de denunciar o falso. Dentro do orçamento e no perfil mínimo._

- [ ] **TEC-09** — Arte nova comprimida na GPU
- [ ] **TEC-10** — LOD nativo e auditoria reprovando
- [ ] **VIS-03** — Chão realista completo
- [ ] **VIS-11** — Grama definitiva
- [ ] **VIS-13** — Trilha no chão (variante A)
- [ ] **VIS-06** — Luz e grading finais, e GI
- [ ] **VIS-12** — Mata com LOD
- [ ] **VIS-23r** — Relevo e horizonte (sem água)
- [ ] **VIS-24** — Vida ambiente e morte súbita barata
- [ ] **BICHO-11** — Elenco coerente
- [ ] **VIS-26** — Animal crível
- [ ] **TEC-36** — Build Android de fumaça nº 1 (relatório)

### [Fase 5 — Interface nova: mockups, menu, HUD e fluxo](docs/roadmap/fase-05-interface-nova-mockups-menu-hud-e-fluxo.md)

_Sai o OnGUI, primeiro aprovado em mockup HTML no celular. Entram, em UI Toolkit e sobre o mundo já bonito: menu, HUD das duas lanes, fluxo menu → partida → menu e fim de partida._

- [ ] **UX-29** — Mockups em HTML/CSS antes do UI Toolkit
- [ ] **UX-01** — Fundação de UI em UI Toolkit
- [ ] **UX-02** — Sistema visual da interface
- [ ] **META-14** — Textos por chave
- [ ] **UX-04** — Fluxo menu → partida → menu
- [ ] **UX-06** — HUD das duas lanes
- [ ] **DES-01** — Morte súbita anunciada
- [ ] **UX-10** — Troca suave de lane
- [ ] **UX-19** — Fim de partida (essencial)
- [ ] **META-02** — Tela de créditos

### [Fase 6 — Loja e seleção de torres e bichos](docs/roadmap/fase-06-loja-e-selecao-de-torres-e-bichos.md)

_Comprar, selecionar e evoluir torre, e escolher e enviar bichos, viram cartões e painéis bonitos, com estrutura para mais conteúdo, abas e o Mercado 2._

- [ ] **VIS-25** — Retratos renderizados dos próprios modelos
- [ ] **UX-16** — Ficha de apresentação por chave
- [ ] **UX-15** — Painel da torre selecionada
- [ ] **UX-12** — Loja de torres em cartões
- [ ] **UX-13** — Mercado de envios em cartões

### [Fase 7 — Torres, efeitos e fortaleza no tema](docs/roadmap/fase-07-torres-efeitos-e-fortaleza-no-tema.md)

_Torres, projéteis, explosões e fortaleza com material e peso de jogo atual, numa fonte coerente. Construir e evoluir viram recompensa visível nos 6 níveis._

- [ ] **VIS-17** — Torres por kitbash de fonte única
- [ ] **VIS-15** — Efeitos por torre com flipbook e luz
- [ ] **VIS-18** — Cerimônia de construção e evolução, com marca por nível
- [ ] **VIS-19** — Bichos com peso (conforme o enquadramento)
- [ ] **VIS-14** — Marcas no chão
- [ ] **VIS-21** — Fortaleza com estados de dano

### [Fase 8 — Som e sensação](docs/roadmap/fase-08-som-e-sensacao.md)

_O jogo deixa de ser mudo. Cada ação tem resposta, e o som das torres é escolhido já conhecendo as torres._

- [ ] **BUG-03** — AudioListener garantido
- [ ] **SOM-01** — Sistema de áudio
- [ ] **SOM-02** — Banco de efeitos
- [ ] **SOM-03** — Vozes dos bichos como aviso
- [ ] **SOM-05** — Ambiente sonoro
- [ ] **SOM-04** — Música segura para stream
- [ ] **UX-08** — Alerta de bichos chegando
- [ ] **UX-17** — Sensação de economia e combate

### [Fase 9 — Fatia do conteúdo pedido](docs/roadmap/fase-09-fatia-do-conteudo-pedido.md)

_Antes do Portão 2, os testadores precisam ver o diferencial: a aba do Mercado 2 aberta, o primeiro inimigo que ataca torre, um bicho realmente novo e uma torre nova. A regra já vem pronta da Trilha S._

- [ ] **MERC-01** — Mercado 2 aberto por tempo
- [ ] **MERC-07** — Fúria que desliga torre (bicho novo)
- [ ] **TORRE-08** — Primeira torre nova: anti-pesado tier 2

### [Fase 10 — Steam cedo, pronto para estranhos e Portão 2](docs/roadmap/fase-10-steam-cedo-pronto-para-estranhos-e-portao-2.md)

_Página 'Em breve' juntando wishlists, Steam Playtest como canal, e um build que aguenta estranhos: qualidade, configurações, save, relato de crash e uma primeira partida guiada mínima. O Portão 2 mede comportamento._

- [ ] **META-03a** — Steam Direct, AppID e página 'Em breve'
- [ ] **UX-28** — Modo foto (mínimo)
- [ ] **TEC-02** — Nome da empresa definitivo
- [ ] **TEC-28** — SaveStore versionado
- [ ] **TEC-29** — Crash e relato
- [ ] **TEC-32** — Backend de script decidido
- [ ] **TEC-07** — Três níveis de qualidade
- [ ] **UX-22** — Configurações
- [ ] **UX-23m** — Primeira partida guiada mínima
- [ ] **UX-24** — Cartões de primeiro encontro
- [ ] **META-14b** — Inglês e pseudo-idioma
- [ ] **TEC-36b** — Build Android de fumaça nº 2 (relatório)
- [ ] **META-01** — Playtest pelo Steam Playtest e Portão 2

### [Fase 11 — Segundo mercado completo: elites e chefe](docs/roadmap/fase-11-segundo-mercado-completo-elites-e-chefe.md)

_O Mercado 2 ganha elites e o primeiro chefe, cada mecânica numa espécie distinta e legível na câmera tele._

- [ ] **DES-23b** — Meta de elenco recalibrada
- [ ] **BICHO-09** — Elite imune a controle (espécie própria)
- [ ] **MERC-04** — Alfas que passam no teste cego
- [ ] **MERC-11** — Chefe: Elefante ancestral (único papel do Elefante)

### [Fase 12 — Cerco: torres com vida e derrubadores](docs/roadmap/fase-12-cerco-torres-com-vida-e-derrubadores.md)

_Inimigo que ataca torre de verdade, como foi pedido. A fase não é condicional: o playtest da fúria só decide os parâmetros._

- [ ] **MERC-05** — Torre com vida e destruição
- [ ] **MERC-08** — Derrubador (v1)
- [ ] **VIS-20** — Ataque procedural (plano B)
- [ ] **TORRE-12** — Oficina de reparo

### [Fase 13 — Mais evoluções, torres novas e trunfos](docs/roadmap/fase-13-mais-evolucoes-torres-novas-e-trunfos.md)

_'Mais evoluções' com escolha de verdade, nas torres e nos envios, e mais ferramentas._

- [ ] **TORRE-07** — Evolução em ramos
- [ ] **MERC-14** — Melhoria de envio
- [ ] **TORRE-05** — Torre de apoio com aura
- [ ] **TORRE-13** — Torre tier 2 cara (cerco ou fortificada)
- [ ] **TORRE-04** — Marco de fronteira (se o Portão 1 confirmou a tese)
- [ ] **TORRE-06** — Reações entre torres (2 primeiras)
- [ ] **DES-10** — Trunfos do comandante

### [Fase 14 — Bichos novos e voo de verdade](docs/roadmap/fase-14-bichos-novos-e-voo-de-verdade.md)

_Completar o elenco do EA com uma receita automatizada, mais um céu que vira um segundo problema de posição._

- [ ] **BICHO-01** — Voo de verdade
- [ ] **TORRE-02** — Alvo terra/ar por torre
- [ ] **BICHO-02** — Bando voador
- [ ] **BICHO-10** — Javali-mãe com leitões
- [ ] **TEC-21** — Receita automatizada de bicho e torre
- [ ] **BICHO-05b** — Portador próprio de armadura (tatu ou pangolim), se houver modelo

### [Fase 15 — Regras por partida e modo Sobrevivência](docs/roadmap/fase-15-regras-por-partida-e-modo-sobrevivencia.md)

_Um modo solo sem fim, sobre a mesma simulação, com as regras de cada partida virando dado._

- [ ] **TEC-15** — MatchRules
- [ ] **TEC-16** — Determinismo provado no exe
- [ ] **DES-21** — Sobrevivência e faxina do legado
- [ ] **DES-19** — Partida personalizada e mutadores

### [Fase 16 — Ensino completo e liga de rivais](docs/roadmap/fase-16-ensino-completo-e-liga-de-rivais.md)

_Quem nunca jogou entende a tese em minutos, e quem joga sozinho ganha objetivo._

- [ ] **UX-23** — Primeira partida guiada completa
- [ ] **META-07** — Liga de rivais nomeados

### [Fase 17 — Vitrine](docs/roadmap/fase-17-vitrine.md)

_Trailer, demo para o Next Fest e Steamworks, com material gerado pelo próprio jogo._

- [ ] **META-12** — Modo cinema
- [ ] **UX-21** — Menu principal vivo
- [ ] **META-04** — Integração Steamworks
- [ ] **META-05** — Conquistas
- [ ] **META-13** — Marcadores na gravação da Steam
- [ ] **MKT-03** — Demo, press kit e cápsula

### [Fase 18 — Lançamento em Early Access e pós-lançamento](docs/roadmap/fase-18-lancamento-em-early-access-e-pos-lancamento.md)

_Levar o jogo à venda e cuidar dele depois._

- [ ] **LAN-01** — Questionário de EA e conteúdo mínimo
- [ ] **LAN-02** — SteamPipe em script com branch beta
- [ ] **LAN-03** — Preço regional e comunidade
- [ ] **LAN-04** — Parte fiscal e marca (ações do Felipe)

## Decisões que esperam o Felipe

- [x] Fase 0: manter ou reverter cada configuração regravada — manter todas (Felipe, 09/10/2026).
- [ ] Fase 0: Git LFS ou arte pesada fora do Git; repositório público ou privado até a página da Steam; licença do código.
- [ ] Fase 0: aprovar o critério de 'architectural' para o CLAUDE.md; aposentar ou adaptar o Sincronizar.ps1; onde fica o backup.
- [ ] Fase 1: tema e bioma pela matriz (natureza/expedição, fantasia realista baixa ou medieval com kit pago), confirmados na fatia de beleza.
- [ ] Fase 1: o elenco segue o bioma, e cada espécie tem no máximo um papel especial; manter os 9 bichos ou migrar para uma fonte única.
- [ ] Fase 1: verba para pacotes pagos e para a cápsula, com teto.
- [ ] Fase 1: escolher 2 ou 3 jogos-régua da Steam.
- [ ] Fase 1: conferir no site do Meshy o plano da conta em 03/10 (javali e tigre).
- [ ] Fase 1: lançar o EA sem multiplayer?
- [ ] Fase 2: o Portão Visual (é isso?); o enquadramento (morre, foge ou cai exausto); TAA/STP e GI; o hardware mínimo.
- [ ] Fase 3: a fronteira diverte, pelo A/B cego? E quem são as 2 ou 3 pessoas de fora.
- [ ] Fase 5: idiomas do EA (sugestão: PT-BR, EN e ZH-Hans), nome e logo provisórios.
- [ ] Fase 6: escolher torres e bichos antes da partida, ou só dentro dela?
- [ ] Fase 8: música e sons comprados ou só CC0/CC BY (sempre fora do Content ID).
- [ ] Fase 10: Steam Direct, verificação de identidade, nome em inglês, nome do estúdio e testadores (há 10? se não, 5 mais o diário).
- [ ] Fase 10: só Windows com Proton, ou Linux nativo; Android primeiro, com iOS só se houver Mac.
- [ ] Fases 7 a 14: cada lote de créditos Meshy, com o número antes e piloto primeiro. Teto acumulado sugerido de 600, com reserva de pelo menos 200 para bichos e tier 2.
- [ ] Fase 12: parâmetros do cerco e se entra o comando Reparar.
- [ ] Fase 17-18: cápsula, data do Next Fest, preço, CPF ou CNPJ.

## Riscos

- O bicho invisível pode ser mais fundo que shader ou culling. Três correções falhando: rever o AnimalLoader inteiro, inclusive trocar Legacy por Animator.
- A fatia de beleza pode reprovar nos dois temas: não existir arte coerente e com licença limpa. A saída é pacote pago na camada privada, o que deixa builds de outras sessões diferentes dos do Felipe.
- Quarta troca de direção de arte. A mitigação agora é a fatia de beleza com jogos-régua, não só uma promessa.
- Rejeição a assets de IA por parte do público da Steam. As torres atuais são IA de terceiros, com CC0 declarado pelo autor do upload. Mitigação: kit não-IA nas torres e nos assets da cápsula, e o campo de IA no manifesto.
- Teste num único hardware (4070 Ti). Mitigação: perfil mínimo desde a Fase 2.
- Perda de dados fora do Git (camada privada, GLBs do Meshy, builds) num disco único. Mitigação: TEC-26 com restauração testada.
- O Felipe é gargalo de aprovação, com mais de 30 pontos. Mitigação: caixa semanal e trabalho que segue em paralelo.
- O gênero vive de multiplayer, e o EA sai só contra a IA. Mitigação: MKT-01, liga de rivais e lockstep virtual cedo.
- A tese pode falhar no Portão 1. Os parâmetros são replanejados; o conteúdo pedido continua.
- Desempenho no fim de partida: dezenas de skinned em CPU (Legacy) mais VFX. Mitigação: -estresse desde a Fase 0.
- Determinismo em float entre Mono, IL2CPP e ARM. A decisão sai no TEC-27, antes do conteúdo caro.
- Animação: quadrúpedes do Meshy só andam. Ataque, fuga e morte procedurais podem parecer robóticos; dar preferência a uma fonte com clipes completos.
- Repositório público com 191 MB e sem licença do código: qualquer um monta o jogo de graça. Decidir antes da página.
- Áudio: o Claude não ouve, então cada som depende do Felipe.
- Escopo de ~110-140 sessões até o EA. Mitigação: recalibração na Fase 0 e no Portão 1, e lista de corte no ROADMAP.
- Prazos da Steam (30 dias depois do Steam Direct, 2 semanas de 'Em breve', Next Fest com data fixa) e verificação de identidade demorada.
- Licença: plano Meshy incerto para javali e tigre, e CC BY exige crédito em todo build, inclusive de playtest.
- Unity: vulnerabilidade que obrigue a recompilar jogos lançados, e o teto de receita do Personal.

## Fila e rejeitadas

43 ideias para depois e 8 rejeitadas, com o motivo de cada uma e o cartão completo: [docs/roadmap/fila.md](docs/roadmap/fila.md).

## Acompanhamento

| Fase | Início | Fim | Sessões estimadas | Sessões reais | O que ensinou |
|---|---|---|---|---|---|
| Fase 0 | | | 5-6 | | |
| Trilha S | | | 12-16, em paralelo (fora do caminho crítico) | | |
| Fase 1 | | | 2-3 (sem Unity, em paralelo à Fase 0) | | |
| Fase 2 | | | 5-7 | | |
| Fase 3 | | | 3-4 | | |
| Fase 4 | | | 6-8 | | |
| Fase 5 | | | 6-8 | | |
| Fase 6 | | | 5-7 | | |
| Fase 7 | | | 7-10 | | |
| Fase 8 | | | 4-5 | | |
| Fase 9 | | | 3-4 | | |
| Fase 10 | | | 6-8 | | |
| Fase 11 | | | 4-6 | | |
| Fase 12 | | | 4-6 | | |
| Fase 13 | | | 5-7 | | |
| Fase 14 | | | 4-6 | | |
| Fase 15 | | | 4-5 | | |
| Fase 16 | | | 7-9 | | |
| Fase 17 | | | 5-6 | | |
| Fase 18 | | | 3-5 | | |

Mapa do código como estava em 06/10/2026: [docs/ESTADO-ATUAL.md](docs/ESTADO-ATUAL.md).
