using System;
using System.ComponentModel;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;
using static System.Text.UTF8Encoding;
using static ItsAlive.Properties.Resources;
using static ItsAlive.Properties.Settings;

namespace ItsAlive
{
    public partial class Form1 : Form
    {
        private BackgroundWorker _pingBw = new BackgroundWorker(); //Ping background worker test
        private BackgroundWorker _emailBw = new BackgroundWorker(); //Email Background Worker
        

        private bool _router = false; //Router Status bool
        private bool _nas = false; // server status bool
        private bool _mailSent = false; //Email status bool
        private int _pings = 0; // num of pings 
        private int _downs = 0; //Counts number of down pings
        private bool _server;
        private bool _running = false;
        private bool _internet = false;
        private bool _main = false;
        private bool _vpn = false; 

        public Form1()
        {
            InitializeComponent();
            //Config background worker for the pings
            _pingBw.WorkerReportsProgress = true;
            _pingBw.WorkerSupportsCancellation = true;
            _pingBw.DoWork += new DoWorkEventHandler(ping_bw_DoWork);
            _pingBw.ProgressChanged += new ProgressChangedEventHandler(ping_bw_ProgressChanged);
            _pingBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ping_bw_RunWorkerCompleted);
            //Config background worker for email
            _emailBw.WorkerReportsProgress = true;
            _emailBw.WorkerSupportsCancellation = false;
            _emailBw.DoWork += new DoWorkEventHandler(email_bw_DoWork);
            _emailBw.ProgressChanged += new ProgressChangedEventHandler(email_bw_ProgressChanged);
            _emailBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(email_bw_RunWorkerCompleted);
            }

        //Minimize to system tray instead of taskbar
        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                Hide();
            }
        }

        //Click system tary icon to maximize
        private void notifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        //Pings the target ip and returns the reply
        private PingReply PingIp(string ip)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);
            options.DontFragment = true;
            const string data = "a";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            const int timeout = 10;
            try
            {
                PingReply reply = pingSender.Send(ip, timeout, buffer, options);
                return reply;
            }
            catch (PingException e)
            {
                Invoke(new Action(() => { label7.Text = "Ping error: " + e; }));
                _pingBw.CancelAsync();
                throw;
            }
        }

        private void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            // If an error occurred, display the exception to the user. 
            if (e.Error != null)
            {
                label6.Text = "Ping failed";
            }
        }

        //Calls pingIP() and will check the returnd ping's Status and set a boolean value for each device
        private void Pinger()
        {
            PingReply ping1;
            PingReply ping2;
            PingReply ping3;
            PingReply ping4;
            PingReply ping5;
            if (!_vpn)
            {
                ping1 = PingIp(Thanatos);
                ping2 = PingIp(Router);
                ping3 = PingIp(Elyptor);
                ping4 = PingIp(Google);
                ping5 = PingIp(Helios);
            }
            else
            {
                ping1 = PingIp(Thanatos_VPN);
                ping2 = PingIp(Router);
                ping3 = PingIp(Elyptor_VPN);
                ping4 = PingIp(Google);
                ping5 = PingIp(Helios_VPN);
            }
            if (ping1.Status == IPStatus.Success)
            {
                _nas = true;
            }
            else
            {
                _nas = false;
            }
           
            if (ping2.Status == IPStatus.Success)
            {
                _router = true;
            }
            else
            {
                _router = false;
            }
        
            if (ping3.Status == IPStatus.Success)
            {
                _server = true;
            }
            else
            {
                _server = false;
            }
            
            if (ping4.Status == IPStatus.Success)
            {
                _internet = true;
            }
            else
            {
                _internet = false;
            }
           
            if (ping5.Status == IPStatus.Success)
            {
                _main = true;
            }
            else
            {
                _main = false;
            }
        }


        //Start the ping background worker
        private void button1_Click(object sender, EventArgs e)
        {
            if (_pingBw.IsBusy != true && _running == false)
            {
                _router = false;
                _nas = false;
                label6.Text = "Started";
                _running = true;
                //button1.Enabled = false;
                //button2.Enabled = true;
                _pingBw.RunWorkerAsync();
                button1.Text = "Stop";
                return;
            }

            if (_pingBw.WorkerSupportsCancellation && _running)
            {
                //button1.Enabled = true;
                //button2.Enabled = false;
                _mailSent = false;
                _pings = 0;
                _pingBw.CancelAsync();
                button1.Enabled = false;
                label6.Text = "Stopping";
                
                while (_pingBw.CancellationPending == false)
                {
                   if (_pingBw.CancellationPending)
                        break;
                }
                button1.Text = "Start";
                label6.Text = "Stopped";
                _running = false;
                button1.Enabled = true;
                

            }
        }


        //Call Pinger(), report the progress to update the labels then sleep
        private void ping_bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (true)
            {
                if (worker.CancellationPending )
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    Pinger();

                    worker.ReportProgress(1);

                    //System.Threading.Thread.Sleep(60000);
                }
            }
        }

        //Ping worker done, not that it should ever finish
        private void ping_bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                label6.Text = "Stopped!";
            }

            else if (e.Error != null)
            {
                label6.Text = "Error: " + e.Error.Message;
            }

            else
            {
                label6.Text = "Done!";
            }
        }

        //Used to update the labels for the status
        private void ping_bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //If server is up and a email has been sent reset that an email has been sent,
            //else if server is down router is up and has been down for 2 pings an email will be sent
            if (_nas)
            {
                label3.Text = "Up";
                _downs = 0;
                if (_mailSent)
                {
                    _mailSent = false;
                    label11.Text = "";
                }
            }
            else
            {
                label3.Text = "Down";
                _downs++;
                if (_emailBw.IsBusy != true)
                {
                    if (_mailSent == false && _router && _downs == 2)
                    {
                        _emailBw.RunWorkerAsync();
                        label5.Text = "starting";
                    }
                }
            }

            if (_server)
            {
                label10.Text = "Up";
            }
            else
            {
                label10.Text = "Down";
            }

            if (_router)
            {
                label4.Text = "Up";
            }
            else
            {
                label4.Text = "Down";
                
            }

            if (_internet)
            {
                label5.Text = "Internet";
            }
            else
            {
                label5.Text = "No Internet";
            }
            if (_running)
                _pings++;
            label7.Text = _pings.ToString();
        }

        //Worker for Email
        private void email_bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            Email mail = new Email();
            if (_nas == false && _internet)
            {
                mail.SendEmail(Thanatos);
                _mailSent = true;
                worker.ReportProgress(1);
            }
            if (_server == false && _internet)
            {
                mail.SendEmail(Elyptor);
                _mailSent = true;
                worker.ReportProgress(1);
            }
        }

        private void email_bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                label11.Text = ("Error: " + e.Error.Message);
            }

            else if (_mailSent)
            {
                label11.Text = "Sent!";
            }
        }

        private void email_bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            label11.Text = "Sending";
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }

    public class Email
    {
        
        
        public void SendEmail(String name)
        {
            SmtpClient client = new SmtpClient
            {
                Port = Convert.ToInt32(SMTP_PORT),
                Host = SMTP_HOST,
                EnableSsl = true,
                Timeout = 10000,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential("areialace@gmail.com", "LOTRlotr123!@#")
            };
            MailMessage mm = new MailMessage("areialace@gmail.com", "areialace@gmail.com", name + " is down",
                name + " is down");
            mm.BodyEncoding = Encoding.UTF8;
            mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            client.Send(mm);
        }
    }
}