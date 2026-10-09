# TDFende — instruções para o Claude

**Antes de qualquer tarefa, leia [`docs/MANUAL.md`](docs/MANUAL.md)** (como trabalhar: comandos,
armadilhas do executável, pipelines de arte, licenças) e pegue a próxima tarefa no
[`ROADMAP.md`](ROADMAP.md) (o que fazer, em ordem, com critério de pronto).

- **Git:** commit e push direto na `main` (pedido do Felipe, 27/09/2026). Nada de branch
  separado: ele roda o projeto pela `main` e mais de uma sessão do Claude trabalha no
  repositório. Começo: `git pull --rebase --autostash`. Push: `git pull --rebase --autostash`
  e `git push`. **Nunca** `git stash`/`stash pop` entre um commit que falhou no pre-commit e a
  nova tentativa (esvazia o índice e o commit sai incompleto). Depois de todo commit,
  `git show --stat HEAD` para conferir os arquivos.
- Mensagens de commit e comentários em português.
- **Verificação:** nada está pronto sem rodar FlowSim + CompileCheck (o pre-commit faz) e, se
  mexeu em algo visível, gerar o executável e conferir o print do `-captura` (MANUAL, seção 4).
  O Felipe joga pelo executável, não pelo editor. `Tools\Captura.ps1` tem que sair com 0.
- **Tarefa "architectural"** (design escrito e aprovado pelo Felipe antes de qualquer código;
  critério aprovado em 09/10/2026): mudança em struct da Sim, no formato do replay ou do
  catálogo, comando novo, vida de torre, `MatchRules` e multiplayer. O resto é "bounded".
- **Repositório público, arte pesada no PC** (09/10/2026): nada acima de 10 MB no git (o
  pre-commit bloqueia); fonte de modelo e GLB original ficam no PC com backup no D:.
- **Direção de arte (06/10/2026):** realista e de última geração, não low-poly nem cartoon; não
  precisa ser medieval.
- **Modelos 3D (pedido do Felipe, 04/10/2026):** primeiro procurar pronto na comunidade para
  baixar (Sketchfab, galeria do Meshy...), conferindo licença e autoria. Gerar no Meshy só em
  último caso, e **sempre perguntar ao Felipe antes** de gastar créditos (dizer quantos: ~30 por
  modelo com textura). Aceitar EULA, logar em conta ou comprar: só o Felipe autoriza.
- Arquivos que o Unity regrava ao rodar em batch (`Assets/Settings/*.asset`,
  `ProjectSettings/*.asset`, `DefaultVolumeProfile.asset`) não entram em commit sem o Felipe
  confirmar. A versão de 09/10/2026 já foi commitada com o ok dele; mudança nova neles = perguntar.
