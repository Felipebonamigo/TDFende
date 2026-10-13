# Roteiro de teste

Atualizado em 13/10/2026 (DOC-01). Vale para o **executável** (`Builds\Windows\TDFende.exe`, gerado por
`Tools\GerarExecutavel.ps1`) e para o Play no editor. O que importa é a **sensação**: uma frase por item basta.
Está em ordem de valor: se der errado no passo 1, pare e me mande o print e o `Player.log`.

Números de hoje: **9 bichos**, **6 torres**, nível máximo **6**.

---

## 1. Abrir (5 min)

1. Gere o executável (Unity fechado, ~2 min) ou abra o projeto com **6000.3.11f1** e aperte **Play**.
2. Confira no menu o selo de versão. No Console/`Player.log` procure `[TDFende]`: avisos como `sem modelo para ...`,
   `camada privada ...` ou `... ignorado (...)` do balanceamento são o que eu preciso ver.
3. Algum material **rosa**, modelo **de dentro para fora**, bicho **invisível**, **de lado** ou **flutuando**? Print ajuda.

## 2. Controles

| Ação | Controle |
|---|---|
| Construir a torre escolhida | Clique esquerdo numa célula livre da **sua** lane (a de baixo) |
| Trocar a torre escolhida | **Q** / **E** |
| Subir de nível | Clique esquerdo na torre, ou **U** (custo cresce a cada nível, máximo 6) |
| Vender | Clique direito, **X** ou **Delete** na torre (devolve 70% do investido) |
| Enviar bicho à lane da IA | Teclas **1-9** ou botões do rodapé |
| Câmera | WASD / setas / botão do meio; **scroll** dá zoom |
| Reiniciar | **R** |
| Gravar a partida (replay) | **F9** (grava em `replays\`; me mande o arquivo) |

Os 9 bichos, do menor ao maior (tecla 1 a 9): **Rato** (enxame de 4), **Cachorro** (barato, é o investimento),
**Lobo** (rápido), **Javali**, **Águia** (voa, ignora a fronteira), **Urso** (gordo, regenera fora da fronteira),
**Tigre** (gordo e rápido), **Rinoceronte** (armadura: tiro fraco quase não fura), **Elefante** (colosso).
As 6 torres: **Canhão** (régua), **Morteiro** (área, contra enxame), **Gelo** (lenta e congela), **Sentinela** (antiaérea),
**Fogo** (queima a vida máxima), **Ar** (empurra para trás).

## 3. Tower Wars, o que importa (10 a 15 min)

Menu → **Tower Wars** → **Normal**. Sua lane é a de baixo, a da IA a de cima.

Cada envio tira vidas do adversário **se passar**: Rato/Cachorro/Lobo tiram 1, Javali/Águia/Urso/Tigre 2,
Rinoceronte/Elefante 3 (a volta repassada custa 1). Cada envio também sobe a **sua renda para sempre**. A torre
sobe a fronteira junto com o nível (o território cresce).

### As perguntas que só você responde (nota de 1 a 10 + uma frase)

1. **Cercar o bicho com fronteira e vê-lo derreter tem graça?** É a tese do projeto. Nota de 1 a 10.
2. **Existe a tensão de "defesa ou ataque?"** Em algum momento você hesitou entre torre e envio?
3. **Dá para acompanhar as duas lanes**, ou você perde o que a IA faz?
4. **O ritmo está bom?** Partida IA×IA no Normal dura ~8-9 min.
5. **A IA parece jogar ou parece trapacear?** E o **Fácil** é ganhável, o **Difícil** dói?
6. **Cada bicho se lê de longe** (silhueta, velocidade) e você sabe **qual torre responde a qual**?
7. **O Rinoceronte parece blindado** na prática (tiro fraco faz cócegas)? **O Urso** curando fora da fronteira ensina a fechar buracos?
8. **Subir o nível da torre muda o mapa** (a fronteira cresce) de um jeito que você nota?

### O que reportar

Não precisa de relatório. Uma frase por item. O que mais ajuda:

- Qualquer coisa que **pareça quebrada** (objeto no lugar errado, cor estranha, algo invisível), com print
- **FPS** com as duas lanes cheias (o `-captura` já mede, mas a sensação conta)
- As notas das perguntas 1 e 2, mesmo que seja "não sei dizer"

## 4. TD clássico (3 min)

Menu → **TD clássico**: uma lane, ondas infinitas. Roda? A câmera enquadra? Uma parede de torres faz a onda desviar de
forma **esperta**? Espaço chama a próxima onda.

## 5. Se sobrar vontade

- **R** reinicia limpo? As três dificuldades?
- Redimensione a janela: o HUD continua utilizável?
- Venda e reconstrua no mesmo lugar; tente murar o caminho (o jogo bloqueia a torre que fecharia a última passagem).

---

## Ajustar você mesmo, sem recompilar

No menu inicial há **Exportar balanceamento**: escreve `envios.txt` e `torres.txt` na pasta de dados do jogo
(`%USERPROFILE%\AppData\LocalLow\DefaultCompany\TDFende\Balanceamento`). Cada linha começa com `key=` (a identidade do bicho ou
da torre; **não mude**) e `name=` (só texto). Campo omitido vale o de fábrica; `#` é comentário; **decimal com ponto**.
Edite, salve, dê Play. Arquivo torto vira aviso no Console e o jogo segue com os valores de fábrica; apagar volta tudo ao padrão.
Detalhes: `Balanceamento\LEIA-ME.txt`.

Para medir o efeito sem abrir o editor:

```bash
dotnet run --project Tools/FlowSim -v quiet -- match 24      # laboratório de balanceamento
dotnet run --project Tools/FlowSim -v quiet -- matriz 160    # torre × bicho com o mesmo ouro
dotnet run --project Tools/FlowSim -v quiet -- replay arquivo.txt
```

Ainda em código (mexer exige recompilar): `Sim/TowerWarsConfig.cs` tem economia, atrito, escalada e morte súbita.
