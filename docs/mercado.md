> **MKT-01, relatório completo** (pesquisa de um agente em 09/10/2026; resumo e conclusões no [MANUAL](MANUAL.md), seção 12).
> **Conferido por mim** na API pública da Steam no mesmo dia: o "Line Tower Wars" da Mithryl Labs (grátis, 30/07/2026), e os
> preços e avaliações de Legion TD 2 (US$24,99; 15.335; 86%) e Element TD 2 (US$14,99; 3.238; 90%).
> **Não conferi:** a declaração de IA generativa na página do Line Tower Wars (o campo não aparece na API pública; veja a página),
> as faixas de donos do SteamSpy e os números de conversão da GameDiscoverCo.

# MKT-01 — Line Tower Wars / TD competitivo: panorama de mercado e posicionamento

Pesquisa feita em 09/10/2026. Somente leitura (nenhum arquivo do repositório foi alterado).
Fontes primárias: API pública da loja Steam (appdetails, appreviews, storesearch, search/results) consultada hoje, e
SteamSpy (api.php). Gamalytic exige chave de API (não consegui usar). SteamDB não foi consultado diretamente (não tentei; sem login).
Onde escrevo "não encontrei" é porque realmente não achei fonte.

## 0. Avisos importantes antes de tudo

1. **Já existe um "Line Tower Wars" na Steam**, lançado há ~10 semanas: https://store.steampowered.com/app/4954340/ (Mithryl Labs LLC,
   30/07/2026, GRÁTIS, free-for-all de 2 a 12 "warbands", bots em 3 dificuldades + online via Steam, Windows/Mac, 105 torres, 7 reviews,
   DLC "Gauntlet Mode" US$5,00 em 22/08/2026 https://store.steampowered.com/app/5037790/). Marca de conteúdo: usa IA generativa em alguns modelos 3D/sons/texturas (declarado na página).
   Consequências: (a) se "Line Tower Wars" for o nome comercial do jogo do Felipe, há colisão de nome/SEO; usar como nome de gênero ("lane wars", "Line TD") e não como título. (b) o nicho de "Line TD com bots" acabou de ganhar um concorrente grátis, mas com visual top-down/estilizado e quase sem tração (7 reviews).
2. Números de "donos" do SteamSpy são **faixas largas** (ex.: 1M..2M) e para jogos recentes costumam vir "0..20.000" por falta de amostra. Tratar como ordem de grandeza.
3. As contagens de reviews são de hoje (api appreviews, `purchase_type=all`, inclui ativações de chave). O número exibido na loja pode diferir um pouco (a loja por padrão exclui chaves: Legion TD 2 deu 12.629 só compras Steam vs 15.335 todas).

## 1. Tabela dos jogos (preço = preço cheio atual na loja americana)

Legenda de modos: S=solo/vs IA, C=co-op, P=PvP. "Envia unidades?" = o jogador manda bichos ao oponente.

