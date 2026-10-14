# TDFende — instruções para o Claude

**Antes de qualquer tarefa, leia [`docs/MANUAL.md`](docs/MANUAL.md)** (como trabalhar: comandos,
armadilhas do executável, pipelines de arte, licenças) e pegue a próxima tarefa no
[`ROADMAP.md`](ROADMAP.md) (o que fazer, em ordem, com critério de pronto).

- **Estúdio:** os jogos do Felipe, ferramentas, pré-configurações e pendências em comum
  estão em https://claude.ai/artifact/72KXW2Tve5gB65BHD1CDp6 (documento do Claude Docs,
  lido pelo conector).
- **Modelos 3D em comum aos jogos do estúdio:** antes de baixar ou gerar, procure na aba "Modelos 3D"
  do doc do estúdio e no repositório `Felipebonamigo/acervo-3d`. Modelo novo entra nos
  dois no mesmo dia, além do `docs/licencas/manifesto.json`.
- **Rotinas do Claude** em `.claude/skills/`: `verificar`, `captura-do-executavel`, `ponte-unreal`,
  `modelo-3d`, `triar-playtest` e `ligar-unreal-mcp` (sessão do PC, porta 8030; depois a skill `unreal-mcp`
  comanda o editor aberto). Desde 14/10/2026 também `revisar-sem-compilar` (C++ do `Unreal/` sem compilar),
  `achar-gargalo`, `ler-crash`, `publicar-na-steam`, `declaracao-ia`, `conferir-fato`, `som-livre` e `traduzir`.
  Também servem as skills do Claude que já vêm instaladas: `skill-creator` (testa se uma rotina dispara com
  o jeito de o Felipe pedir), `statistical-analysis` (quantas partidas ou rodadas uma comparação precisa
  para valer), `computer-use` (no PC, pelo app do Claude, abre o executável, navega pelos menus e tira
  print; o Felipe liga em Settings > This computer > Computer use) e `fewer-permission-prompts` (no PC,
  libera de vez os comandos só de leitura que mais pedem permissão). Na nuvem, o hook
  `.claude/hooks/session-start.sh` instala o .NET 10.
  Problemas de playtest chegam como Issues "[Playtest]" (`.github/ISSUE_TEMPLATE/playtest.yml`); o
  commit que corrige diz "fecha #N".
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
- **Plataforma (09/10/2026, revista em 14/10/2026):** Steam no PC; celular fora da meta. **O Felipe decidiu migrar a vista para o
  Unreal Engine 5** ("não tem problema a demora, é melhor"). A **Sim em C# continua** (`Runtime/Sim` e `Grid`, testada no FlowSim) e
  fala com o Unreal por uma ponte; o Unity fica congelado como referência até a paridade. O **design da migração está em
  `docs/designs/migracao-unreal.md` (aprovado em 14/10/2026; fases U0 a U4, começando pela ponte)**. Divisão entre a
  sessão na nuvem e a do PC: MANUAL, seção 10.
- **Modelos 3D (pedido do Felipe, 04/10/2026):** primeiro procurar pronto na comunidade para
  baixar (Sketchfab, galeria do Meshy...), conferindo licença e autoria. Gerar no Meshy só em
  último caso, e **sempre perguntar ao Felipe antes** de gastar créditos (dizer quantos: ~30 por
  modelo com textura). Aceitar EULA, logar em conta ou comprar: só o Felipe autoriza.
- Arquivos que o Unity regrava ao rodar em batch (`Assets/Settings/*.asset`,
  `ProjectSettings/*.asset`, `DefaultVolumeProfile.asset`) não entram em commit sem o Felipe
  confirmar. A versão de 09/10/2026 já foi commitada com o ok dele; mudança nova neles = perguntar.
