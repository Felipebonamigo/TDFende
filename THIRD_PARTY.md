# Conteúdo de terceiros

<!-- GERADO por Tools/AuditaLicencas (--gera) a partir de docs/licencas/manifesto.json. Não edite à mão: mude o manifesto. -->

Tudo que o jogo usa e não foi escrito por nós, com origem, autor, licença e o que alteramos. Texturas fotográficas ficam em `Assets/Resources/TDFende/Textures/` (JPG guardado como `.bytes` para o importador do Unity não recomprimir). Os materiais sem foto continuam procedurais (`Runtime/Art/ProcTex.cs`).

CC0 não exige crédito; fica registrado para saber de onde veio cada coisa. O que exige crédito sai em `Assets/Resources/TDFende/creditos.txt`, que a tela de créditos mostra.

**Declaração de IA na Steam: sim** (decidido pelo Felipe em 09/10/2026). O campo `ia` de cada entrada é a fonte: `própria` (gerado por nós no Meshy) e `terceiro` (gerado por IA por outra pessoa) entram na declaração da página (META-03a).

**Legenda.** *IA*: `própria` = gerado por nós com IA; `terceiro` = gerado por IA por outra pessoa; `não` = sem declaração de IA. *Estado*: `pendente` = falta conferir algo antes de publicar (veja a nota do grupo).

## Texturas fotográficas e materiais

