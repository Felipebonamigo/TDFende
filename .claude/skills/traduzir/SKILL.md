---
name: traduzir
description: 'Escreve e confere o texto do TDFende nos idiomas do jogo (PT-BR e EN primeiro), com um glossário único dos termos, e acha texto que ficou fora da tradução. Use ao criar ou mudar qualquer texto que o jogador lê (menu, HUD, tutorial, loja), quando o Felipe pedir "traduz", "põe em inglês/espanhol" ou "tem texto sem tradução".'
---
# Traduzir

Idiomas: decisão pendente da Fase 5; a sugestão é PT-BR, EN e ZH-Hans no Acesso Antecipado (`docs/roadmap/fase-05-*.md`).

## Como o texto funciona neste jogo

Ainda não há sistema de tradução. Os textos estão fixos: no Unity, em OnGUI (`Core/ModeSelect.cs`, `UI/DebugHud.cs`,
`TowerWars/TowerWarsController.cs`); no Unreal, `TEXT()` dentro de `FString::Printf` (`TdfHUD.cpp`, `RejectText` em
`TdfSimActor.cpp`). O plano é a META-14 (Fase 5): uma tabela CSV de chave para idioma, PT-BR e EN primeiro, um
pseudo-idioma 40 % mais largo, fontes Noto (OFL) e teste de chaves completas no pre-commit; a META-14b (Fase 10) traz o
inglês e o print em pseudo-idioma. Os nomes do catálogo chegam ao Unreal por `tdf_catalog_text`, e o `Name` nunca é
traduzido: a chave estável é a do TEC-12. Até a META-14 existir, texto novo de jogador leva a chave anotada num
comentário, para a migração.

## Passos

1. Escreva o texto já em todos os idiomas do jogo, no mesmo commit. Texto só em PT não entra.
2. Use os termos do glossário: `docs/GLOSSARIO.csv` (colunas `termo_pt`, `en`, `nota`; crie na primeira vez). Termo novo de jogo (golpe, modo, peça, recurso) entra no glossário
   antes de aparecer na tela.
3. Confira: quando a META-14 existir, o teste de chaves do pre-commit; antes disso,
   `grep -rn 'GUI.Label\|TEXT("' Assets/_Project/Scripts Unreal/TDFende/Source` acha o texto fixo.
4. Texto longo (tutorial, loja): releia no idioma de destino como alguém daquele país leria. Tradução literal de
   gíria gaúcha ou de futebol não funciona; ache o equivalente.
5. Veja na tela: o inglês e o espanhol costumam ser 20 a 30 % mais longos que o português. Confira no print do executável (skill `captura-do-executavel`) e no pseudo-idioma, quando existir.

## Armadilhas

- Na Steam, "Espanhol - Espanha" e "Espanhol - América Latina" são idiomas separados; o mesmo vale para "Português -
  Brasil" e "Português - Portugal". Declare só os que o jogo tem de verdade.
- Número, data e moeda mudam com o idioma (1.000,5 em PT, 1,000.5 em EN).
- Nome próprio de lugar real, marca e nome de pessoa não se traduzem; e o jogo não tem marca real (CLAUDE.md).
