using System;
using System.Linq;
using Sttd;

namespace BltCli
{
    internal sealed class BltFormatter
    {
        public enum BltOffset : byte
        {
            ManId = 0x00,
            Assembly = 0x10,
            SerialNumber = 0x20,
            ManufacturingDate = 0x30,
            InstallDate = 0x37,
            CycleCounter = 0x40,
            GlobalCounter = 0x47,
            Firmware1 = 0x50,
            Firmware2 = 0x54,
            Firmware3 = 0x58,
            BoardId = 0x60,
            Hardware = 0x69,
            Firmware4 = 0x70,
            Firmware5 = 0x71,
            Custom = 0x72
        }

        public const int BltRfdbOffset = 0x0D;
        public const int BltCrcOffset = 0x0E;
        public const int BltSize = 0x80;

        public byte[] Build(BltConfigValues config)
        {
            byte[] blt = Enumerable.Repeat((byte)0xFF, BltSize).ToArray();

            PutStringToCharArray(blt, config.ManufacturerId, (int)BltOffset.ManId);
            PutStringToCharArray(blt, NormalizeAssemblyNumber(config.AssemblyNo), (int)BltOffset.Assembly);
            PutStringToCharArray(blt, config.SerialNo, (int)BltOffset.SerialNumber);
            PutStringToCharArray(blt, config.ManufacturingDate, (int)BltOffset.ManufacturingDate);
            PutStringToCharArray(blt, config.InstallDate, (int)BltOffset.ManufacturingDate + 7);
            StringToHexArray(blt, config.CycleCounter, (int)BltOffset.CycleCounter);
            StringToHexArray(blt, config.GlobalCounter, (int)BltOffset.GlobalCounter);
            FirmwareVersion(blt, config.FirmwareRevision1, (int)BltOffset.Firmware1);
            FirmwareVersion(blt, config.FirmwareRevision2, (int)BltOffset.Firmware1 + 4);
            FirmwareVersion(blt, config.FirmwareRevision3, (int)BltOffset.Firmware1 + 8);
            PutStringToCharArray(blt, config.FirmwareRevision4, (int)BltOffset.Firmware4);
            PutStringToCharArray(blt, config.FirmwareRevision5, (int)BltOffset.Firmware4 + 1);
            PutStringToCharArray(blt, config.Custom, (int)BltOffset.Firmware4 + 2);
            PutStringToCharArray(blt, config.BoardId, (int)BltOffset.BoardId);
            HardwareRev(blt, config.HardwareRevision, (int)BltOffset.BoardId + 0x0D);

            blt[(byte)(BltOffset.ManId + BltRfdbOffset)] = 0x01;
            blt[(byte)(BltOffset.Assembly + BltRfdbOffset)] = 0x01;
            blt[(byte)(BltOffset.SerialNumber + BltRfdbOffset)] = 0x01;
            blt[(byte)(BltOffset.ManufacturingDate + BltRfdbOffset)] = 0x31;
            blt[(byte)(BltOffset.CycleCounter + BltRfdbOffset)] = 0x10;
            blt[(byte)(BltOffset.Firmware1 + BltRfdbOffset)] = 0x01;
            blt[(byte)(BltOffset.BoardId + BltRfdbOffset)] = 0x01;
            blt[(byte)(BltOffset.Firmware4 + BltRfdbOffset)] = 0x01;

            for (int i = 0; i < BltSize; i += 0x10)
            {
                BltCrc.AppendChecksum(blt, (ulong)(i + BltCrcOffset));
            }

            return blt;
        }

        public void ShowRead(byte[] blt)
        {
            ShowBltContent(
                CharArrayToString(blt, (int)BltOffset.ManId, 13),
                CharArrayToString(blt, (int)BltOffset.Assembly, 13),
                CharArrayToString(blt, (int)BltOffset.SerialNumber, 13),
                CharArrayToString(blt, (int)BltOffset.ManufacturingDate, 6),
                CharArrayToString(blt, (int)BltOffset.InstallDate, 6),
                CharArrayToDecString(blt, (int)BltOffset.CycleCounter, 3),
                CharArrayToDecString(blt, (int)BltOffset.GlobalCounter, 3),
                CharArrayToString(blt, (int)BltOffset.Firmware1, 4),
                CharArrayToString(blt, (int)BltOffset.Firmware2, 4),
                CharArrayToString(blt, (int)BltOffset.Firmware3, 4),
                CharArrayToString(blt, (int)BltOffset.BoardId, 4),
                CharArrayToString(blt, (int)BltOffset.Hardware, 4),
                CharArrayToString(blt, (int)BltOffset.Firmware4, 1),
                CharArrayToString(blt, (int)BltOffset.Firmware5, 1),
                CharArrayToString(blt, (int)BltOffset.Custom, 11));
        }

