# Importa a arte para o Content do Unreal, sem abrir o editor (UnrealEditor-Cmd + Python).
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\ImportarArte.ps1                      torres e bichos (importa_arte.py)
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\ImportarArte.ps1 -Script importa_ambiente.py   chão, rochas, troncos, grama
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\ImportarArte.ps1 -Script importa_efeitos.py    materiais dos efeitos de combate
# Refaz /Game/TDFende (Content/ fica fora do git: os scripts são a receita; backup no D: pelo Tools\Backup.ps1).
param([string]$Script = 'importa_arte.py')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$uproj = Join-Path $PSScriptRoot 'TDFende.uproject'
$py = Join-Path $PSScriptRoot (Join-Path 'Scripts' $Script)
$ue = 'C:\Program Files\Epic Games\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe'
$env:TDF_REPO = $repo
$base = [System.IO.Path]::GetFileNameWithoutExtension($Script)
$log = Join-Path $PSScriptRoot "Saved\$base`_unreal.log"
$rel = Join-Path $PSScriptRoot "Saved\$base.log"
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
if (Test-Path $rel) { Remove-Item $rel }
& $ue "`"$uproj`"" '-run=pythonscript' "-script=`"$py`"" -unattended -nosplash -nullrhi -NoSound "-abslog=`"$log`""
if (Test-Path $rel) { Get-Content $rel } else { Write-Host "sem relatório: veja $log"; exit 1 }
$erros = Select-String -Path $log -Pattern 'LogPython: Error' | Select-Object -First 8
if ($erros) { $erros | ForEach-Object { $_.Line }; exit 1 }
exit 0
