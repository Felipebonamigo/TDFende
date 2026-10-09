# Escolha do tema (VIS-01)

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

**Estado em 09/10/2026: decidido pelo Felipe** (ver "Decisão" no fim): fantasia realista baixa com natureza,
régua Total War: Warhammer III, nenhuma verba para pacotes pagos, IA declarada na Steam. Continua provisório
até a fatia de beleza (VIS-27) provar ou reprovar o tema; se reprovar, o plano B é natureza/expedição.

Três agentes inventariaram um tema cada, só com metadados e miniaturas (nenhum modelo aberto, nada baixado
ou comprado, zero crédito do Meshy): [natureza/expedição](natureza.md) · [fantasia realista baixa](fantasia.md) ·
[medieval realista com kit pago](medieval.md). Dados brutos em `inventario-*.json`.

O critério do Felipe (ROADMAP): **beleza alcançável, coerência e fonte única**. Reuso só desempata.

## Notas dos agentes (1 a 5)

| | Natureza/expedição | Fantasia realista baixa | Medieval com kit pago |
|---|---|---|---|
| Beleza realista alcançável | 3,5 | 3,5 | 4 (kit médio) ou 3 (kit barato) |
| Coerência | 3 | 2,5 | 3 |
| Fonte única | 1,5 | 1,5 | 2 |
| Custo para fechar o essencial | ~US$ 95-130 | ~US$ 170 | ~US$ 115 (barato) a ~US$ 505 (médio) |
| Torres de Gelo, Fogo e Ar | montagem própria | existem só estilizadas | montagem própria |

**Leia com cuidado:** as notas saem de capas e descrições, e as diferenças entre os temas (menos de 1 ponto)
estão dentro do ruído. Nenhum tema ganha pelas notas. O que decide é o que vem abaixo.

## O que os três relatórios dizem em comum

1. **Chão, rochas, céu e props não decidem nada.** Poly Haven e ambientCG (CC0) cobrem os três temas no mesmo
   nível (nota ~4,5). As árvores deles pesam de 4 a 17 milhões de triângulos e pedem LOD (TEC-10).
2. **Torres elementais realistas prontas não existem em nenhum tema, grátis ou paga.** Gelo, Fogo e Ar viram
   composição própria (base de pedra mais prop, mais VFX). Morteiro e Canhão também, fora o canhão do Poly Haven.
3. **Tudo que é torre, fortaleza e acampamento de graça e de boa qualidade foi gerado por IA no Meshy.** Isso
   obriga a declarar IA na Steam (regra de jan/2026) e já provocou reação de parte do público em outro jogo do
   gênero (MKT-01). Pelas miniaturas, o estilo é "pintado", não fotorrealista.
4. **Bichos:** nenhuma fonte livre cobre os 9 animais com clipes completos. O pacote "Ultimate 3D Animal Pack"
   (WildMesh/AnimalMesh, RenderHub, US$ 39,99 segundo os agentes) promete 100+ animais com idle, walk, run,
   attack e death, e **cobre 8 dos 9** (a águia realista fica de fora). Em 09/10/2026 achei a listagem dele
   com **"Extended Use License (IP Restricted)" e "Editorial Use Only Restriction"**, e o contrato completo não
   foi lido. Uso editorial não vale para um jogo à venda. **Não comprar antes de ler o "3D Content Licensing
   Agreement" e, se preciso, perguntar ao autor.** Confirme também se o pacote usou IA (a página não diz).
5. Conferi eu mesmo: `cannon_01` (Poly Haven, CC0, 41,4 k triângulos, com rig) e `modular_fort_01` (CC0,
   28,2 k) existem, mas os dois são da coleção **Smuggler's Cove**, ou seja, tema pirata e colonial. Servem de
   peça, não de identidade.

## Minha leitura

Como as notas empatam, a pergunta útil é outra: **o que fazer com as 21 peças que já estão no jogo**
(18 torres do autor Karrades, a fortaleza e o acampamento).

- **Natureza/expedição** trocaria todas as torres (palanques e postos de guarda, que não existem em série) e
  mantém só chão e bichos. É o tema que mais descarta e o que mais depende de compra.
- **Medieval com kit pago** pode manter as torres de pedra, mas o ganho de beleza vem do kit de ~US$ 505, e o
  Gelo, o Fogo e o Ar continuam como montagem própria.
