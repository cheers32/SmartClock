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
        private List<Task> loadedTasks;
        private Task currentTask;

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

            comboBox5.SelectedIndex = 0;
            ChangeQuadrant();
            label9.Text = "";
            label10.Text = "";
            label11.Text = "";
            label12.Text = "";
            loadedTasks = new List<Task>();
            currentTask = null;

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

        void HandleSyncClick()
        {
            if (remainSecs == 0)
            {
                LogEvent("Sync only when timer started.");
                return;
            }
            SyncTime();
        }

        void UpdateTask()
        {
            var isTaskSelected = comboBox3.SelectedIndex != -1;
            if (!isTaskSelected)
            {
                label9.Text = "Select a task to update.";
                return;
            }
            var task = GatherTaskInfo();

            var insertQuery = "UPDATE Task SET ValidUntil=current_timestamp WHERE Id="+task.Id+
                " INSERT INTO Task VALUES (" + GetField(task.TaskName) + GetField(task.Urgent) + GetField(task.Important) + GetField(task.Type) + GetField(task.Status)
                              + GetField(task.Priority1) + GetField(task.Priority2) + GetField(task.Comments) + GetField(task.Estimate) + "current_timestamp," + GetField(task.Id.ToString()) + " '9999-12-31 23:59:59')";
            OpenConnection();
            SqlCommand insert = new SqlCommand(insertQuery.ToString(), Conn);
            insert.ExecuteNonQuery();
            CloseConnection();

            label9.Text = "Task ID:"+task.Id+" Updated (Status:"+task.Status+").";
        }

        Task GatherTaskInfo()
        {
            var task = new Task();
            task.TaskName = textBox2.Text;
            task.Urgent = checkBox1.Checked.ToString();
            task.Important = checkBox2.Checked.ToString();
            task.Type = label2.Text;
            task.Status = comboBox5.Text.Equals("All") ? "No Plan" : comboBox5.Text;
            task.Priority1 = textBox4.Text;
            task.Priority2 = textBox5.Text;
            task.Comments = textBox1.Text;
            task.Estimate = textBox6.Text;
            task.Id = currentTask == null ? -1 : currentTask.Id;
            return task;
        }

        void InsertTask()
        {
            if (string.IsNullOrWhiteSpace(textBox2.Text))
            {
                label9.Text = "Task name cannot be empty";
                return;
            }
            var task = GatherTaskInfo();
            
            var insertQuery = " INSERT INTO Task VALUES (" + GetField(task.TaskName) + GetField(task.Urgent) + GetField(task.Important) + GetField(task.Type) + GetField(task.Status)
                              + GetField(task.Priority1) + GetField(task.Priority2) + GetField(task.Comments) + GetField(task.Estimate) + "current_timestamp" + ",(SELECT MAX(Id)+1 FROM Task), '9999-12-31 23:59:59')";
            OpenConnection();
            SqlCommand insert = new SqlCommand(insertQuery, Conn);
            insert.ExecuteNonQuery();
            CloseConnection();

            label9.Text = "New task added (Status:" + task.Status + ").";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            UpdateTask();
            LoadTasks();
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

        private void button8_Click(object sender, EventArgs e)
        {
            LoadTasks();
            label9.Text = loadedTasks.Count + " tasks loaded.";
        }

        void LoadTasks()
        {
            var status = comboBox5.SelectedItem.ToString();
            loadedTasks = LoadTasksByStatus(status);
            comboBox3.Items.Clear();
            foreach (var task in loadedTasks)
            {
                comboBox3.Items.Add(task.Type + " - " + task.TaskName+" ("+task.Status+")");
            }
            if (loadedTasks.Count > 0)
                comboBox3.SelectedIndex = 0;
        }

        List<Task> LoadTasksByText(string text)
        {
            var selectQuery = "SELECT * FROM Task WHERE Task LIKE " + GetField("%"+text+"%", false) + " ORDER BY UpdateTime DESC";
            OpenConnection();
            SqlCommand select = new SqlCommand(selectQuery.ToString(), Conn);
            var res = new List<Task>();
            using (SqlDataReader reader = select.ExecuteReader())
            {
                while (reader.Read())
                {
                    Task task = new Task();
                    task.TaskName = reader["Task"].ToString();
                    task.Urgent = reader["Urgent"].ToString();
                    task.Important = reader["Important"].ToString();
                    task.Type = reader["Type"].ToString();
                    task.Status = reader["Status"].ToString();
                    task.Priority1 = reader["Priority1"].ToString();
                    task.Priority2 = reader["Priority2"].ToString();
                    task.Comments = reader["Comments"].ToString();
                    task.Estimate = reader["Estimate"].ToString();
                    task.UpdateTime = DateTime.Parse(reader["UpdateTime"].ToString());
                    task.Id = int.Parse(reader["Id"].ToString());
                    task.ValidUntil = DateTime.Parse(reader["ValidUntil"].ToString());
                    res.Add(task);
                }
            }
            CloseConnection();
            return res;
        }

        void LoadHistory()
        {
            if (currentTask == null)
            {
                label9.Text = "Select a task to load history ORDER BY Id DESC";
                return;
            }

            loadedTasks = LoadTasksById(currentTask.Id.ToString());
            comboBox3.Items.Clear();
            foreach (var task in loadedTasks)
            {
                comboBox3.Items.Add(task.Type + " - " + task.TaskName + " (" + task.Status + ")");
            }

        }

        List<Task> LoadTasksById(string id)
        {
            var selectQuery = "SELECT * FROM Task WHERE Id =" + GetField(id, false) + "ORDER BY UpdateTime DESC";
            OpenConnection();
            SqlCommand select = new SqlCommand(selectQuery.ToString(), Conn);
            var res = new List<Task>();
            using (SqlDataReader reader = select.ExecuteReader())
            {
                while (reader.Read())
                {
                    Task task = new Task();
                    task.TaskName = reader["Task"].ToString();
                    task.Urgent = reader["Urgent"].ToString();
                    task.Important = reader["Important"].ToString();
                    task.Type = reader["Type"].ToString();
                    task.Status = reader["Status"].ToString();
                    task.Priority1 = reader["Priority1"].ToString();
                    task.Priority2 = reader["Priority2"].ToString();
                    task.Comments = reader["Comments"].ToString();
                    task.Estimate = reader["Estimate"].ToString();
                    task.UpdateTime = DateTime.Parse(reader["UpdateTime"].ToString());
                    task.Id = int.Parse(reader["Id"].ToString());
                    task.ValidUntil = DateTime.Parse(reader["ValidUntil"].ToString());
                    res.Add(task);
                }
            }
            CloseConnection();
            return res;
        }

        List<Task> LoadTasksByStatus(string status)
        {
            var selectQuery = status.Equals("All")? " SELECT * FROM Task WHERE ValidUntil>=current_timestamp ORDER BY UpdateTime DESC" 
                : " SELECT * FROM Task WHERE ValidUntil>=current_timestamp AND Status =" + GetField(status,false) + "ORDER BY UpdateTime DESC";
            OpenConnection();
            SqlCommand select = new SqlCommand(selectQuery.ToString(), Conn);
            var res = new List<Task>();
            using (SqlDataReader reader = select.ExecuteReader())
            {
                while (reader.Read())
                {
                    Task task = new Task();
                    task.TaskName = reader["Task"].ToString();
                    task.Urgent = reader["Urgent"].ToString();
                    task.Important = reader["Important"].ToString();
                    task.Type = reader["Type"].ToString();
                    task.Status = reader["Status"].ToString();
                    task.Priority1 = reader["Priority1"].ToString();
                    task.Priority2 = reader["Priority2"].ToString();
                    task.Comments = reader["Comments"].ToString();
                    task.Estimate = reader["Estimate"].ToString();
                    task.UpdateTime = DateTime.Parse(reader["UpdateTime"].ToString());
                    task.Id = int.Parse(reader["Id"].ToString());
                    task.ValidUntil = DateTime.Parse(reader["ValidUntil"].ToString());
                    res.Add(task);
                }
            }
            CloseConnection();
            return res;
        }

        public class Task
        {
            public string TaskName { get; set; }
            public string Urgent { get; set; }
            public string Important { get; set; }
            public string Type { get; set; }
            public string Status { get; set; }
            public string Priority1 { get; set; }
            public string Priority2 { get; set; }
            public string Comments { get; set; }
            public string Estimate { get; set; }
            public DateTime UpdateTime { get; set; }
            public int Id { get; set; }
            public DateTime ValidUntil { get; set; }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            ChangeQuadrant();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            ChangeQuadrant();
        }

        void ChangeQuadrant()
        {
            var urgent = checkBox1.Checked;
            var important = checkBox2.Checked;
            if (urgent && important)
            {
                label2.Text = "Q1";
                label2.ForeColor = Color.Gold;
            }
            else if (!urgent && important)
            {
                label2.Text = "Q2";
                label2.ForeColor = Color.MediumSeaGreen;
            }
            else if (urgent && !important)
            {
                label2.Text = "Q3";
                label2.ForeColor = Color.LightSkyBlue;
            }
            else if (!urgent && !important)
            {
                label2.Text = "Q4";
                label2.ForeColor = Color.Red;
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentTask = loadedTasks[comboBox3.SelectedIndex];
            DisplayTask(currentTask);
        }

        void DisplayTask(Task task)
        {
            this.textBox2.Text = task.TaskName;
            this.checkBox1.Checked = bool.Parse(task.Urgent);
            this.checkBox2.Checked = bool.Parse(task.Important);
            this.comboBox5.Text = task.Status;
            this.textBox4.Text = task.Priority1;
            this.textBox5.Text = task.Priority2;
            this.textBox1.Text = task.Comments;
            this.textBox6.Text = task.Estimate;
            this.label10.Text = task.UpdateTime.ToString();
            this.label12.Text = "Id: "+task.Id.ToString();
            this.label11.Text = task.ValidUntil.ToString();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            InsertTask();
            ClearInput();
            LoadTasks();
            ClearInput();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            LoadHistory();
            label9.Text = loadedTasks.Count + " history loaded.";
        }

        void ClearInput()
        {
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            textBox4.Text = "";
            textBox5.Text = "";
            textBox6.Text = "";
            comboBox5.SelectedIndex = 0;
            textBox1.Text = "";
            textBox2.Text = "";
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            var status = comboBox5.SelectedItem.ToString();
            switch (status)
            {

                case "Completed":
                    comboBox5.BackColor = Color.Purple;
                    comboBox5.ForeColor = Color.Beige;
                    break;
                case "No Plan":
                    comboBox5.BackColor = Color.LightGray;
                    comboBox5.ForeColor = Color.Black;
                    break;
                case "Planned":
                    comboBox5.BackColor = Color.SkyBlue;
                    comboBox5.ForeColor = Color.Black;
                    break;
                case "In Progress":
                    comboBox5.BackColor = Color.MediumSeaGreen;
                    comboBox5.ForeColor = Color.Black;
                    break;
                case "Cancelled":
                    comboBox5.BackColor = Color.Gray;
                    comboBox5.ForeColor = Color.Black;
                    break;
                case "All":
                    comboBox5.BackColor = Color.DimGray;
                    comboBox5.ForeColor = Color.Beige;
                    break;
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && button1.Enabled)
            {
                InsertTask();
                ClearInput();
                LoadTasks();
                ClearInput();
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            SearchText();
            label9.Text = loadedTasks.Count + " search results returned.";
        }

        void SearchText()
        {
            loadedTasks = LoadTasksByText(textBox1.Text);
            comboBox3.Items.Clear();
            foreach (var task in loadedTasks)
            {
                comboBox3.Items.Add(task.Type + " - " + task.TaskName + " (" + task.Status + ")");
            }
        }

        private void textBox1_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && button1.Enabled)
            {
                SearchText();
                label9.Text = loadedTasks.Count + " search results returned.";
                if(loadedTasks.Count>0)
                    comboBox3.SelectedIndex = 0;
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            ClearInput();
        }
    }
}
