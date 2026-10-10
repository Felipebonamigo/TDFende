# Design — BUG-04: catálogo de envios completa por nome e valida o id

[← cronograma](../../ROADMAP.md) · cartão: [Trilha S, BUG-04](../roadmap/trilha-s-simulacao-em-paralelo-headless-worktree-sem-unity.md)

**Estado:** escrito e **aprovado pelo Felipe em 09/10/2026, com D1 a D4 como recomendado** (tarefa "architectural": mexe no
formato do catálogo). Implementado em 10/10/2026 (c9835ef). Esforço estimado: meia sessão. Vem **antes** do BUG-05.

## 1. O problema, como o código está hoje

1. `SendCatalog.LoadFrom` troca a lista inteira: `All = parsed` (`SendCatalog.cs`). Um `envios.txt` exportado quando
   havia 9 bichos e lido quando o jogo tem 10 **apaga o décimo** em silêncio. É por onde o primeiro bicho novo ou a
   primeira elite sumiria do jogo do Felipe sem erro nenhum.
2. **O cartão pede "completar por nome como o `TowerCatalog` já faz", mas o `TowerCatalog` não faz o que precisamos.** O
   `TowerCatalog.LoadFrom` completa as torres que faltam, mas mantém **a ordem do arquivo** e acrescenta as que
   faltam no fim. O índice de uma torre ou de um envio é a identidade dele (modelo 3D em `ModelLib`, efeitos em `Vfx`,
   linha do replay, tecla do HUD). Um arquivo com duas linhas trocadas de lugar **troca o Gelo pelo Fogo em silêncio**,
   hoje, nas torres.
3. O parser (`CatalogJson`) preenche campo ausente com um **valor genérico** (custo 10, vida 40, velocidade 2,2...) e não
   com o da unidade de fábrica de mesmo nome. O comentário do arquivo promete "campo que faltar usa o padrão do código",
   o que na prática só é verdade para um campo recém-criado, não para um arquivo editado à mão que omite `hp`.
4. `SendCatalog.Get(id)` é `All[id]`: id inválido estoura com `IndexOutOfRangeException` sem dizer qual id nem quantos
   envios existem. (O `TowerCatalog.Get`, ao contrário, **engole** o id inválido e devolve a torre 0. Isso é um defeito
   parecido, tratado como dívida na seção 7.)
5. O `CanAfford` e o `TrySend` do `LaneSim` chamam `Get(sendId)` direto. O `Replay.TryParse` já recusa id fora do
   catálogo, e o HUD só gera ids válidos; hoje um id ruim só chega por bug.

**Visto rodando no código de hoje** (programa descartável fora do repositório, com os mesmos arquivos da Sim):

| Teste | Resultado |
|---|---|
| `envios.txt` com 8 dos 9 bichos (sem o Elefante) | `LoadFrom` devolve `true`, `Count` cai para **8**, o Elefante deixa de existir |
| `torres.txt` com o Gelo na 1ª linha e o Canhão na 2ª | `TowerCatalog.Get(0)` passa a ser **"Gelo"** (de fábrica é "Canhão") |
| linha `name=Lobo;cost=40` | vida **40** e velocidade **2,2** (o Lobo de fábrica tem 70 e 4) |
| `SendCatalog.Get(99)` | `IndexOutOfRangeException: Index was outside the bounds of the array.` |
| `TowerCatalog.Get(99)` | devolve o **Canhão** sem avisar |

## 2. O que muda (e o que não muda)

**Muda:** como o arquivo de balanceamento se junta ao catálogo de fábrica, e o que `Get` faz com id inválido.
**Não muda:** o formato do arquivo (`name=Rato;cost=22;...`), o `SerializeSends`, o `Locked`, o `Count` do jogo, nem a
numeração dos envios. Um `envios.txt` que o jogo já exportou continua lendo igual.

## 3. Regras do merge (a decisão central)

Função pura, em arquivo novo `Sim/CatalogMerge.cs`, genérica para envios e torres:

```
Merge(fábrica[], doArquivo[]) -> resultado[]   (ou erro)
```

1. **O resultado tem a ordem e o tamanho da fábrica.** Sempre. A ordem das linhas no arquivo não importa. O índice de
   cada bicho continua o que o código compilado diz.
2. **Casamento por nome, exato** (com acento e maiúscula). Linha do arquivo com o nome de uma unidade de fábrica
   sobrepõe os campos dela; unidade de fábrica que o arquivo não cita **entra com os valores de fábrica**.
3. **Campo ausente numa linha = valor de fábrica daquela unidade** (não o genérico). Para isso o parser recebe a
   unidade de fábrica de mesmo nome como base: `TryParseSends(text, fábrica, out units, out error)`. A versão antiga
   (sem fábrica) continua existindo e usa os genéricos de hoje, para os testes de parser e a ida e volta não mudarem.
4. **Nome repetido no arquivo = erro** (`linha 7: "Lobo" já apareceu na linha 3`). Hoje as duas linhas entrariam.
5. **Nome que não existe na fábrica = erro**, e o arquivo inteiro é recusado com a mensagem
   `linha 5: "Aguia" não existe no jogo (envios: Rato, Cachorro, ...). Quis dizer "Águia"?`. A dica aparece quando o nome
   só difere em acento ou maiúscula.
