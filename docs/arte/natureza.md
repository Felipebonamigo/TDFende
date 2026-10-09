> **Pesquisa de um agente em 09/10/2026** (VIS-01), só metadados e miniaturas: nada foi baixado nem comprado, nenhum crédito do Meshy foi gasto. As notas de 1 a 5 vêm de capas e descrições, **nenhum modelo foi aberto**. Dados brutos: [`inventario-natureza.json`](inventario-natureza.json). Análise e recomendação: [`tema.md`](tema.md).

# Tema "Natureza/expedição" — inventário de arte (2026-10-09)

Dados completos em `inventario.json` (118 candidatos + 6 pacotes pagos). Miniaturas de conferência em `thumbs/`.
Método: só metadados e miniaturas; nada baixado; nenhum POST no Meshy (zero créditos).
Hosts: Sketchfab, Meshy (GET), Poly Haven, ambientCG responderam. Patreon e Fab deram 403, então não li o preço do Patreon nem a página da Fab.

## (a) O que o tema JÁ cobre bem de graça
- **Terreno, rochas, vegetação, props, céu: forte.** Poly Haven é CC0 e de nível AAA. Tem coleção Namaqualand (savana seca sul-africana: kokerboom, searsia, rooibos, boulders, penhascos) e Pine Forest / Verdant Trail (temperado). 867 texturas PBR e 997 HDRIs (Kloofendal, Kiara, Spruit, Rooitou, Satara, Dikhololo...). ambientCG (CC0) complementa com grama, solo, rocha e musgo.
- **Props:** barris, caixotes, escada, balde, cesto, binóculo, holofote, lanterna de madeira, fogueira de pedra, maçarico, caixas militares. Todos CC0 do Poly Haven e consistentes entre si.
- **Canhão:** `cannon_01` (CC0, 41k tris, rigged) é ótimo.
- **Fortaleza:** `modular_fort_01` (CC0, 28k tris, kit modular de pedra) mais portão de ferro e píer/passarela de madeira.
- **Bichos (melhor achado):** WildMesh 3D / AnimalMesh 3D publicam no Sketchfab uma série realista e coerente com dezenas de clipes. Gratuitos em CC-BY (versão antiga, autor avisa de "problemas de rig"):
  - Urso (7,5k tris, 81 clipes), Javali (8,5k, 11 clipes), Alce, Corça, Burro, 2 Raposas (50 a 117 clipes).
  - As demos "Realistic (DEMO FREE)" de lobo, hiena, gnu, búfalo, leoa, facócero e raposa parecem ótimas nas miniaturas, mas são **CC BY-NC**: servem para avaliar, não para o jogo.
  - Rato: Black Rat (Nestaeric, 31k tris, 12 clipes, CC-BY).
- **Tendas:**
  - Safari tent scan (Glamx, CC-BY, 573k faces). É a única tenda safári real que achei, com 0 likes.
  - Cabana rondavel africana (scan, CC-BY).
  - Série theanh75 no Meshy: Camping Tent, Command Tent, fogueira, cerca de madeira, torre de palha.

## (b) Lacunas
1. **Torres de vigia realistas em 3 estágios do mesmo autor, grátis: não existe.** O que há grátis:
   - **Meshy, série Watchtower T1/T2/T3 (AZURE_LANTERN_STUDIO).** Três estágios reais, mas é fantasia pintada (telhado azul, tag 'scifi' no T3), com ~1M tris sem retopologia.
   - **Sketchfab, Helyeouka "lvl 1-3" e nico_hartl.** São estilizados low-poly, o que contradiz o "realista".
   - **Realistas soltos:** Old Hunting Tower, Timber Lookout, Mehdi Shahsavan. Peças isoladas, sem série.
2. **Morteiro, Gelo e Ar: sem arte pronta e realista.** Só existem versões "fantasia" no Meshy (ice tower, windmill). Será preciso compor: base de torre + prop.
   - Gelo: tanque criogênico ou bloco de gelo + VFX.
   - Ar: cata-vento ou moinho de bomba d'água + hélice animada por script.
   - Morteiro: tubo de ferro sobre cavalete, montado a partir de peças.
3. **Águia realista animada: não existe grátis.** O pacote WildMesh só tem "Eagle" e "Buzzard" estilizadas. Os modelos grátis do Sketchfab têm 3 a 4k tris e 1 clipe.
4. **Acácia, baobá e zebra:** o Poly Haven não tem acácia nem baobá. A zebra e a girafa não estão na lista realista do WildMesh.
5. **Clipes por bicho (parado/andar/correr/golpe/morte/deitar):** não consegui verificar nomes de clipes sem baixar. O RenderHub diz que cada modelo do pacote pago traz idle, walk, run, attack, death. "Deitar" não confirmado.
6. **Árvores e capim do Poly Haven são pesadíssimos** (4,6M a 17M tris; grama de 1,6M). Precisam de LODs, impostores ou re-bake. Não são "drag and drop" no Unity.

