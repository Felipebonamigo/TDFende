#!/usr/bin/env bash
# Teste de fumaça da ponte nativa (Linux/nuvem): publica a Sim em NativeAOT, roda um replay pelo C e compara o fingerprint
# com o do FlowSim (C# gerenciado). Sai com 0 se forem iguais. Uso: Tools/SimBridge/Smoke.sh [replay.txt]
set -euo pipefail
cd "$(dirname "$0")/../.."
REPLAY="${1:-Tools/SimBridge/smoke/demo_replay.txt}"
dotnet publish Tools/SimBridge -c Release -r linux-x64 --nologo -v quiet
gcc -O1 -o /tmp/tdf_smoke Tools/SimBridge/smoke/smoke.c -ldl
NATIVE=$(/tmp/tdf_smoke Tools/SimBridge/bin/Release/net10.0/linux-x64/publish/tdfende_sim.so "$REPLAY" | grep '^fingerprint:' | sed 's/^fingerprint: //')
MANAGED=$(dotnet run --project Tools/FlowSim -v quiet -- replay "$REPLAY" --forcar | grep 'fingerprint:' | tail -1 | sed 's/^ *fingerprint: //')
echo "nativo:     $NATIVE"
echo "gerenciado: $MANAGED"
[ "$NATIVE" = "$MANAGED" ] && echo "PONTE OK: fingerprints iguais" || { echo "PONTE FALHOU: fingerprints diferentes"; exit 1; }
