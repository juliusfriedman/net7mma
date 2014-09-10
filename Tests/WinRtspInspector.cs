using Media.Common;
using Media.Rtsp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tests
{
    public partial class WinRtspInspector : Form
    {
        public WinRtspInspector()
        {
            InitializeComponent();
        }


        Thread ClientThreadProc;

        BindingSource RTPPacketBinding = new BindingSource();

        BindingSource RTCPPacketBinding = new BindingSource();

        Media.Rtsp.RtspClient Client;

        private void button1_Click(object sender, EventArgs e)
        {
            if (ClientThreadProc != null)
            {

                Client.Dispose();
                
                Client = null;

                Media.Utility.Abort(ref ClientThreadProc);

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

                Client.OnConnect += client_OnConnect;

                ClientThreadProc = new Thread(Client.Connect);

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

        void client_OnConnect(RtspClient sender, object args)
        {
            sender.OnPlay += sender_OnPlay;

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

        void sender_OnResponse(RtspClient sender, RtspMessage request, RtspMessage response)
        {

            if (request != null && response != null)
            {

                if (checkBox1.Checked) Client.KeepAliveTimeout = TimeSpan.Zero;

                if (this.InvokeRequired)
                {
                    Invoke(new FillTextRtsp(UpdateRtsp), request);

                    Invoke(new FillTextRtsp(UpdateRtsp), response);
                }
                else
                {
                    textBox2.AppendText(string.Format(Program.TestingFormat, request.ToString(), response.ToString()));
                }
            }
        }

        void sender_OnPlay(RtspClient sender, object args)
        {
            sender.OnDisconnect += sender_OnDisconnect;
            sender.m_RtpClient.RtpPacketReceieved += ShowRtpPacket;
            sender.m_RtpClient.RtcpPacketReceieved += ShowRtcpPacket;

            sender.m_RtpClient.RtcpPacketSent += ShowRtcpPacket;      

            sender.m_RtpClient.MaximumRtcpBandwidthPercentage = (double)numericUpDown2.Value;

            sender.OnStop += sender_OnStop;            
        }

        void sender_OnStop(RtspClient sender, object args)
        {
            
        }

        void ShowRtcpPacket(object sender, Media.Rtcp.RtcpPacket packet)
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
                if (!Client.LivePlay) label2.Text = "Remaining: " + (DateTime.UtcNow - Client.StartedListening.Value).Subtract(Client.EndTime.Value).ToString();
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
                if (!Client.LivePlay) label2.Text = "Remaining: " + (DateTime.UtcNow - Client.StartedListening.Value).Subtract(Client.EndTime.Value).ToString();
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
                if (Client == null || !Client.Connected) return;

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
                label4.Text = LastDrawTime.ToString() + " (Delay =) " + x.TotalMilliseconds.ToString() + " @ " + Client.m_RtpClient.TotalBytesReceieved / Client.m_RtpClient.Uptime.TotalSeconds + " /Sec";

                LastDrawTime = DateTime.UtcNow;
            }
            catch
            {

            }
        }

        public void UpdateRtsp(Media.Rtsp.RtspMessage p)
        {

            if (Client == null || Client.Disposed) return;

            textBox2.AppendText("@" + p.Created.ToUniversalTime().ToString() + " - " + p.ToString() + Environment.NewLine);


            if (Client.Playing)
            {
                button1.Text = "(Playing)STOP";

                if (!Client.LivePlay) label2.Text = "Remaining: " + (DateTime.UtcNow - Client.StartedListening.Value).Subtract(Client.EndTime.Value).ToString();
            }
            else
            {
                button1.Text = "STOP";
                if (Client.LivePlay) label2.Text = "Live Play";
                else label2.Text = "Done Playing";
            }
        }
    

        void ShowRtpPacket(object sender, Media.Rtp.RtpPacket packet)
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

            if (sender == null || sender.Disposed) return;

            sender.m_RtpClient.RtpPacketReceieved -= ShowRtpPacket;
            sender.m_RtpClient.RtcpPacketReceieved -= ShowRtcpPacket;
            
        
            sender.OnPlay -= sender_OnPlay;

            sender.OnResponse -= sender_OnResponse;

            sender.OnDisconnect -= sender_OnDisconnect;
            sender.m_RtpClient.RtpPacketReceieved -= ShowRtpPacket;
            sender.m_RtpClient.RtcpPacketReceieved -= ShowRtcpPacket;

            sender.m_RtpClient.RtcpPacketSent -= ShowRtcpPacket;

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

    }
}