## (c) Custo estimado para fechar as lacunas (valores conferidos ou citados em busca; não comprei nada)
| Item | Preço | Fecha |
|---|---|---|
| Ultimate Animal Pack WildMesh+AnimalMesh (RenderHub, licença estendida) | **US$ 39,99** (Patreon: valores conflitantes, não confirmado) | 8 dos 9 bichos + extras: lobo, urso, javali, tigre, rinoceronte, elefante, rato, cachorro, hiena, gnu, búfalo, leão, facócero, cervo, alce, raposa, corvo |
| Ultimate Medieval Wooden Fort and Watchtower Pack (Fab, abada studio) | a partir de **US$ 14,99** | torres de vigia/palanque de madeira e muralhas modulares (não li a página) |
| Base Camp Asset Pack (DEEZL, itch.io) | **€ 19,99** | 2 tendas, 2 torres, posto, barris, caixas, sacos de areia; PBR com mapas URP e HDRP. Estilo militar moderno |
| Águia (Unity Asset Store: White-tailed 4K ou Golden Eagle) | ~US$ 20 (Golden Eagle, cache), White-tailed não verificado | águia |
| Vegetação savana (African Bushveld Trees) | preço não verificado; só Built-in | acácia/baobá, exige adaptar shader para URP |
| Opcional: African Big Pack (4toon) | ~US$ 160 (dado de blog, pode estar defasado) | zebra, girafa, hipopótamo, guepardo; animação incompleta |

Fecha o essencial por **~US$ 95–130**, sem vegetação africana e sem o Big Pack. Morteiro, Gelo e Ar continuariam como montagem própria.

## (d) Risco de licença e de IA
- **Meshy (todos os candidatos "Meshy" no JSON): GERADOS POR IA.**
  - O campo `license` do showcase diz cc0. Não confirmei os termos do Meshy, nem se o usuário podia relicenciar, nem a Steam Content Survey de IA (a Steam exige declarar IA generativa pré-gerada).
  - Todos têm de 400k a 3M tris sem retopologia. A maioria sem mapas PBR separados (`isGeneratePBRMaps=false`), e os "avocado" e "blueberry" saem pesadíssimos.
  - Há também Pigcraft no Sketchfab ("AI-assisted modeling", declarado).
- **Sketchfab:** CC-BY exige atribuição na tela de créditos. Rejeitei NC, ND e reuploads (planeta-elefante = modelos ripados de Zoo Tycoon 2). Restam suspeitas: malha de 32.418 faces do rinoceronte reenviada por 2 contas, e Shiba de 33.384 faces idem. Tiger rebuilt é remix. Autoria incerta.
- **WildMesh:** as demos NC não podem entrar no jogo. O pacote pago tem licença estendida no RenderHub com uma tag "Editorial use only restriction" que preciso ler no EULA. A página não declara se há IA no processo.
- **Poly Haven e ambientCG:** CC0, sem risco.

## (e) Coerência (fonte única possível?)
- **Não existe fonte única.** O mais próximo de uma estética fechada vem de dois blocos:
  - **Bloco natureza e bichos:** Poly Haven (fotorrealismo, PBR 4K a 8K) + WildMesh (realismo mid-poly, pelagem boa nas miniaturas). Combina razoavelmente, desde que se aplique um grading único.
  - **Bloco construções:** nada gratuito iguala o nível do Poly Haven. As torres do Meshy são pintadas à mão, com leve ar de "mobile/fantasia", e ficam destoantes ao lado dos fotoescaneados.
- Teleobjetiva a ~50°, com bichos pequenos em tela, ajuda a esconder diferenças de detalhe. Texturas e grading unificados no Unity (um único LUT, mesma paleta de luz do HDRI) resolvem boa parte.
- Como mitigação, a fortaleza `modular_fort_01` + canhão `cannon_01` + props Poly Haven poderiam fazer o "kit de pedra e madeira rústica" 100% Poly Haven. Só falta o palanque de madeira, comprável por ~US$ 15.

## (f) Veredito honesto (1 a 5)
- **Beleza realista alcançável: 3,5.** Chão, pedras, plantas, props e bichos chegam a 4–4,5. As torres elementais (Gelo, Ar, Morteiro) ficam em ~2,5 e puxam a média para baixo.
- **Coerência: 3.** Natureza e bichos formam um bloco (4). Torres vindas do Meshy ou de outros autores quebram a unidade.
- **Fonte única: 1,5.** Nenhuma fonte cobre torres, base, tendas, props, vegetação e bichos. A combinação Poly Haven (ambiente, props, canhão, fortaleza) + WildMesh (bichos, pago) cobre ~75%.

Não verifiquei, por não baixar nada: formatos reais e rigs dos modelos, nomes dos clipes, qualidade real das texturas, a existência de modelo "deitar" e o EULA completo do pacote WildMesh.
