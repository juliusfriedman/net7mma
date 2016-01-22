using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Media.UnitTests
{

    public partial class ServerTester : Form
    {

        public class FormLogger : Media.Rtsp.Server.RtspServerLogger
        {
            public TextBox textBox;

            internal override void LogRequest(Rtsp.RtspMessage request, Rtsp.ClientSession session)
            {
                textBox.AppendText("Request:" + request.ToString() + Environment.NewLine + " from=" + session.RemoteEndPoint.ToString() + Environment.NewLine);
            }

            internal override void LogResponse(Rtsp.RtspMessage response, Rtsp.ClientSession session)
            {
                textBox.AppendText("Response:" + response.ToString() + Environment.NewLine + " from=" + session.RemoteEndPoint.ToString() + Environment.NewLine);
            }

            public override void Log(string message)
            {
                textBox.AppendText(message + Environment.NewLine);
            }

            public override void LogException(Exception exception)
            {
                textBox.AppendText("----Exception:----" + exception.Message + Environment.NewLine);
            }
        }

        public ServerTester()
        {
            Form.CheckForIllegalCrossThreadCalls = false;

            InitializeComponent();
        }

        public Media.Rtsp.RtspServer RtspServer;

        private void ServerTester_Load(object sender, EventArgs e)
        {

        }

        //Handle starting or stopping the server
        private void button1_Click(object sender, EventArgs e)
        {

            if (RtspServer != null && RtspServer.IsRunning)
            {
                RtspServer.Stop();

                RtspServer.Dispose();

                RtspServer = null;

                listBox1.BeginUpdate();

                listBox1.Items.Clear();

                listBox1.EndUpdate();

                button1.Text = "Start Server";
            }
            else
            {
                //Get listen EndPoint from text box 2 or use a default

                System.Net.IPAddress serverIp;

                if (string.IsNullOrWhiteSpace(textBox2.Text))
                {
                    serverIp = Media.Common.Extensions.Socket.SocketExtensions.GetFirstV4IPAddress();

                    textBox2.Text = serverIp.ToString();
                }
                else
                {
                    serverIp = System.Net.IPAddress.Parse(textBox2.Text);
                }

                var serverPort = 554;

                //IPEndPoint.Parse...

                RtspServer = new Rtsp.RtspServer(serverIp, serverPort);

                var logger = new FormLogger();

                logger.textBox = textBox1;

                RtspServer.Logger = logger;

                RtspServer.Start();

                button1.Text = "Stop Server";
            }
        }


        class ListBoxItem
        {
            public readonly Media.Rtsp.Server.IMedia Media;

            public ListBoxItem(Media.Rtsp.Server.IMedia media)
            {
                if (media == null) throw new ArgumentNullException();

                Media = media;
            }

            public override string ToString()
            {
                return string.Join(",", Media.Name, Media.State, Media.Ready);
            }

        }

        //Refresh the Streams listbox
        private void button8_Click(object sender, EventArgs e)
        {
            listBox1.BeginUpdate();

            listBox1.Items.Clear();

            if(RtspServer != null) foreach (var media in RtspServer.MediaStreams)
            {
                listBox1.Items.Add(new ListBoxItem(media));
            }

            listBox1.EndUpdate();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (RtspServer == null) return;

            Media.Rtsp.RtspClient.ClientProtocolType protcol;

            //Determine the protocol
            switch (comboBox1.SelectedText)
            {
                default://TCP is default
                case "TCP":
                    protcol = Rtsp.RtspClient.ClientProtocolType.Tcp;
                    break;
                case "UDP":
                    protcol = Rtsp.RtspClient.ClientProtocolType.Udp;
                    break;
                case "HTTP":
                    protcol = Rtsp.RtspClient.ClientProtocolType.Http;
                    break;
            }

            //If the media was added
            if (RtspServer.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource(textBox3.Text, textBox4.Text, protcol)))
            {
                textBox3.Text = textBox4.Text = string.Empty;

                //Refresh Streams
                button8_Click(sender, e);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (RtspServer == null) return;

            //Use the selected Text of the listbox1 to get the name / id and stop the stream

            int index = listBox1.SelectedIndex;

            if (index < 0) return;

            ListBoxItem selected = listBox1.Items[index] as ListBoxItem;

            if (selected == null) return;

            selected.Media.Stop();

            //Refresh Streams
            button8_Click(sender, e);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (RtspServer == null) return;

            //Use the selected Text of the listbox1 to get the name / id and remove the stream

            int index = listBox1.SelectedIndex;

            if (index < 0) return;

            ListBoxItem selected = listBox1.Items[index] as ListBoxItem;

            if (selected == null) return;

            if (RtspServer.TryRemoveMedia(selected.Media.Id, true))
            {
                //Refresh Streams
                button8_Click(sender, e);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (RtspServer == null) return;

            //Create a Child of the stream...

            int index = listBox1.SelectedIndex;

            if (index < 0) return;

            ListBoxItem selected = listBox1.Items[index] as ListBoxItem;

            if (selected == null) return;

            //Selected has IMedia, create child depending on type

            if (selected.Media is Media.Rtsp.Server.MediaTypes.RtspSource)
            {
                //needs to actually create the appropriate child type...
                var child = new Media.Rtsp.Server.ChildMedia(selected.Media as Media.Rtsp.Server.MediaTypes.RtspSource);

                //Take the name from the Textbox 3 or add child plus the orig name
                child.Name = false == string.IsNullOrWhiteSpace(textBox3.Text) && textBox3.Text != selected.Media.Name ? textBox3.Text : "Child" + selected.Media.Name;

                //If the media was added
                if (RtspServer.TryAddMedia(child))
                {
                    //Refresh Streams
                    button8_Click(sender, e);
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
           
            int index = listBox1.SelectedIndex;

            if (index < 0) return;

            ListBoxItem selected = listBox1.Items[index] as ListBoxItem;

            if (selected == null) return;

            if (selected.Media is Media.Rtsp.Server.MediaTypes.RtspSource)
            {
                var ins = new UnitTests.RtspInspector();

                ins.Show();// Dialog();

                var rtspClient = selected.Media as Media.Rtsp.Server.MediaTypes.RtspSource;

                ins.Client = rtspClient.RtspClient;

                ins.sender_init();

                ins.sender_OnPlay(ins.Client, e);
            }
            
        }
    }
}
