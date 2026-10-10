# Design — TEC-12: conteúdo por chave estável

[← cronograma](../../ROADMAP.md) · cartão: [Trilha S, TEC-12](../roadmap/trilha-s-simulacao-em-paralelo-headless-worktree-sem-unity.md)

**Estado:** escrito, **aguardando aprovação do Felipe** (tarefa "architectural": acrescenta campo em struct da Sim, muda o
formato do catálogo e a assinatura do replay). Nenhum código escrito. Esforço estimado: 1 a 2 sessões (a parte da Sim e
dos testes na nuvem; a parte da vista só se confere no `-captura` da sessão do PC). Vem **depois** do BUG-04 e do BUG-05, já feitos.

## 1. O problema, como o código está hoje

O índice de um bicho ou de uma torre (0, 1, 2...) é a identidade dele, e a **vista** o usa em 6 lugares escritos à mão:

| Lugar | O que decide pelo índice | Fallback quando o índice não está na lista |
|---|---|---|
| `ModelLib.Enemy(int)` | modelo 3D do bicho (`0 => Rato`, `2 => Lobo`...) | **o Cachorro**, em silêncio |
| `ModelLib.Tower(int)` | modelo 3D da torre | **o Canhão**, em silêncio |
| `ModelLib.Projectile(int)` | modelo do tiro | **a bola do Canhão**, em silêncio |
| `ModelLibTiers` (`switch (type)`) | enfeites dos níveis 2 a 6 de cada torre | nenhum enfeite, em silêncio |
| `Vfx` (`Muzzle`, `Impact`, `AddTrail`) | fumaça, explosão e rastro de cada torre | efeito genérico, em silêncio |
| `ProjectileView` (`_arc = TowerTypeId == 1`) | só o Morteiro sobe em arco | sem arco |

Mais **72 chamadas nos testes do FlowSim** com índice literal (`Send(2)`, `Build(x, y, 4)`...). A Sim em si **já é
orientada a dado** (nenhum `if (tipo == Gelo)`: gelo, fogo e voo vêm de campos), então o buraco está só na vista e nos testes.

Duas consequências concretas, que o cartão chama de "10º bicho nasce com o modelo errado":

1. Acrescentar o 10º bicho (ou a 7ª torre) **sem** lembrar de editar os 6 lugares faz ele nascer **com o modelo de outro**
   (o Cachorro) e sem erro nenhum.
2. A chave de casamento do `envios.txt` e do `torres.txt` é hoje o **nome exibido** (BUG-04). Se o tema renomear
   ("Canhão" vira "Balista", "Rato" vira "Ratazana"), todo arquivo de balanceamento já exportado é recusado, e o
   modelo continua a depender do índice. O BUG-04 já registrou isto como risco e apontou o TEC-12 como a cura.

## 2. Objetivo e o que fica de fora

**Objetivo:** cada bicho e cada torre tem uma **chave** estável (texto, não muda com o tema nem com a tradução). A vista
escolhe modelo, efeito e arco **por chave**, e o que não tem entrada vira um **aviso alto no log** (e não um modelo errado
em silêncio). Os testes passam a falar "Lobo", não "2".
**Fora do escopo:** mudar os ids inteiros (continuam sendo o índice no replay e na Sim); criar bicho ou torre novos;
renomear qualquer coisa que o jogador vê; mudar regra de jogo (o BalanceLab fica idêntico).

## 3. Design

### 3.1 Campo `Key` em `SendUnit` e `TowerType`

- `public string Key;` ao lado de `Name`. ASCII minúsculo, sem acento nem espaço: `rato`, `cachorro`, `lobo`, `javali`,
  `aguia`, `urso`, `tigre`, `rinoceronte`, `elefante`; `canhao`, `morteiro`, `gelo`, `sentinela`, `fogo`, `ar`.
- `Name` continua sendo **só o texto exibido** (HUD, log, `Torre_Canhão` do objeto de cena). O tema pode mudá-lo à vontade.
- Chave vazia ou repetida no catálogo de fábrica **reprova um teste** (e a ordem/tamanho da fábrica não muda).

### 3.2 Casamento do arquivo de balanceamento por `key=`

- `SerializeSends/Towers` passam a escrever `key=lobo;name=Lobo;cost=...` (a chave primeiro).
- O merge do BUG-04 casa por **chave**. Linha **sem** `key=` (arquivo exportado antes do TEC-12) casa pelo **nome**, como hoje,
  e o carregador diz no log "envios.txt sem chave: casado pelo nome; exporte de novo". Nenhum arquivo do Felipe quebra.
- `name=` no arquivo vira **informativo** quando há `key=`: o nome exibido sai do código/tema, não do arquivo (senão
  editar o arquivo renomearia o jogo). Isso é o único comportamento que muda para quem edita à mão; está na D2.
- A troca é uma linha em `CatalogMerge.Apply` (`nameOf` → `keyOf`), como o BUG-04 previu.

### 3.3 Tabela de vista por chave

- `ModelLib.Enemy(string key)`, `Tower(string key)`, `Projectile(string towerKey)` consultam um
  `Dictionary<string, Func<ModelDef>>` em vez de `switch` por inteiro. As sobrecargas por `int` ficam como atalho
  (`Enemy(int id) => Enemy(SendCatalog.Get(id).Key)`), para os chamadores (`LaneView`, `GameController`) mudarem pouco.
