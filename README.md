# BLT FRU CLI

A standalone .NET Framework 4.7.2 console application for reading and writing BLT (Board Level Traceability) data to an MCP2210-connected EEPROM on the AIC/boards.

This CLI is a self-contained extraction of the `readblt` and `writeblt` operations from the parent `20vNG` project. The logic is intentionally duplicated so that future BLT format or CRC changes can be applied independently.

---

## Requirements

### Hardware
- MCP2210 USB-to-SPI bridge connected and powered
- Board with accessible EEPROM

### Runtime Files (current working directory)
Before running either command, the directory you invoke `BltCli.exe` from must contain:

| File | Description |
|---|---|
| `*_BLT.ini` | Exactly one INI file with BLT field values (e.g. `NG_20V_BLT.ini`) |
| `ITUFFTemplate.xml` | ITUFF logger template |
| `binlist.xml` | ITUFF bin list |

The CLI resolves all runtime files from `Environment.CurrentDirectory` at startup and exits immediately with a clear error message if any file is missing or if more than one `*_BLT.ini` is found.

---

## Usage

```powershell
BltCli.exe readblt
BltCli.exe writeblt
```

### `readblt`
1. Connects to the MCP2210 device via `UsbToSpiDevice`.
2. Reads 128 bytes from the EEPROM using `BLT_Access.ReadBLT`.
3. Decodes and logs the BLT field content.
4. Rebuilds expected BLT bytes from the `.ini` file in the current directory.
5. Compares EEPROM content against expected bytes byte-by-byte.
6. Logs pass/fail and returns a non-zero exit code on mismatch.

### `writeblt`
1. Connects to the MCP2210 device via `UsbToSpiDevice`.
2. Loads and parses the `.ini` file from the current directory.
3. Builds the 128-byte BLT write buffer including CRC checksums.
4. Logs the planned BLT content.
5. Writes all 128 bytes to the EEPROM using `BLT_Access.WriteBLT`.
6. Logs pass/fail and returns a non-zero exit code on failure.

Both commands wrap execution with a full ITUFF logger lifecycle (`StartLot` → `StartDut` → `StartTest` → `EndTest` → `SetDutResult` → `EndDut` → `EndLot`).

---

## Exit Codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | Runtime failure (hardware, file mismatch, write error) |
| `2` | Invalid arguments or missing required argument |

---

## INI File Format

The `*_BLT.ini` file uses `KEY=VALUE` pairs. Lines starting with `#` or `;` are treated as comments.

```ini
Manufacturer_ID=INTEL
Assembly_No=G12345-001
Serial_No=SN0001234
Board_ID=NG20V
HW_Revision=A0
FW_Revision_1=1.0
FW_Revision_2=2.0
FW_Revision_3=3.0
FW_Revision_4=X
FW_Revision_5=Y
Custom=CUSTOM_VAL
Cycle_Counter=0
Global_Counter=0
```

> `Manufc_Date` and `Install_Date` are derived automatically from the current UTC date in `YYMMDD` format and are not read from the INI file.

---

## BLT EEPROM Layout

| Offset | Field | Size |
|---|---|---|
| `0x00` | Manufacturer ID | 13 bytes |
| `0x10` | Assembly No | 13 bytes |
| `0x20` | Serial No | 13 bytes |
| `0x30` | Manufacturing Date | 6 bytes |
| `0x37` | Install Date | 6 bytes |
| `0x40` | Cycle Counter | 3 bytes |
| `0x47` | Global Counter | 3 bytes |
| `0x50` | FW Revision 1 | 4 bytes |
| `0x54` | FW Revision 2 | 4 bytes |
| `0x58` | FW Revision 3 | 4 bytes |
| `0x60` | Board ID | 4 bytes |
| `0x69` | Hardware Rev | 4 bytes |
| `0x70` | FW Revision 4 | 1 byte |
| `0x71` | FW Revision 5 | 1 byte |
| `0x72` | Custom | 11 bytes |

Each 16-byte row includes a 2-byte CRC-16 checksum at bytes `[0x0E–0x0F]` of the row, computed using the `CRCEEPROM` algorithm.

Total EEPROM size: **128 bytes (0x80)**.

---

## Project Structure

```
BltCli\
  BltCli.csproj        # .NET Framework 4.7.2 console app project
  Program.cs           # CLI entry point: argument validation and dispatch
  BltRunner.cs         # ITUFF lifecycle, readblt/writeblt execution flows
  BltConfig.cs         # Current-directory runtime file resolution and INI parsing
  BltFormatter.cs      # BLT field layout, byte-array construction, display helpers
  BltCrc.cs            # CRC_APPEND_CHECKSUM / CRC_VALUE / CRC_CHECKSUM helpers
  CRCEEPROM.cs         # EEPROM CRC-16 calculator (CalculateCRC)
  App.config           # .NET runtime config
  Properties\
	AssemblyInfo.cs
  lib\                 # Local binary dependencies (not on NuGet)
	BLT.dll
	MCP2210.dll
	HidSharp.dll
	Log.exe
	itufflogger.exe
	MCP2210DLL-M-dotNet4.dll
	msvcp100.dll
	msvcr100.dll
```

---

## Building

Open `BltCli.slnx` in Visual Studio 2022+ and build, or use MSBuild directly:

```powershell
msbuild BltCli\BltCli.csproj /p:Configuration=Release /p:Platform=AnyCPU
```

The output folder (`bin\Release\`) will contain `BltCli.exe` and all required dependencies.

---

## Runtime Packaging

Copy the following to the folder you will run `BltCli.exe` from:

```
BltCli.exe
BLT.dll
MCP2210.dll
HidSharp.dll
Log.exe
itufflogger.exe
MCP2210DLL-M-dotNet4.dll
msvcp100.dll
msvcr100.dll
BltCli.exe.config
<ProductName>_BLT.ini        ← exactly one
ITUFFTemplate.xml
binlist.xml
```

---

## Relationship to 20vNG

This project is a standalone extraction of the `readblt` and `writeblt` switch cases in `20vNG\Program.cs`. The `20vNG` project remains unchanged. High-level BLT orchestration logic is **intentionally duplicated** between the two projects — future BLT format, field, or CRC changes must be applied to both.
