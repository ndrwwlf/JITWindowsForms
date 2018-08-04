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

namespace WeatherForm
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
            //string userDir = "C:\\Users\\User";

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.FromLogContext()
            //to outsite of project
            .WriteTo.RollingFile(userDir + "/Logs/log-{Date}.log", retainedFileCountLimit: null)
            .WriteTo.Console()
            .CreateLogger();

            AerisJobParams aerisJobParams = new AerisJobParams();
            aerisJobParams.AerisClientId = "vgayNZkz1o2JK6VRhOTBZ";
            aerisJobParams.AerisClientSecret = "8YK1bmJlOPJCIO2darWs48qmXPKzGxQHdWWzWmNg";
            //aerisJobParams.JitWeatherConnectionString = "Data Source=WINDEV1805EVAL\\SQLEXPRESS ; Initial Catalog=Weather ; User ID=foo; Password=bar ; MultipleActiveResultSets=true";
            //aerisJobParams.JitWebData3ConnectionString = "Data Source=JITSQL02 ; Initial Catalog=JitWebData3 ; User ID=WorkWeeksql;  Password=Jon23505#sql ; MultipleActiveResultSets=true";
            aerisJobParams.JitWeatherConnectionString = "Data Source=.\\SQLEXPRESS;Initial Catalog=Weather;User ID=WorkWeeksql;Password=Jon23505#sql; MultipleActiveResultSets=true";
            aerisJobParams.JitWebData3ConnectionString = "Data Source = .\\SQLEXPRESS; Initial Catalog = JitWebData3; User ID = WorkWeeksql; Password = Jon23505#sql; MultipleActiveResultSets=true";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(aerisJobParams));
        }
    }
}
