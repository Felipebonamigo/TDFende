# Publica a Sim em NativeAOT (Windows) e copia a DLL para o projeto Unreal. Rode no PC, na raiz do repositório.
# Uso: powershell -ExecutionPolicy Bypass -File Tools\SimBridge\PublicarParaUnreal.ps1
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repo
# o ILCompiler chama 'vswhere' pelo PATH para achar o link.exe do Visual Studio; o VS não o põe lá (U0, 14/10/2026)
$vswhereDir = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if ((Test-Path $vswhereDir) -and ($env:PATH -notlike "*$vswhereDir*")) { $env:PATH = "$vswhereDir;$env:PATH" }
dotnet publish Tools/SimBridge -c Release -r win-x64 --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou (NativeAOT no Windows precisa do 'Desktop development with C++' do Visual Studio Build Tools)" }
$dll = Join-Path $repo 'Tools\SimBridge\bin\Release\net10.0\win-x64\publish\tdfende_sim.dll'
if (-not (Test-Path $dll)) { throw "não achei $dll" }
$dest = Join-Path $repo 'Unreal\TDFende\Binaries\Win64'
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item $dll $dest -Force
Write-Host "copiado: $dest\tdfende_sim.dll"
