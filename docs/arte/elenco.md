# Meta de elenco do Early Access (DES-23) — rascunho

[← escolha do tema](tema.md) · [cronograma](../../ROADMAP.md)

**Rascunho de 09/10/2026, condicionado ao tema e ao pacote de animais.** O cartão pede "lista com números e
espécies, tirada do inventário", e o inventário diz que a oferta de bicho realista e animado é o gargalo, não a
vontade. Recalibra na Fase 11 (DES-23b). Os números abaixo são metas de desenho, não promessa de arte pronta.

## Números

| | Hoje | Meta do EA | De onde vem |
|---|---|---|---|
| Bichos comuns | 9 | 14 a 16 | os 9 atuais + 5 a 7 espécies do pacote de animais |
| Elites | 0 | 4 | espécies do mesmo pacote, 1 mecânica cada |
| Chefes | 0 | 2 | elefante ancestral (MERC-11) e um segundo, a definir |
| Torres | 6 | 10 a 11 | as 6 + 4 ou 5 (tier 2, apoio, cerco), cada uma com 3 estágios visuais |
| Ramos por torre | 0 | 2 | TORRE-07 |

Métrica de elenco no BalanceLab (recalibrada na Fase 11): por exemplo, 6 ou mais tipos de bicho com 5% ou mais das
compras. O custo de arte é o que limita: de 9 para ~26 bichos animados são +17 modelos, e cada torre nova são 3
estágios (+12 a +15 modelos).

## Regra de papéis (um papel especial por espécie)

| Espécie | Papel | Tarefa |
|---|---|---|
| Rinoceronte | armadura | BICHO-05 |
| Urso | regeneração fora da fronteira | BICHO-06 |
| Javali | ninhada (javali-mãe com leitões) | BICHO-10 |
| Elefante | **só** o chefe ancestral | MERC-11 |
| Búfalo ou bisão (novo) | fúria que desliga torre | MERC-07 |
| Uma espécie do bioma (novo) | elite imune a controle | BICHO-09 |
| Espécie com clipe de ataque (novo) | derrubador | MERC-08 |

## Espécies candidatas e o que o inventário sabe delas

O pacote "Ultimate 3D Animal Pack" (WildMesh e AnimalMesh, RenderHub) lista, segundo os agentes: lobo, urso, javali,
tigre, rinoceronte, elefante, rato, cachorro, hiena, gnu, búfalo, leão, facócero, cervo, alce, raposa e corvo, cada
um com idle, walk, run, attack e death. **Dois "se":** a listagem diz "Editorial Use Only Restriction" (precisa ler o
contrato antes de comprar, ver `tema.md`), e eu não vi os clipes reais. A águia realista animada não está no pacote.

| Papel no elenco | Candidata | Disponível hoje? |
|---|---|---|
| Comum novo | cervo, alce, raposa, hiena, gnu, facócero | só pelo pacote pago (alce, corça e raposa também em CC BY grátis, versão antiga com "problemas de rig") |
| Voo | águia (já no jogo, é um gavião), corvo | corvo só pelo pacote; águia realista não achei |
| Fúria que desliga torre | búfalo | pacote pago; versão "demo" grátis é CC BY-NC e **não pode entrar no jogo** |
| Elite imune a controle | leão ou hiena | pacote pago |
| Derrubador | tigre ou leão (clipe de ataque) | pacote pago |

## O que fica claro

- Sem o pacote de animais (ou um equivalente com licença limpa), o elenco **não passa de 9 a 11 bichos**.
- Os modelos CC BY grátis são de autores diferentes e têm clipes desiguais: servem de tapa-buraco, não de elenco.
- Gerar bicho no Meshy gasta crédito (rig 5 e animação 3 por ação, segundo a tabela da API; geração ~30) e, para
  quadrúpede, só dá a animação "Andando" (MANUAL, seção 6). Cada pedido passa pelo Felipe (seção 14).

## Efeito da decisão "sem verba" (09/10/2026)

O Felipe decidiu **não gastar com pacotes pagos**. Então o pacote de animais está fora, e o elenco só cresce com:
1. **CC BY grátis do Sketchfab**, de autores diferentes (urso, javali, alce, corça, raposa; versão antiga, com
   "problemas de rig" segundo o autor) e **CC0** quando houver;
2. **Meshy da conta do Felipe**, bicho a bicho, sempre com pedido de aprovação e custo dito antes (rig 5 e animação 3 por
   ação segundo a tabela; geração ~30). Quadrúpede só ganha "Andando".

Em números, a meta de 14 a 16 comuns, 4 elites e 2 chefes **não fecha** sem arte nova. O que dá para prometer hoje:
**9 atuais + 2 a 4 espécies grátis** (cervo ou alce, raposa, javali novo), sem elite nem chefe animados até haver
modelo. Isso empurra MERC-11 (chefe elefante) e BICHO-09 (elite imune) para depois de uma decisão sobre quanto
Meshy gastar nos bichos. Pergunta aberta no ROADMAP.
