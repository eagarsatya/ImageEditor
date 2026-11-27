# ImageEditor — Nametag Generator

A user-friendly `.NET 10` interactive TUI & CLI tool for batch-generating event nametag PNGs from **JSON**, **CSV**, or **Excel (`.xlsx`)** attendee lists.

## Features

- **Interactive Terminal UI (TUI)**: Run with no arguments for arrow-key menus, live progress bars, dataset inspection, and an interactive settings editor.
- **Scriptable CLI**: Run `generate` or `inspect` commands directly with flags for fast repeat runs and previews.
- **Multi-Format Input**: Supports `.json`, `.csv`, and `.xlsx` files (case-insensitive headers: `Name`/`Nama`, `Kelompok`/`Group`, `Kamar`/`Room`).
- **Smart Formatting & Auto-Fit**:
  - Automatic Title Case (including initials like `M.` or `T.K`) and configurable word limits.
  - Auto-shrinks font size for long names so text never clips past the nametag edges.
  - Clean, deterministic filenames with invalid-character sanitization and duplicate handling (`_2`, `_3`) instead of random GUIDs.
- **Externalized Configuration**: All paths, fonts, colors, layout positions, and behavior options live in `nametag.config.json`—no code edits or recompiling required.
- **Fast In-Memory Rendering**: Loads the base template image once and clones in memory per attendee.

## Quick Start

### 1. Interactive TUI Mode (Recommended)

```powershell
dotnet run --project src/ImageEditor.Cli
```

From the menu you can:
- **Generate Nametags**: Pick a data file, choose All / Preview first N / Search by Name / Filter by Kelompok / Dry Run, and watch live progress.
- **Inspect Attendee Data**: Preview formatted names, group counts, longest name, and Panitia/Kamar status before generating.
- **Configure Settings & Layout**: Edit template path, output folder, font, size, color, and Y-coordinates directly in `nametag.config.json`.

### 2. Direct CLI Mode

Inspect a dataset:
```powershell
dotnet run --project src/ImageEditor.Cli -- inspect --data "Peserta 1.json"
```

Dry-run validation (no files written):
```powershell
dotnet run --project src/ImageEditor.Cli -- generate --data "Peserta 1.json" --dry-run
```

Generate a single person or small preview:
```powershell
dotnet run --project src/ImageEditor.Cli -- generate --data "Peserta 1.json" --filter "Agnes"
dotnet run --project src/ImageEditor.Cli -- generate --data "Peserta 1.json" --limit 3
```

Generate a specific group or all Panitia:
```powershell
dotnet run --project src/ImageEditor.Cli -- generate --data "Peserta 1.json" --kelompok "09"
dotnet run --project src/ImageEditor.Cli -- generate --data "Panitia.json" --kelompok "Panitia"
```

## Configuration (`nametag.config.json`)

Default configuration lives at the repository root:

```json
{
  "Name": "PLB Nametag Default",
  "TemplateImagePath": "BaseImage/PLB Nametag.png",
  "OutputDirectory": "BaseImage/Result",
  "FontFamily": "Segoe UI",
  "FontFilePath": null,
  "FontSize": 75,
  "MinFontSize": 32,
  "FontBold": true,
  "TextColor": "#000000",
  "MaxTextWidthRatio": 0.85,
  "NameMaxWords": 2,
  "ApplyTitleCase": true,
  "KelompokPrefix": "Kel - ",
  "ShowKamarWhenNoKelompok": false,
  "OrganizeByKelompok": false,
  "OverwriteExisting": true,
  "Layout": {
    "XRatio": 0.5,
    "NameY": 800,
    "NameOnlyY": 925,
    "KelompokY": 925,
    "KamarY": 1050
  }
}
```

## Project Structure

```text
ImageEditor/
├── ImageEditor.sln
├── nametag.config.json
├── src/
│   ├── ImageEditor.Core/        # Rendering engine, data loaders, formatter, config service
│   └── ImageEditor.Cli/         # Spectre.Console interactive TUI + CLI commands
└── tests/
    └── ImageEditor.Core.Tests/  # xUnit tests
```

## Running Tests

```powershell
dotnet test ImageEditor.sln
```
