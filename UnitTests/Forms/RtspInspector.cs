using Media.Common;
using Media.Rtsp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Media.UnitTests
{
    public partial class RtspInspector : Form
    {
        public RtspInspector()
        {
            InitializeComponent();
        }


        Thread ClientThreadProc;

        BindingSource RTPPacketBinding = new BindingSource();

        BindingSource RTCPPacketBinding = new BindingSource();

        internal Media.Rtsp.RtspClient Client;

        private void button1_Click(object sender, EventArgs e)
        {
            if (ClientThreadProc != null)
            {

                if (Client != null)
                {
                    Client.Dispose();

                    Client = null;
                }

                Media.Common.Extensions.Thread.ThreadExtensions.TryAbortAndFree(ref ClientThreadProc);

                ClientThreadProc = null;

                GC.WaitForPendingFinalizers();

                button1.Text = "Start";
            }
            else
            {

                if (comboBox1.SelectedIndex == 1)
                {
                    Client = new RtspClient(textBox1.Text, RtspClient.ClientProtocolType.Tcp); //comboBox1.SelectedItem);
                }
                else if (comboBox1.SelectedIndex == 2)
                {
                    Client = new RtspClient(textBox1.Text, RtspClient.ClientProtocolType.Udp); //comboBox1.SelectedItem);
                }
                else
                {
                    Client = new RtspClient(textBox1.Text); //comboBox1.SelectedItem);
                }

                if (!string.IsNullOrWhiteSpace(textBox3.Text))
                {                 
                    Client.Credential = new System.Net.NetworkCredential(textBox3.Text, textBox4.Text);
                    if (comboBox2.SelectedIndex == 1) Client.AuthenticationScheme = System.Net.AuthenticationSchemes.Digest;
                }

                Client.DisableKeepAliveRequest = checkBox1.Checked;

                Client.OnConnect += client_OnConnect;

                ClientThreadProc = new Thread(() => Client.Connect());

                ClientThreadProc.Start();

                button1.Text = "Stop";                

                // Initialize the DataGridView.
                dataGridView2.AutoGenerateColumns = true;
                dataGridView2.AutoSize = true;
                dataGridView2.DataSource = RTPPacketBinding;

                dataGridView1.AutoGenerateColumns = true;
                dataGridView1.AutoSize = true;
                dataGridView1.DataSource = RTCPPacketBinding;
            }    

        }

        internal void client_OnConnect(RtspClient sender, object args)
        {
            sender.OnPlay += sender_OnPlay;

            sender.OnRequest += sender_OnRequest;

            sender.OnResponse += sender_OnResponse;

            try
            {
                if (numericUpDown1.Value == 0) sender.StartPlaying();
                else sender.StartPlaying(TimeSpan.FromSeconds((double)numericUpDown1.Value));
            }
            catch
            {
                return;
            }
        }


        void sender_OnRequest(RtspClient sender, RtspMessage request)
        {

            if (request != null)
            {

                //Disable keep alives if indicated
                Client.DisableKeepAliveRequest = checkBox1.Checked;

                if (this.InvokeRequired)
                {
                    Invoke(new FillTextRtsp(UpdateRtsp), request);
                }
                else
                {
                    textBox2.AppendText(string.Format(Media.UnitTests.Program.TestingFormat, request.ToString()));
                }
            }
        }

        void sender_OnResponse(RtspClient sender, RtspMessage request, RtspMessage response)
        {

            if (request != null && response != null)
            {

                //Disable keep alives if indicated
                Client.DisableKeepAliveRequest = checkBox1.Checked;

                if (this.InvokeRequired)
                {
                    Invoke(new FillTextRtsp(UpdateRtsp), request);

                    Invoke(new FillTextRtsp(UpdateRtsp), response);
                }
                else
                {
                    textBox2.AppendText(string.Format(Media.UnitTests.Program.TestingFormat, request.ToString(), response.ToString()));
                }
            }
        }

        internal void sender_init()
        {
            button1.Text = "Stop";

            // Initialize the DataGridView.
            dataGridView2.AutoGenerateColumns = true;
            dataGridView2.AutoSize = true;
            dataGridView2.DataSource = RTPPacketBinding;

            dataGridView1.AutoGenerateColumns = true;
            dataGridView1.AutoSize = true;
            dataGridView1.DataSource = RTCPPacketBinding;

            Client.OnRequest += sender_OnRequest;

            Client.OnResponse += sender_OnResponse;
        }

        internal void sender_OnPlay(RtspClient sender, object args)
        {
            sender.OnDisconnect += sender_OnDisconnect;
            sender.Client.RtpPacketReceieved += ShowRtpPacket;
            sender.Client.RtcpPacketReceieved += ShowRtcpPacket;

            sender.Client.RtcpPacketSent += ShowRtcpPacket;

            sender.Client.AverageMaximumRtcpBandwidthPercentage = (double)numericUpDown2.Value;

            sender.OnStop += sender_OnStop;            
        }

        void sender_OnStop(RtspClient sender, object args)
        {
            
        }

        void ShowRtcpPacket(object sender, Media.Rtcp.RtcpPacket packet, Media.Rtp.RtpClient.TransportContext tc = null)
        {
            try
            {
                if (this.InvokeRequired) Invoke(new FillGridRtcp(AddRtcp), packet.Clone(true, true, false));
                else RTCPPacketBinding.Add(packet.Clone(true, true, false));
            }
            catch { }
        }

        delegate void FillGridRtp(Media.Rtp.RtpPacket packet);

        delegate void FillGridRtcp(Media.Rtcp.RtcpPacket packet);

        delegate void FillTextRtsp(Media.Rtsp.RtspMessage packet);

        public void AddRtcp(Media.Rtcp.RtcpPacket p)
        {
            if (Client == null) return;
            try
            {
                RTCPPacketBinding.Add(p);
                if (!Client.LivePlay) label2.Text = "Remaining: " + (DateTime.UtcNow - Client.StartedPlaying.Value).Subtract(Client.EndTime.Value).ToString();
                DrawBandwidth(Color.Red);
            }            
            catch{}
            
        }
        public void AddRtp(Media.Rtp.RtpPacket p)
        {
            if (Client == null) return;
            try
            {
                RTPPacketBinding.Add(p);
                if (!Client.LivePlay) label2.Text = "Remaining: " + (DateTime.UtcNow - Client.StartedPlaying.Value).Subtract(Client.EndTime.Value).ToString();
                DrawBandwidth(Color.Blue);
            }
            catch { }
        }

        Bitmap BandwidthBitmap = new Bitmap(798, 50);

        DateTime LastDrawTime = DateTime.UtcNow;

        public void DrawBandwidth(Color? c = null)
        {
            try
            {
                if (Client == null || !Client.IsConnected) return;

                var x = DateTime.UtcNow - LastDrawTime;

                Color C;

                if (c == null)
                {
                    Random randomGen = new Random();
                    KnownColor[] names = (KnownColor[])Enum.GetValues(typeof(KnownColor));
                    KnownColor randomColorName = names[randomGen.Next(names.Length)];
                    Color randomColor = Color.FromKnownColor(randomColorName);
                    C = randomColor;
                }
                else
                {
                    C = c.Value;
                }

                BandwidthBitmap.SetPixel(Math.Max(0, Math.Min(BandwidthBitmap.Width - 1, (int)x.TotalSeconds - 1)), Math.Max(0, Math.Min(BandwidthBitmap.Height - 1, (int)x.TotalSeconds - 1)), C);
                BandwidthBitmap.SetPixel(Math.Max(0, Math.Min(BandwidthBitmap.Width - 1, (int)x.TotalMilliseconds - 1)), Math.Max(0, Math.Min(BandwidthBitmap.Height - 1, (int)x.TotalMilliseconds - 1)), C);

                pictureBox1.Image = BandwidthBitmap;

                pictureBox1.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);

                pictureBox1.Refresh();
                label4.Text = LastDrawTime.ToString() + " (Delay =) " + x.TotalMilliseconds.ToString() + " @ " + Client.Client.TotalBytesReceieved / Client.Client.Uptime.TotalSeconds + " /Sec";

                LastDrawTime = DateTime.UtcNow;
            }
            catch
            {

            }
        }

        public void UpdateRtsp(Media.Rtsp.RtspMessage p)
        {

            if (Client == null || Client.IsDisposed) return;

            textBox2.AppendText("@" + p.Created.ToUniversalTime().ToString() + " - " + p.ToString() + Environment.NewLine);


            if (Client.IsPlaying)
            {
                button1.Text = "(Playing)STOP";

                if (!Client.LivePlay) label2.Text = "Remaining: " + (DateTime.UtcNow - Client.StartedPlaying.Value).Subtract(Client.EndTime.Value).ToString();
            }
            else
            {
                button1.Text = "STOP";
                if (Client.LivePlay) label2.Text = "Live Play";
                else label2.Text = "Done Playing";
            }
        }
    

        void ShowRtpPacket(object sender, Media.Rtp.RtpPacket packet, Media.Rtp.RtpClient.TransportContext tc = null)
        {
            try
            {
                if (this.InvokeRequired) Invoke(new FillGridRtp(AddRtp), packet.Clone(true, true, true, true, true));
                else RTPPacketBinding.Add((IPacket)packet.Clone(true, true, true, true, true));
            }
            catch { }
        }

        void sender_OnDisconnect(RtspClient sender, object args)
        {

            if (sender == null || sender.IsDisposed) return;

            sender.Client.RtpPacketReceieved -= ShowRtpPacket;
            sender.Client.RtcpPacketReceieved -= ShowRtcpPacket;
            
        
            sender.OnPlay -= sender_OnPlay;

            sender.OnResponse -= sender_OnResponse;
            sender.OnRequest -= sender_OnRequest;

            sender.OnDisconnect -= sender_OnDisconnect;
            sender.Client.RtpPacketReceieved -= ShowRtpPacket;
            sender.Client.RtcpPacketReceieved -= ShowRtcpPacket;

            sender.Client.RtcpPacketSent -= ShowRtcpPacket;

            sender.OnStop -= sender_OnStop;            


            sender.Dispose();
            button1_Click(this, EventArgs.Empty);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            BandwidthBitmap = new Bitmap(BandwidthBitmap.Width, BandwidthBitmap.Height);
            RTPPacketBinding.Clear();
            RTCPPacketBinding.Clear();
            textBox2.Clear();
            label2.Text = label4.Text = string.Empty;
            pictureBox1.Image = BandwidthBitmap;
            pictureBox1.Refresh();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkBox1.Checked = comboBox1.SelectedIndex == 1;
        }
        
        //Dispose should Dispose Bitmap...

        private void RtspInspector_Load(object sender, EventArgs e)
        {

        }

    }
}
