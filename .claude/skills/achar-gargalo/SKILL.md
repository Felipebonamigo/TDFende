---
name: achar-gargalo
description: 'Acha o que está pesando no quadro do TDFende: qual linha de execução manda, qual função ou passo de GPU, quanto passa da meta (ponte abaixo de 0,5 ms; p95 de 8 ms no Unity) e onde anotar. Use quando o fps cair, antes de aprovar sistema novo de dano, tráfego, efeito ou multidão, ou quando o Felipe disser "está travando", "caiu o fps", "mede isso" ou "por que pesa".'
---
# Achar o gargalo

A meta: ainda não há orçamento de fps escrito para o Unreal (a U4 vai fixar; o perfil de hardware mínimo, TEC-33, está
pendente). O que está escrito: a ponte gasta menos de 0,5 ms por quadro (medido de 0,036 a 0,056 ms no design U0) e, no
Unity, a captura avisa acima de 8 ms de p95, 2,5 M triângulos e 2000 draws (`SmokeCapture.cs`).

Medir o fps diz *se* pesa; esta rotina diz *o quê*. Sem saber o quê, otimização é chute.

## Passos

1. Reproduza com números: a mesma cena, a mesma câmera, o mesmo preset.
   No Unreal: `Unreal\TDFende\MedirFps.ps1 -Tiques 1500` faz A/B de fps em seis casos (completo, sem grama, sem
   neblina volumétrica, sem nuvens, sem Lumen, sem sombras virtuais) e grava em `Saved\Fps\`; o `TdfSimActor` loga `FPS` a cada
   5 s e, no fim do replay, a linha `CUSTO` (step, redesenho, total: média, p50, p95, p99, máximo). No Unity (congelado):
   `Tools\Captura.ps1 -Estresse` e o `print_metricas.json`.
2. Ache a linha de execução ou a etapa que manda (seções abaixo).
3. Ache a função ou o passo culpado e meça de novo sem ele (desligue o sistema, troque o efeito por nada) para provar.
4. Corrija o culpado e meça de novo nas mesmas condições. Sem antes e depois, não está provado.
5. Anote data, cena, preset, máquina, antes e depois, e o que mudou em `Unreal/LEIA-ME.md` (medições do Unreal) e `docs/ESTADO-ATUAL.md`.

## No Unreal (PC)

1. **Qual linha de execução manda.** Com o jogo rodando, `stat unit`: `Frame` é o total; o maior entre `Game` (lógica),
   `Draw` (preparo da renderização) e `GPU` é o gargalo. `stat unitgraph` mostra no tempo, bom para picos.
2. **Gravar para ler depois.** O perfil em CSV é texto e o Claude lê direto:
   `-csvCaptureFrames=600` na linha de comando, ou `csvprofile start` e `csvprofile stop` no console.
   O arquivo sai em `Saved/Profiling/CSV/`. Cada coluna é um contador (FrameTime, GameThreadTime, RenderThreadTime, GPUTime
   e os stats ligados); some a média, o p95 e o pior quadro.
3. **Achar a função.** Unreal Insights: rode com `-trace=cpu,gpu,frame,bookmark -statnamedevents`; o `.utrace` sai em
   `Saved/Profiling/` (ou no servidor de traces do Insights). Abra no `UnrealInsights.exe`, Timing Insights, e ordene os
   timers por tempo exclusivo na linha de execução que o passo 1 apontou. Para o Claude ler, exporte os timers para CSV
   pelo próprio Insights; antes de pôr o comando de exportação num script, confira o nome exato na documentação do
   Insights da versão 5.8.
4. **GPU.** `ProfileGPU` (Ctrl+Shift+,) escreve no log o custo de cada passo de renderização; `stat gpu` mostra ao vivo.
   Compare com e sem o efeito novo, mesma câmera.
5. Meça sempre com o build de desenvolvimento ou de teste, nunca no editor com a janela pequena; anote a resolução, o
   preset gráfico e o upscaler.


## Armadilhas

- Média esconde engasgo: olhe também o p95 e o pior quadro.
- Primeira execução compila shader e carrega asset: descarte os primeiros segundos.
- O fim de partida com 60 bichos ainda não foi medido no Unreal: é o pior caso, comece por ele.
- No Unity, a janela de 144 Hz prende o quadro no sincronismo vertical: p95 baixo pode ser só o vsync.