        public void ShowWrite(byte[] blt)
        {
            ShowBltContent(
                CharArrayToString(blt, (int)BltOffset.ManId, 13),
                CharArrayToString(blt, (int)BltOffset.Assembly, 13),
                CharArrayToString(blt, (int)BltOffset.SerialNumber, 13),
                CharArrayToString(blt, (int)BltOffset.ManufacturingDate, 6),
                CharArrayToString(blt, (int)BltOffset.InstallDate, 6),
                CharArrayToDecString(blt, (int)BltOffset.CycleCounter, 3),
                CharArrayToDecString(blt, (int)BltOffset.GlobalCounter, 3),
                CharArrayToString(blt, (int)BltOffset.Firmware1, 4),
                CharArrayToString(blt, (int)BltOffset.Firmware2, 4),
                CharArrayToString(blt, (int)BltOffset.Firmware3, 4),
                CharArrayToString(blt, (int)BltOffset.BoardId, 4),
                CharArrayToString(blt, (int)BltOffset.Hardware, 4),
                CharArrayToString(blt, (int)BltOffset.Firmware4, 4),
                CharArrayToString(blt, (int)BltOffset.Firmware5, 4),
                CharArrayToString(blt, (int)BltOffset.Custom, 4));
        }

        private static string NormalizeAssemblyNumber(string asmno)
        {
            if (string.IsNullOrEmpty(asmno))
            {
                return string.Empty;
            }

            for (int i = 0; i < asmno.Length; i++)
            {
                if (!asmno.Substring(i, 1).All(char.IsLetter))
                {
                    return asmno.Substring(Math.Max(0, i - 1));
                }
            }

            return asmno;
        }

        private static void PutStringToCharArray(byte[] blt, string str, int off)
        {
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            char[] tmp = str.ToCharArray();
            for (int i = 0; i < tmp.Length && i + off < blt.Length; i++)
            {
                blt[i + off] = (byte)tmp[i];
            }
        }

        private static void StringToHexArray(byte[] blt, string num, int off)
        {
            string hex = string.Format("{0:X6}", Convert.ToUInt32(string.IsNullOrWhiteSpace(num) ? "0" : num));

            for (int i = 0; i < 3 && i + off < blt.Length; i++)
            {
                try
                {
                    blt[i + off] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
                catch
                {
                    blt[i + off] = 0x00;
                }
            }
        }

        private static void FirmwareVersion(byte[] blt, string str, int off)
        {
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            char[] tmp;
            if (str.Contains('.'))
            {
                string[] s0 = str.Split('.');
                string s1 = "00" + s0[0];
                string s2 = "00" + s0[1];
                tmp = (s1.Substring(s1.Length - 2) + s2.Substring(s2.Length - 2)).ToCharArray();
            }
            else
            {
                tmp = str.ToCharArray();
            }

            for (int i = 0; i < str.Length && i < tmp.Length && i + off < blt.Length; i++)
            {
                blt[i + off] = (byte)tmp[i];
            }
        }

        private static void HardwareRev(byte[] blt, string str, int off)
        {
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            char[] tmp = str.ToCharArray();
            for (int i = 0; i < tmp.Length; i++)
            {
                int index = i + off - tmp.Length;
                if (index >= 0 && index < blt.Length)
                {
                    blt[index] = (byte)tmp[i];
                }
            }
        }

        private static string CharArrayToString(byte[] blt, int off, int len)
        {
            string str = string.Empty;

            for (int i = 0; i < len && i + off < blt.Length; i++)
            {
                if (blt[i + off] != 0xFF)
                {
                    str += Convert.ToChar(blt[i + off]).ToString();
                }
            }

            return str;
        }

        private static string CharArrayToDecString(byte[] blt, int off, int len)
        {
            int dec = blt[off];
            for (int i = 1; i < len && i + off < blt.Length; i++)
            {
                dec = (dec << 8) | blt[i + off];
            }

            return dec >= 0xFFFFFF ? string.Empty : dec.ToString();
        }

        private static void ShowBltContent(string manid, string asmno, string snumb, string mdate, string idate, string cycnt, string gycnt, string fwar1, string fwar2, string fwar3, string brdid, string hwrev, string fwar4, string fwar5, string cust)
        {
            Log.Info("Manufacturing ID : " + manid);
            Log.Info("Assembly No : " + asmno);
            Log.Info("Serial No : " + snumb);
            Log.Info("Manufacturing Date : " + mdate);
            Log.Info("Install Date : " + idate);
            Log.Info("Cycle Counter : " + cycnt);
            Log.Info("Global Counter : " + gycnt);
            Log.Info("FW ver1 : " + fwar1);
            Log.Info("FW ver2 : " + fwar2);
            Log.Info("FW ver3 : " + fwar3);
            Log.Info("Board ID : " + brdid);
            Log.Info("Hardware Rev : " + hwrev);
            Log.Info("FW ver4 : " + fwar4);
            Log.Info("FW ver5 : " + fwar5);
            Log.Info("Custom : " + cust);
        }
    }
}