- **Fantasia realista baixa** é o único em que as 18 torres atuais (torres de pedra com musgo e ruína, telhado vermelho, braseiros de pedra) **já são o tema**, e onde Gelo, Fogo e Ar são naturais em vez de remendo. Não exige kit.

**Recomendação: Fantasia realista baixa**, no sentido de vale ou planalto de ruínas de pedra com musgo, bestas
realistas e luz de fim de tarde, mantendo as torres atuais.
- **Plano B:** Natureza/expedição, se o Portão Visual reprovar as torres do Meshy (o ROADMAP manda que o
  segundo tema vire fatia, nunca greybox).
- **Medieval com kit pago** só entra se os dois reprovarem, e com verba aprovada.
- **Por que ainda é provisório:** o ponto fraco é o mesmo de antes. Pelas miniaturas, as torres do Meshy
  destoam ao lado de arte fotoescaneada. Só a fatia de beleza, com o jogo rodando e a câmera teleobjetiva,
  mostra se isso incomoda de verdade.

## Bíblia de arte (rascunho, para o tema escolhido)

- **Lugar:** vale alto de planalto, com ruínas de pedra, pinheiros esparsos, rochas musgosas e neblina baixa.
- **Hora e luz:** fim de tarde, sol baixo e quente, céu parcialmente nublado. O HDRI atual
  (`kloofendal_48d_partly_cloudy_puresky`) é sul-africano: trocar por um de clima temperado ou montanha.
- **Materiais:** pedra úmida com musgo, madeira envelhecida, ferro oxidado, bronze, couro e pelo. Tudo PBR.
- **Paleta:** verdes escuros e ocres, pedra cinza-azulada. Acento quente (âmbar) e frio (azul-gelo) **só** nas
  torres de Fogo e Gelo e nos seus efeitos.
- **Proibido:** plástico (`plastic_crate`), tenda em cone, cor saturada de desenho animado, neon, qualquer coisa
  que lembre jogo de celular, mistura de escalas e de estilos de pintura.
- **Frase de venda** (do MKT-01): "Cada inimigo que você manda paga a sua defesa. Cada torre que você ergue empurra a
  sua fronteira."
- **Jogo-régua (escolhido pelo Felipe): *Total War: Warhammer III*** — fantasia realista numa câmera de
  estratégia: unidades legíveis de longe, terreno e vegetação com peso, luz com atmosfera. A fatia de beleza
  (VIS-27) é medida ao lado dele, em capturas do mesmo enquadramento. Eram sugestões também *Manor Lords* e
  *Frostpunk 2*; o Felipe pode acrescentar mais um a qualquer momento.

## Decisão do Felipe (09/10/2026)

1. **Tema: "fantasia com natureza".** Leitura adotada: **fantasia realista baixa num bioma natural** (vale, floresta e
   planalto de rochas musgosas, com ruínas de pedra), que é a opção recomendada acima. Não é o tema "natureza/
   expedição" (savana, safári, postos de guarda). *Se a leitura estiver errada, é só dizer.*
2. **Jogo-régua: Total War: Warhammer III.**
3. **Verba para pacotes pagos: nenhuma.** Consequências, escritas aqui para ninguém tropeçar nelas:
   - nada de comprar o pacote de animais (a dúvida do "Editorial Use Only" deixa de ser urgente), nem kit de
     castelo, águia ou acampamento;
   - a arte nova só vem de **CC0, CC BY e do que o Meshy da conta do Felipe gerar, sempre com pedido de aprovação
     antes** (MANUAL, seção 14);
   - **o elenco de bichos fica limitado**: sem o pacote, ele não passa de 9 a 11 espécies, e as grátis em CC BY são
     de autores diferentes, algumas com "problemas de rig" (`elenco.md`);
   - a camada privada (TEC-23) continua útil para o Mixamo e para o que o Felipe vier a ter, mas ninguém compra nada
     por ela agora.
4. **Declarar IA na Steam: sim.** As torres, a fortaleza e o acampamento da comunidade do Meshy, e o que o Felipe
   gerar lá, entram na declaração da página (META-03a). O campo `ia` do manifesto (`docs/licencas/manifesto.json`)
   é a fonte dessa declaração.

Ainda em aberto, e que não depende do tema: o plano do Meshy em 03/10 e 04/10, os 150 créditos sem registro, e a
verba da cápsula (marketing), que é outra pergunta.
