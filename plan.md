# 🎯 Standalone BLT CLI Extraction

Create a separate standalone .NET Framework 4.7.2 console application/solution for BLT read/write operations, independent from the existing `20vNG.sln`. The existing `20vNG` project and `20vNG\Program.cs` must remain unchanged; its existing `readblt` and `writeblt` switch cases continue to work as-is.

The new CLI will copy the current BLT orchestration logic from `20vNG\Program.cs` instead of sharing/refactoring it. This intentionally duplicates the high-level BLT logic, per user preference, so future BLT format or CRC changes must be applied to both code paths.

Target CLI behavior:

```powershell
BltCli.exe readblt
BltCli.exe writeblt
```

The CLI must use its current working directory for all required runtime files:

- Find exactly one `*_BLT.ini` file using `Directory.GetFiles(Environment.CurrentDirectory, "*_BLT.ini")`.
- Use `Path.Combine(Environment.CurrentDirectory, "ITUFFTemplate.xml")`.
- Use `Path.Combine(Environment.CurrentDirectory, "binlist.xml")`.

Fail early with a clear console/log error and non-zero exit code if no `*_BLT.ini` file is found, more than one `*_BLT.ini` file is found, `ITUFFTemplate.xml` is missing, or `binlist.xml` is missing.

Preserve full parity with the current ITUFF/binlist behavior from `20vNG\Program.cs`: create `Logger`, call `StartLot`, `LoadBinList`, `StartDut`, `StartTest`, `EndTest`, `SetDutResult`, `EndDut`, and `EndLot` similarly to the existing `readblt` and `writeblt` cases.

Required copied/adapted logic from `20vNG\Program.cs` includes:

- `readBLTTest()` and `writeBLTTest()` high-level flows.
- BLT state/constants: `BLT_OFFSET`, `BLT_RFDBOFFSET`, `BLT_CRCOFFSET`, `BLT_SIZE`, `BLT_WR`, `BLT_RD`.
- Config loading/parsing: `LoadConfig()`, `ReadConfigFile()` adapted to accept/use the discovered `*_BLT.ini` path.
- Date handling: `GetDate()` and required `SYSTEMTIME`/`GetSystemTime` interop, unless replaced with equivalent UTC date behavior that preserves current `YYMMDD` output.
- BLT formatting/display helpers: `BltShow()`, `BltShowWR()`, `ShowBltContent()`, `CharArrayToString()`, `CharArrayToStringWR()`, `CharArrayToDecString()`, `CharArrayToDecStringWR()`.
- BLT byte-array construction helpers: `BltInit()`, `PutStringToCharArray()`, `StringToHexArray()`, `FirmwareVersion()`, `HardwareRev()`.
- CRC helpers: `CRC_APPEND_CHECKSUM()`, `CRC_VALUE()`, `CRC_CHECKSUM()`, `CRC_TABLE`, plus `CRCEEPROM` support from `20vNG\CRCEEPROM.cs` if needed to match current checksum behavior.

Required project references/dependencies for the standalone CLI:

- `MCP2210` functionality for `IUsbToSpiDevice`, `UsbToSpiDevice`, and EEPROM access.
- Current `BLT` library or copied `BLT_Access` logic for `ReadBLT()` and per-byte `WriteBLT()`.
- `Log.exe` reference if keeping existing `Log.Info/Error` calls.
- `itufflogger.exe` reference for `Logger`.
- `MCP2210DLL-M-dotNet4.dll` and any native DLLs required by the MCP2210 hardware path.

Recommended standalone folder layout:

```plaintext
StandaloneBltCli\
  StandaloneBltCli.sln
  BltCli\
    BltCli.csproj
    Program.cs
    BltRunner.cs
    BltConfig.cs
    BltFormatter.cs
    BltCrc.cs
    CRCEEPROM.cs
```

`Program.cs` should only handle argument validation, usage text, dispatching `readblt`/`writeblt`, and returning process exit codes. `BltRunner.cs` should contain the logger setup/teardown and read/write execution. `BltConfig.cs` should resolve and parse the current-directory files. `BltFormatter.cs` should contain BLT layout, byte-array construction, and display helpers. `BltCrc.cs`/`CRCEEPROM.cs` should contain checksum support.

**Progress**: 0% [░░░░░░░░░░]

**Last Updated**: 2026-06-18 07:39:46

## 📝 Plan Steps
- ❌ **Create standalone solution/project — create a new `.NET Framework 4.7.2` console app under a separate folder such as `StandaloneBltCli`, not added to `20vNG.sln`.**
-  **Add references/dependencies — reference or copy required dependencies for `MCP2210`, `BLT_Access`, `Log.exe`, `itufflogger.exe`, `MCP2210DLL-M-dotNet4.dll`, and any required native DLLs so the CLI can run independently from `20vNG`.**
-  **Implement `Program.cs` CLI dispatch — accept exactly one command argument, support only `readblt` and `writeblt`, print usage for missing/invalid args, and return non-zero exit codes on errors.**
-  **Implement current-directory file resolution — find exactly one `*_BLT.ini`, require `ITUFFTemplate.xml`, require `binlist.xml`, and fail clearly if any condition is not met.**
-  **Copy/adapt BLT config logic — port `LoadConfig()`/`ReadConfigFile()` from `20vNG\Program.cs` and adapt it to use the discovered `*_BLT.ini` path instead of a hardcoded path.**
-  **Copy/adapt BLT formatter logic — port BLT constants, offsets, field variables, `BltInit()`, field encoding helpers, read/write display helpers, and date handling into the new CLI.**
-  **Copy/adapt CRC logic — port checksum helpers and `CRCEEPROM` support needed to generate identical CRC bytes to the current `20vNG` behavior.**
-  **Implement BLT read flow — connect to `UsbToSpiDevice`, call `BLT_Access.ReadBLT(device.EEPROM)`, display decoded content, rebuild expected BLT bytes from the discovered `.ini`, compare EEPROM content against expected bytes, log pass/fail, and disconnect in all paths.**
-  **Implement BLT write flow — connect to `UsbToSpiDevice`, load `.ini`, build `BLT_WR`, display planned content, write all `BLT_SIZE` bytes using `BLT_Access.WriteBLT(device.EEPROM, i, BLT_WR[i])`, log pass/fail, and disconnect in all paths.**
-  **Preserve ITUFF parity — wrap `readblt` and `writeblt` with the same `Logger`/`StartLot`/`LoadBinList`/`StartDut`/`StartTest`/`EndTest`/`SetDutResult`/`EndDut`/`EndLot` behavior and bin codes currently used in `20vNG\Program.cs`.**
-  **Validate runtime packaging — ensure the output folder contains `BltCli.exe`, dependencies, one `*_BLT.ini`, `ITUFFTemplate.xml`, and `binlist.xml`; test both `BltCli.exe readblt` and `BltCli.exe writeblt` from that folder.**
-  **Leave existing project unchanged — do not remove or refactor `readblt`/`writeblt` in `20vNG\Program.cs`, and do not add the standalone CLI to `20vNG.sln` unless explicitly requested later.**

