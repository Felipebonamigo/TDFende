# Roteiro do primeiro teste

Nada disto nunca rodou no editor. O código compila contra as DLLs reais do Unity e a
lógica pura tem 91 verificações headless, mas **ninguém apertou Play uma vez sequer**.

Este roteiro existe para que 20 minutos seus rendam o máximo. Está em ordem de valor:
se der errado no passo 1, pare e me mande o erro — o resto não importa ainda.

---

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

- Clique na sua lane: torre nova (25 ouro).
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

## Ajustar você mesmo

Todo o balanceamento vive em dois arquivos, e mudar um número não exige recompilar nada além
do próprio script:

- `Assets/_Project/Scripts/Runtime/Sim/TowerWarsConfig.cs` — economia, atrito, escalada, upgrades
- `Assets/_Project/Scripts/Runtime/Sim/SendCatalog.cs` — os seis tipos de envio

Se mexer, dá para medir o efeito sem abrir o editor:

```bash
cd Tools/FlowSim
dotnet run -- match 60
```
