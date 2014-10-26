using System;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Transactions;
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
                return Ut.RunPostBuildChecks(args[1], Assembly.GetExecutingAssembly());

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

            using (var sc = new ServiceController("SQL Server (SQLEXPRESS)"))
                if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                {
                    Console.WriteLine("Starting SQL Server service...");
                    sc.Start();
                }

            PropellerUtil.RunStandalone(PathUtil.AppPathCombine("LiBackgammon.Settings.json"), new LiBackgammonPropellerModule());
            Console.ReadLine();
            return 0;
        }

        public static TransactionScope NewTransaction()
        {
            return new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable });
        }
    }
}
