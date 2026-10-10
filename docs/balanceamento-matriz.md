# Matriz de contras (TEC-14) — primeira medição

[← cronograma](../ROADMAP.md) · gerada por `dotnet run --project Tools/FlowSim -v quiet -- matriz [ouro] [contras.txt]`

**O que mede.** Cada par torre × bicho com o **mesmo ouro**: o jogador gasta todo o ouro em torres de **um só tipo**
(sem upgrade, mesma disposição de células perto do corredor) e recebe 18 compras do mesmo bicho, uma a cada 1,2 s,
por 100 s simulados. Nota = fração dos bichos que **não** chegaram à base (1 = defesa perfeita). Fronteira e atrito
contam: é a tese do jogo. Não mede combinação de tipos, nível alto nem a IA.

**Contratos (relatório hoje, portão quando o catálogo estabilizar):** cada torre é a melhor contra pelo menos 1 envio; nenhuma
é a melhor contra mais de 40%; cada envio tem uma resposta 15% a 30% melhor que o Canhão. A máquina dos contratos é
testada no FlowSim (`CounterMatrixTests`: torre inútil, bicho sem resposta e torre dominante são acusados).

## Primeira medição (10/10/2026, catálogo pós DES-05 e TORRE-03)

```
Matriz de contras — 160 de ouro em torres (sem upgrade), 18 compras de cada bicho, 100 s
nota = fração dos bichos que NÃO chegaram à base (maior = a torre responde melhor)

                Canhão  Morteiro      Gelo Sentinela      Fogo        Ar   melhor
Rato             1.00*     1.00      0.76      0.72      1.00      1.00    Canhão
Cachorro         1.00*     1.00      1.00      1.00      1.00      0.94    Canhão
Lobo             1.00*     0.00      0.06      0.00      0.06      0.00    Canhão
Javali           1.00*     0.11      1.00      0.06      0.61      0.06    Canhão
Águia            0.33*     0.00      0.00      0.22      0.00      0.00    Canhão
Urso             1.00*     0.06      1.00      0.06      1.00      1.00    Canhão
Tigre            0.11*     0.00      0.06      0.00      0.06      0.00    Canhão
Rinoceronte      1.00*     0.00      1.00      0.00      0.83      0.17    Canhão
Elefante         1.00*     0.17      1.00      0.06      1.00      1.00    Canhão

Contratos (relatório; só viram portão quando o catálogo estabilizar):
  OK    Canhão é a melhor contra pelo menos 1 envio  [9 envio(s)]
  FALHA Morteiro é a melhor contra pelo menos 1 envio  [0 envio(s)]
  FALHA Gelo é a melhor contra pelo menos 1 envio  [0 envio(s)]
  FALHA Sentinela é a melhor contra pelo menos 1 envio  [0 envio(s)]
  FALHA Fogo é a melhor contra pelo menos 1 envio  [0 envio(s)]
  FALHA Ar é a melhor contra pelo menos 1 envio  [0 envio(s)]
  FALHA Canhão é a melhor contra no máximo 40% dos envios  [9 de 9]
  OK    Morteiro é a melhor contra no máximo 40% dos envios  [0 de 9]
  OK    Gelo é a melhor contra no máximo 40% dos envios  [0 de 9]
  OK    Sentinela é a melhor contra no máximo 40% dos envios  [0 de 9]
  OK    Fogo é a melhor contra no máximo 40% dos envios  [0 de 9]
  OK    Ar é a melhor contra no máximo 40% dos envios  [0 de 9]
  FALHA Rato tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Cachorro tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Lobo tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Javali tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Águia tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Urso tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Tigre tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Rinoceronte tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Elefante tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]

15 contrato(s) fora do alvo.

Matriz de contras — 100 de ouro em torres (sem upgrade), 18 compras de cada bicho, 100 s
nota = fração dos bichos que NÃO chegaram à base (maior = a torre responde melhor)

                Canhão  Morteiro      Gelo Sentinela      Fogo        Ar   melhor
Rato             0.97      1.00*     0.72      0.72      0.72      0.72    Morteiro
Cachorro         1.00*     0.83      0.56      0.72      0.56      0.11    Canhão
Lobo             0.44*     0.00      0.00      0.00      0.00      0.00    Canhão
Javali           1.00*     0.00      0.17      0.00      0.06      0.00    Canhão
Águia            0.06*     0.00      0.00      0.06      0.00      0.00    Canhão
Urso             1.00*     0.00      0.94      0.00      0.06      0.00    Canhão
Tigre            0.00*     0.00      0.00      0.00      0.00      0.00    Canhão
Rinoceronte      0.33      0.00      0.67*     0.00      0.06      0.00    Gelo
Elefante         1.00*     0.00      1.00      0.00      0.06      0.00    Canhão

Contratos (relatório; só viram portão quando o catálogo estabilizar):
  OK    Canhão é a melhor contra pelo menos 1 envio  [7 envio(s)]
  OK    Morteiro é a melhor contra pelo menos 1 envio  [1 envio(s)]
  OK    Gelo é a melhor contra pelo menos 1 envio  [1 envio(s)]
  FALHA Sentinela é a melhor contra pelo menos 1 envio  [0 envio(s)]
  FALHA Fogo é a melhor contra pelo menos 1 envio  [0 envio(s)]
  FALHA Ar é a melhor contra pelo menos 1 envio  [0 envio(s)]
  FALHA Canhão é a melhor contra no máximo 40% dos envios  [7 de 9]
  OK    Morteiro é a melhor contra no máximo 40% dos envios  [1 de 9]
  OK    Gelo é a melhor contra no máximo 40% dos envios  [1 de 9]
  OK    Sentinela é a melhor contra no máximo 40% dos envios  [0 de 9]
  OK    Fogo é a melhor contra no máximo 40% dos envios  [0 de 9]
  OK    Ar é a melhor contra no máximo 40% dos envios  [0 de 9]
  FALHA Rato tem uma resposta 15% a 30% melhor que o Canhão  [Morteiro +3%]
  FALHA Cachorro tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Lobo tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Javali tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Águia tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Urso tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Tigre tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]
  FALHA Rinoceronte tem uma resposta 15% a 30% melhor que o Canhão  [Gelo +100%]
  FALHA Elefante tem uma resposta 15% a 30% melhor que o Canhão  [Canhão +0%]

13 contrato(s) fora do alvo.
```

