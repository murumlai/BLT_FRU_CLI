# BLT CLI Implementation Progress

Last updated: 2026-06-18 after build validation resumed

## Current Goal
Implement the standalone `.NET Framework 4.7.2` BLT CLI described in `plan.md` for:

```powershell
BltCli.exe readblt
BltCli.exe writeblt
```

The existing adjacent `20vNG` project was only used as a source/reference. It was not modified.

## Completed Work

### 1. Located plan and legacy sources
- Found repo plan at `plan.md`.
- Found legacy repository at:
  - `C:\Users\lloganat\source\repos\20vNG`
- Read/cross-checked legacy BLT logic from:
  - `C:\Users\lloganat\source\repos\20vNG\20vNG\Program.cs`
  - `C:\Users\lloganat\source\repos\20vNG\20vNG\CRCEEPROM.cs`
  - `C:\Users\lloganat\source\repos\20vNG\BLT\PDB_BLT.cs`

### 2. Copied runtime dependencies into this repo
Created:

- `BltCli\lib\`

Copied these binaries into `BltCli\lib`:

- `BLT.dll`
- `MCP2210.dll`
- `HidSharp.dll`
- `Log.exe`
- `itufflogger.exe`
- `MCP2210DLL-M-dotNet4.dll`
- `msvcp100.dll`
- `msvcr100.dll`

### 3. Implemented CLI entry point
Updated:

- `BltCli\Program.cs`

Current behavior:

- Accepts exactly one argument.
- Supports only:
  - `readblt`
  - `writeblt`
- Prints usage for missing/invalid args.
- Returns non-zero exit codes for invalid args/runtime failures.
- Resolves runtime files from `Environment.CurrentDirectory`.
- Dispatches to `BltRunner`.

### 4. Added current-directory config/runtime resolution
Created:

- `BltCli\BltConfig.cs`

Current behavior:

- Finds exactly one `*_BLT.ini` in the current working directory.
- Requires `ITUFFTemplate.xml` in the current working directory.
- Requires `binlist.xml` in the current working directory.
- Fails early with clear exceptions when runtime files are missing/ambiguous.
- Parses config values equivalent to legacy BLT keys.
- Uses UTC `yyMMdd` date behavior equivalent to the legacy `GetSystemTime` `YYMMDD` output.

### 5. Added BLT layout/formatter/checksum logic
Created:

- `BltCli\BltFormatter.cs`
- `BltCli\BltCrc.cs`
- `BltCli\CRCEEPROM.cs`

Current behavior:

- Duplicates BLT offsets/constants:
  - `BLT_RFDBOFFSET`
  - `BLT_CRCOFFSET`
  - `BLT_SIZE`
- Builds BLT byte arrays from parsed `.ini` values.
- Displays decoded BLT content for read/write flows.
- Includes copied/adapted CRC table and `CRCEEPROM.CalculateCRC()` behavior.

### 6. Added BLT runner/read/write flows
Created:

- `BltCli\BltRunner.cs`

Current behavior:

- Wraps `readblt` and `writeblt` with ITUFF-style lifecycle:
  - `Logger`
  - `StartLot`
  - `LoadBinList`
  - `StartDut`
  - `StartTest`
  - `EndTest`
  - `SetDutResult`
  - `EndDut`
  - `EndLot`
- `readblt` flow:
  - Connects to `UsbToSpiDevice`.
  - Calls `BLT_Access.ReadBLT(device.EEPROM)`.
  - Displays decoded EEPROM BLT content.
  - Rebuilds expected BLT bytes from current-directory `.ini`.
  - Compares EEPROM bytes against expected bytes.
  - Disconnects in `finally`.
- `writeblt` flow:
  - Connects to `UsbToSpiDevice`.
  - Loads current-directory `.ini`.
  - Builds BLT bytes.
  - Displays planned content.
  - Writes all `BltFormatter.BltSize` bytes using `BLT_Access.WriteBLT(...)`.
  - Disconnects in `finally`.

## Build Validation Status

`BltCli\BltCli.csproj` was repaired and updated after the editor was reopened. The project now builds successfully with MSBuild:

```powershell
msbuild 'C:\Users\lloganat\source\repos\BltCli\BltCli\BltCli.csproj' /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal
```

Build output:

```text
BltCli -> C:\Users\lloganat\source\repos\BltCli\BltCli\bin\Debug\BltCli.exe
```

The x86 MCP2210 dependency warning was resolved by aligning project configurations to x86/Prefer32Bit.

## Completed `BltCli.csproj` Changes

The project file includes references to local dependency binaries:

- `lib\BLT.dll`
- `lib\HidSharp.dll`
- `lib\itufflogger.exe`
- `lib\Log.exe`
- `lib\MCP2210.dll`
- `lib\MCP2210DLL-M-dotNet4.dll`
- `System.ServiceModel`

It includes compile entries for:

- `BltConfig.cs`
- `BltCrc.cs`
- `BltFormatter.cs`
- `BltRunner.cs`
- `CRCEEPROM.cs`
- `Program.cs`
- `Properties\AssemblyInfo.cs`

It copies native runtime DLLs to the output root:

- `lib\msvcp100.dll`
- `lib\msvcr100.dll`

## Validation Performed

### Build

Build succeeded with:

```powershell
msbuild 'C:\Users\lloganat\source\repos\BltCli\BltCli\BltCli.csproj' /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal
```

### Output Packaging

Verified `BltCli\bin\Debug` output root contains:

- `BltCli.exe`
- `BLT.dll`
- `HidSharp.dll`
- `itufflogger.exe`
- `Log.exe`
- `MCP2210.dll`
- `MCP2210DLL-M-dotNet4.dll`
- `msvcp100.dll`
- `msvcr100.dll`
- `BltCli.exe.config`

### CLI Behavior Without Hardware

Verified no-argument usage:

```text
Usage:
  BltCli.exe readblt
  BltCli.exe writeblt
