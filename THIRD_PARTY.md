# Conteúdo de terceiros

Texturas fotográficas em `Assets/Resources/TDFende/Textures/` (JPG guardado como `.bytes`
para o importador do Unity não recomprimir). Todas foram redimensionadas, recoloridas para
a paleta do jogo e, quando vinham no padrão DirectX, tiveram o verde da normal invertido.
Os materiais sem foto continuam procedurais (`Runtime/Art/ProcTex.cs`).

| Arquivo(s) | Origem | Licença |
|---|---|---|
| `Stone_*`, `StoneDark_*`, `StoneFrost_*` | *castle_brick_02_red* — Poly Haven (texturehaven.com), via repositório O3DE (`Gems/AtomContent/TestData/.../TextureHaven`) | CC0 (domínio público) |
| `Rock_*` | *Rock030* — ambientCG, via repositório O3DE (`TestData/Textures/cc0`) | CC0 |
| `Bark_*` | *bark1* — via repositório O3DE (`TestData/Textures/cc0`) | CC0 |
| `Wood_*`, `WoodDark_*`, `Iron_*`, `Leather_*`, `Cloth_*`, `ClothTeam_*`, `Dirt_*` | Open 3D Engine, `Gems/AtomContent/ReferenceMaterials` (WoodPlanks, WornMetal, Leather, BasicFabric, Ground) | Apache 2.0 ou MIT, à escolha (O3DE). Usado sob MIT — aviso abaixo |

CC0 não exige crédito; fica registrado para saber de onde veio cada coisa.

**Baixados pelo `Tools/BaixarArte.ps1`** (`Assets/Resources/TDFende/Cenario/`, `RoofTile_*`,
`Slate_*`): Poly Haven, CC0. Cada pasta leva o id do asset na Poly Haven.

