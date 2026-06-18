using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BltCli
{
    internal sealed class BltRuntimeFiles
    {
        public BltRuntimeFiles(string workingDirectory, string configPath, string ituffTemplatePath, string binListPath)
        {
            WorkingDirectory = workingDirectory;
            ConfigPath = configPath;
            ItuffTemplatePath = ituffTemplatePath;
            BinListPath = binListPath;
        }

        public string WorkingDirectory { get; private set; }

        public string ConfigPath { get; private set; }

        public string ItuffTemplatePath { get; private set; }

        public string BinListPath { get; private set; }
    }

    internal sealed class BltConfigValues
    {
        public string ManufacturerId { get; set; }

        public string AssemblyNo { get; set; }

        public string SerialNo { get; set; }

        public string ManufacturingDate { get; set; }

        public string InstallDate { get; set; }

        public string CycleCounter { get; set; }

        public string GlobalCounter { get; set; }

        public string FirmwareRevision1 { get; set; }

        public string FirmwareRevision2 { get; set; }

        public string FirmwareRevision3 { get; set; }

        public string BoardId { get; set; }

        public string HardwareRevision { get; set; }

        public string FirmwareRevision4 { get; set; }

        public string FirmwareRevision5 { get; set; }

        public string Custom { get; set; }
    }

    internal static class BltConfig
    {
        public static BltRuntimeFiles Resolve(string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new InvalidOperationException("Current working directory is not available.");
            }

            string[] configFiles = Directory.GetFiles(workingDirectory, "*_BLT.ini");
            if (configFiles.Length == 0)
            {
                throw new FileNotFoundException("No *_BLT.ini file was found in " + workingDirectory + ".");
            }

            if (configFiles.Length > 1)
            {
                throw new InvalidOperationException("More than one *_BLT.ini file was found in " + workingDirectory + ". Exactly one is required.");
            }

            string ituffTemplatePath = Path.Combine(workingDirectory, "ITUFFTemplate.xml");
            if (!File.Exists(ituffTemplatePath))
            {
                throw new FileNotFoundException("Required ITUFFTemplate.xml file was not found in " + workingDirectory + ".", ituffTemplatePath);
            }

            string binListPath = Path.Combine(workingDirectory, "binlist.xml");
            if (!File.Exists(binListPath))
            {
                throw new FileNotFoundException("Required binlist.xml file was not found in " + workingDirectory + ".", binListPath);
            }

            return new BltRuntimeFiles(workingDirectory, configFiles[0], ituffTemplatePath, binListPath);
        }

        public static BltConfigValues LoadConfig(string configPath)
        {
            return new BltConfigValues
            {
                ManufacturerId = ReadConfigFile(configPath, "Manufacturer_ID"),
                AssemblyNo = ReadConfigFile(configPath, "Assembly_No"),
                SerialNo = ReadConfigFile(configPath, "Serial_No"),
                BoardId = ReadConfigFile(configPath, "Board_ID"),
                HardwareRevision = ReadConfigFile(configPath, "HW_Revision"),
                ManufacturingDate = GetDate(),
                InstallDate = GetDate(),
                FirmwareRevision1 = ReadConfigFile(configPath, "FW_Revision_1"),
                FirmwareRevision2 = ReadConfigFile(configPath, "FW_Revision_2"),
                FirmwareRevision3 = ReadConfigFile(configPath, "FW_Revision_3"),
                FirmwareRevision4 = ReadConfigFile(configPath, "FW_Revision_4"),
                FirmwareRevision5 = ReadConfigFile(configPath, "FW_Revision_5"),
                Custom = ReadConfigFile(configPath, "Custom"),
                CycleCounter = ReadConfigFile(configPath, "Cycle_Counter"),
                GlobalCounter = ReadConfigFile(configPath, "Global_Counter")
            };
        }

        private static string ReadConfigFile(string configPath, string param)
        {
            Dictionary<string, string> values = File.ReadLines(configPath)
                .Select(ParseConfigLine)
                .Where(pair => pair.HasValue)
                .ToDictionary(pair => pair.Value.Key, pair => pair.Value.Value, StringComparer.OrdinalIgnoreCase);

            string ret;
            return values.TryGetValue(param, out ret) ? ret : "0";
        }

        private static KeyValuePair<string, string>? ParseConfigLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            string trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith(";", StringComparison.Ordinal))
            {
                return null;
            }

            int equalsIndex = trimmed.IndexOf('=');
            if (equalsIndex < 0)
            {
                return null;
            }

            string key = trimmed.Substring(0, equalsIndex).Trim();
            string value = trimmed.Substring(equalsIndex + 1).Trim();
            return new KeyValuePair<string, string>(key, value);
        }

        private static string GetDate()
        {
            return DateTime.UtcNow.ToString("yyMMdd");
        }
    }
}