6. O erro volta pelo `out string error` que já existe, o `CatalogLoader` já o transforma em aviso no log e segue com
   os valores de fábrica. Nada novo na camada de Unity.
7. A checagem atual "arquivo de uma versão antiga (soldados)" fica: se **nenhum** nome casa, a mensagem continua a
   específica de hoje.

## 4. `Get` com id inválido

- `SendCatalog.Get(int id)`: id fora de `0..Count-1` **lança** `ArgumentOutOfRangeException` com
  `"envio 99 não existe (o catálogo tem 10: 0..9)"`. Nunca devolve outra unidade.
- Novo `SendCatalog.IsValidId(int id)` e `TryGet(int id, out SendUnit unit)` para quem recebe id **de fora** (comando,
  tecla, arquivo).
- `LaneSim.CanAfford` e `LaneSim.TrySend` testam `IsValidId` antes de tudo e devolvem `false` para id inválido, sem
  mexer em ouro, renda nem contadores. Com isso o `MatchRunner.Apply` já recusa o comando (devolve `false`, não grava no
  replay) e a partida segue.
- **A divergência do cartão, resolvida:** o cartão diz "falha alto" e também "`Get(99)` não pode estourar". Os dois só
  cabem juntos assim: o `Get` **falha alto** (programador chamou com id errado: exceção com mensagem clara) e a
  **borda** do sistema (comando do jogador, replay, UI) **recusa** em vez de estourar.

## 5. Testes (escritos antes, vistos falhar pelo motivo certo)

Todos no FlowSim, contra a função pura, **sem tocar nos estáticos** (por isso `CatalogMerge` recebe a fábrica como
parâmetro; o cartão pedia "um catálogo de 10 envios só de teste", e assim ele existe sem hack):

1. Fábrica de **10** + arquivo de **9** linhas: `Count == 10`, o décimo com os valores de fábrica. (Hoje daria 9.)
2. Arquivo com as linhas **embaralhadas**: o resultado sai na ordem da fábrica.
3. Linha só com `name=Lobo;cost=40`: custo 40 e **os outros campos do Lobo de fábrica** (não os genéricos).
4. Nome desconhecido: erro com a lista de nomes e a dica de acento; o catálogo não muda.
5. Nome repetido: erro com as duas linhas.
6. `Get(-1)`, `Get(Count)`, `Get(99)`: lançam com a mensagem; `IsValidId` e `TryGet` corretos.
7. `TrySend(99, ...)` e `CanAfford(-1)`: `false`, ouro e renda intactos; `MatchRunner` com `Send(99)` na fila: comando
   recusado e fora do replay.
8. O que já existe segue verde: ida e volta sem perder envio, arquivo de soldados recusado, trava `Locked`, o teste
   "arquivo antigo não apaga torre nova".
9. O `SerializeSends` do catálogo de fábrica, relido pelo merge, devolve exatamente a fábrica.

## 6. Riscos

- **Renomear um bicho no código** (ex.: "Rato" para "Ratazana") passa a recusar todo `envios.txt` já exportado, em vez
  de apagar em silêncio. É o comportamento certo (alto e visível), e a mensagem diz o que fazer. A raiz disso é o
  nome ser a chave: o **TEC-12** (conteúdo por chave estável) troca a chave de `Name` para uma `Key` que não muda. O
  merge fica num lugar só, então essa troca será uma linha.
- Um arquivo com **um** nome errado perde **todas** as edições dele (o arquivo é recusado inteiro). Alternativa na
  decisão D1.
- A mudança de campo ausente (D2) altera a leitura de arquivos editados à mão que omitiam campos. Arquivos exportados
  pelo jogo trazem todos os campos e não mudam.
- Não mexe em regra de jogo: o BalanceLab (`match 12`) tem que dar saída **idêntica** antes e depois (é o critério).

## 7. Fora do escopo (dívida registrada)

- `TowerCatalog.Get` continua engolindo id inválido (`id < 0 || id >= len ? 0`). Convém abrir um bug próprio: o mesmo
  tratamento (`IsValidId`, exceção) e revisar quem chama com `-1` hoje (`TowerTypeAt` devolve -1 mas não passa por `Get`).

## 8. Decisões pedidas ao Felipe

| | Pergunta | Recomendação |
|---|---|---|
| **D1** | Nome que não existe no jogo: recusar o arquivo inteiro com erro (A) ou ignorar só aquela linha e avisar (B)? | **A.** Previsível e simples. B aplicaria edições de um arquivo que o jogo já sabe estar torto. |
| **D2** | Campo omitido numa linha: usa o valor de fábrica do mesmo bicho (A) ou o genérico de hoje (B)? | **A.** É o que o comentário do arquivo já promete. |
| **D3** | Aplicar o mesmo merge ao `TowerCatalog` agora (A) ou só aos envios (B)? | **A.** Corrige a troca silenciosa de torres por linhas embaralhadas, com o mesmo código. Custo: +15 linhas e 3 testes. |
| **D4** | `Get` com id inválido lança exceção e a borda recusa (A), como na seção 4? | **A.** |
