using System;
using BLT;
using itufflogger;
using MCP2210;
using Sttd;

namespace BltCli
{
    internal sealed class BltRunner
    {
        private const int PassBin = 01000000;
        private const int ReadFailureBin = 10630101;
        private const int WriteFailureBin = 10630102;
        private const int BltWriteFailureBin = 10630201;
        private const int BltReadFailureBin = 10630202;

        private readonly BltRuntimeFiles _files;
        private readonly BltFormatter _formatter;
        private int _bin = 10639999;

        public BltRunner(BltRuntimeFiles files)
        {
            _files = files;
            _formatter = new BltFormatter();
        }

        public void ReadBlt()
        {
            RunWithLogger("Read BLT Test", ReadBltTest, ReadFailureBin, "Success : Reading BLT for NG 20V PDB1 completed.", "Read blt failed with ");
        }

        public void WriteBlt()
        {
            RunWithLogger("Write BLT Test", WriteBltTest, WriteFailureBin, "Success : Writing BLT for NG 20V PDB1 completed.", "Write blt failed with ");
        }

        private void RunWithLogger(string testName, Action testAction, int failureBin, string successMessage, string failurePrefix)
        {
            Logger logger = null;

            try
            {
                logger = new Logger();
                logger.StartLot(_files.ItuffTemplatePath);
                logger.LoadBinList(_files.BinListPath);
                logger.StartDut();
                logger.StartTest(testName);

                testAction();

                logger.EndTest(true);
                logger.SetDutResult(PassBin);
                logger.EndDut();
                logger.EndLot();
                Log.Info(successMessage);
            }
            catch (Exception ex)
            {
                SafeEndLogger(logger, _bin == 10639999 ? failureBin : _bin);
                Log.Error("Disaster : Test failed. " + ex.Message);
                throw new Exception(failurePrefix + ex.Message, ex);
            }
        }

        private static void SafeEndLogger(Logger logger, int bin)
        {
            if (logger == null)
            {
                return;
            }

            try
            {
                logger.SetDutResult(bin);
                logger.EndDut();
                logger.EndLot();
            }
            catch
            {
            }
        }

        private void ReadBltTest()
        {
            IUsbToSpiDevice device = new UsbToSpiDevice();
            device.Connect();

            try
            {
                Log.Info("Starting BLT read test for MPDU 20V PDB1");

                byte[] bltRead = BLT_Access.ReadBLT(device.EEPROM);
                Log.Info("+++++++++++++++++++++++++++++++++++++++++++");
                Log.Info("Done read BLT");
                Log.Info("+++++++++++++++++++++++++++++++++++++++++++");
                Log.Info("Displaying eeprom BLT content ....");
                Log.Info(" ");
                _formatter.ShowRead(bltRead);

                BltConfigValues config = BltConfig.LoadConfig(_files.ConfigPath);
                byte[] bltExpected = _formatter.Build(config);

                for (int i = 0; i < BltFormatter.BltSize; i++)
                {
                    if (bltRead[i] != bltExpected[i])
                    {
                        Log.Error("Mismatch at eeprom offset: " + i + " Current Content : " + bltExpected[i].ToString("X") + " vs Expected Content : " + bltRead[i].ToString("X"));
                        Log.Error("BLT content mismatch!");
                        throw new Exception("BLT content mismatch!!!");
                    }
                }

                Log.Info("BLT content matches the " + System.IO.Path.GetFileName(_files.ConfigPath) + " file.");
                Log.Info("+++++++++++++++++++++++++++++++++++++++++++");
            }
            catch (Exception ex)
            {
                _bin = BltReadFailureBin;
                Log.Error(ex.ToString());
                throw new Exception("BLT read failed", ex);
            }
            finally
            {
                Disconnect(device);
            }
        }

        private void WriteBltTest()
        {
            IUsbToSpiDevice device = new UsbToSpiDevice();
            device.Connect();

            try
            {
                Log.Info("Loading BLT info from the .ini file...");
                BltConfigValues config = BltConfig.LoadConfig(_files.ConfigPath);
                Log.Info("Preparing data to be written to the eeprom...");
                byte[] bltWrite = _formatter.Build(config);
                Log.Info("Showing BLT content that going to be written to eeprom");
                _formatter.ShowWrite(bltWrite);

                Log.Info("Starting BLT write test for MPDU 20V PDB1");
                for (byte i = 0x00; i < BltFormatter.BltSize; i += 0x01)
                {
                    BLT_Access.WriteBLT(device.EEPROM, i, bltWrite[i]);
                }

                Log.Info("+++++++++++++++++++++++++++++++++++++++++++++");
            }
            catch (Exception ex)
            {
                _bin = BltWriteFailureBin;
                Log.Error(ex.ToString());
                throw new Exception("BLT write failed", ex);
            }
            finally
            {
                Disconnect(device);
            }
        }

        private static void Disconnect(IUsbToSpiDevice device)
        {
            try
            {
                if (device != null)
                {
                    device.Disconnect();
                }
            }
            catch
            {
            }
        }
    }
}
