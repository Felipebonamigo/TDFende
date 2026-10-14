# Abre o TDFende no Unreal (versão de desenvolvimento, sem empacotar), em janela, contra a IA.
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\Jogar.ps1 [-Dificuldade 0|1|2] [-TelaCheia]
# Controles: botão esquerdo constrói (ou sobe a torre), direito vende, Q/E troca a torre, 1-9 envia bichos,
# WASD/setas movem a câmera, a roda do mouse aproxima. A partida fica gravada em Unreal\TDFende\Saved\Tdf\partida.txt.
# Antes da primeira vez: Tools\SimBridge\PublicarParaUnreal.ps1, RodarU0.ps1 (compila) e ImportarArte.ps1 (as duas receitas).
param([int]$Dificuldade = 1, [switch]$TelaCheia)
$uproj = Join-Path $PSScriptRoot 'TDFende.uproject'
$ue = 'C:\Program Files\Epic Games\UE_5.8\Engine\Binaries\Win64\UnrealEditor.exe'
$janela = if ($TelaCheia) { @('-fullscreen') } else { @('-windowed', '-ResX=1600', '-ResY=900') }
Start-Process $ue -ArgumentList (@("`"$uproj`"", '-game', '-nosplash', "-TdfDificuldade=$Dificuldade") + $janela)