| Jogo (dev) | Lanç. | Preço | Reviews (% pos.) | Donos (SteamSpy) | Modos | Envia unid.? | Visual |
|---|---|---|---|---|---|---|---|
| **Legion TD 2** (AutoAttack) | EA 2017 (Wikipedia diz 20/11/2017); 1.0 em 01/10/2021 | US$24,99 (campanhas DLC US$9,99 cada) | 15.335 (86%) Very Positive | 1M..2M | S (bots + campanha DLC 1-4 jog.), C, P ranqueado 2v2/4v4 | Sim (mercenários) | Estilizado 3D, "fantasia", baixo custo |
| **Element TD 2** (Element Studios) | EA 19/08/2020 (US$9,99); 1.0 em 02/04/2021 | US$14,99 | 3.238 (90%) Very Positive | 200k..500k | S (campanha 14->28 missões), C, P | Em parte (competitivo/ranked; foco em maze solo/co-op) | Estilizado 3D |
| **Tower Wars** (SuperVillain) | 14/08/2012 | US$7,99 | 1.755 (72%) Mostly Positive | 200k..500k | P, C, S (10 bots, adicionado depois) | Sim (1v1 enviar unidades) | Cartunesco "steampunk" |
| **Bloons TD Battles 2** (Ninja Kiwi) | 30/11/2021 | Grátis (F2P, compras) | 34.269 (84%) | SteamSpy mostrou "0..20k" (obviamente errado/sem amostra; não confiar) | P 1v1 | Sim (envia bloons) | Cartoon 2D/3D |
| **Bloons TD Battles** (1) | 20/04/2016 | Grátis | 29.323 (91%) | não encontrei confiável | P | Sim | Cartoon |
| **Project RTD** (NGELGAMES) | EA 17/02/2020 | Grátis | 906 (48%) Mixed | 100k..200k | P | Sim | Estilizado |
| **Elemental War Clash** (Clockwork Origins) | 28/05/2025 | US$19,99 | 16 (81%) | 0..20k | S (3 modos), P "Clash" 1v1 cross-platform | Sim (só no modo Clash) | 3D, única TD+PvP com tag "Realistic" recente; na prática pintado/estilizado |
| **Topple Tactics** (B. Strak) | 20/10/2023 | US$9,99 | 1.562 (88%) | 50k..100k | P assimétrico (1 gigante x vários) | Não (outro formato) | Cartoon/físico |
| **Warstone TD** (Battlecruiser) | 23/05/2018 | US$14,99 | 3.893 (82%) | 200k..500k | S, C, P | Não central | Hi-res 2D/3D, "pintado" |
| **Line Tower Wars** (Mithryl Labs) | 30/07/2026 | Grátis (DLC US$5) | 7 (86%) | 0..20k | S (bots), P até 12 | Sim | Top-down, parte IA gen. |
| **Thornline TD** (Corby Wenzelbach) | "em breve" (EA anunciado) | n/d | 0 | n/d | S (50 níveis/10 mundos), P até 8 + bots | Sim (envio aumenta renda) | n/d. Página https://store.steampowered.com/app/5300580/ |
| **Send More Monsters** (MightyCodeDragon) | 10/09/2026 | US$8,99 | 0 | n/d | S (campanha vs computador), P, times até 6 | Sim | 2D top-down |
| **Tower Offense** (4 devs, Alemanha) | 28/03/2025 | US$3,99 | 0 | n/d | P 1v1 local/remote play | Sim (atacante x defensor) | 2.5D estilizado |
| **Invaders TD Online** (Sagui) | EA 01/08/2024 | US$2,99 | 27 (93%) | 0..20k | P | Sim | 3D isométrico |
| **Rampartloom** (M. Bayar) | 2026 | Grátis | 0 | n/d | P 3v3 + S | Sim | 3D estilizado |
| **Zoo Wars** (Studio GG) | 31/03/2026 | US$3,99 | 40 (98%) | 0..20k | P online | Sim (cartas) | 2D cartoon |
| **Crystal Clash** (Crunchy Leaf) | 14/01/2022 | Grátis | 1.379 (83%) | 0..20k | P/C 2v2, lane battle + cartas | Sim (lane tug-of-war) | 3D colorido |
| Sanctum 2 (Coffee Stain) — referência solo/co-op, sem envio | 15/05/2013 | US$14,99 | 14.392 (90%) | 2M..5M | S, C | Não | Sci-fi 3D |
| Defense Grid (Hidden Path) — referência solo puro | 08/12/2008 | US$9,99 | 5.381 (96%) | 500k..1M | S | Não | 3D sci-fi |
| Monster Tiles TD: Tower Wars (Swell) | 11/02/2024 | Grátis | 506 (82%) | n/d | S roguelite | Não (só nome) | 3D colorido |

