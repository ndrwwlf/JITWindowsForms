using Quartz;
using Quartz.Impl;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeatherServiceForm;
using WeatherServiceForm.Scheduled;

namespace WeatherServiceForm
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string userDir = "C:\\Users\\workweek";
            //string userDir = "C:\\Users\\andy";

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .MinimumLevel.Override("Quartz", LogEventLevel.Error)
            .Enrich.FromLogContext()
            //to outsite of project
            .WriteTo.File(userDir + "/Logs/MasterLog.log", restrictedToMinimumLevel: LogEventLevel.Information, fileSizeLimitBytes: 512000,
                            rollOnFileSizeLimit: true, retainedFileCountLimit: 2, shared: true)
            .WriteTo.RollingFile(userDir + "/Logs/Daily/log-{Date}.log", retainedFileCountLimit: 60, shared: true)
            .WriteTo.Console()
            .CreateLogger();

            AerisJobParams aerisJobParams = new AerisJobParams();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(aerisJobParams));
        }
    }
}
