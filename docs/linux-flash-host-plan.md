# Linux Flash Host Plan

Goal: make Skua playable on Linux from the existing Avalonia branch without Wine/VM and without patching the Artix launcher.

## Current state

- Working branch: `linux-flash-host`, based on `origin/avalonia`.
- `Skua.App.Avalonia` builds on Linux.
- `Skua.Core` and `Skua.Core.Interfaces` already target `net10.0` on `avalonia`.
- Windows Flash path still uses ActiveX/CleanFlash.
- Linux Flash path is stubbed in `Skua.App.Avalonia/Flash/FlashUtil.cs`.
- `Skua.App.Avalonia/Views/MainWindow.axaml.cs` only calls `_flash.InitializeFlash()` on Windows.
- `SkuaStartupHandler` expects SWF to emit `requestLoadGame`, then calls `Flash.Call("loadClient")`.

## Architecture decision

- Keep Avalonia UI.
- Keep Skua C# core.
- Do not extract/patch Artix launcher.
- Build a small owned Electron 8/Chromium 80 PPAPI Flash sidecar.
- Use sidecar only as Flash runtime/game window.
- Connect sidecar to Avalonia/C# via localhost WebSocket JSON-RPC.
- Implement Linux `IFlashUtil` by calling this sidecar.

## Phase 0 — repo setup

- [x] Fork upstream repo with `gh`.
- [x] Clone to `~/Development/Skua`.
- [x] Create `linux-flash-host` from `origin/avalonia`.
- [x] Confirm Avalonia Linux build succeeds.
- [x] Write implementation checklist in `docs/linux-flash-host-plan.md`.

## Phase 1 — Flash runtime proof

- [x] Locate or obtain working Linux PPAPI Flash: `libpepflashplayer.so`.
- [x] Record source/path in docs and avoid committing binary until redistribution legality is clear.
- [x] Add minimal flash host folder, proposed path: `tools/linux-flash-host/`.
- [x] Pin Electron version compatible with PPAPI Flash, likely `electron@8.5.5` to match Artix launcher.
- [x] Create host `package.json`, `main.js`, `preload.js`, `skua.html`.
- [x] Launch Chromium with:
  - [x] `ppapi-flash-path=<path-to-libpepflashplayer.so>`
  - [x] `ppapi-flash-version=32.0.0.371`
  - [x] `plugins: true`
- [x] Serve `skua.html` from `http://game.aq.com/` via localhost proxy/host resolver.
- [x] Create Flash trust file on Linux if needed:
  - [x] `~/.macromedia/Flash_Player/#Security/FlashPlayerTrust/Skua.cfg`
- [x] Serve `skua.swf` from the C# host.
- [x] Embed SWF with `allowScriptAccess=always`.
- [x] Verify SWF loads without crash.
- [x] Verify SWF can reach `https://game.aq.com/game/api/data/gameversion` through the proxy.
- [x] Verify SWF emits JS call `requestLoadGame`.
- [x] Verify JS can call registered SWF callback `loadClient`.
- [x] Verify SWF emits `loaded`.

Hard gate: if PPAPI Flash cannot run or `ExternalInterface` does not work, stop and reassess before touching core.

## Phase 2 — host bridge protocol

- [x] Define WebSocket URL: `ws://127.0.0.1:<port>/skua`.
- [x] Generate random session token in C# and require it in first host message.
- [x] Define message envelope:

```json
{
  "id": 1,
  "type": "call",
  "function": "loadClient",
  "args": []
}
```

```json
{
  "id": 1,
  "type": "result",
  "ok": true,
  "value": "..."
}
```

```json
{
  "type": "flashCall",
  "function": "loaded",
  "args": []
}
```

- [x] Implement JS side:
  - [x] receive C# `call` messages
  - [x] invoke SWF callback/function
  - [x] return result/error
  - [x] expose JS functions called by `ExternalInterface.call`
  - [x] forward SWF calls to C# as `flashCall`
- [x] Support special SWF outbound calls:
  - [x] `requestLoadGame`
  - [x] `pre-load`
  - [x] `loaded`
  - [x] `openWebsite`
  - [x] `debug`
  - [x] `pext`
  - [x] `packet`
  - [x] `packetFromServer`
- [x] Keep protocol localhost-only.
- [x] Log host stdout/stderr through Linux flash trace when `SKUA_FLASH_TRACE=1` is set; keep errors/timeouts logged even when verbose trace is disabled.

## Phase 3 — Linux `IFlashUtil`

- [x] Split XML/object conversion out of Windows `FlashUtil` into shared helper, proposed: `Skua.App.Avalonia/Flash/FlashXmlCodec.cs`.
- [x] Add `Skua.App.Avalonia/Flash/LinuxElectronFlashUtil.cs` or fill `#else` implementation.
- [x] Add small process wrapper, proposed: `ElectronFlashHostProcess`.
- [x] Add WebSocket client/server helper, proposed: `FlashRpcClient`.
- [x] `InitializeFlash()` behavior on Linux:
  - [x] find host files
  - [x] find `skua.swf`
  - [x] find `libpepflashplayer.so`
  - [x] allocate localhost port/token
  - [x] create/update Flash trust file if needed
  - [x] start Electron host process
  - [x] wait for bridge ready
