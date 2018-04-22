using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Reset();            
        }
        Graphics g;

        public void LogEventTime(string eventText, string minutes, string project, string type)
        {
            var insertQuery = " INSERT INTO EventLog VALUES (" + GetField(eventText) + GetField(minutes) + GetField(project) + GetField(type) + "current_timestamp)";
            OpenConnection();
            SqlCommand insert = new SqlCommand(insertQuery.ToString(), Conn);
            insert.ExecuteNonQuery();
            CloseConnection();
        }

        public void LogSystemLog(string log, string tool, string type)
        {
            var insertQuery = " INSERT INTO SystemLog VALUES (" + GetField(log) + GetField(tool) + GetField(type) + "current_timestamp)";
            OpenConnection();
            SqlCommand insert = new SqlCommand(insertQuery.ToString(), Conn);
            insert.ExecuteNonQuery();
            CloseConnection();
        }        

        protected SqlConnection Conn;
        void OpenConnection()
        {
            Conn = new SqlConnection();
            Conn.ConnectionString =
                "Server = WT-ENDEAVOR\\SQLEXPRESS; Initial Catalog = CalendarDb; User ID =test; Password =1;";
            Conn.Open();
        }

        void CloseConnection()
        {
            Conn.Close();
            Conn.Dispose();
        }

        string GetField(string value, bool useComma = true, bool useAnd = false)
        {
            var res = "'" + value + "'";
            if (useComma)
                res += ",";
            if (useAnd)
                res += " and";
            return res;
        }


        public void DrawPieChartOnForm(int percent)
        {   
            var startX = 222;
            var startY = 100;
            var radius = 40;
            var diameter = radius * 2;
            float factor = radius-(float)(radius / Math.Sqrt(2));
                        
            int[] myPiePercent = { 100-percent, percent };            
            Color[] myPieColors = { Color.DarkGray, Color.LightBlue };            
            
            int PiePercentTotal = 0;
            for (int i = 0; i < myPiePercent.Length; i++)
            {
                using (SolidBrush brush = new SolidBrush(myPieColors[i]))
                {                    
                    var startAngle = Convert.ToSingle(PiePercentTotal * 360 / 100)-90;
                    var sweepAngle = Convert.ToSingle(myPiePercent[i] * 360 / 100);
                    g.FillPie(brush, startX, startY, diameter, diameter, startAngle, sweepAngle);
                }
                PiePercentTotal += myPiePercent[i];
            }

            Pen black = new Pen(Color.Azure,1);            
            Pen blue = new Pen(Color.DarkBlue,1);
            //g.DrawEllipse(black, startX, startY, diameter, diameter);
            g.DrawLine(black, startX, startY+ radius, startX+diameter, startY+radius);
            g.DrawLine(black, startX + radius, startY, startX + radius, startY + diameter);
            g.DrawLine(black, startX + factor, startY+ factor, startX + diameter -factor, startY + diameter - factor);
            g.DrawLine(black, startX + diameter - factor, startY + factor, startX + factor, startY + diameter - factor);
        }

        Timer timer;        
        int remainSecs;        

        private void Reset()
        {
            if (timer != null)
                timer.Stop();

            if (comboBox1.Items.Count == 0)
            {
                var tasksList = ConfigurationManager.AppSettings["tasksList"].Split(',');
                this.comboBox1.Items.AddRange(tasksList);
                comboBox1.SelectedIndex = 0;
                comboBox2.SelectedIndex = 0;
            }

            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
            remainSecs = 0;
            remainSecsBackup = 0;
            label1.Text = "";
            label6.Text = "";
            button1.Enabled = true;
            button1.Text = "Start";
            progressBar1.Value = 0;
            progressBar1.Maximum = 0;
            label5.Text = "";
            button3.Enabled = false;
            label8.Text = "";
            panel1.Visible = false;
            label4.Text = "";
            startTime = new DateTime();
            g = this.CreateGraphics();
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;            
            LogEvent("Timer is reset.");            
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            remainSecs--;
            ProcessTick();
        }

        // To support flashing.
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        //Flash both the window caption and taskbar button.
        //This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
        public const UInt32 FLASHW_ALL = 3;

        // Flash continuously until the window comes to the foreground. 
        public const UInt32 FLASHW_TIMERNOFG = 12;

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        // Do the flashing - this does not involve a raincoat.
        public static bool FlashWindowEx(Form form)
        {
            IntPtr hWnd = form.Handle;
            FLASHWINFO fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = hWnd;
            fInfo.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
            fInfo.uCount = UInt32.MaxValue;
            fInfo.dwTimeout = 0;

            return FlashWindowEx(ref fInfo);
        }

        void Alarm()
        {
            timer.Stop();
            button2.Text = "Resume";
            //button1.Enabled = true;
            button1.Text = "Completed";
            LogEvent("Alarm! " + comboBox1.Text);            
            player.PlayLooping();
            button3.Enabled = true;
            label8.Text = comboBox1.Text;
            panel1.Visible = true;
            this.label3.Text = DateTime.Now.ToString();
            this.label1.Text = "Alarming now.";
            //this.TopMost = true;
            FlashWindowEx(this);
        }

        void ProcessTick()
        {
            if (remainSecs < 0)
                remainSecs = 0;
            var hours = remainSecs / 3600;            
            var secsWithoutHrs = (remainSecs - 3600 * hours);
            var hoursPassed = (remainSecsBackup-remainSecs) / 3600;
            var secsPassedWithoutHrs = (remainSecsBackup-remainSecs) - 3600 * hoursPassed;

            label1.Text = "Countdown: " + hours.ToString("0#") + ":" + (secsWithoutHrs / 60).ToString("0#") + ":"+ (secsWithoutHrs % 60).ToString("0#") + "";
            label6.Text = "Passed: " + hoursPassed.ToString("0#") + ":" + (secsPassedWithoutHrs / 60).ToString("0#") + ":" + (secsPassedWithoutHrs % 60).ToString("0#") + "";

            var percent = remainSecs * 100d / remainSecsBackup;
            progressBar1.Value = remainSecs;
            label5.Text = Math.Round(percent, 1).ToString("N1") + "%";

            //progressBar1.Value = remainSecsBackup - remainSecs;
            //var percent = progressBar1.Value * 100d / progressBar1.Maximum ;
            //label5.Text = (Math.Round(100-percent, 1)).ToString("N1") + "%";


            if (remainSecs == 0)
            {
                Alarm();
            }            
            DrawPieChartOnForm((int)Math.Round(percent, 0));
            if(remainSecsBackup-remainSecs==2)
                playerStart.Stop();

            if (remainSecs % syncSec == 0)
                SyncTime();
        }
                
        System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"Media\Computer_Magic-Microsift-1901299923.wav");
        System.Media.SoundPlayer playerStart = new System.Media.SoundPlayer(@"Media\Ticking_Clock-KevanGC-1934595011.wav");

        private void button1_Click(object sender, EventArgs e)
        {           
            StartTimer();            
        }

        DateTime startTime;
        int remainSecsBackup;

        void StartTimer()
        {
            double mins = double.Parse(comboBox2.Text);
            remainSecs = Convert.ToInt32(mins * 60);
            remainSecsBackup = remainSecs;
            if (remainSecs <= 0)
            {
                LogEvent("Input requires a positive number.");                
                return;
            }
            //if (comboBox1.SelectedIndex== 0)
            //{
            //    LogEvent("Please select or type an event.");
            //    return;
            //}

            var hours = remainSecs / 3600;
            var secsWithoutHrs = (remainSecs - 3600 * hours);
            var hoursPassed = (remainSecsBackup - remainSecs) / 3600;
            var secsPassedWithoutHrs = (remainSecsBackup - remainSecs) - 3600 * hoursPassed;

            timer.Start();
            label1.Text = "Countdown: " + hours.ToString("0#") + ":" + (secsWithoutHrs / 60).ToString("0#") + ":" + (secsWithoutHrs % 60).ToString("0#") + "";
            label6.Text = "Passed: " + hoursPassed.ToString("0#") + ":" + (secsPassedWithoutHrs / 60).ToString("0#") + ":" + (secsPassedWithoutHrs % 60).ToString("0#") + "";
            button1.Enabled = false;
            button1.Text = "Running";
            button2.Text = "Pause";
            LogEvent("Timer started for " + remainSecs + " seconds.");            
            progressBar1.Maximum = remainSecs;
            progressBar1.Value = remainSecs;
            //progressBar1.Value = 0;
            label5.Text = "100%";
            button3.Enabled = false;
            label8.Text = "";
            panel1.Visible = false;
            DrawPieChartOnForm(100);
            startTime = DateTime.Now;
            isTimeLogged = false;
            playerStart.Play();
        }

        void StopTimer()
        {
            if (remainSecs == 0)
            {
                LogEvent("Timer is not currently running.");                
                return;
            }

            if (timer.Enabled)
            {
                timer.Stop();
                button2.Text = "Resume";
                button1.Enabled = true;
                button1.Text = "Restart";
                LogEvent("Timer paused.");                
            }
            else
            {
                timer.Start();
                button2.Text = "Pause";
                button1.Enabled = false;
                button1.Text = "Running";
                LogEvent("Timer resumed.");                
            }
        }

        void LogEvent(string log)
        {            
            textBox3.AppendText(DateTime.Now.ToString("HH:mm:ss")+": "+ log);
            textBox3.AppendText(Environment.NewLine);
            LogSystemLog(log, "SmartClock", "Info");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            StopTimer();
        }

        void ConfirmTimer()
        {            
            player.Stop();
            LogEvent("Timer is confirmed.");
            button3.Enabled = false;
            label8.Text = "";
            panel1.Visible = false;
            LogTime();
            Reset();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ConfirmTimer();
            //DrawPieChartOnForm(0);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && button1.Enabled)
                StartTimer();
        }

        bool isTimeLogged = false;

        private void button4_Click_1(object sender, EventArgs e)
        {
            if (startTime.Year == 1)
            {
                LogEvent("Timer is not started.");
                return;
            }

            if (MessageBox.Show("Confirm manual log time before automatic log on completion? Only one logging is allowed per session.","Log Time",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question).Equals(DialogResult.Yes)) {
                LogTime();
            }
            
        }

        void LogTime()
        {  
            if(isTimeLogged)
            {
                LogEvent("Time was already logged, skip logging.");
                return;
            }

            var timeElapsed = DateTime.Now.Subtract(startTime).TotalMinutes;
            var roundedTimeElapsed = Math.Round(timeElapsed, 0);
            var text = comboBox1.Text.Split(':');
            var eventText = text.Length > 1 ? text[1].Trim() : text[0];
            var project = text.Length > 2 ? text[2].Trim() : "";
            var type = text.Length > 1 ? text[0].Trim() : "";

            LogEventTime(eventText, roundedTimeElapsed.ToString(), project, type);
            LogEvent(roundedTimeElapsed + " minutes are logged.");
            isTimeLogged = true;
        }

        int syncSec = 9;

        void SyncTime()
        {            
            var elapsedSeconds = (int)DateTime.Now.Subtract(startTime).TotalSeconds;
            remainSecs = remainSecsBackup - elapsedSeconds;

            if (!timer.Enabled && remainSecs>0 && remainSecs% syncSec != 0)
            {
                ProcessTick();
                LogEvent("Timer is manually synchonized.");
            }            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (remainSecs == 0)
            {
                LogEvent("Sync only when timer started.");
                return;
            }
            SyncTime();
        }

        private void panel1_Leave(object sender, EventArgs e)
        {
            this.panel1.Focus();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //We'll need this for when the Form starts to move
            lastClick = new Point(e.X, e.Y);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            //Draws a border to make the Form stand out; Just done for appearance, not necessary
            /*Pen p = new Pen(Color.SlateGray, 3);
            e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
            p.Dispose();*/
            //DrawPieChartOnForm(0);
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            //Move the Form the same difference the mouse cursor moved;
            if (e.Button == MouseButtons.Left) //Only when mouse is clicked
            {
                this.Left += e.X - lastClick.X;
                this.Top += e.Y - lastClick.Y;
            }
        }

        Point lastClick; //Holds where the Form was clicked

        private void button6_Click(object sender, EventArgs e)
        {
            SnoozeTimer();
        }

        void SnoozeTimer()
        {
            player.Stop();
            label4.Text = "Overtime";
            label1.Text = "Alarm is snoozed";
            LogEvent("Alarm is snoozed.");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