- `Vfx.Muzzle/Impact/AddTrail` e `ModelLibTiers` trocam `switch (int)` por `switch (string key)`.
- O `_arc` do `ProjectileView` vira uma tabela da vista por chave (`morteiro` sobe em arco). **Não** viro campo novo na Sim,
  porque é só desenho do tiro; se algum dia o arco afetar acerto, aí entra no `TowerType` (e na assinatura).
- **Chave sem entrada na tabela:** `Debug.LogWarning("[TDFende] sem modelo para 'lobo' (envio 2)")` **uma vez por chave**, e
  cai no modelo genérico de hoje (Cachorro/Canhão). O jogo segue, mas o log e o `-captura` mostram o buraco (o `-captura` já
  tem a lista de avisos; a chave ausente entra nela e faz a captura sair com código ≠ 0 se for um bicho do catálogo de fábrica).

### 3.4 Assinatura do replay (BUG-05)

`Key` entra nas listas de `SimSignature` (`Sends`, `Towers`) — o teste de guarda do BUG-05 exige. Efeito: replays do formato 2
gravados antes do TEC-12 passam a ser recusados pela assinatura (`sends`/`towers` diferem). **Sem novo formato de arquivo**
(o cabeçalho continua `tdfende-replay 2`): a assinatura faz a invalidação, e a mensagem já diz qual bloco. Os ids inteiros
nos comandos do replay **não mudam**.

### 3.5 Testes (no FlowSim, escritos antes)

1. `SendCatalog.IdOf("lobo")` e `TowerCatalog.IdOf("gelo")` (novos; devolvem -1 se não existe, `TryIdOf` também). Os 72 usos de
   índice literal nos testes passam a `IdOf(...)` onde o índice é a identidade (os que testam "o 1º" por posição ficam).
2. Chaves da fábrica: não vazias, únicas, ASCII minúsculo.
3. Merge por chave: arquivo com nomes renomeados (`key=lobo;name=Lupus`) casa e **não** renomeia; arquivo antigo (só `name=`)
   continua casando; chave desconhecida recusa com a lista de chaves.
4. Ida e volta do `Serialize` com `key=`; o `dump-catalogs` escreve a chave.
5. Assinatura: mudar só `Key` muda `sends`/`towers` (o teste por reflexão do BUG-05 já cobre assim que o campo existir).
6. **Cobertura da vista** (o ganho central), em C# puro no FlowSim: `ViewKeys.Enemy` e `ViewKeys.Tower` são as listas de chaves
   que a vista conhece (arquivo puro, sem Unity, usado pela vista); um teste exige que **toda chave do catálogo de fábrica esteja nelas**.
   Acrescentar o 10º bicho sem dar modelo a ele **reprova no FlowSim**, não só no log do jogo.

## 4. Riscos

- A parte da vista (ModelLib, Vfx, ProjectileView, ModelLibTiers) **só se prova no executável** (`-captura`, sessão do PC).
  Na nuvem confiro compilação contra o Unity 2021 e o teste de cobertura de chaves; o Felipe/PC confere que cada bicho e cada
  torre mantém o próprio modelo, com um `envios.txt` embaralhado e com um renomeado.
- Mudar `name=` do arquivo para informativo (D2) surpreende quem renomeava bicho pelo arquivo. Mitigação: o log avisa quando o
  nome do arquivo difere do nome do código.
- Toca arquivos de Unity já carregados de vista (`ModelLib` é grande). A troca é mecânica; o `-captura` antes e depois, comparado,
  é o critério.
- Chave é um segundo identificador a manter. Custo: uma string por linha de catálogo; o teste de cobertura impede esquecer.

## 5. Decisões pedidas ao Felipe

| | Pergunta | Recomendação |
|---|---|---|
| **D1** | Chave como campo novo (`Key`) em `SendUnit` e `TowerType` (A), ou só usar o nome atual como chave e adiar a separação (B)? | **A.** O tema já foi decidido e vai renomear; B repete o problema na primeira troca de nome. |
| **D2** | No arquivo de balanceamento, `key=` casa e `name=` fica só informativo (A), ou `name=` continua podendo renomear (B)? Arquivo antigo (só `name=`) funciona nos dois casos. | **A.** Senão o nome exibido do arquivo e o do tema brigam. |
| **D3** | Chave sem modelo na vista: aviso alto no log e modelo genérico (A), ou recusar subir o jogo (B)? | **A**, com o `-captura` e o teste do FlowSim fazendo a falha aparecer cedo. B derruba o jogo do Felipe por um desenho faltando. |
| **D4** | O arco do Morteiro fica numa tabela da vista (A) ou vira campo do `TowerType` (B)? | **A.** É desenho, não regra; vira B só se o arco passar a afetar o acerto. |
| **D5** | Replays do formato 2 já gravados passam a ser recusados pela assinatura (A, sem mudar o cabeçalho), ou subo para o formato 3 (B)? | **A.** A recusa já diz "os envios/torres mudaram"; um formato novo seria cerimônia. |
