> **Pesquisa de um agente em 09/10/2026** (VIS-01), só metadados e miniaturas: nada foi baixado nem comprado, nenhum crédito do Meshy foi gasto. As notas de 1 a 5 vêm de capas e descrições, **nenhum modelo foi aberto**. Dados brutos: [`inventario-medieval.json`](inventario-medieval.json). Análise e recomendação: [`tema.md`](tema.md).

# Tema "Medieval realista com kit pago" - resumo (09/10/2026)

Pesquisa somente-leitura. Nada comprado, nenhuma conta, nenhum EULA aceito; so metadados e miniaturas.
Detalhes por candidato em `inventario.json` (36 entradas). Precos em US$ sem imposto.
Nao consegui verificar: **Fab inteira** (403 Cloudflare em curl e WebFetch), contagem de tipos de torre nos kits pagos
(a pagina da Unity traz so a descricao do vendedor), lista completa de especies do Animalia (gim.studio) e do WildMesh Ultimate Pack,
e o estado 2026 da licenca do Megascans. Nenhum 3D foi aberto: as notas de beleza vem de capas/miniaturas e descricoes.

## (a) O que o tema cobre de graca
- **Texturas/terreno/ceu: cobertura excelente.** Poly Haven (CC0): 867 texturas (medieval_blocks_02/03/05/06, medieval_wall_01/02, castle_brick_*, castle_wall_slates, cobblestone_*, telhas, madeira), 997 HDRIs. ambientCG (CC0): rock 76, moss 49, plaster 35, roof 22.
- **Props/rochas/vegetacao:** Poly Haven CC0: rochas musgosas, boulders, pinheiros (pesados, 17M tris, decimar), arbustos, barris, tocos, maca/adaga/martelo medievais.
- **Canhao:** `cannon_01` (Poly Haven, CC0, 41k tris, rigged) ja serve de Canhao com qualidade alta. `modular_fort_01` (28k tris) e `large_castle_door` (12k) cobrem pedaco da fortaleza, mas o forte e colonial.
- **Torres/fortaleza/tenda:** so ha pecas soltas CC-BY no Sketchfab (Medieval Fortress Kit 92k faces, Medieval Castle 64k, Tower and Castle Walls 10k, tenda 90k). Ao ver as miniaturas: estilos todos diferentes, varios pintados a mao. Nada de kit modular realista CC0. Exige creditos. Torres CC0 do Meshy: 100+ achadas, todas geradas por IA, sem PBR, uma por prompt, sem serie de 3 estagios (unica serie de "tiers" e do ProteinPenguin, estilizada, 1-2k tris).
- **Bichos animados:** gratis e fraco/incoerente. CC-BY: urso WildMesh (7.5k faces, 81 clipes, versao antiga com rig problematico), javali (22.8k faces, 73 clipes), lobo (11.8k, 6 clipes), elefante (4.8k, 20 clipes). Tigre/rinoceronte/aguia/cao realistas animados: so estilizados. CC0 realista animado: nenhum. Rato: `street_rat` (Poly Haven, CC0, 14k) e estatico.
- **Torres elementais (gelo/fogo/vento):** nada pronto em lugar nenhum. Moinho animado CC-BY (KaramellGlass, 24k) existe mas e pintado.

## (b) O que exige compra e custo estimado
| Pacote coerente | Kit | Bichos | Total |
|---|---|---|---|
| Barato | Dexsoft Medieval Castle URP 39.99 + Medieval Army Camp 19.99 + Siege Engines 14.99 | WildMesh Ultimate 3D Animal Pack 39.99 | **~US$115** |
| Medio (recomendado se o tema for escolhido) | Hivemind Modular Castle & Dungeon 139.99 + Hivemind War Camp 79.99 (+ Siege Engines 14.99) | Animals Full Pack 149.99 + Realistic Tiger 69.99 + German Shepherd 19.99 + Rat 29.99 | **~US$505** |
| Premium | Hivemind (idem) | Animalia (gim.studio) urso 179.99, tigre 179.99, cao 99.99, aguia 99.99 + MalberS lobo/elefante/rinoceronte 189.97 (javali e rato ainda sem fonte) | **~US$970** |

