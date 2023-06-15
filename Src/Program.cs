using System;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Transactions;
using RT.PostBuild;
using RT.PropellerApi;
using RT.Util;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace LiBackgammon
{
    class Program
    {
#if DEBUG
        public const bool IsDebug = true;
        public static string SourceDir = null;
#else
        public const bool IsDebug = false;
#endif

        static int Main(string[] args)
        {
            try { Console.OutputEncoding = Encoding.UTF8; }
            catch { }

            if (args.Length == 2 && args[0] == "--post-build-check")
                return PostBuildChecker.RunPostBuildChecks(args[1], Assembly.GetExecutingAssembly());

#if DEBUG
            if (args.Length == 1)
                SourceDir = args[0];
#endif

            ConsoleUtil.WriteLine((IsDebug ? "DEBUG MODE" : "RELEASE MODE").Apply(msg => "{0}┌──{1}──╖{0}{4}{0}│  {2}  ║{0}{4}{0}╘══{3}══╝{0}".Fmt(
                new string(' ', (Console.BufferWidth - msg.Length - 7) / 2),
                new string('─', msg.Length),
                msg,
                new string('═', msg.Length),
                Environment.NewLine).Color(ConsoleColor.White, IsDebug ? ConsoleColor.DarkBlue : ConsoleColor.DarkRed)));

            PropellerUtil.RunStandalone(@"D:\Daten\Config\LiBackgammon.config.json", new LiBackgammonPropellerModule());
            return 0;
        }

        public static TransactionScope NewTransaction()
        {
            return new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable });
        }
    }
}
