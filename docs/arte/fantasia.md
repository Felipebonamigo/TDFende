> **Pesquisa de um agente em 09/10/2026** (VIS-01), só metadados e miniaturas: nada foi baixado nem comprado, nenhum crédito do Meshy foi gasto. As notas de 1 a 5 vêm de capas e descrições, **nenhum modelo foi aberto**. Dados brutos: [`inventario-fantasia.json`](inventario-fantasia.json). Análise e recomendação: [`tema.md`](tema.md).

# Tema "Fantasia realista baixa": inventario de arte (pesquisa somente leitura, 2026-10-09)

Metodo: APIs publicas (Sketchfab v3, Meshy showcases GET, Poly Haven, ambientCG), miniaturas (so vi montagens pequenas), WebSearch/WebFetch para pagos. Nada baixado alem de miniaturas/JSON. Fab e Patreon devolveram 403 no fetch (precos/estilo de itens Fab NAO verificados). 60 candidatos em `inventario.json` (campos: papel, licenca, triangulos, animacoes, risco, nota 1-5).

## (a) O que o tema ja cobre bem de graca
- **Terreno/rochas/ceu/texturas (CC0, Poly Haven + ambientCG):** muito bem. 37 rochas/penhascos 8K (boulder_01, rock_moss_set, rock_face, namaqualand_cliff), 867 texturas (castle_brick, medieval_blocks, mossy_cobblestone, rock_wall_01-17, forest_ground), 997 HDRIs (kloofendal_misty_morning, drakensberg, mossy_forest, neurathen_rock_castle...), ambientCG ~2000 materiais. Nota 4.5.
- **Vegetacao:** Poly Haven (abetos/pinheiros, troncos, musgo, arbustos, gramas), porem arvores de fotogrametria tem 4M-17M tris: precisa LOD/retopo.
- **Ruinas e fortaleza (parcial):** Poly Haven modular_fort_01 (28k, CC0), large_castle_door, stone_fire_pit; Sketchfab CC-BY realistas: "Pack of old towers in ruins" (JB3D, 73k), "Abbey Wall Ruins", "Modular Medieval Wall - Realistic" (68k), "Medieval Fortress Kit" (92k). Nota 3.5-4.
- **Canhao/Morteiro:** Poly Haven cannon_01 (CC0, rigged, 41k, 8K PBR) + Sketchfab "Medieval Mortier" (6k), balistas/trabucos CC-BY. Nota 3.5-4, mas sem estagios.
- **Props de acampamento:** barris, caixotes, bau, armas medievais ornadas (Poly Haven CC0); tenda e carroca (rakutin, CC-BY).
- **Bichos (parcial, gratis):** urso WildMesh (81 clipes), javali AnimalMesh3D (11), rato Nestaeric (12), elefante jimmyho905 (20), tigre-branco (1 clipe), rinocerontes/aguias (so loop). Realistas, mas de 6+ autores, qualidade e clipes desiguais.

## (b) Lacunas
1. **Torres elementais com 3 estagios: nao existe serie realista pronta.** Busquei Sketchfab (CC0/BY), Meshy (CC0) e Unity/Fab/CGTrader. Nenhum pacote de "torres de pedra realistas Gelo/Fogo/Ar com upgrades" apareceu.
   - **Sentinela:** unica serie com estagios do mesmo autor: AZURE_LANTERN_STUDIO (Meshy, CC0) Watchtower T1/T2/T3 + Sentinel + Gatehouse + palicadas. E **estilizada** (Warcraft pintado), IA, ~1M tris.
   - **Fogo:** serie de 6+ braseiros de pedra do Karrades (Meshy, 8k-97k tris) e torre de fogo do Drubco (85k). Chama = VFX.
   - **Gelo:** aglomerados de gelo/cristal CC-BY (chrismartin1337) e "Temple Tower - Carved Ice" (CC-BY, 420k): pecas, nao serie.
   - **Ar/vento: lacuna total.** Unico caminho: moinho (KaramellGlass, CC-BY) ou torre arcana + VFX de vento.
   - **Canhao/Morteiro/Sentinela de 3 estagios:** nao ha; teria que ser montado (base de pedra + peca + enfeites).
2. **Acampamento de guerra realista gratis:** so tenda/carroca soltas.
3. **Bichos completos:** faltam lobo, cao e aguia realistas animados com clipes decentes; golpe/morte so existem nos de varios clipes (urso, javali, rato, elefante).
4. **Criaturas de fantasia animadas:** Meshy CC0 tem muitas (Karrades: wyverns, grifos, megaloceros, terrorbird), mas **estaticas e sem rig** (0 modelos CC0 animados de lobo/urso/javali/aguia/rato; tigre/elefante/cao so humanoides).

