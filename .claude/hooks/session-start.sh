#!/bin/bash
# Prepara a sessão do Claude na nuvem: instala o .NET SDK 10 (FlowSim, AuditaLicencas,
# AuditaAssets e a ponte NativeAOT do Tools/SimBridge/Smoke.sh) e baixa os pacotes uma vez.
# Não liga o .githooks na nuvem: o pre-commit roda o CompileCheck, que precisa das DLLs
# do Unity instalado no PC.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

DOTNET_DIR="$HOME/.dotnet"
if ! "$DOTNET_DIR/dotnet" --list-sdks 2>/dev/null | grep -q '^10\.'; then
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$DOTNET_DIR" --no-path
fi

export DOTNET_ROOT="$DOTNET_DIR"
export PATH="$DOTNET_DIR:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo "export DOTNET_ROOT=\"$DOTNET_DIR\""
    echo "export PATH=\"$DOTNET_DIR:\$PATH\""
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
    echo 'export DOTNET_NOLOGO=1'
  } >> "$CLAUDE_ENV_FILE"
fi

cd "$CLAUDE_PROJECT_DIR"
dotnet restore Tools/FlowSim -v quiet
dotnet restore Tools/AuditaLicencas -v quiet
dotnet restore Tools/AuditaAssets -v quiet