Fontes (todas Steam, consultadas hoje via API): legion td 2 https://store.steampowered.com/app/469600/ ; element td 2 https://store.steampowered.com/app/1018830/ ;
tower wars https://store.steampowered.com/app/214360/ ; btdb2 https://store.steampowered.com/app/1276390/ ; btdb https://store.steampowered.com/app/444640/ ;
project rtd https://store.steampowered.com/app/1207850/ ; elemental war clash https://store.steampowered.com/app/3188970/ ; topple tactics https://store.steampowered.com/app/2390260/ ;
warstone https://store.steampowered.com/app/562500/ ; send more monsters https://store.steampowered.com/app/5088870/ ; tower offense https://store.steampowered.com/app/3530750/ ;
invaders https://store.steampowered.com/app/2826540/ ; rampartloom https://store.steampowered.com/app/5040240/ ; zoo wars https://store.steampowered.com/app/3989360/ ;
crystal clash https://store.steampowered.com/app/1839660/ ; sanctum 2 https://store.steampowered.com/app/210770/ ; defense grid https://store.steampowered.com/app/18500/ .
Donos: https://steamspy.com/api.php?request=appdetails&appid=<appid> (cada appid acima).

Tags Steam (top 12) dos principais:
- Legion TD 2: Tower Defense, Multiplayer, Auto Battler, Strategy, PvP, Replay Value, Competitive, Online Co-Op, Co-op, PvE, Base Building, Resource Management.
- Element TD 2: Tower Defense, Real Time Tactics, Multiplayer, Fantasy, Strategy, PvE, PvP, Team-Based, Competitive, Character Customization, RTS, 3D.
- Tower Wars: Tower Defense, Strategy, Indie, Steampunk, Multiplayer, Co-op, Action, Singleplayer, Online Co-Op, Difficult.
- Line Tower Wars (Mithryl): Strategy, Tower Defense, Tactical RPG, Top-Down, Fantasy, Combat, PvE, PvP, Free to Play, Multiplayer, Singleplayer.
- Elemental War Clash: Strategy, Tower Defense, Roguelite, RTS, Level Editor, 3D, Realistic, Top-Down, Fantasy, Magic, Medieval, PvP.
- Send More Monsters: Strategy, Tower Defense, Incremental, Wargame, 2D, Top-Down, Fantasy, Magic, War, Indie, PvP, Base Building.

## 2. Notas por jogo (o que elogiam / criticam)

