using log4net;
using System;
using System.Collections.Generic;

namespace TradingAnalytics.TradingProcess
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            Functions functions = new Functions();

            Logger.Debug("Trading Process console started.");

            while (1 == 1)
            {
                functions.ProcessTrades();
                System.Threading.Thread.Sleep(180000);
            }
        }
    }
}
