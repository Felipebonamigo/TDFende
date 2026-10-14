---
name: revisar-sem-compilar
description: 'Confere o C++ do Unreal do TDFende antes do commit, porque a nuvem não compila: roda o conferidor (generated.h, GENERATED_BODY, UPROPERTY, *_API, .h × .cpp, includes e Build.cs), revisa à mão o que ele não pega e diz ao PC o que compilar. Use ao mexer em .h ou .cpp de Unreal/TDFende/Source/TDFende, antes de commitar C++, ou quando o Felipe disser "vai compilar?", "confere o C++" ou "o build quebrou".'
---
# Revisar o C++ sem compilar

A nuvem edita o C++ mas não tem o Unreal. Erro bobo só aparece quando o Felipe compila no PC, e custa uma volta inteira.
Esta rotina pega o erro mecânico antes do commit. Ela não substitui a compilação: a palavra final é do PC.

## Passos

1. Rode o conferidor na raiz do repositório (Python 3, sem dependência):
   ```bash
   python3 .claude/skills/revisar-sem-compilar/conferir_cpp.py                    # o que mudou desde o HEAD
   python3 .claude/skills/revisar-sem-compilar/conferir_cpp.py --base origin/main   # tudo o que o branch mudou
   python3 .claude/skills/revisar-sem-compilar/conferir_cpp.py --todos            # todo o C++ (leva segundos)
   ```
   Sai 1 se achou ERRO. ERRO se conserta antes do commit. AVISO se lê e decide. NOTA lembra o que conferir à mão.
   Hoje o código do `main` passa sem erro (8 avisos: os componentes `ATdfSimActor::Pool` e `TowerIsms` e seis widgets do `UTdfHudWidget`, todos
   seguros pelo dono, o ator ou a árvore de widgets; confira que continuam presos ao dono antes de mexer neles, porque o
   único crash do projeto, a cerca de 60 s, foi um ponteiro cru que o coletor de lixo apagou).
2. Revise à mão a lista "O que o conferidor não pega", abaixo, só nos trechos que você mudou.
3. Siga as regras do TDFende: a Sim em C# é a dona das regras e o Unreal só desenha
   (`docs/designs/migracao-unreal.md`); mudança de struct da Sim, da ABI ou do formato do replay é tarefa "architectural"
   e pede design aprovado pelo Felipe antes do código (CLAUDE.md). O C++ fala com a DLL por `TdfSim.h/.cpp` (`TDF_BIND`,
   conferência de ABI e de tamanho dos structs); objeto do Unreal que o C++ precisa manter vivo vai num `UPROPERTY()`
   (veja o `KeepAlive` de `TdfSimActor.h`). Na nuvem, rode também a ponte (skill `ponte-unreal`: `Tools/SimBridge/Smoke.sh`).
4. Escreva no commit, ou na resposta ao Felipe, a linha "Para o PC:" que o conferidor imprime no fim: ela diz se basta o
   Live Coding ou se é preciso fechar o editor e compilar.
5. Compilar e testar no PC: a skill `ponte-unreal` roda `Tools\SimBridge\PublicarParaUnreal.ps1` se a Sim mudou, depois
   `Unreal\TDFende\RodarU0.ps1 -AutoTeste` (compila o `TDFendeEditor` e reproduz o replay no FlowSim; sai 0 ou 1).
6. Se o build do PC quebrar mesmo assim, o erro que escapou vira regra nova no conferidor
   (`.claude/skills/revisar-sem-compilar/conferir_cpp.py`), com o caso real no comentário. Copie a mudança para os outros
   jogos em Unreal (Detonauto e Age of Earth): o arquivo é o mesmo nos três.

## O que o conferidor não pega

- Assinatura igual no .h e no .cpp: `const`, referência, tipo de cada parâmetro, `virtual` e `override`.
- `Super::` chamado nos overrides de `BeginPlay`, `EndPlay`, `Tick`, `PostInitializeComponents` e afins.
- Membro novo com valor inicial (na declaração ou no construtor); `bool` e ponteiro sem valor são lixo.
- Texto que o jogador lê é `FText` (`LOCTEXT`/`NSLOCTEXT` ou String Table; skill `traduzir`). `FString` para o resto, `FName` para id.
- No .h, declaração adiantada (`class UFoo;`) em vez de include; o include vai no .cpp, e o .cpp começa pelo próprio .h.
- `check()` só para erro de programação. Dado ruim que o jogador pode causar pede `ensure` ou retorno com `UE_LOG`.
- Remover de `TArray` dentro de um laço: de trás para frente ou `RemoveAll`.
- Tick ligado só quando precisa. Sistema novo de dano, tráfego ou efeito passa pela skill `achar-gargalo` antes de ser aprovado.
- Formato de dado salvo ou trocado entre processos. ABI (`TDF_ABI_VERSION` em `Tools/SimBridge/include/tdfende_sim.h`, hoje 2, só acréscimos), tamanho dos structs
  (testado no FlowSim) e replay: sobe a versão, atualiza os dois lados e o teste no mesmo commit.

## Armadilhas

- O conferidor procura módulos pelo `*.Build.cs` a partir da pasta onde você está: rode sempre da raiz do repositório.
- Include de cabeçalho do motor fora da tabela do conferidor não é checado contra o `Build.cs`. Na dúvida, procure o
  módulo na página da classe na documentação da Epic ("Module") e confira o `Build.cs`.