exit=2
```

Verified `readblt` fails early from output folder when no `*_BLT.ini` exists:

```text
Error: No *_BLT.ini file was found in C:\Users\lloganat\source\repos\BltCli\BltCli\bin\Debug.
exit=1
```

## Remaining Runtime Validation

Hardware/runtime validation still requires running from a folder that contains:

- exactly one `*_BLT.ini`
- `ITUFFTemplate.xml`
- `binlist.xml`
- MCP2210 hardware connected and powered

Then run:

```powershell
BltCli.exe readblt
BltCli.exe writeblt
```

## Active Plan Status

Completed main work:

- Dependency discovery
- Dependency copy
- Program dispatch
- Current-directory config resolution
- BLT formatter/CRC support
- BLT read/write runner flows

Completed:

- Build validation
- Output dependency packaging
- No-argument usage validation
- Missing `*_BLT.ini` early-failure validation

Remaining:

- Full hardware/runtime validation with real `*_BLT.ini`, `ITUFFTemplate.xml`, `binlist.xml`, and MCP2210 device.

---

## Session Update — 2026-06-18 (Git + README)

### 7. Git repository initialized and pushed to GitHub

- Initialized a new git repository at the workspace root:
  `C:\Users\lloganat\source\repos\BltCli`
- Created `.gitignore` excluding `bin/`, `obj/`, `.vs/`, `CopilotSnapshots/`, `*.user`
- Staged and committed all source files, project files, and `lib/` dependencies
- Added remote origin: `https://github.com/murumlai/BLT_FRU_CLI.git`
- Renamed default branch to `main`
- Pushed initial commit to remote

### 8. README.md created and pushed

- Created `README.md` at the repo root covering:
  - Hardware and runtime file requirements
  - `readblt` / `writeblt` usage and step-by-step behavior
  - Exit code table
  - INI file format with example
  - BLT EEPROM field layout table (offsets, sizes)
  - Project structure with per-file role descriptions
  - Build instructions (Visual Studio and MSBuild CLI)
  - Runtime packaging checklist
  - Relationship to the `20vNG` parent project
- Committed and pushed (`3b81f0c`)
- User updated BLT description and hardware requirement text
- Updated README committed and pushed (`2bcc175`)

### Git Log (current state)

```
2bcc175  docs: update BLT description and hardware requirement in README
3b81f0c  docs: add README.md
86a4450  Initial commit: standalone BLT CLI (.NET Framework 4.7.2)
```

### Remote

`https://github.com/murumlai/BLT_FRU_CLI` — branch `main`
