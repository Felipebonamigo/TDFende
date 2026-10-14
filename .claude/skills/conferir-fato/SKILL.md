---
name: conferir-fato
description: 'Confere em fonte confiável o texto do TDFende que fala do mundo real (bichos reais e números de mercado, quando aparecerem), anota a fonte e só então libera o texto. Use antes de escrever ou mudar o bestiário (UX-25), os cartões de primeiro encontro (UX-24), os créditos ou o texto da loja, quando houver texto marcado para revisão, ou quando o Felipe perguntar "isso está certo?".'
---
# Conferir um fato

Hoje o TDFende não mostra fato do mundo real: o tema é fantasia realista baixa, e os nomes do catálogo (Rato a
Elefante) são de jogo. Esta rotina vale quando entrar texto que afirma algo real: o bestiário (UX-25: nome popular e
científico, fatos reais com fonte), os cartões de primeiro encontro (UX-24) ou um número de mercado na página da loja.

## Onde está o texto

- `docs/roadmap/fila.md` (UX-25) e a Fase 10 (UX-24), quando virarem código.
- `Assets/Resources/TDFende/creditos.txt` (nome real dos modelos, como "African Elephant").
- `docs/mercado.md` (interno, já com fontes).

## Fontes que valem

- Bicho: IUCN Red List, Animal Diversity Web (Universidade de Michigan) e GBIF para o nome científico.
- Número da Steam: SteamDB e a própria página da Steam, com a data.
Não valem: busca de IA, resumo de blog, Wikipédia sozinha (serve para achar a fonte primária), página sem autor nem data.

## Passos

1. Liste o que falta conferir: o que o pedido vai mostrar ao jogador e afirma algo real.
2. Para cada fato (data, medida, autor, regra, nome), abra a página da fonte com o WebFetch. Trecho de resultado de busca
   não é fonte. Para pesquisa longa, use a skill `deep-research`.
3. Se a fonte confirma: anote a fonte (URL e data de acesso) ao lado do texto, no arquivo de dados ou num comentário do catálogo; o FlowSim e o
   pre-commit não conferem isso, então cite também no commit.
4. Se a fonte desmente ou não acha: reescreva o texto com o que a fonte diz, ou tire o fato. Não deixe "aproximadamente" no lugar de conferir.
5. Texto que nasceu de IA continua sendo de IA depois de conferido: ele entra na declaração (skill `declaracao-ia`).
6. Commit com a lista do que foi conferido e as fontes.

## Armadilhas

- O `Name` do catálogo é texto exibido e nunca é traduzido (META-14): fato vai em campo próprio.
