#pragma once

#include "CoreMinimal.h"

/**
 * Selo de build (MANUAL, seção do selo): "build <hash>" de `git describe --always --dirty` ("-dirty" = gerado com mudança sem
 * commit). No jogo empacotado vem do arquivo Selo.txt que o Empacotar.ps1 grava na pasta do projeto empacotado; rodando pelo
 * editor, pergunta ao git na hora. Sem os dois: "sem selo" (e o autoteste reprova).
 */
FString TdfBuildStamp();