**Bichos 3D** (`Assets/Resources/TDFende/Bichos/*.fbx` e `Bichos/Textures/`): Sketchfab, licença
**CC BY 4.0** (https://creativecommons.org/licenses/by/4.0/) — uso comercial liberado, **crédito
obrigatório** nos créditos do jogo. Alterações nossas, em todos: convertidos de GLB para FBX,
malha reduzida, texturas reduzidas para 1024 px, só as animações de andar/correr/parado/morrer,
ossos sem uso removidos (`Tools/ConverterBichos`). No cão, o colete "POLICE" foi tirado.

| Bicho no jogo | Modelo | Autor | Link |
|---|---|---|---|
| Rato | *Black Rat (Free download)* | Nestaeric | https://sketchfab.com/3d-models/black-rat-free-download-3db3acb4140d4de8bd62a171212bad9c |
| Cachorro | *Police Dog* | Chenchanchong | https://sketchfab.com/3d-models/police-dog-414a3970c7674bc0bedbab32350f56b6 |
| Lobo | *Animated Wolf Scene* | Roo (roo3d) | https://sketchfab.com/3d-models/animated-wolf-scene-5d55506494e5460eaadf04370e07cd5c |
| Águia | *Red-tailed Hawk - in Flight* (gavião-de-cauda-vermelha) | osuecampus (OSU.Multimedia) | https://sketchfab.com/3d-models/red-tailed-hawk-in-flight-4abcab5af9044a939fb5a55bff9e5000 |
| Urso | *Realistic Animated Bear 3D Model* | WildMesh 3D | https://sketchfab.com/3d-models/realistic-animated-bear-3d-model-bffc3c87d2d148ff8533e1cc8a11c9f1 |
| Rinoceronte | *Rhino Rebuilt* | kenchoo (a partir de *Gray Rhino*, AIUM2, CC BY) | https://sketchfab.com/3d-models/rhino-rebuilt-5626816de2734e748f68cb0c00151d3b |
| Elefante | *African Elephant* | jimmyho905 | https://sketchfab.com/3d-models/african-elephant-b960467b14f34cfc84feeb3361a21f54 |

Texto pronto para a tela de créditos (formato pedido pela CC BY):
"Black Rat" by Nestaeric; "Police Dog" by Chenchanchong; "Animated Wolf Scene" by Roo;
"Red-tailed Hawk - in Flight" by osuecampus; "Realistic Animated Bear 3D Model" by WildMesh 3D;
"Rhino Rebuilt" by kenchoo; "African Elephant" by jimmyho905 — all from sketchfab.com, licensed under CC BY 4.0, modified.

Cuidado anotado na escolha: a licença de cada um foi conferida na página do Sketchfab, e ficaram
de fora modelos com "personal use only" na descrição, versões NonCommercial e reenvios evidentes
de pacotes pagos ou de jogos.

**Javali e tigre: sem modelo baixado, de propósito.** Os que estavam aqui (*Boar_PBR_M*,
selvapandi95; *Tiger rebuilt*, kenchoo) saíram porque a origem não se comprova: o javali não tem
descrição e vem no padrão de pacote de jogo; o tigre usa o mesmo esqueleto (Biped do 3ds Max) e
as mesmas animações (Attack, Eat, Howl, Walk Fast...) de outros envios do mesmo tigre, sinal de
um pacote de terceiros. Em 04/10/2026 não havia no Sketchfab javali nem tigre realista, animado
e com autoria clara; o que tem autoria clara é estilizado (*Stylized Bengal Tiger*, Jungle Jim,
desenho animado) ou simples demais (*Wild Boar*, jbnotjeebe, 2017, low-poly).

**Javali e tigre agora** (`Bichos/javali.fbx`, `Bichos/tigre.fbx`): gerados no Meshy pela conta do
Felipe (imagem para 3D, 03/10/2026). Em 06/10/2026 ganharam esqueleto e a animação de andar no
próprio Meshy (malha reduzida a ~10 mil triângulos, Rig de quadrúpede, animação "Andando") e
passaram pelo `Tools/ConverterBichos/converte.py` como os outros bichos. Licença pelos termos do Meshy: no plano pago o modelo é de quem gerou; uma das gerações
daquela noite foi às 23:06, ainda no plano grátis (CC BY 4.0, crédito ao Meshy), e não dá para saber
qual dos dois arquivos é. **Na dúvida, pôr "3D models generated with Meshy (meshy.ai)" nos créditos.**

**Pacote "Realistic Animated Pack": fora do projeto.** Era a origem do lobo, urso e javali que o
`AnimalPackSplitter` gerava em `Bichos/Gerados/` (só no PC, nunca foi para o Git), e não tem licença
comprovada. Em 04/10/2026 o pacote, as texturas dele e os prefabs gerados foram para
`C:\Users\Felip\TDFende-quarentena`; lobo e urso voltaram aos modelos CC BY acima.

**Torres geradas no Meshy** (`Assets/Resources/TDFende/Torres/`): geradas por IA no Meshy
(https://www.meshy.ai), na conta do Felipe, pela API. A de Canhão saiu das tarefas
`01a104b8-2063-7663-baf7-42c6a6d83e9e` (modelo) e `01a104bb-05d0-72f1-af81-a4033f72ad91` (textura).
Alterações nossas: canhão do modelo removido, escala e corte em Shaft/Top, texturas em 1024 px com
leve correção de tom (`Tools/ConverterTorres`). Licença pelos termos do Meshy: em plano pago, o
asset é de quem gerou (uso comercial livre, sem crédito obrigatório); em plano grátis, sai em
CC BY 4.0 e precisa de crédito ao Meshy. **Conferir o plano da conta antes de publicar.**

**Torres em 3 estágios, fortaleza e acampamento da comunidade do Meshy**
(`Assets/Resources/TDFende/Torres/Torre_*_1..3`, `Fortaleza`, `Acampamento`): modelos prontos da
galeria pública, todos com licença **CC0** (domínio público: sem crédito obrigatório), baixados pelo
site em 04/10/2026. 18 são do autor **Karrades**; a fortaleza é de **Matson** e o acampamento de
**nathi.mashabane**. Link e autor de cada um em `Tools/ConverterTorres/comunidade.json`. Alterações
nossas: escala, corte em Shaft/Top (torres), texturas em 1024 px (`Tools/ConverterTorres`).

**Personagens do Mixamo** (`Assets/Resources/TDFende/Personagens/`): licença da Adobe —
uso em jogo liberado, redistribuição do arquivo cru não. Por isso não entram no Git.

**Ao publicar o jogo** (Steam), o aviso MIT abaixo precisa ir junto — nos créditos ou num
arquivo de licenças que acompanha o executável.

## Open 3D Engine — licença MIT

```
Copyright Contributors to the Open 3D Engine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```