## (c) Custo estimado de pagos para fechar lacunas (precos lidos nas paginas; podem variar)
| Item | Fonte | Preco |
|---|---|---|
| Ultimate 3D Animal Pack (100+ animais, idle/walk/run/attack/death, Extended Use) | RenderHub | US$39,99 |
| Dark Fantasy Environment (Souls-Like): torre, portao, castelo, ruinas | Unity (PHIDEAS) | US$80 (visto tambem 60/120) |
| Medieval Army Camp (tendas, armeiros, fogueiras, bandeiras) | Unity (Red Blue Pixel) | US$19,99 |
| Realistic Ruins (13 ruinas + 51 rochas, 4K) | itch.io | US$17+ (amostra gratis) |
| Siege Trebuchet Pack | CGTrader | ~US$13,99 (pagina nao lida) |
| Opcional: Dark Fantasy Kit 550+ prefabs | Unity (Runemark) | US$41,12 |
**Total do minimo util: ~US$170 (~US$210 com o kit extra).** Isso NAO fecha torres Gelo/Fogo/Ar: exigem modelagem sob encomenda, kitbash (muralha + cristal + braseiro) e VFX. Preco desse trabalho nao estimado.

## (d) Risco de licenca / IA
- **IA (Meshy):** AZURE_LANTERN, Karrades, Furio, Drubco, arq.thompson, TimosBilder, -X-ScornGames etc. Todos gerados por IA; Steam exige declarar conteudo gerado por IA no questionario; copyright incerto; texturas com sombra "assada"; malha 300k-2M tris sem retopo. O campo `license` da API diz "cc0", mas **nao li os ToS do Meshy** sobre uso comercial de modelos da comunidade (conferir). Regra do CLAUDE.md: gerar no Meshy so perguntando ao Felipe (~30 creditos/modelo).
- **CC-BY (Sketchfab):** exige atribuicao (tela de creditos) e conferir cada autor; ha reuploads (elefante) e itens "Free Standard"/NC no meio (descartados: WildMesh DEMO lobo/leoa/ pack 400k sao CC-BY-NC; "Lightning Mage" etc. fora de tema).
- **WildMesh pack:** RenderHub diz Extended Use, mas um resumo de busca citou "Editorial Use Only"; a pagina mostra preview rotulado "AI-generated". **Conferir a licenca completa e se os modelos sao IA antes de comprar.** Versao Patreon (assinatura) nao lida.
- **Pagos Unity:** EULA padrao do Asset Store (single entity). Pacotes de 2017-2021 (PBR Temple, Dark Fantasy Kit) podem ter shaders legados para URP/HDRP.
- **Poly Haven / ambientCG:** CC0, sem risco. Megascans/Fab: nao usar sem conferir (gratuidade acabou em 2024; termos para Unity nao confirmados).

## (e) Coerencia (fonte unica possivel?)
- **Nao ha fonte unica.** Melhor combinacao coerente: **Poly Haven (ambiente CC0) + um kit pago de castelo gotico (PHIDEAS ou Runemark) + WildMesh pack (bichos)** unificados por mesma paleta/HDRI/color grading. Torres elementais seriam o elo fraco.
- Alternativa "coerente mas estilizada": AZURE_LANTERN (Sentinela+Gatehouse+paliçadas+oficina) e Karrades (braseiros, torres, bestas), todos IA, estilo pintado; incompatível com "realista, nao cartoon".
- Os modelos Meshy vistos nas miniaturas sao claramente **pintados/estilizados**, nao PBR realistas (poucos, como Borogrim e -X-ScornGames, chegam perto).

## (f) Veredito honesto (1-5)
- **Beleza realista alcancavel: 3,5** (ambiente/bichos 4+, torres elementais 2,5-3; sobe a 4 so com modelagem/kitbash e VFX proprios).
- **Coerencia: 2,5** (3,5 se aceitar uma unica base de castelo + kitbash de torres; 2 se misturar fontes livres).
- **Fonte unica: 1,5** (nenhuma cobre torres+fortaleza+acampamento+bichos; o mais perto e Unity PHIDEAS para estrutura e WildMesh para bichos, cada um cobrindo so uma parte).
- Nao verifiquei: aparencia real dos pacotes Unity/Fab (paginas sem descricao/403), clipes reais do WildMesh, ToS do Meshy, atribuicoes exigidas por autor, se ha torres/ruinas no Sketchfab Store pagas.
