# Livro-caixa dos créditos do Meshy

Regra (MANUAL, seção 14): **toda operação que gasta crédito entra aqui na hora**, com o saldo lido depois
(`GET https://api.meshy.ai/openapi/v1/balance`, que não gasta nada) e quem aprovou. O saldo daqui tem que
bater com o do site; se não bater, pare e descubra a diferença antes de gastar mais.

| Data | Operação | Créditos | Saldo depois | Aprovou | Fonte |
|---|---|---|---|---|---|
| 03/10/2026 | Javali e tigre: imagem para 3D (geração) | não registrado | não registrado | Felipe | THIRD_PARTY / manifesto (plano do Meshy a conferir) |
| 04/10/2026 | Torre de Canhão: texto para 3D, preview 20 + textura 10 (tarefas `01a104b8…` e `01a104bb…`) | 30 | 992 | Felipe pediu a torre | `consumed_credits` da API; saldo lido na API |
| 04/10 a 09/10/2026 | **Sem registro** (suspeita: rig, remesh e animação do javali e do tigre) | 150 | 842 | não sei | diferença entre 992 e 842; conferir no histórico do site |
| 09/10/2026 | Consulta de saldo | 0 | 842 | — | `GET /openapi/v1/balance` |

Os 150 créditos sem registro e o plano da conta nas datas de 03 e 04/10 são pendências do Felipe
(`docs/licencas/manifesto.json`, entradas `pendente`).