Notas por item:
- **Hivemind "Castle Of Eternal Mist"**: 945 meshes, URP+HDRP, 5/5 (15 aval.), 4K, capa mostra ruinas cobertas de hera, realismo muito alto, mas e ruina/fantasia, nao fortaleza intacta de polvora. Preco de bundle "Giga Bundle" nao verificado. Fab snippet cita "Medieval Mega Bundle" ~US$400 (nao verificado).
- **Dexsoft Medieval Castle URP**: 247 meshes, torres redondas de telhado conico, torres de vigia, muralhas, tochas com fogo, so URP, sem avaliacoes. Barato e legivel, mas com cara de conto de fadas. Texturas vem do ambientCG.
- **Animals Full Pack (Protofactor)**: bear, boar, elephant, rhino, wolf, golden eagle (+lion, zebra, etc.), 5/5 com 208 aval., Unity 6000.5, shaders Built-in (converter para URP). Cobre 6 das 9 especies; faltam tigre, cao, rato. Qualidade PBR de jogo, sem fur shader.
- **MalberS** (tigre 69.99, lobo 69.99, rinoceronte 69.99, elefante 49.99): URP/HDRP, Unity 6, mas amarram ao Animal Controller deles.
- **Gato por lebre**: "Rigged Rhinoceros" (3D Skill Up, US$12.90) se declara criado com IA.
- **Torres elementais exigem trabalho nosso**, nenhum kit as da prontas: gelo = torre de pedra + shader de geada + cristais (VFX/decal), fogo = braseiro/caldeirao (Dexsoft ja traz tigelas de fogo), ar = moinho ou torre de sinal (nao achei moinho realista PBR). 3 estagios por torre = compor modulos (andares, ameias, bandeiras), nao vem pronto.

## (c) Risco de licenca e de IA
- Unity Asset Store EULA (Single Entity): uso comercial em jogo permitido, **proibe redistribuir os arquivos**. Arquivos pagos devem ficar fora do Git publico (usar .gitignore + pasta local no PC do Felipe; no repo so um README com a lista de compras). Como CLAUDE.md manda push direto na `main`, isso exige disciplina: nunca `git add` de `Assets/ThirdParty`.
- Sketchfab CC-BY: exige credito no jogo. NonCommercial/NoDerivs rejeitados (ex.: WildMesh lobo/tigre demo e SimplePolygon Fortress NoDerivs).
- WildMesh Ultimate Pack: listing do RenderHub diz Extended Use (comercial), mas outra indexacao cita "Editorial Use Only / IP Restricted": **contradicao a esclarecer antes de comprar**.
- IA: Meshy cc0 e 100% IA (Steam exige declarar IA pre-gerada); Rigged Rhinoceros (Unity) admite IA; Pigcraft "Realistic Wildlife" (Fab) tambem IA, segundo busca. Os kits Hivemind/Dexsoft/MalberS/gim.studio/Protofactor nao declaram IA nas paginas lidas, mas nao ha garantia.
- Fab Standard License: uso em qualquer engine (fontes de 2024); nao verifiquei 2026.

## (d) Coerencia / fonte unica
Fonte unica so existe dentro de cada bloco: castelo+acampamento (Hivemind, ou Dexsoft+RedBlue), cenario (Poly Haven/ambientCG, mesma qualidade PBR). **Os bichos vem de 3-5 autores** (Protofactor+MalberS+Rip Vertices+Rifat) com ratos/caes em escalas e pelagens diferentes: e a costura mais visivel. Unica via de fonte quase unica para os 9 bichos e o WildMesh pack (100+ especies, mesma mao), com licenca/qualidade a confirmar. Torres elementais virao de composicao nossa sobre o kit. Camera teleobjetiva de cima ajuda: silhueta e cor valem mais que detalhe de pelagem.

## (e) Veredito honesto
- **Beleza realista alcancavel: 4/5** com Hivemind + HDRIs Poly Haven + pos-processamento URP (ruinas cobertas de hera sao o melhor visual achado). 3/5 com Dexsoft. Bichos: 3.5/5 (Protofactor/MalberS).
- **Coerencia: 3/5.** Cenario e torres coerentes; bichos e torres elementais, nao.
- **Fonte unica: 2/5.** Pelo menos 5 publicadores + Poly Haven/ambientCG. Subiria a 3 com Hivemind (castelo+acampamento+props) e WildMesh (bichos).
- Risco central: tema "ruina fantasy" do melhor kit nao e "fim da Idade Media/polvora"; e as torres de gelo/fogo/vento exigirao VFX/materiais proprios. Custo real do medio: ~US$505 mais horas de montagem.