**Legion TD 2** — o líder do subgênero. Elogios: "ultimate competitive tower defence", economia (ouro x trabalhadores/Mythium) e tensão defender x pressionar; tutorial bom; bots e campanha para praticar (review de 46h: "Great opportunities to practice against AI, campaign, 4vs4 PVE, PVP"). Críticas (reviews de hoje): base de jogadores fina em ranks altos ("you play with and against the same people match after match", review de 501h), sem matchmaking no modo Classic, "always online even for single player" (71h), preço alto + microtransações, difícil escalar dificuldade da campanha, desempenho ruim em máquina fraca, e (novo) alguém reclama de **arte gerada por IA** ("AI slop"). Dados do dev em thread da Steam (2023): ~830 jogadores simultâneos em média, ~10 mil únicos/dia, ~50 mil/mês — https://steamcommunity.com/app/469600/discussions/0/3421062490359978499 (dado do dev, não verificado por terceiro, 2023). SteamSpy hoje: CCU 687. Esteve de graça na Epic de 24 a 31/07/2025 (https://www.techarp.com/gaming/legion-td-2-free-game/). Campanhas solo/co-op são DLC separado (US$9,99), com poucas reviews (29 e 13, 76-77%) — a campanha solo NÃO foi o motor de vendas.
Conflito de datas: Wikipedia fala em lançamento standalone em 20/11/2017 (https://en.wikipedia.org/wiki/Legion_TD); PCGamesN confirma 1.0 em out/2021 (https://www.pcgamesn.com/legion-td-2/release) — "20% off = US$13,59" no lançamento, ~2.000 reviews "Mostly Positive" na época.

**Element TD 2** — mod de WC3 com "5 milhões de downloads" (release do dev: https://element-studios.prezly.com/element-td-2-launches-into-early-access-to-rave-reviews). Entrou em EA a US$9,99 e a campanha solo veio como primeiro marco do EA, em set/2020 (14 missões, meta 28; https://element-studios.prezly.com/famed-warcraft-iii-mods-standalone-sequel-releases-epic-campaign-mode). 90% positivo com 3.238 reviews. Crítica de review: curva de dificuldade do capítulo 2/3 da campanha. Sinal importante: o jogo mais próximo do Felipe em "modelo de negócio" (mod de WC3 -> EA barato -> 1.0 mais caro) vendeu na faixa 200k..500k (SteamSpy, impreciso). Sem números de vendas oficiais (não encontrei). SteamBase mostrava US$5,99 em promoção de 60% em jun/2026 (https://steambase.io/apps/element-td-2-tower-defense).

**Tower Wars (2012)** — o caso mais útil sobre solo x PvP: Destructoid e Everyeye criticaram a falta de modo solo no lançamento (https://destructoid.com/reviews/review-tower-wars); depois a loja passou a listar 10 bots de IA. 72% positivo, 1,7 mil reviews, 200k..500k donos, mas reviews atuais: rede ruim, "doesn't work any more", sem jogadores (CCU 1). Lição: sem solo, o jogo morreu quando a base sumiu; os bots vieram depois (data da adição: não encontrei).

**Warstone TD** (2018) — review típica negativa: "do not buy... forces you to play online with other players... waited hours... PvP completely broken", ou seja, solo bloqueado por PvP morto. Lição direta: nunca travar progressão atrás de PvP. 82% / 3,9k reviews.

**Bloons TD Battles 1 e 2** — prova de que "mandar bichos ao oponente" tem apelo de massa, mas o dev (Ninja Kiwi) tem marca gigante e F2P. Não é comparável como benchmark de venda.

**Elemental War Clash** (mai/2025, US$19,99) — o único com tag "Realistic" + PvP + TD. Só 16 reviews, 0..20k donos: lançamento praticamente sem tração, apesar de série com histórico (Elemental War 1: 26 reviews; EW2: 26 reviews, 69%). Críticas: desempenho/som com muitas torres, dificuldade/imunidade a status. Clockwork Origins vende o solo, PvP é anexo ("haven't tried any of the online modes... love how calm it is in single player").

**Line Tower Wars (Mithryl)**, **Thornline TD**, **Send More Monsters**, **Tower Offense**, **Rampartloom**, **Zoo Wars**, **Invaders TD Online** — onda 2024-2026 de indies do mesmo formato. Todos com poucas ou zero reviews, preços de US$0 a US$9, visual 2D/top-down/estilizado; quase todos oferecem bots/solo + PvP online. Mostra que o nicho está ocupado por jogos pequenos, mas ninguém "decolou" desde Legion TD 2.

## 3. Respostas às perguntas

### (1) O gênero sobrevive sem PvP ativo? Exemplos solo vs IA
- O que encontrei: os jogos do gênero que **vivem** (Legion TD 2, Element TD 2, Bloons Battles) têm base PvP grande ou uma marca/mod de WC3 conhecido; os que dependem só de PvP **morrem** (Tower Wars 2012: CCU 1 hoje; Warstone: reviews "PvP morto"; Project RTD 48% Mixed). Fonte: páginas Steam acima.
- Solo vs IA como produto principal nesse subgênero (envio + defesa contra bots): **não encontrei nenhum caso de sucesso comprovado.** Os candidatos recentes (Line Tower Wars, Send More Monsters, Thornline) são novos demais para concluir. O Tower Wars (2012) é o contraexemplo: lançado sem solo, criticado por isso.
- Sucessos solo em TD "adjacente" (não é Line TD): Defense Grid (96%, 500k..1M donos), Sanctum 2 (90%, 14k reviews, 2M..5M), Super Sanctum TD (200k..500k), Mindustry (1M..2M). Isso mostra que **o público de TD compra solo**; o risco é o público de "Line TD" querer PvP.
- Dado de mercado: o próprio Legion TD 2 ganhou campanhas solo como DLC e elas tiveram pouquíssimas reviews (29 e 13), então solo foi acessório, não motor.
- Conclusão honesta: dá para sobreviver sem PvP **se o jogo se vender como TD solo com a mecânica de envio de bichos como diferencial** (e não como "jogo competitivo"); dá para vender como "Line Tower Wars" esperando comunidade PvP só se houver multiplayer.

### (2) Faixa de preço para EA indie
- Referências: Element TD 2 EA US$9,99 -> 1.0 US$14,99; Warstone US$14,99; Sanctum 2 US$14,99; Elemental War Clash US$19,99 (16 reviews); Send More Monsters US$8,99; Legion TD 2 US$24,99 (1.0 após 4 anos de EA + PvP). Tower Wars US$7,99; Tower Offense US$3,99.
- GameDiscoverCo (700 jogos pós-set/2023, só os com >=5k wishlists e >=500 vendas no 1o mês — amostra viesada para sucessos): conversão mediana wishlist->venda no 1o mês 27%; **EA converte ~um terço a menos que 1.0**; conversão maior em US$1-10 e grátis, **pior em US$15-30**. https://gamedevreports.substack.com/p/gamediscoverco-conversion-benchmarks (o texto do substack é ambíguo sobre "by third"; método do cálculo não explicado).
- Recomendação de faixa: **US$9,99 (R$ equivalente regional ~R$ 29-39) no EA, subir para US$14,99 no 1.0.** Evitar US$19,99+ sem multiplayer (Elemental War Clash é o aviso). Preço regional na Steam é recomendado pela própria Valve (sugestão geral; não fui buscar a doc).

### (3) Tags para mirar
Núcleo (obrigatórias): **Tower Defense, Strategy, Singleplayer, Replay Value**. Diferencial: **Real Time Tactics, Base Building, Resource Management, Competitive** (só se o PvP existir). Visual: **Realistic, 3D, Atmospheric, Sci-fi/Military** (conforme o tema). Evitar prometer: **Multiplayer, PvP, Co-op** enquanto não existirem (reviews negativas por "falsa promessa" e a regra da Steam de listar só o que existe). Tags "Auto Battler" e "Tactical RPG" aparecem em concorrentes mas não descrevem bem o Line TD. "Roguelite" aumenta alcance (Elemental War Clash, Monster Tiles TD) mas é outro público.
(Escolha de tags por inferência a partir das tabelas acima; não encontrei dado de volume de busca por tag.)

### (4) Lacuna: Line TD realista e de aparência atual?
- Busca na Steam (search/results com tags Tower Defense + PvP + Realistic): **apenas 4 resultados** (Elemental War Clash, Night of the Dead, 2089 - Space Divided, War of Wizards); nenhum é um Line TD/lane wars com visual realista. Tower Defense + Realistic são 45 resultados (contra ~5.000 de TD), em geral jogos de zumbi/militar/VR de baixa produção. Contagens da busca variaram entre consultas (106 vs 308 para TD+PvP), então **trate como ordem de grandeza**.
- Os líderes do subgênero são visualmente estilizados/low-budget (Legion TD 2, Element TD 2, Tower Wars cartunesco, Line Tower Wars top-down). **Não encontrei nenhum Line TD com visual realista e aparência de 2025-26.** A lacuna parece real, mas pode existir por motivo (custo de arte, legibilidade de unidades em visão de cima). Cuidado: o visual realista também eleva expectativas de polimento.
- Atenção a **IA generativa**: Line Tower Wars (Mithryl) declara usar; um review do Legion TD 2 já derruba o jogo por "AI slop". Se o Felipe gerar modelos no Meshy, a Steam exige declarar o uso de IA na página (regra geral da Steam, não verifiquei a página de hoje) e isso pode custar reviews/wishlists em parte do público.

### (5) Frases de venda candidatas
1. PT: "Construa o labirinto. Mande o enxame. Ganhe o território." / EN: "Build the maze. Send the swarm. Own the territory."
2. PT: "O clássico Line Tower Wars de Warcraft III, agora com visual realista e uma fronteira que suas torres conquistam." / EN: "The Warcraft III lane-wars classic reborn in realistic 3D — where your towers claim the ground."
3. PT: "Cada inimigo que você manda paga a sua defesa. Cada torre que você ergue empurra a sua fronteira." / EN: "Every enemy you send funds your defense. Every tower you build pushes your border."
(Opcional, honesta para EA sem multiplayer: "Duelo de estratégia contra a IA — multiplayer a caminho" / "Outsmart the AI today, multiplayer on the roadmap.")

### (6) Riscos
1. **Expectativa de PvP:** público de Line TD vem do multiplayer; sem PvP, reviews "cadê o multiplayer" (cf. Tower Wars 2012 invertido; Warstone). Mitigar: não usar tag PvP/Multiplayer; roadmap público; fórum de feedback.
2. **IA do oponente ruim** vira o jogo inteiro (único oponente). Não encontrei reviews detalhadas de IA de Line TD (exceto "pode vencer o bot mais difícil sem problema" no Line Tower Wars). Investir em 3 dificuldades e 2-3 personalidades.
3. **Concorrente grátis com o mesmo nome/formato** (Line Tower Wars, US$0) e Legion TD 2 (bots + campanha). Preço > grátis exige diferencial claro (visual, fronteira territorial).
4. **EA converte menos** (GameDiscoverCo) e primeiras reviews definem a vida do jogo (Zukowski trata o EA como o lançamento real: https://gameworldobserver.com/2025/03/11/steam-page-launch-guide-wishlists-zukowski, resumo de busca; não li a página inteira).
5. **Visual realista é caro e pode perder legibilidade** de dezenas de bichos na lane.
6. **IA generativa/Meshy:** reação negativa de parte do público + declaração obrigatória na Steam; licenciamento (já há regra do CLAUDE.md de preferir modelos prontos).
7. **Nome:** "Line Tower Wars" já está em uso na Steam (e "Tower Wars" também, 2012).
8. Amostra pequena: quase todos os concorrentes recentes têm <50 reviews; os dados de vendas são faixas do SteamSpy sem confirmação.

## 4. O que NÃO consegui confirmar
- Vendas/receita reais de qualquer título (Gamalytic exige chave; SteamDB não consultei). Só tenho faixas do SteamSpy.
- Data em que o Tower Wars (2012) ganhou bots; qualidade da IA de qualquer um dos jogos.
- Se "Battle Towers" existe: busca na Steam não achou jogo com esse nome exato (o mais próximo: "Battle of Kings"; Roblox "Tower Battles" não é Steam). https://www.mobygames.com/game/112693
- Warcraft III Line Tower Wars original: não achei autor/data confiáveis. (Mod Legion TD: "final dos anos 2000", por Lisk — Wikipedia.)
- Comunidade no Reddit: a busca não retornou threads de Reddit; as citações de reviews vêm da Steam.
- Dados de 2026 de jogadores ativos de Legion TD 2 além do CCU 687 do SteamSpy hoje.
- Kingdom Rush e similares foram deixados de fora, como pedido.

## Recomendação: EA sem multiplayer?

- **Sim, com ressalvas:** vale um EA sem multiplayer *só se* for posicionado como TD de estratégia solo contra IA com o envio de bichos e a fronteira como diferencial, e não como "o novo Legion TD".
- O público de TD compra solo (Defense Grid, Sanctum 2), mas não achei nenhum Line TD solo que tenha vendido bem; o risco é real e não comprovado em nenhum sentido.
- Preço: US$9,99 no EA (US$14,99 no 1.0); evitar US$15-30 no EA. Abaixo de US$10 converte melhor e perdoa a falta de PvP.
- Tags: Tower Defense, Strategy, Singleplayer, Replay Value, Real Time Tactics, Realistic/3D. Não marcar PvP/Multiplayer até existir.
- Antes de lançar: página Steam cedo para juntar wishlists, demo no Next Fest, 3 dificuldades de IA bem calibradas, roadmap público de multiplayer.
- Resolver o nome (colisão com "Line Tower Wars" da Mithryl Labs) e a política de IA generativa/Meshy antes de abrir a página.
- Diferencial vendável e aparentemente vago no mercado: Line TD de visual realista moderno (confirmado só por busca de tags).
- Alternativa mais segura: soft launch/playtest com demo e medir wishlists; só entrar em EA com >= alguns milhares de wishlists (a meta exata é decisão do Felipe; não tenho benchmark específico de Line TD).