| Obra | Autor | Licença | IA | Estado |
|---|---|---|---|---|
| [castle_brick_02_red (Stone, StoneDark, StoneFrost)](https://polyhaven.com/a/castle_brick_02_red) | Poly Haven | CC0 | não | ok |
| [Rock030](https://ambientcg.com/view?id=Rock030) | ambientCG | CC0 | não | ok |
| bark1 | não identificado (pasta cc0 do O3DE) | CC0 | não | ok |
| [Madeira, ferro, couro, tecido e terra (O3DE ReferenceMaterials)](https://github.com/o3de/o3de) | Contributors to the Open 3D Engine | MIT | não | ok |
| [RoofTile e Slate](https://polyhaven.com/textures) | Poly Haven | CC0 | não | ok |

- **castle_brick_02_red (Stone, StoneDark, StoneFrost)**. Origem: Poly Haven, via repositório O3DE (Gems/AtomContent/TestData/.../TextureHaven). Alterações nossas: redimensionadas, recoloridas para a paleta do jogo e com o verde da normal invertido (padrão DirectX).
- **Rock030**. Origem: ambientCG, via repositório O3DE (TestData/Textures/cc0). Alterações nossas: redimensionada e recolorida para a paleta do jogo.
- **bark1**. Origem: repositório O3DE (TestData/Textures/cc0). Alterações nossas: redimensionada e recolorida para a paleta do jogo. O autor original não consta no repositório do O3DE; a pasta se chama cc0.
- **Madeira, ferro, couro, tecido e terra (O3DE ReferenceMaterials)**. Origem: Open 3D Engine, Gems/AtomContent/ReferenceMaterials (WoodPlanks, WornMetal, Leather, BasicFabric, Ground). Alterações nossas: redimensionadas e recoloridas para a paleta do jogo. O O3DE oferece MIT ou Apache 2.0, à escolha; usado sob MIT (aviso completo no fim deste arquivo).
- **RoofTile e Slate**. Origem: Poly Haven, baixadas por Tools/BaixarArte.ps1. Alterações nossas: redimensionadas e recoloridas para a paleta do jogo.

<details><summary>Arquivos deste grupo</summary>

- `tex-stone`: `Assets/Resources/TDFende/Textures/Stone_*`, `Assets/Resources/TDFende/Textures/StoneDark_*`, `Assets/Resources/TDFende/Textures/StoneFrost_*`
- `tex-rock`: `Assets/Resources/TDFende/Textures/Rock_*`
- `tex-bark`: `Assets/Resources/TDFende/Textures/Bark_*`
- `tex-o3de`: `Assets/Resources/TDFende/Textures/Wood_*`, `Assets/Resources/TDFende/Textures/WoodDark_*`, `Assets/Resources/TDFende/Textures/Iron_*`, `Assets/Resources/TDFende/Textures/Leather_*`, `Assets/Resources/TDFende/Textures/Cloth_*`, `Assets/Resources/TDFende/Textures/ClothTeam_*`, `Assets/Resources/TDFende/Textures/Dirt_*`
- `tex-polyhaven`: `Assets/Resources/TDFende/Textures/RoofTile_*`, `Assets/Resources/TDFende/Textures/Slate_*`

</details>

## Cenário, chão, grama e céu (Poly Haven)

| Obra | Autor | Licença | IA | Estado |
|---|---|---|---|---|
| [Barris, caixas, pedras, tocos e troncos](https://polyhaven.com/models) | Poly Haven (vários artistas) | CC0 | não | ok |
| [grass_medium_01/02, leafy_grass, aerial_grass_rock e céu kloofendal_48d_partly_cloudy_puresky](https://polyhaven.com) | Poly Haven (vários artistas) | CC0 | não | ok |

- **Barris, caixas, pedras, tocos e troncos**. Origem: Poly Haven, baixados por Tools/BaixarArte.ps1 (cada pasta leva o id do asset). Alterações nossas: usados como baixados. plastic_crate_01 e _02 saem na VIS-04 (denunciam maquete).
- **grass_medium_01/02, leafy_grass, aerial_grass_rock e céu kloofendal_48d_partly_cloudy_puresky**. Origem: Poly Haven, baixados por Tools/BaixarArte.ps1. Alterações nossas: usados como baixados.

<details><summary>Arquivos deste grupo</summary>

- `cenario-polyhaven`: `Assets/Resources/TDFende/Cenario/**`
- `chao-grama-ceu`: `Assets/Resources/Art/**`

</details>

## Gerado pelo próprio projeto

| Obra | Autor | Licença | IA | Estado |
|---|---|---|---|---|
| ShaderKeep (materiais que seguram os shaders no build) | TDFende | Propria | não | ok |
| creditos.txt | TDFende | Propria | não | ok |
| LEIA-ME.txt das pastas de arte | TDFende | Propria | não | ok |

- **ShaderKeep (materiais que seguram os shaders no build), creditos.txt, LEIA-ME.txt das pastas de arte**. Origem: gerado pelo BuildJogo a cada build. Alterações nossas: —.

<details><summary>Arquivos deste grupo</summary>

- `shaderkeep`: `Assets/Resources/TDFende/ShaderKeep/**`
- `creditos`: `Assets/Resources/TDFende/creditos.txt`
- `leiame`: `Assets/Resources/TDFende/**/LEIA-ME.txt`

</details>

## Bichos 3D (Sketchfab, CC BY 4.0)

| Obra | Autor | Licença | IA | Estado |
|---|---|---|---|---|
| [Black Rat (Free download)](https://sketchfab.com/3d-models/black-rat-free-download-3db3acb4140d4de8bd62a171212bad9c) | Nestaeric | CC-BY-4.0 | não | ok |
| [Police Dog](https://sketchfab.com/3d-models/police-dog-414a3970c7674bc0bedbab32350f56b6) | Chenchanchong | CC-BY-4.0 | não | ok |
| [Animated Wolf Scene](https://sketchfab.com/3d-models/animated-wolf-scene-5d55506494e5460eaadf04370e07cd5c) | Roo (roo3d) | CC-BY-4.0 | não | ok |
| [Red-tailed Hawk - in Flight (gavião-de-cauda-vermelha, usado como a águia)](https://sketchfab.com/3d-models/red-tailed-hawk-in-flight-4abcab5af9044a939fb5a55bff9e5000) | osuecampus (OSU.Multimedia) | CC-BY-4.0 | não | ok |
| [Realistic Animated Bear 3D Model](https://sketchfab.com/3d-models/realistic-animated-bear-3d-model-bffc3c87d2d148ff8533e1cc8a11c9f1) | WildMesh 3D | CC-BY-4.0 | não | ok |
| [Rhino Rebuilt](https://sketchfab.com/3d-models/rhino-rebuilt-5626816de2734e748f68cb0c00151d3b) | kenchoo (a partir de "Gray Rhino", AIUM2, CC BY) | CC-BY-4.0 | não | ok |
| [African Elephant](https://sketchfab.com/3d-models/african-elephant-b960467b14f34cfc84feeb3361a21f54) | jimmyho905 | CC-BY-4.0 | não | ok |

- **Black Rat (Free download)**. Origem: Sketchfab. Alterações nossas: convertido de GLB para FBX, malha reduzida, texturas reduzidas para 1024 px, só as animações de andar/correr/parado/morrer, ossos sem uso removidos (Tools/ConverterBichos). Licença conferida na página do Sketchfab. Sem sinal de IA nas tags e na descrição (API, 09/10/2026); o autor não declara.
- **Police Dog**. Origem: Sketchfab. Alterações nossas: convertido de GLB para FBX, malha reduzida, texturas reduzidas para 1024 px, só as animações de andar/correr/parado/morrer, ossos sem uso removidos (Tools/ConverterBichos); o colete "POLICE" foi tirado.
- **5 obras da tabela acima**. Origem: Sketchfab. Alterações nossas: convertido de GLB para FBX, malha reduzida, texturas reduzidas para 1024 px, só as animações de andar/correr/parado/morrer, ossos sem uso removidos (Tools/ConverterBichos).

<details><summary>Arquivos deste grupo</summary>

- `bicho-rato`: `Assets/Resources/TDFende/Bichos/rato.fbx`, `Assets/Resources/TDFende/Bichos/Textures/rato_*`
- `bicho-cachorro`: `Assets/Resources/TDFende/Bichos/cachorro.fbx`, `Assets/Resources/TDFende/Bichos/Textures/cachorro_*`
- `bicho-lobo`: `Assets/Resources/TDFende/Bichos/lobo.fbx`, `Assets/Resources/TDFende/Bichos/Textures/lobo_*`
- `bicho-aguia`: `Assets/Resources/TDFende/Bichos/aguia.fbx`, `Assets/Resources/TDFende/Bichos/Textures/aguia_*`
- `bicho-urso`: `Assets/Resources/TDFende/Bichos/urso.fbx`, `Assets/Resources/TDFende/Bichos/Textures/urso_*`
- `bicho-rinoceronte`: `Assets/Resources/TDFende/Bichos/rinoceronte.fbx`, `Assets/Resources/TDFende/Bichos/Textures/rinoceronte_*`
- `bicho-elefante`: `Assets/Resources/TDFende/Bichos/elefante.fbx`, `Assets/Resources/TDFende/Bichos/Textures/elefante_*`

</details>

## Modelos gerados no Meshy pela conta do Felipe

| Obra | Autor | Licença | IA | Plano Meshy | Estado |
|---|---|---|---|---|---|
| Javali | Felipe Bonamigo (conta no Meshy) | Meshy-conta-Felipe | própria | desconhecido | pendente |
| Tigre | Felipe Bonamigo (conta no Meshy) | Meshy-conta-Felipe | própria | desconhecido | pendente |
| Torre de Canhão (primeira versão, gerada por texto) | Felipe Bonamigo (conta no Meshy) | Meshy-conta-Felipe | própria | desconhecido | pendente |

- **Javali, Tigre**. Origem: Meshy (imagem para 3D em 03/10/2026; rig de quadrúpede e animação "Andando" no próprio Meshy em 06/10/2026). Alterações nossas: malha reduzida a ~10 mil triângulos no Meshy, convertido por Tools/ConverterBichos. **Pendente:** Conferir no site do Meshy o plano da conta na data da geração. Plano pago: o modelo é do Felipe, sem crédito obrigatório. Plano grátis: CC BY 4.0, com crédito ao Meshy. Na dúvida o crédito fica nos créditos do jogo. Uma das gerações de 03/10 foi às 23:06, possivelmente ainda no plano grátis.
- **Torre de Canhão (primeira versão, gerada por texto)**. Origem: Meshy (texto para 3D, tarefas 01a104b8-2063-7663-baf7-42c6a6d83e9e e 01a104bb-05d0-72f1-af81-a4033f72ad91, 04/10/2026). Alterações nossas: canhão do modelo removido, escala e corte em Shaft/Top, texturas em 1024 px com correção de tom (Tools/ConverterTorres). **Pendente:** Conferir no site do Meshy o plano da conta na data da geração. Plano pago: o modelo é do Felipe, sem crédito obrigatório. Plano grátis: CC BY 4.0, com crédito ao Meshy. Na dúvida o crédito fica nos créditos do jogo. Sai do jogo na VIS-04 (os 3 estágios da comunidade a substituem).

<details><summary>Arquivos deste grupo</summary>

- `bicho-javali`: `Assets/Resources/TDFende/Bichos/javali.fbx`, `Assets/Resources/TDFende/Bichos/Textures/javali_*`
- `bicho-tigre`: `Assets/Resources/TDFende/Bichos/tigre.fbx`, `Assets/Resources/TDFende/Bichos/Textures/tigre_*`
- `torre-canhao-meshy`: `Assets/Resources/TDFende/Torres/Torre_Canhao.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Canhao_cor.bytes`, `Assets/Resources/TDFende/Torres/Textures/Torre_Canhao_normal.bytes`

</details>

## Torres, fortaleza e acampamento (galeria da comunidade do Meshy, CC0)

| Obra | Autor | Licença | IA | Plano Meshy | Estado |
|---|---|---|---|---|---|
| [Torre de Canhão, estágio 1](https://www.meshy.ai/posts/0196d11d-3599-7b70-8f8e-e6a28d7ff2fb) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Canhão, estágio 2](https://www.meshy.ai/posts/0196d123-4168-7b70-af30-d236c36a97e1) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Canhão, estágio 3](https://www.meshy.ai/posts/0196d120-afca-7245-a903-93cf8eea5859) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Morteiro, estágio 1](https://www.meshy.ai/posts/0196eb6f-bf01-7694-9256-8af08f04a19b) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Morteiro, estágio 2](https://www.meshy.ai/posts/0196eb27-c03d-72b9-b24a-edbab7772722) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Morteiro, estágio 3](https://www.meshy.ai/posts/0196eb28-83fb-768d-8d73-600b9507239e) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Gelo, estágio 1](https://www.meshy.ai/posts/0195b5e8-ef56-7797-b75a-c95e4097097e) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Gelo, estágio 2](https://www.meshy.ai/posts/0195b5e8-80e5-7c4f-9d05-485670cf70fb) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Gelo, estágio 3](https://www.meshy.ai/posts/0198cf42-cdd1-7e79-915d-4f9361b12a22) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Sentinela, estágio 1](https://www.meshy.ai/posts/0195b183-5b65-7bd6-95b9-f1ced30c476a) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Sentinela, estágio 2](https://www.meshy.ai/posts/0195b184-d3a5-7bd6-a84a-7276babc1525) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Sentinela, estágio 3](https://www.meshy.ai/posts/01993651-8ab0-7422-a4d7-3f816ca68d2a) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Fogo, estágio 1](https://www.meshy.ai/posts/01968ebf-2658-7143-ae45-7b0860f1306f) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Fogo, estágio 2](https://www.meshy.ai/posts/01968ec1-1603-7143-a89e-3e8e35a754dc) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Fogo, estágio 3](https://www.meshy.ai/posts/01968ec2-efd3-7411-b372-cddccfc159e3) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Ar, estágio 1](https://www.meshy.ai/posts/0196d669-1c66-7c06-b4c1-d6dc21d8e954) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Ar, estágio 2](https://www.meshy.ai/posts/0196f062-cbe6-76c3-9d8d-5f1c5942e127) | Karrades | CC0 | terceiro | comunidade | ok |
| [Torre de Ar, estágio 3](https://www.meshy.ai/posts/0196d66d-75b9-77dc-95fb-c9654be19573) | Karrades | CC0 | terceiro | comunidade | ok |
| [Fortaleza](https://www.meshy.ai/posts/01964efc-a77f-73fd-b7b1-bbc7bc5c3d91) | Matson | CC0 | terceiro | comunidade | ok |
| [Acampamento](https://www.meshy.ai/posts/019741d2-5909-7308-9462-a123fe58f217) | nathi.mashabane | CC0 | terceiro | comunidade | ok |

- **20 obras da tabela acima**. Origem: Galeria pública da comunidade do Meshy, baixado pelo site em 04/10/2026. Alterações nossas: escala, corte em Shaft/Top (torres) e texturas em 1024 px (Tools/ConverterTorres). Publicar na galeria do Meshy implica CC0: o diálogo de publicação diz "Models: CC0 — free for anyone to use, no credit needed". Os modelos são gerados por IA por terceiros; o jogo precisa declarar isso na página da Steam.

<details><summary>Arquivos deste grupo</summary>

- `comunidade-torre_canhao_1`: `Assets/Resources/TDFende/Torres/Torre_Canhao_1.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Canhao_1_*`
- `comunidade-torre_canhao_2`: `Assets/Resources/TDFende/Torres/Torre_Canhao_2.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Canhao_2_*`
- `comunidade-torre_canhao_3`: `Assets/Resources/TDFende/Torres/Torre_Canhao_3.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Canhao_3_*`
- `comunidade-torre_morteiro_1`: `Assets/Resources/TDFende/Torres/Torre_Morteiro_1.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Morteiro_1_*`
- `comunidade-torre_morteiro_2`: `Assets/Resources/TDFende/Torres/Torre_Morteiro_2.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Morteiro_2_*`
- `comunidade-torre_morteiro_3`: `Assets/Resources/TDFende/Torres/Torre_Morteiro_3.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Morteiro_3_*`
- `comunidade-torre_gelo_1`: `Assets/Resources/TDFende/Torres/Torre_Gelo_1.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Gelo_1_*`
- `comunidade-torre_gelo_2`: `Assets/Resources/TDFende/Torres/Torre_Gelo_2.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Gelo_2_*`
- `comunidade-torre_gelo_3`: `Assets/Resources/TDFende/Torres/Torre_Gelo_3.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Gelo_3_*`
- `comunidade-torre_sentinela_1`: `Assets/Resources/TDFende/Torres/Torre_Sentinela_1.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Sentinela_1_*`
- `comunidade-torre_sentinela_2`: `Assets/Resources/TDFende/Torres/Torre_Sentinela_2.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Sentinela_2_*`
- `comunidade-torre_sentinela_3`: `Assets/Resources/TDFende/Torres/Torre_Sentinela_3.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Sentinela_3_*`
- `comunidade-torre_fogo_1`: `Assets/Resources/TDFende/Torres/Torre_Fogo_1.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Fogo_1_*`
- `comunidade-torre_fogo_2`: `Assets/Resources/TDFende/Torres/Torre_Fogo_2.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Fogo_2_*`
- `comunidade-torre_fogo_3`: `Assets/Resources/TDFende/Torres/Torre_Fogo_3.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Fogo_3_*`
- `comunidade-torre_ar_1`: `Assets/Resources/TDFende/Torres/Torre_Ar_1.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Ar_1_*`
- `comunidade-torre_ar_2`: `Assets/Resources/TDFende/Torres/Torre_Ar_2.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Ar_2_*`
- `comunidade-torre_ar_3`: `Assets/Resources/TDFende/Torres/Torre_Ar_3.fbx`, `Assets/Resources/TDFende/Torres/Textures/Torre_Ar_3_*`
- `comunidade-fortaleza`: `Assets/Resources/TDFende/Torres/Fortaleza.fbx`, `Assets/Resources/TDFende/Torres/Textures/Fortaleza_*`
- `comunidade-acampamento`: `Assets/Resources/TDFende/Torres/Acampamento.fbx`, `Assets/Resources/TDFende/Torres/Textures/Acampamento_*`

</details>

## Personagens (Mixamo, só no PC)

| Obra | Autor | Licença | IA | Estado |
|---|---|---|---|---|
| [Personagens e animações do Mixamo](https://www.mixamo.com) | Adobe Systems (Mixamo) | Adobe-Mixamo | não | ok |

- **Personagens e animações do Mixamo**. Origem: Mixamo (Adobe), baixados pelo Felipe. Alterações nossas: usados como baixados. A licença da Adobe libera o uso em jogo, mas não a redistribuição do arquivo cru: ficam só no PC, nunca no git público.

<details><summary>Arquivos deste grupo</summary>

- `mixamo`: `Assets/Resources/TDFende/Personagens/**`

</details>

**Ao publicar o jogo** (Steam), o aviso MIT abaixo precisa ir junto — nos créditos ou num arquivo de licenças que acompanha o executável.

## Open 3D Engine — licença MIT

```
Copyright Contributors to the Open 3D Engine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```
