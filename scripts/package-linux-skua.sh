#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
configuration="${CONFIGURATION:-Release}"
runtime="${RUNTIME:-linux-x64}"
short_sha="$(git -C "$repo_root" rev-parse --short HEAD 2>/dev/null || echo local)"
version="${VERSION:-$short_sha}"
runtime_label="${runtime#linux-}"
artifact_name="skua-linux-${runtime_label}-${version}"
releases_dir="${RELEASES_DIR:-$repo_root/releases}"
publish_dir="$repo_root/artifacts/publish/$artifact_name"
stage_dir="$repo_root/artifacts/package/$artifact_name"
archive_path="$releases_dir/$artifact_name.tar.gz"

if [[ ! -f "$repo_root/Skua.AS3/skua/bin/skua.swf" ]]; then
  cat >&2 <<'MSG'
skua.swf is missing at Skua.AS3/skua/bin/skua.swf.
Build the AS3 client first, or provide the SWF before packaging.
MSG
  exit 2
fi

rm -rf "$publish_dir" "$stage_dir"
mkdir -p "$publish_dir" "$stage_dir" "$releases_dir"

dotnet publish "$repo_root/Skua.App.Avalonia/Skua.App.Avalonia.csproj" \
  --configuration "$configuration" \
  --runtime "$runtime" \
  --self-contained false \
  --output "$publish_dir" \
  -p:PublishSingleFile=false \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -m:1

cp -a "$publish_dir/." "$stage_dir/"
# The project copies the open-source host files into publish output. Strip local-only
# runtime blobs unless explicitly requested below.
rm -rf \
  "$stage_dir/linux-flash-host/electron8" \
  "$stage_dir/linux-flash-host/plugins" \
  "$stage_dir/linux-flash-host/node_modules" \
  "$stage_dir/linux-flash-host/package-lock.json"

cat > "$stage_dir/run-skua.sh" <<'RUNNER'
#!/usr/bin/env bash
set -euo pipefail
app_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export SKUA_FLASH_HOST_DIR="${SKUA_FLASH_HOST_DIR:-$app_dir/linux-flash-host}"
export SKUA_SWF_PATH="${SKUA_SWF_PATH:-$app_dir/skua.swf}"

if [[ -z "${SKUA_ELECTRON_BIN:-}" && -x "$app_dir/linux-flash-host/electron8/electron" ]]; then
  export SKUA_ELECTRON_BIN="$app_dir/linux-flash-host/electron8/electron"
elif [[ -z "${SKUA_ELECTRON_BIN:-}" ]]; then
  export SKUA_ELECTRON_BIN="electron"
fi

if [[ -z "${SKUA_FLASH_PLUGIN:-}" && -f "$app_dir/linux-flash-host/plugins/libpepflashplayer.so" ]]; then
  export SKUA_FLASH_PLUGIN="$app_dir/linux-flash-host/plugins/libpepflashplayer.so"
fi

unset NODE_OPTIONS

if [[ -z "${SKUA_FLASH_PLUGIN:-}" ]]; then
  cat >&2 <<'MSG'
SKUA_FLASH_PLUGIN is not set.
Set it to a Linux PPAPI Flash plugin path, for example:
  export SKUA_FLASH_PLUGIN=/path/to/libpepflashplayer.so
MSG
  exit 2
fi

exec "$app_dir/Skua.App.Avalonia" "$@"
RUNNER
chmod +x "$stage_dir/run-skua.sh"

cat > "$stage_dir/README-LINUX.md" <<'README'
# Skua Linux Preview

Run:

```bash
./run-skua.sh
```

Requirements:

- .NET runtime matching the project target (`net10.0`) unless you publish self-contained yourself.
- Electron 8.5.x with PPAPI Flash support, either on `PATH` as `electron` or set with `SKUA_ELECTRON_BIN`.
- Linux PPAPI Flash plugin set with `SKUA_FLASH_PLUGIN=/path/to/libpepflashplayer.so`.

Optional environment:

```bash
SKUA_ELECTRON_BIN=/path/to/electron
SKUA_FLASH_PLUGIN=/path/to/libpepflashplayer.so
SKUA_FLASH_TRACE=1                 # verbose Linux Flash bridge diagnostics
SKUA_FLASH_TRACE_PAYLOADS=1        # include larger payload previews
```

Runtime blobs such as Electron, node_modules, and Flash are not redistributed in source packages by default.
README

if [[ "${SKUA_PACKAGE_LOCAL_RUNTIME:-0}" == "1" ]]; then
  mkdir -p "$stage_dir/linux-flash-host"
  for path in electron8 plugins node_modules package-lock.json; do
    if [[ -e "$repo_root/tools/linux-flash-host/$path" ]]; then
      cp -a "$repo_root/tools/linux-flash-host/$path" "$stage_dir/linux-flash-host/"
    fi
  done
fi

tar -C "$(dirname "$stage_dir")" -czf "$archive_path" "$(basename "$stage_dir")"
(
  cd "$releases_dir"
  sha256sum "$(basename "$archive_path")" > "$(basename "$archive_path").sha256"
)

printf 'Created %s\n' "$archive_path"
printf 'Checksum %s.sha256\n' "$archive_path"
