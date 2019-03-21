using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingAnalytics.OpenOrdersChecking
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            Functions functions = new Functions();

            Logger.Debug("Open Orders Checking console started.");
            Console.WriteLine("Open Orders Checking console started.");

            while (1 == 1)
            {
                functions.CheckOpenOrders();
                System.Threading.Thread.Sleep(30000);
            }

            //Logger.Debug("Open Orders Checking console finished.");
            //Console.WriteLine("Open Orders Checking console finished.");
        }
    }
}
