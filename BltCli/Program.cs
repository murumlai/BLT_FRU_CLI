using System;
using Sttd;

namespace BltCli
{
    internal class Program
    {
        private const int Success = 0;
        private const int InvalidArguments = 2;
        private const int RuntimeFailure = 1;

        private static int Main(string[] args)
        {
            if (args == null || args.Length != 1)
            {
                PrintUsage();
                return InvalidArguments;
            }

            string command = args[0].Trim().ToLowerInvariant();
            if (command != "readblt" && command != "writeblt")
            {
                Console.Error.WriteLine("Unsupported command: " + args[0]);
                PrintUsage();
                return InvalidArguments;
            }

            try
            {
                BltRuntimeFiles files = BltConfig.Resolve(Environment.CurrentDirectory);
                BltRunner runner = new BltRunner(files);

                if (command == "readblt")
                {
                    runner.ReadBlt();
                }
                else
                {
                    runner.WriteBlt();
                }

                return Success;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                TryLogError("Disaster : " + ex.Message);
                return RuntimeFailure;
            }
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  BltCli.exe readblt");
            Console.Error.WriteLine("  BltCli.exe writeblt");
        }

        private static void TryLogError(string message)
        {
            try
            {
                Log.Error(message);
            }
            catch
            {
            }
        }
    }
}
