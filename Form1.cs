using WeatherServiceForm.Scheduled;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WeatherServiceForm.Repository;
using WeatherServiceForm.ConsoleRedirection;

namespace WeatherServiceForm
{
    public partial class Form1 : Form
    {
        TextWriter _writer = null;

        AerisJobParams _aerisJobParams;
        IWeatherRepository _weatherRepository;
        AerisJob _aerisJob = new AerisJob();
        WNRdngData01RegressionJob _wNRdngData01RegressionJob = new WNRdngData01RegressionJob();

        public Form1(AerisJobParams aerisJobParams)
        {
            InitializeComponent();

            _aerisJobParams = aerisJobParams;
            _weatherRepository = new WeatherRepository(_aerisJobParams);

            //MessageBox.Show("Starting the Weather Job will take in your selected date and call Aeris for weather information if there are any gaps in WeatherData table " +
            //    "(possibly from failed scheduled starting of JITWeatherService Windows Service) from then up to, and including, yesterday. Then it will check for all Readings " +
            //    "(starting at the same selected date and ending no later than we have weather info--hopefully yesterday) that should be normalized, and for every one NOT " +
            //    "already calculated and stored in WthExpUsage, respectively calculate and insert."+
            //    "\n\n'Force Historical Run' should only be needed if:\n " +
            //    "1) The number of zip codes targeted for weather information has descreased AND target date has been pushed back. \n" +
            //    "2) The number of zip codes is the same, but one has been deleted entirely and a new zip code has been entered. \n\n" +
            //    "Starting the WNRdngData01 Regression Job: The WNRdngData01 stored procedure is executed to find new Acc/Util/UnitIDs in need of regression " +
            //    "analysis. For each new Acc/Util/UnitID the best regression model is found and inserted into/updated in WthNormalParams. Then its respective " +
            //    "Readings' WthExpUsages are calculated using the new regression model and insterted into/updated in WthExpUsage. \n" +
            //    "The daily logs contain Debug-level logging, WeatherData insert info, and WthExpUsage insert/update info.\n" +
            //    "WthExpUsage will be calculated and updated as far back as there is WeatherData in the database. \n\n" +
            //    "The DateTimePicker's inital value is the last used Start Date. Selecting a more recent Start Date will not delete any older entries.");

            MessageBox.Show("Weather Job makes sure the database has the most recent WeatherData and will gather historical data if new ZipCodes have been added. \n" +
                "ExpUsage is then calculated for new or missing Readings for each Account. \n" +
                "Regression Job executes the Stored Procedure to find Accounts in need of finding the best regression model, then uses the returned Readings to fit the models.");

            //datePicker = new DateTimePicker();
            //datePicker.Name = "Get WeatherData and WthExpUsage back to:";
            //datePicker.Size = new Size(209, 20);
            //datePicker.Location = new Point(91, 118);
            //datePicker.Value = _weatherRepository.GetCurrentOldestWeatherDataDate();
            //datePicker.MinDate = new DateTime(2013, 1, 1);
            //datePicker.MaxDate = yesterday;
            //datePicker.CustomFormat = "MMMM dd, yyyy";
            //datePicker.Format = DateTimePickerFormat.Custom;
            //this.Controls.Add(datePicker);

            this.Controls.Add(button1);
            button1.Click += new EventHandler(Button1_Click);
            
            this.Controls.Add(button2);
            button2.Click += new EventHandler(Button2_Click);

            //this.Controls.Add(button2);
            //button2.Click += new EventHandler(Button3_Click);

        }

        private void Button1_Click(object sender, EventArgs e)
        {
            DisableButtons();

            DialogResult proceed = MessageBox.Show("The Console will display the running log while it executes the Job.\n",
                "Start the Regression Job?", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);

            //Console.SetOut(_writer);

            if (proceed == DialogResult.Yes)
            {
                Cursor.Current = Cursors.WaitCursor;

                //MessageBox.Show("Checking both WeatherData and WthExpUsage for new entries..", "Weather Job Started.", MessageBoxButtons.OK, MessageBoxIcon.None);

                _aerisJob.Execute(_aerisJobParams);

                //_writer.FlushAsync();
                //EnableButtons();
            }
            else
            {
                MessageBox.Show("Weather Job not started", "Request Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }

            //_writer.Close();
            EnableButtons();
        }

        //private void Button2_Click(object sender, EventArgs e)
        //{
        //    DisableButtons();

        //    DialogResult proceed = MessageBox.Show("Will check for/insert all WeatherData (forced historical run individually checks WeatherData " +
        //        "table for every needed entry of each zip code and every date. Takes longer than regular Weather Job, which figures if a historical run " +
        //        "is needed by multiplying zip codes by number of days needed then checking against number of actual WeatherData entries) AND WthExpUsage " +
        //        "going back to \n\n" + datePicker.Value.ToShortDateString(), 
        //        "Start Weather Job with Forced Historical Run?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            
        //    Console.SetOut(_writer);

        //    if (proceed == DialogResult.Yes)
        //    {
        //        Cursor.Current = Cursors.WaitCursor;
        //        MessageBox.Show("Starting Weather Job!\nForcing Historical Run on WeatherData and WthExpUsage for Readings starting:\n" + 
        //            datePicker.Value.ToShortDateString(), "Weather Job Started!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

        //        _aerisJob.Execute(_aerisJobParams, datePicker.Value, forceHistorical: true);
        //        _writer.FlushAsync();
        //    }
        //    else
        //    {
        //        MessageBox.Show("Weather Job NOT started", "Request Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        //    }

        //    _writer.Close();
        //    EnableButtons();
        //}

        private void Button2_Click(object sender, EventArgs e)
        {
            DisableButtons();

            DialogResult proceed = MessageBox.Show("The Console will display the running log while it executes the Job.\n",
                "Start the Regression Job?", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);

            //Console.SetOut(_writer);

            if (proceed == DialogResult.Yes)
            {
                Cursor.Current = Cursors.WaitCursor;
                //MessageBox.Show("The Console will display the running log while it executes the job.", 
                //    "Regression Job Started.", MessageBoxButtons.OK, MessageBoxIcon.None);

                _wNRdngData01RegressionJob.Execute(_aerisJobParams);
                //_writer.FlushAsync();
            }
            else
            {
                MessageBox.Show("Regression Job not started", "Request Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }

            //_writer.Close();
            EnableButtons();
        }

        private void DisableButtons()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            //button2.Enabled = false;
        }

        private void EnableButtons()
        {
            button1.Enabled = true;
            button2.Enabled = true;
            //button2.Enabled = true;
        }

        //private void Form1_Load_1(object sender, EventArgs e)
        //{
        //    // Instantiate the writer
        //    //_writer = new TextBoxStreamWriter(richTextBox1);
        //    // Redirect the out Console stream
        //    //Console.SetOut(_writer);

        //    //Console.WriteLine("This is a tail of log created in C:\\Users\\workweek\\Logs\\Daily\\log-{Date}_ManualStart.log");
        //    _writer.Flush();
        //    _writer.Close();
        //}
    }
}
