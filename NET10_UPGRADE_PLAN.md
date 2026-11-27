# Plan: Upgrade ImageEditor from .NET 8 to .NET 10

## Goal

Upgrade all projects in `ImageEditor.sln` from `net8.0` to `net10.0`, update documentation references, and verify that restore, build, unit tests, and CLI smoke tests pass cleanly with zero errors.

## Current State

- **Installed SDKs:** `.NET 10` SDK is already installed on this machine (`10.0.202`, `10.0.401`).
- **Solution file:** `ImageEditor.sln`
- **Projects targeting `net8.0`:**
  1. `src/ImageEditor.Core/ImageEditor.Core.csproj`
     - Packages: `MiniExcel` (`1.34.2`), `SixLabors.ImageSharp` (`3.1.6`), `SixLabors.ImageSharp.Drawing` (`2.1.5`)
  2. `src/ImageEditor.Cli/ImageEditor.Cli.csproj`
     - Packages: `Spectre.Console` (`0.49.1`), `Spectre.Console.Cli` (`0.49.1`)
  3. `tests/ImageEditor.Core.Tests/ImageEditor.Core.Tests.csproj`
     - Packages: `Microsoft.NET.Test.Sdk` (`17.11.1`), `xunit` (`2.9.2`), `xunit.runner.visualstudio` (`2.8.2`)
- **Version references:** No `.NET 8` / framework-specific references exist in `.cs` files, and there is no `global.json` or CI workflow file. Only `README.md` mentions `.NET 8`.

## Execution Steps

### 1. Pre-flight Check
Confirm the `.NET 10` SDK is active and capture baseline test results:
```powershell
dotnet --list-sdks
dotnet test ImageEditor.sln
```

### 2. Update Target Frameworks
Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>net10.0</TargetFramework>` in:
- `src/ImageEditor.Core/ImageEditor.Core.csproj`
- `src/ImageEditor.Cli/ImageEditor.Cli.csproj`
- `tests/ImageEditor.Core.Tests/ImageEditor.Core.Tests.csproj`

Keep existing NuGet package versions unchanged initially to minimize churn.

### 3. Clean, Restore, and Build
```powershell
dotnet clean ImageEditor.sln
dotnet restore ImageEditor.sln
dotnet build ImageEditor.sln --no-restore
```
- Check for any new `.NET 10` compiler warnings, obsolete API warnings, or analyzer diagnostics. Fix any issues cleanly in code.
- Only upgrade NuGet packages (e.g., `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`) if restore, build, or test execution fails due to a `.NET 10` incompatibility.

### 4. Run Tests & CLI Smoke Tests
```powershell
dotnet test ImageEditor.sln --no-build
dotnet run --project src/ImageEditor.Cli -- inspect --data "Peserta 1.json"
dotnet run --project src/ImageEditor.Cli -- generate --data "Peserta 1.json" --dry-run
```
- Confirm all unit tests pass.
- Confirm both `inspect` and `generate --dry-run` execute without runtime or dependency errors.

### 5. Update Documentation
- Update `README.md` (line 3) to replace `.NET 8` with `.NET 10`.

## Notes & Guardrails

- **Legacy artifacts:** There are old untracked/ignored `bin/Debug/net6.0` and `obj/Debug/net6.0` folders at the repository root from an earlier project structure. Leave them untouched unless explicitly asked to clean them up.
- **No behavioral changes:** Do not refactor application logic or change `nametag.config.json` defaults as part of this upgrade.

## Verification Checklist

- [ ] All 3 `.csproj` files target `net10.0`
- [ ] `dotnet build ImageEditor.sln` succeeds with 0 errors and 0 new warnings
- [ ] `dotnet test ImageEditor.sln` passes 100% of tests
- [ ] CLI smoke tests (`inspect` and `generate --dry-run`) succeed
- [ ] `README.md` updated to `.NET 10`
- [ ] `git status` and `git diff` reviewed to ensure only intended files changed
