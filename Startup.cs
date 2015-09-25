using System;
using BizArk.Core.CmdLine;
using System.Diagnostics;
using System.Reflection;
using Nancy.Hosting.Self;
using System.Net.Sockets;
using NLog;
using System.Threading;

namespace OpenStreetMapCache
{
    class Startup
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Main()
        {
            ConsoleApplication.RunProgram<CommandLineArguments>(Run);
        }

        static public void Run(CommandLineArguments args)
        {
            var f = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var version = String.Format("{0}.{1}.{2}.{3}", f.ProductMajorPart, f.ProductMinorPart, f.ProductBuildPart, f.ProductPrivatePart);

            var url = new Uri("http://localhost:2000");
            using (var nancyHost = new NancyHost(url))
            {
                try
                {
                    nancyHost.Start();
                }
                catch (SocketException e)
                {
                    logger.Info("Failed starting web service {0}: {1}", e.SocketErrorCode, e.Message);
                    return;
                }

                logger.Info("OpenStreetMapCache {1} listening at {0}", url, version);

                if (args.RunAsService)
                {
                    logger.Info("Running as a service");
                    while (true)
                        Thread.Sleep(100);
                }

                Console.ReadLine();
            }
        }
    }
}
