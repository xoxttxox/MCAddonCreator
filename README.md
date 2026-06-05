# MC Addon Creator

<div align="center">

[![Version](https://img.shields.io/github/v/release/xoxttxox/MC-Addon-Ersteller)](https://github.com/xoxttxox/MC-Addon-Ersteller/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/xoxttxox/MC-Addon-Ersteller/total)](https://github.com/xoxttxox/MC-Addon-Ersteller/releases)
[![License](https://img.shields.io/github/license/xoxttxox/MC-Addon-Ersteller)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-blue)](https://dotnet.microsoft.com/)

A modern Windows application for creating Minecraft Bedrock Edition Addons (`.mcaddon`) from Behavior Packs (BP) and Resource Packs (RP).

</div>

## Overview

**MC Addon Creator** is a lightweight Windows application that allows you to quickly build Minecraft Bedrock Edition Addons in the `.mcaddon` format.

![Preview](docs/preview.png)

The application supports Behavior Packs, Resource Packs, or a combination of both and automatically generates a clean `.mcaddon` package with a versioned file name.

Example:

```txt
MyAddon_v1_0_0.mcaddon
```

## Features

* Create `.mcaddon` files from Behavior Packs and Resource Packs
* Supports:

  * Behavior Pack only
  * Resource Pack only
  * Behavior Pack + Resource Pack
* Supports folders, `.zip`, `.mcpack`, and `.mcaddon` files as input
* Automatically reads `manifest.json`
* Detects packs even when they are stored inside an additional root directory
* Automatic output filename generation using pack name and version
* Detailed build log
* Progress display in the main window and status bar
* Fixed and compact application size
* Native Windows title bar and controls
* Dark status bar support
* Original source files are never modified

## Supported Inputs

| Source               | Supported |
| -------------------- | --------- |
| Behavior Pack Folder | Yes       |
| Resource Pack Folder | Yes       |
| .zip Archive         | Yes       |
| .mcpack              | Yes       |
| .mcaddon             | Yes       |

## Output

Generated output format:

```txt
Name_v1_0_0.mcaddon
```

Examples:

```txt
MyAddon_v1_0_0.mcaddon
CityTextures_v2_1_0.mcaddon
```

## Development Requirements

To build or modify the project, you will need:

* Windows 10 or Windows 11
* Visual Studio 2022
* .NET Desktop Development workload
* .NET 10 SDK

Target Framework:

```txt
net10.0-windows
```

## Opening the Project

Open the solution:

```txt
MCAddonErsteller.sln
```

Or directly open the project:

```txt
src\MCAddonErsteller\MCAddonErsteller.csproj
```

## Standard Build

```bat
build\build-release.bat
```

Or:

```bat
dotnet build src\MCAddonErsteller\MCAddonErsteller.csproj -c Release
```

## Publish Single Executable

Recommended for GitHub Releases:

```bat
build\publish-win-x64.bat
```

Output:

```txt
release\MCAddonErsteller.exe
```

The executable is published as a self-contained single-file application, meaning users typically do not need to install the .NET Runtime separately.

## PowerShell Build

```powershell
.\build\publish-win-x64.ps1
```

## GitHub Release

1. Push the project to GitHub.
2. Build the release:

```bat
build\publish-win-x64.bat
```

3. Create a new GitHub Release.
4. Upload:

```txt
release\MCAddonErsteller.exe
```

Optionally, upload the source code archive as well.

## Project Structure

```txt
MC-Addon-Ersteller/
├─ .github/
│  └─ workflows/
│     └─ build.yml
├─ assets/
│  ├─ fonts/
│  ├─ icons/
│  ├─ app.ico
│  ├─ app_icon.png
│  ├─ app_icon_trans.png
│  └─ background.png
├─ build/
│  ├─ build-release.bat
│  ├─ publish-win-x64.bat
│  └─ publish-win-x64.ps1
├─ docs/
│  └─ preview.png
├─ release/
├─ src/
│  └─ MCAddonErsteller/
│     ├─ assets/
│     ├─ Controls/
│     ├─ Models/
│     ├─ Properties/
│     ├─ Resources/
│     ├─ Services/
│     ├─ FontManager.cs
│     ├─ MainForm.cs
│     ├─ MainForm.resx
│     ├─ Program.cs
│     ├─ app.manifest
│     └─ MCAddonErsteller.csproj
├─ .gitattributes
├─ .gitignore
├─ LICENSE
├─ MCAddonErsteller.sln
└─ README.md
```

## Notes

* `.mcaddon` files are technically ZIP archives with a different file extension.
* Behavior Packs and Resource Packs must contain a valid `manifest.json`.
* If only a Behavior Pack is selected, only that pack will be included.
* If only a Resource Pack is selected, only that pack will be included.
* If both packs are selected, both will be packaged together.

## License

This project is licensed under the MIT License.

See the [LICENSE](LICENSE) file for more information.
