<div align="center">

![Skua Icon](https://raw.githubusercontent.com/auqw/Skua/refs/heads/master/SkuaIcon.ico)

## [Linux Release](https://github.com/NaxeCode/Skua/releases/latest) | [Usage](./usage.md) | [Build Guide](./BUILD.md) | [Linux Preview](#linux-preview-avalonia) | [Contributors](#contributors) | [Support](#skua-developers)

</div>

### About Skua

Skua is the successor to [RBot](https://github.com/rodit/RBot) (originally made by "[rodit](https://github.com/rodit)"), now remade and rebranded by [BrenoHenrike](https://github.com/BrenoHenrike/), with the help of [Lord Exelot](https://github.com/BrenoHenrike/), and a handful of scripters. It is a third-party client made by the people mentioned above. It also has many "features" and quirks. Overall, it will make this glorified flash game on steroids a piece of cake.

This fork is extended by [NaxeCode](https://github.com/NaxeCode) to bring Skua to Linux through the Avalonia client, a native Electron 8 + PPAPI Flash sidecar, localhost Flash/C# bridge, Linux packaging, and release artifacts.

## Linux Preview (Avalonia)

**Latest Linux preview release:** <https://github.com/NaxeCode/Skua/releases/latest>

The Avalonia build can run AQW natively on Linux without Wine by launching a small Electron 8 + PPAPI Flash sidecar and bridging Flash `ExternalInterface` calls back to Skua over localhost WebSocket RPC.

Status:

- Native Linux UI through `Skua.App.Avalonia`.
- AQW loads in a separate Flash host window.
- Login, server selection, packet events, and basic scripts work.
- Windows ActiveX Flash path remains unchanged.

### Linux release contents

The public tarball includes Skua, `skua.swf`, the Linux Flash host source files, and `run-skua.sh`.

It does **not** redistribute these local/proprietary runtime files by default:

- Electron 8 binary
- `node_modules`
- PPAPI Flash plugin (`libpepflashplayer.so`)

You must provide Electron 8 and PPAPI Flash paths with environment variables.

### Install runtime dependencies

Arch / EndeavourOS / Manjaro:

```bash
sudo pacman -S dotnet-runtime dotnet-sdk nss gtk3 libxss alsa-lib
```

Ubuntu / Debian / Pop!_OS / Mint:

```bash
sudo apt update
sudo apt install dotnet-runtime-10.0 dotnet-sdk-10.0 libnss3 libgtk-3-0 libxss1 libasound2t64
```

If `libasound2t64` is not available on your Debian/Ubuntu version, use:

```bash
sudo apt install libasound2
```

Fedora:

```bash
sudo dnf install dotnet-runtime-10.0 dotnet-sdk-10.0 nss gtk3 libXScrnSaver alsa-lib
```

openSUSE:

```bash
sudo zypper install dotnet-runtime-10.0 dotnet-sdk-10.0 mozilla-nss gtk3 libXScrnSaver alsa
```

NixOS / nix shell example:

```bash
nix shell nixpkgs#dotnet-sdk_10 nixpkgs#nss nixpkgs#gtk3 nixpkgs#libXScrnSaver nixpkgs#alsa-lib
```

### Download and run release

```bash
curl -L -o skua-linux-x64.tar.gz \
  https://github.com/NaxeCode/Skua/releases/latest/download/skua-linux-x64.tar.gz

tar -xzf skua-linux-x64.tar.gz
cd skua-linux-x64-*

export SKUA_ELECTRON_BIN=/path/to/electron-8/electron
export SKUA_FLASH_PLUGIN=/path/to/libpepflashplayer.so

./run-skua.sh
```

Optional checksum verification:

```bash
curl -L -o skua-linux-x64.tar.gz.sha256 \
  https://github.com/NaxeCode/Skua/releases/latest/download/skua-linux-x64.tar.gz.sha256
sha256sum -c skua-linux-x64.tar.gz.sha256
```

### Runtime variables

```bash
SKUA_ELECTRON_BIN=/path/to/electron-8/electron
SKUA_FLASH_PLUGIN=/path/to/libpepflashplayer.so
SKUA_FLASH_TRACE=1                 # verbose Linux Flash bridge diagnostics
SKUA_FLASH_TRACE_PAYLOADS=1        # larger payload previews
```

### Development run from source

```bash
export SKUA_ELECTRON_BIN=/path/to/electron-8/electron
export SKUA_FLASH_PLUGIN=/path/to/libpepflashplayer.so
./scripts/dev-linux-skua.sh
```

Create a Linux release artifact from source:

```bash
./scripts/package-linux-skua.sh
```

Output:

```txt
releases/skua-linux-x64-<commit>.tar.gz
releases/skua-linux-x64-<commit>.tar.gz.sha256
```

For local/private bundles where you may include local runtime files:

```bash
SKUA_PACKAGE_LOCAL_RUNTIME=1 ./scripts/package-linux-skua.sh
```

Implementation checklist: [`docs/linux-flash-host-plan.md`](./docs/linux-flash-host-plan.md).

### Do we store information online?

The *only* things that get recorded are: the auto-generated number **(not your actual game user ID)** to identify you, the number of scripts run (stopped & started), and the start and stop timestamps. This can be completely opted out of when first running a script, or you can edit the text file ***“DataCollectionSettings”*** in your `Documents\Skua > DataCollectionSettings.txt`. If you make it look as shown below, it will send absolutely nothing 👍

```txt
UserID: null
genericDataConsent: false
scriptNameConsent: false
stopTimeConsent: false
```

### What do we use this data for?

To keep track of what bots are run, how often, how long, and just how popular some bots are.

### For Account Manager

Your **Account Info** will be stored only in your **appdata** and never shown anywhere, nor in a text file. We **DO NOT** store it online because we intended to make an account manager with **no database**.

### Some examples of the types of scripts Skua has

- **Story scripts** found in the `Story` folder.
- **Merge scripts** found in the `Other > MergeShops` folder.
- **Farming scripts** found in the `Farm` folder. These include, but are not limited to, Gold, Experience, Class Points, and Reputation.
- **Faction-specific** (nation/legion/etc) can be found in their respective folders.
- Specific tools such as **Butler** (a follow and kill [doesn't support quests]), "ChooseBestGear" (a script that will look at your inv, and equip the appropriate setting for the race type you select.), BuyOut ( will either buy **all/non-ac/ac** (will prompt due to ACs) from a specified shop)
- **Core Script Files** are not meant to be run.
- **0ScriptName.cs** are basically "Do everything required for this script."
- If you wanted to have a new farming script that doesn't exist, though, please request it
in the Discord

### [Skua Discord](https://discord.com/invite/CKKbk2zr3p) Join the community and get help with Skua

### For questions or help, go to the [#skua-help](https://discord.com/channels/1090693457586176013/1090741396970938399) channel

## Skua Developers

Skua developers need your support to improve Skua. You can donate or sponsor us by clicking the PayPal link below. Thank you for your support.

### purple/SharpTheNightmare (Current Dev)

- [Ko-Fi](https://ko-fi.com/sharpthenightmare)
- ETH: `0xd66fb89f503c9c14093479178d817c9e87d7c0de`

### [Breno Henrike's PayPal (Inactive) (Creator)](https://www.paypal.com/donate?hosted_button_id=QVQ4Q7XSH9VBY)

### [Lord Exelot's PayPal (Inactive) (Brief work on Skua, Ex Scripts Manager)](www.paypal.me/LordExelot)

## Contributors

- **Breno Henrike**, the artist of Skua.
- **SharpTheNightmare**, Lead Developer from 1.2.4.0-Current.
- **Lord Exelot**, Ex scripts manager.
- **Tato**, the current scripts manager and Skua Discord owner.
- **Skua Heroes**, the script makers and helpers.
- **Boaters** are the ones who sail overnight using Skua and help the Skua team to improve, thanks to their feedback and suggestions **which is you**.
