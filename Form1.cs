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
using WeatherServiceForm;
using WeatherServiceForm.ConsoleRedirection;
using WeatherServiceForm.Scheduled;

namespace WeatherForm
{
    public partial class Form1 : Form
    {
        TextWriter _writer = null;

        AerisJobParams _aerisJobParams;
        AerisJob aerisJob = new AerisJob();
        DateTimePicker datePicker;
        DateTime yesterday = DateTime.Now.AddDays(-1);

        string todayStr = DateTime.Now.ToShortDateString();        

        //Button button1;
        //Button button2;

        public Form1(AerisJobParams aerisJobParams)
        {
            InitializeComponent();

            _aerisJobParams = aerisJobParams;
            
            MessageBox.Show("Starting the Weather Job will take in your selected date and call Aeris for weather information if there are any gaps in WeatherData table " +
                "(possibly from failed scheduled starting of JITWeatherService Windows Service) from then up to, and including, yesterday. Then it will check for all Readings " +
                "(starting at the same selected date and ending no later than we have weather info--hopefully yesterday) that should be normalized, and for every one NOT " +
                "already calculated and stored in WthExpUsage, respectively calculate and insert."+
                "\n\n'Force Historical Run' should only be needed if:\n " +
                "1) The number of zip codes targeted for weather information has descreased AND target date has been pushed back. \n" +
                "2) The number of zip codes is the same, but one has been deleted entirely and a new zip code has been entered.");

            datePicker = new DateTimePicker();
            datePicker.Name = "Get WeatherData and WthExpUsage back to:";
            datePicker.Size = new Size(209, 20);
            datePicker.Location = new Point(91, 118);
            datePicker.Value = new DateTime(2015, 1, 1);
            datePicker.MinDate = new DateTime(2013, 1, 1);
            datePicker.MaxDate = yesterday;
            datePicker.CustomFormat = "MMMM dd, yyyy";
            datePicker.Format = DateTimePickerFormat.Custom;
            this.Controls.Add(datePicker);

            //button1 = new Button();
            //button1.Size = new Size(120, 60);
            //button1.Location = new Point(30, 80);
            //button1.Text = "Start Weather Job";
            this.Controls.Add(button3);
            button3.Click += new EventHandler(Button3_Click);

            //button2 = new Button();
            //button2.Size = new Size(120, 60);
            //button2.Location = new Point(30, 200);
            //button2.Text = "Start Weather Job with\nForced Historical Run";
            this.Controls.Add(button4);
            button4.Click += new EventHandler(Button4_Click);

        }

        private void Button3_Click(object sender, EventArgs e)
        {
            DialogResult proceed = MessageBox.Show("Will check for/insert all WeatherData AND WthExpUsage going back to \n\n" + 
                datePicker.Value.ToShortDateString(), "Start Weather Job?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            Console.SetOut(_writer);

            if (proceed == DialogResult.Yes)
            {
                Cursor.Current = Cursors.WaitCursor;
                Form1.
                MessageBox.Show("Checking for WeatherData and WthExpUsage for Readings starting:\n" + 
                    datePicker.Value.ToShortDateString(), "Weather Job Started!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                aerisJob.Execute(_aerisJobParams, datePicker.Value, false);
                _writer.FlushAsync();

            }
            else
            {
                MessageBox.Show("Weather Job NOT started", "Request Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            _writer.Close();
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            DialogResult proceed = MessageBox.Show("Will check for/insert all WeatherData (forced historical run manually checks WeatherData " +
                "table for every needed entry of each zip code and every date. Takes longer than regular Weather Job, which guesses if a historical run is needed by multiplying " +
                "zip codes by number of days needed then checking against number of actual WeatherData entries) AND WthExpUsage going back to \n\n" +
                datePicker.Value.ToShortDateString(), "Start Weather Job with Forced Historical Run?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            
            Console.SetOut(_writer);

            if (proceed == DialogResult.Yes)
            {
                Cursor.Current = Cursors.WaitCursor;
                MessageBox.Show("Starting Weather Job!\nForcing Historical Run on WeatherData and WthExpUsage for Readings starting:\n" + 
                    datePicker.Value.ToShortDateString(), "Weather Job Started!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                aerisJob.Execute(_aerisJobParams, datePicker.Value, true);
                _writer.FlushAsync();
            }
            else
            {
                MessageBox.Show("Weather Job NOT started", "Request Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            _writer.Close();
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            // Instantiate the writer
            _writer = new TextBoxStreamWriter(richTextBox1);
            // Redirect the out Console stream
            Console.SetOut(_writer);

            Console.WriteLine("Welcome to JIT Weather Service - Manual Start Option");
            Console.WriteLine("This is a trail of log created in C:\\Users\\workweek\\Logs\\log-{Date}.log");
            _writer.Flush();
            _writer.Close();
        }
        
    }
}
