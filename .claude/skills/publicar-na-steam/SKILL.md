---
name: publicar-na-steam
description: 'Empacota o TDFende (Unreal, ou o executável do Unity), confere o build e sobe para a Steam pelo SteamPipe, parando onde só o Felipe pode seguir (login, Steam Guard, branch padrão). Use quando o Felipe pedir "sobe para a Steam", "gera o build da loja", "publica no beta" ou "prepara o lançamento".'
---
# Publicar na Steam

Estado hoje: não há App ID, conta, depot nem script de envio. O plano está na Fase 18 (LAN-01 a LAN-04: steamcmd por script,
branch beta, notas por hash, hotfix) e na META-03a da Fase 10 (Steam Direct, App ID, página "Em breve"); os testes da
Steam usam o App ID 480 (META-04).

## O que só o Felipe faz

- Conta Steamworks, taxa do Steam Direct, dados fiscais e bancários, criar o app (sai o **App ID**) e os depots.
- O login no `steamcmd`, com a **conta de build** (nunca a pessoal) e o Steam Guard. O Claude nunca digita senha nem código.
- Pôr o build no branch **padrão** (o que todo mundo baixa) e apertar o botão de lançar. O `steamcmd` só põe no ar
  branches beta; o padrão é sempre no painel, pela mão do Felipe.
- Aceitar termos, pagar ou mudar preço.

## Passos

1. Verifique o jogo (skill `verificar`). Nada sobe com teste vermelho.
2. Empacote: 
   - Unreal (o caminho da venda): ainda não há script; é a U4 do `ROADMAP.md`. O comando-base:
     `RunUAT.bat BuildCookRun -project=<...>\Unreal\TDFende\TDFende.uproject -platform=Win64 -clientconfig=Shipping -build -cook -stage -pak -archive -archivedirectory=<pasta> -noP4`.
     Confira que a `tdfende_sim.dll` foi junto (o `Build.cs` declara em `RuntimeDependencies`) e que o `DefaultGame.ini`
     cozinha `/Game/TDFende`.
   - Unity (congelado, só se o Felipe pedir): `Tools\GerarExecutavel.ps1` gera `Builds\Windows\TDFende.exe` e confere as
     licenças antes. Antes de qualquer build público, atualize para o último Unity 6000.3.x (CVE-2025-59489, MANUAL §9).
3. Confira o build antes de subir:
   - roda sem o editor e sem a pasta do código (abra o executável empacotado);
   - tamanho total e o que entrou: nada de `.pdb`, log, save de teste, arquivo-fonte de arte ou pasta `Saved/`;
   - versão do jogo visível no menu ou no log, igual à da tag;
   - licenças e créditos no build (skill `declaracao-ia` e o registro de licenças do jogo).
4. Prepare o SteamPipe: não existe ainda; crie em `Tools/Steam/` o `app_build_<AppID>.vdf` e o depot com `"FileExclusion" "*.pdb"` quando o
   Felipe passar o App ID. O build nunca entra no git (nada acima de 10 MB). No `app_build`, `SetLive` aponta para um branch beta (ex.: `"SetLive" "beta"`).
   Guarde fora do depot os símbolos (`.pdb`) da mesma versão: sem eles, a pilha de um crash vem só com endereços.
5. Peça ao Felipe para rodar o envio, com a conta de build: `steamcmd +login <conta_de_build> +run_app_build <caminho>\Tools\Steam\app_build_<AppID>.vdf +quit`.
6. O Felipe instala pelo cliente Steam, no branch beta, e joga 5 minutos. Só então ele promove ao padrão no painel.
7. Anote a versão, a data, o branch e o tamanho em `docs/ESTADO-ATUAL.md`.

## Armadilhas

- A Valve revisa a página e o build antes do lançamento e exige a página "Em breve" no ar por um tempo mínimo. Prazos
  mudam: confira no Steamworks antes de marcar data.
- Conteúdo de IA: a declaração da página tem que bater com o build (skill `declaracao-ia`).
- Royalties da Epic (5 % acima de US$ 1 milhão) estão "a conferir pelo Felipe".
