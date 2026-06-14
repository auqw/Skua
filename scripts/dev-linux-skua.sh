#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

export SKUA_FLASH_HOST_DIR="${SKUA_FLASH_HOST_DIR:-$repo_root/tools/linux-flash-host}"
export SKUA_SWF_PATH="${SKUA_SWF_PATH:-$repo_root/Skua.AS3/skua/bin/skua.swf}"
if [[ -z "${SKUA_ELECTRON_BIN:-}" && -x "$repo_root/tools/linux-flash-host/electron8/electron" ]]; then
  export SKUA_ELECTRON_BIN="$repo_root/tools/linux-flash-host/electron8/electron"
else
  export SKUA_ELECTRON_BIN="${SKUA_ELECTRON_BIN:-electron}"
fi
unset NODE_OPTIONS

if [[ -z "${SKUA_FLASH_PLUGIN:-}" ]]; then
  cat >&2 <<'MSG'
SKUA_FLASH_PLUGIN is not set.
Set it to a Linux PPAPI Flash plugin, e.g.:
  export SKUA_FLASH_PLUGIN=/path/to/libpepflashplayer.so
MSG
  exit 2
fi

if [[ ! -f "$SKUA_SWF_PATH" ]]; then
  cat >&2 <<MSG
skua.swf not found at: $SKUA_SWF_PATH
Build the AS3 client or set SKUA_SWF_PATH to an existing skua.swf.
MSG
  exit 2
fi

if ! command -v "$SKUA_ELECTRON_BIN" >/dev/null 2>&1 && [[ ! -x "$SKUA_ELECTRON_BIN" ]]; then
  cat >&2 <<MSG
Electron binary not found: $SKUA_ELECTRON_BIN
Install/use Electron 8.5.5 for the flash host, or set SKUA_ELECTRON_BIN.
MSG
  exit 2
fi

dotnet run --project "$repo_root/Skua.App.Avalonia/Skua.App.Avalonia.csproj" -- "$@"