- [x] `Call(string function, params object[] args)` sends RPC to host and returns string result.
- [x] `Call<T>` converts returned value same as Windows behavior.
- [x] `Call(string function, Type type, params object[] args)` mirrors Windows default/error behavior.
- [x] `FlashCall` fires when sidecar sends `flashCall`.
- [x] `CreateFlashObject<T>` continues using existing `FlashObject<T>` wrappers over `lnkCreate` etc.
- [x] `Dispose()` stops sidecar cleanly.
- [ ] Keep Windows ActiveX implementation untouched behind `#if IS_WINDOWS`.

## Phase 4 — Avalonia integration

- [x] Update `MainWindow_Opened` to call `_flash.InitializeFlash()` on Linux too.
- [ ] On Linux, hide embedded `WinFormsFlashHost` and show placeholder text: game runs in separate Flash host window.
- [ ] Preserve Windows embedded host behavior.
- [x] Make `loadClient` idempotent so early/duplicate `requestLoadGame` cannot break load.
- [x] Ensure loading bar hides on `loaded` even when game window is sidecar.
- [ ] Add menu/action to restart Flash host if it crashes.
- [ ] Add user-facing error if Flash plugin missing.

## Phase 5 — assets and build workflow

- [ ] Decide source of `skua.swf`:
  - [ ] from AS3 build output `Skua.AS3/skua/bin/skua.swf`, or
  - [ ] extracted from official release for dev-only bootstrap.
- [x] Add Linux dev script, proposed: `scripts/dev-linux-skua.sh`.
- [ ] Add build docs for Linux:
  - [ ] .NET 10 SDK
  - [ ] Node/npm or bundled Electron host package
  - [ ] PPAPI Flash path config
  - [ ] `dotnet run --project Skua.App.Avalonia`
- [ ] Do not commit proprietary Flash binary unless license allows.
- [ ] Add `.gitignore` entries for local Flash plugin/cache if needed.

## Phase 6 — validation

- [x] `dotnet build Skua.App.Avalonia/Skua.App.Avalonia.csproj` passes on Linux.
- [x] App starts on Linux without `NotImplementedException`.
- [x] Flash host opens and loads AQW game SWF.
- [x] Manual login works.
- [x] Server list loads.
- [x] `loaded` initializes Skua state.
- [x] Script search/load works.
- [x] Simple script starts/stops.
- [x] Packet events (`pext`) reach C#.
- [x] Logs show Flash errors clearly.
- [ ] Closing Avalonia app kills sidecar process.

## Phase 7 — packaging later

- [x] Add `skua` launcher script outside repo for workstation use.
- [ ] Add optional desktop entry.
- [ ] Add optional alias after user approval.
- [ ] Decide whether to bundle host Electron or require npm install.
- [ ] Decide whether to vendor Flash plugin, download it, or require manual path.

## Linux bridge diagnostics

- Verbose bridge tracing is off by default.
- Enable full trace with `SKUA_FLASH_TRACE=1 skua` or `SKUA_FLASH_TRACE=1 ./scripts/dev-linux-skua.sh`.
- Trace path defaults to `/tmp/skua-linux-flash-trace.log`; override with `SKUA_FLASH_TRACE_PATH=/path/to/log`.
- Payloads are truncated/redacted by default; opt into larger payload previews with `SKUA_FLASH_TRACE_PAYLOADS=1`.
- Error, timeout, crash, close, and pending-failure events remain logged even without verbose trace.
- Flash callbacks are queued off the WebSocket receive loop; do not invoke script/game handlers synchronously from the receive loop.

## AQW Flash compatibility notes

- AQW `Game*.swf` calls `SharedObject.getLocal("AQWChars", "/", true)`.
- Flash throws `Error #2134: Cannot create SharedObject` when that secure SharedObject is created from the local HTTP proxy origin.
- Linux proxy patches the loaded AQW game SWF bytecode in memory only: `pushtrue` -> `pushfalse` for that exact `AQWChars` call.
- The patch is limited to `Game*.swf`; title/background SWFs remain unmodified.

## Risks

- PPAPI Flash binary may be unavailable or non-redistributable.
- Local SWF sandbox may block network or ExternalInterface until trust config is right.
- Electron 8 is old and insecure; keep localhost only and load only local host page + AQW SWF traffic.
- Ruffle fallback likely fragile for AQW/Skua; treat as later experiment, not primary path.
- Full UI polish should wait until Flash bridge works.

## Definition of done for MVP

- Linux user runs Avalonia Skua.
- Separate Flash host window opens AQW.
- User logs in.
- Skua sees `loaded`.
- User loads and starts a basic script.
- User stops script.
- App exits cleanly.

Current MVP status: manual Linux login + `LinuxBridgeSmokeTest.cs` script start/join succeeded on 2026-06-14.