## O que a medição diz (leitura honesta)

- **O Canhão é a melhor resposta (ou empata) contra quase tudo**: com o mesmo ouro ele põe mais torres (25 de ouro contra
  40 a 50 das outras) e a fronteira dele é a maior. Os contratos de papel das outras cinco torres **falham hoje**.
- A **Sentinela** (a resposta pensada para a Águia) fica abaixo do Canhão até contra a Águia com 160 de ouro: o multiplicador
  contra voador (x2,6) não compensa ela custar o dobro e ter fronteira pequena. É o achado mais claro.
- **Gelo** empata com o Canhão nos corpos grandes e lentos; **Morteiro** só ganha dele contra o Rato (enxame); **Fogo** e **Ar**
  não são a melhor contra nenhum bicho sozinhos (o papel deles é de apoio: queima e empurrão rendem em **combinação**, que esta
  medição não cobre).
- **Limites:** defesa de um tipo só e sem upgrade é um corte estreito. A conclusão certa não é "mexa nos números agora", é: antes
  de declarar o roster balanceado no Portão 1, medir também **defesas mistas** e **nível alto** (próxima extensão do modo
  `matriz`), e então abrir uma tarefa de rebalanceamento das torres com esta tabela como régua.
- `contras.txt` (chaves, não nomes) é a saída que a loja da Fase 6 vai ler para o "bom contra" medido.
