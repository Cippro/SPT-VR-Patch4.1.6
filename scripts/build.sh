#!/usr/bin/env bash
set -euo pipefail
repository=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
if [[ -f /workspace/shared/spt-vr-setup/env.sh ]]; then
  source /workspace/shared/spt-vr-setup/env.sh
fi
cd "$repository"
options=()
if [[ -n ${SPT_VR_REFERENCES:-} ]]; then options+=("-p:SptVrReferences=$SPT_VR_REFERENCES"); fi
dotnet restore src/SPT-VR/TarkovVR.csproj --locked-mode "${options[@]}"
dotnet build src/SPT-VR/TarkovVR.csproj --no-restore --configuration Release --maxcpucount:2 "${options[@]}"
if [[ -n ${SPT_VR_REFERENCES:-} ]]; then
  references=$SPT_VR_REFERENCES
else
  references=$(python3 - <<'PY'
import xml.etree.ElementTree as ET
print(ET.parse('/workspace/shared/spt-vr-setup/publicized.props').findtext('PropertyGroup/SptVrReferences'))
PY
)
fi
dotnet restore tests/Compatibility/Compatibility.csproj --locked-mode
dotnet build tests/Compatibility/Compatibility.csproj --no-restore --configuration Release
mkdir -p artifacts
dotnet run --no-build --no-restore --configuration Release --project tests/Compatibility/Compatibility.csproj -- \
  "$repository/src/SPT-VR/bin/Release/netstandard2.1/SPT-VR.dll" \
  "$references/Managed" "$repository/src/SPT-VR/lib" > artifacts/compatibility.json
python3 - <<'PY'
import json
from pathlib import Path
report = json.loads(Path('artifacts/compatibility.json').read_text())
print('Compatibility:', report['bound'], 'bound patches;', len(report['failures']), 'failures;', report['dynamicCount'], 'dynamic targets.')
PY
raw_references=${SPT_VR_RAW_REFERENCES:-$references}
if [[ -z ${SPT_VR_RAW_REFERENCES:-} && -z ${SPT_VR_REFERENCES:-} && -f /workspace/shared/spt-vr-setup/references.props ]]; then
  raw_references=$(python3 - <<'PY'
import xml.etree.ElementTree as ET
print(ET.parse('/workspace/shared/spt-vr-setup/references.props').findtext('PropertyGroup/SptVrReferences'))
PY
)
fi
dotnet restore tests/JsonRegression/JsonRegression.csproj --locked-mode "-p:SptVrReferences=$raw_references"
dotnet build tests/JsonRegression/JsonRegression.csproj --no-restore --configuration Release "-p:SptVrReferences=$raw_references"
dotnet run --no-build --no-restore --configuration Release --project tests/JsonRegression/JsonRegression.csproj -- \
  "$raw_references/Managed" | tee artifacts/json-regression.log
cp src/SPT-VR/bin/Release/netstandard2.1/SPT-VR.dll artifacts/SPT-VR.dll
python3 scripts/package.py
