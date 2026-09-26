# Roteiro de teste

O jogo já abriu e rodou no editor (25/09). Desde então o visual inteiro foi trocado para
**realista** (26/09): modelos, texturas, luz, efeitos e HUD, tudo gerado em código. Essa
parte **nunca rodou no Unity** — compila contra a API do Unity e os modelos foram conferidos
num preview fora do editor (`Tools/ArtPreview`), mas luz, sombra, pós-processamento e
desempenho só se veem apertando Play. Por isso o passo 0 vem antes de tudo.

Está em ordem de valor: se der errado no passo 0 ou 1, pare e me mande o erro.

---

## 0. Visual realista (5 min)

1. `git pull` (ou troque para o branch do visual) e abra o projeto. O Unity vai importar os
   arquivos novos (`Art/`, `SoftParticle.shader`) e recompilar.
2. **Play** → **Tower Wars** → **Normal**. No Console deve aparecer
   `[TDFende] 22 texturas procedurais em N ms`. **Me diga o N** — é o tempo de geração
   no boot; acima de ~1500 ms eu passo a guardar as texturas em disco.
3. O que olhar, uma frase por item:
   - Algum material **rosa/magenta**? (shader não achado — me diga em quê)
   - Algum modelo **de dentro para fora** ou escuro demais? (normal ou face invertida)
   - A pedra, a madeira e o bronze **parecem material** ou parecem tinta?
   - Os inimigos **andam** (perna mexendo) e **viram** para onde vão? O Colosso rola?
   - O céu aparece? Tem **sombra**? O bronze do canhão **brilha** um pouco?
   - A barra de vida aparece em quem levou dano? O brilho gelado aparece em quem está
     dentro da sua fronteira?
   - **FPS** com as duas lanes cheias (o cenário em volta tem ~100 mil vértices)
   - No geral: **parece realista**, ou parece maquete? Se for maquete, o que mais incomoda?

Se algo parecer quebrado, um print ajuda mais que a descrição.


## 1. Abrir e compilar (5 min, quase tudo é espera)

1. **Unity Hub** → **Add** → **Add project from disk** → `C:\Users\Felip\source\repos\TDFende`
2. Abrir com **6000.3.11f1**. A primeira vez demora: importa, instala o URP sozinho, recompila.
   Acompanhe as mensagens `[TDFende]` no Console.
3. Aperte **Play**.

**Se der erro:** copie o texto do Console e me mande. Erro de compilação aqui seria surpresa
(o `Tools/CompileCheck` compila os mesmos arquivos contra as DLLs do editor), mas erro de
*importação de pacote* ou de *shader* é plausível — nenhum dos dois passa por aquela verificação.

**Risco conhecido e nunca testado:** a assembly opcional `TDFende.Urp` usa `defineConstraints`
+ `versionDefines`. Se aparecer erro de referência não resolvida, é ali — e o jogo deve rodar
mesmo assim, porque tudo tem fallback para o pipeline Built-in.

Já foi verificado à mão, contra a fonte do URP embutido nesta versão do editor, que **todos os
nomes de API usados pelo `PostFx.cs` existem**. Então, se aquele arquivo der erro, o problema é
o padrão de asmdef — não um nome de API errado. Isso encurta a investigação.

## 2. TD clássico (3 min)

No menu, escolha **TD clássico**. É a fase 0, a parte mais antiga e a única que já passou por
uma revisão completa.

- Roda? A câmera enquadra o tabuleiro?
- Construa uma parede de torres e veja a onda desviar. Parece **esperta** ou parece burra?
- Olhe o FPS (canto superior esquerdo) lá pela onda 8-10.

## 3. Tower Wars (10 min) — é aqui que está o valor

Menu → **Tower Wars** → **Normal**. Sua lane é a **de baixo**, a da IA é a de cima.

- Clique na sua lane: torre nova (25 ouro). O anel claro no chão é o alcance dela.
- Clique numa torre sua: sobe o nível dela (custo cresce a cada nível).
- **1-6** ou os botões do rodapé: compra inimigo e manda na lane da IA.

Cada envio sobe a sua renda **para sempre**. O jogo inteiro é decidir entre torre, envio e renda.

### As perguntas que eu não consigo responder sozinho

1. **Cercar o inimigo com fronteira e vê-lo derreter tem graça?** É a tese do projeto inteiro.
   Se a resposta for "meh", a gente corta e vira TD clássico — e você economizou meses.
2. **A tensão de "gasto em defesa ou em ataque?" existe?** Era isso que fazia o Line Tower
   Wars viciar.
3. **Dá para acompanhar as duas lanes ao mesmo tempo**, ou você perde o que a IA está fazendo?
4. **O ritmo está bom?** As partidas IA×IA duram ~7 min no Normal. Contra humano é outra coisa.
5. **A IA parece jogar ou parece trapacear?** Normal ganha do Fácil em 95% das simulações.

### O que reportar

Não precisa de relatório. Uma frase por item já resolve. O que mais ajuda:

- Qualquer coisa que **pareça quebrada** (objeto no lugar errado, cor estranha, algo invisível)
- **FPS** quando as duas lanes estiverem cheias
- A resposta da pergunta 1, mesmo que seja "não sei dizer"

## 4. Se sobrar vontade

- **R** reinicia a partida — confira que reinicia limpo.
- Tente as três dificuldades. Fácil deve ser ganhável; Difícil deve doer.
- Redimensione a janela do Game: o HUD deve continuar utilizável.

---

## Ajustar você mesmo, sem recompilar

No menu inicial há **Exportar balanceamento**. Ele escreve dois arquivos de texto na sua
pasta de dados do jogo:

- `envios.txt` — os seis tipos de inimigo que se compra
- `torres.txt` — os quatro tipos de torre

Edite, salve, dê Play de novo. O jogo carrega no boot. Uma linha por unidade, campo ausente
usa o padrão, `#` é comentário. **Decimal com ponto** (`2.75`, nunca `2,75`).

Arquivo torto nunca derruba o jogo: vira aviso no Console com a linha do erro e o jogo segue
com os valores de fábrica. Apagar os arquivos volta tudo ao padrão.

Para medir o efeito sem abrir o editor:

```bash
cd Tools/FlowSim
dotnet run -- match 60
```

E se algo parecer errado durante o jogo, aperte **F9**: ele grava a partida inteira num
arquivo. Me mande esse arquivo e eu reproduzo aqui, tique a tique, em vez de depender da
descrição.

Ainda em código (mexer exige recompilar): `Sim/TowerWarsConfig.cs` tem economia, atrito,
escalada e morte súbita.
