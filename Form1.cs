using JitTopshelf.Scheduled;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeatherForm.Repository;
using WeatherServiceApp.Properties;
using WeatherServiceForm;
using WeatherServiceForm.ConsoleRedirection;
using WeatherServiceForm.Scheduled;

namespace WeatherForm
{
    public partial class Form1 : Form
    {
        TextWriter _writer = null;

        AerisJobParams _aerisJobParams;
        IWeatherRepository _weatherRepository;
        AerisJob _aerisJob = new AerisJob();
        WNRdngData01RegressionJob _wNRdngData01RegressionJob = new WNRdngData01RegressionJob();

        DateTimePicker datePicker;
        DateTime yesterday = DateTime.Now.AddDays(-1);

        string todayStr = DateTime.Now.ToShortDateString();        

        //Button button1;
        //Button button2;

        public Form1(AerisJobParams aerisJobParams)
        {
            InitializeComponent();

            _aerisJobParams = aerisJobParams;
            _weatherRepository = new WeatherRepository(_aerisJobParams);
            
            MessageBox.Show("Starting the Weather Job will take in your selected date and call Aeris for weather information if there are any gaps in WeatherData table " +
                "(possibly from failed scheduled starting of JITWeatherService Windows Service) from then up to, and including, yesterday. Then it will check for all Readings " +
                "(starting at the same selected date and ending no later than we have weather info--hopefully yesterday) that should be normalized, and for every one NOT " +
                "already calculated and stored in WthExpUsage, respectively calculate and insert."+
                "\n\n'Force Historical Run' should only be needed if:\n " +
                "1) The number of zip codes targeted for weather information has descreased AND target date has been pushed back. \n" +
                "2) The number of zip codes is the same, but one has been deleted entirely and a new zip code has been entered. \n\n" +
                "Starting the WNRdngData01 Regression Job: The WNRdngData01 stored procedure is executed to find new Acc/Util/UnitIDs in need of regression " +
                "analysis. For each new Acc/Util/UnitID the best regression model is found and inserted into/updated in WthNormalParams. Then its respective " +
                "Readings' WthExpUsages are calculated using the new regression model and insterted into/updated in WthExpUsage. \n" +
                "The daily logs contain Debug-level logging, WeatherData insert info, and WthExpUsage insert/update info.\n" +
                "WthExpUsage will be calculated and updated as far back as there is WeatherData in the database. \n\n" +
                "The DateTimePicker's inital value is the last used Start Date. Selecting a more recent Start Date will not delete any older entries.");

            datePicker = new DateTimePicker();
            datePicker.Name = "Get WeatherData and WthExpUsage back to:";
            datePicker.Size = new Size(209, 20);
            datePicker.Location = new Point(91, 118);
            datePicker.Value = _weatherRepository.GetCurrentOldestWeatherDataDate();
            datePicker.MinDate = new DateTime(2013, 1, 1);
            datePicker.MaxDate = yesterday;
            datePicker.CustomFormat = "MMMM dd, yyyy";
            datePicker.Format = DateTimePickerFormat.Custom;
            this.Controls.Add(datePicker);

            this.Controls.Add(button1);
            button1.Click += new EventHandler(Button1_Click);
            
            this.Controls.Add(button2);
            button2.Click += new EventHandler(Button2_Click);

            this.Controls.Add(button3);
            button3.Click += new EventHandler(Button3_Click);

        }

        private void Button1_Click(object sender, EventArgs e)
        {
            DisableButtons();

            DialogResult proceed = MessageBox.Show("Will check for/insert all WeatherData AND WthExpUsage going back to \n\n" + 
                datePicker.Value.ToShortDateString(), "Start Weather Job?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            Console.SetOut(_writer);

            if (proceed == DialogResult.Yes)
            {
                Cursor.Current = Cursors.WaitCursor;

                MessageBox.Show("Checking for WeatherData and WthExpUsage for Readings starting:\n" + 
                    datePicker.Value.ToShortDateString(), "Weather Job Started!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                _aerisJob.Execute(_aerisJobParams, datePicker.Value, forceHistorical: false);

                _writer.FlushAsync();
                EnableButtons();
            }
            else
            {
                MessageBox.Show("Weather Job NOT started", "Request Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }

            _writer.Close();
            EnableButtons();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            DisableButtons();

            DialogResult proceed = MessageBox.Show("Will check for/insert all WeatherData (forced historical run individually checks WeatherData " +
                "table for every needed entry of each zip code and every date. Takes longer than regular Weather Job, which figures if a historical run " +
                "is needed by multiplying zip codes by number of days needed then checking against number of actual WeatherData entries) AND WthExpUsage " +
                "going back to \n\n" + datePicker.Value.ToShortDateString(), 
                "Start Weather Job with Forced Historical Run?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            
            Console.SetOut(_writer);

            if (proceed == DialogResult.Yes)
            {
                Cursor.Current = Cursors.WaitCursor;
                MessageBox.Show("Starting Weather Job!\nForcing Historical Run on WeatherData and WthExpUsage for Readings starting:\n" + 
                    datePicker.Value.ToShortDateString(), "Weather Job Started!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                _aerisJob.Execute(_aerisJobParams, datePicker.Value, forceHistorical: true);
                _writer.FlushAsync();
            }
            else
            {
                MessageBox.Show("Weather Job NOT started", "Request Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }

            _writer.Close();
            EnableButtons();
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            DisableButtons();

            DialogResult proceed = MessageBox.Show("Starting the WNRdngData01 Regression Job: The WNRdngData01 stored procedure is executed to find new " +
                "Acc/Util/UnitIDs in need of regression analysis. For each new Acc/Util/UnitID the best regression model is found and inserted into/updated in " +
                "WthNormalParams. Then its respective Readings' WthExpUsages are calculated using the new regression model and insterted into/updated in WthExpUsage. \n" +
                "The daily logs contain Debug-level logging, WeatherData insert info, and WthExpUsage insert/update info.\n" +
                "WthExpUsage will be calculated and updated as far back as there is WeatherData in the database.",
                "Start WNRdngData01 RegressionJob?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            Console.SetOut(_writer);

            if (proceed == DialogResult.Yes)
            {
                Cursor.Current = Cursors.WaitCursor;
                MessageBox.Show("Starting WNRdngData01 Regression Job!", "WNRdngData01 Regression Job Started!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                _wNRdngData01RegressionJob.Execute(_aerisJobParams);
                _writer.FlushAsync();
            }
            else
            {
                MessageBox.Show("WNRdngData01 Regression Job NOT started", "Request Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }

            _writer.Close();
            EnableButtons();
        }

        private void DisableButtons()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
        }

        private void EnableButtons()
        {
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            // Instantiate the writer
            _writer = new TextBoxStreamWriter(richTextBox1);
            // Redirect the out Console stream
            Console.SetOut(_writer);

            Console.WriteLine("Welcome to JIT Weather Regression Service - Manual Start Option");
            Console.WriteLine("Selecting a more recent Start Date will not delete any older entries.");
            Console.WriteLine("This is a tail of log created in C:\\Users\\workweek\\Logs\\Daily\\log-{Date}.log");
            _writer.Flush();
            _writer.Close();
        }
    }
}
