using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateStatus.Services;

namespace GateStatus
{
    public partial class Form1 : Form
    {
        private SomiodClient somiodClient;
        private HttpListener listener;
        private bool isListening = false;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            string somiodUrl = "http://localhost:57880";
            somiodClient = new SomiodClient(somiodUrl, Log);

            Log("Starting SOMIOD configuration...");

            try
            {
                // Creating resources (Resources might already exist, resulting in 'Conflict' logs)
                await somiodClient.CreateApplicationAsync();
                await somiodClient.CreateContainerAsync();
                await somiodClient.CreateSubscriptionAsync();

                StartNotificationServer();
            }
            catch (Exception ex)
            {
                Log("Setup Error: " + ex.Message);
            }
        }

        private void StartNotificationServer()
        {
            try
            {
                listener = new HttpListener();
                // Ensure this matches the endpoint in your subscription
                listener.Prefixes.Add("http://localhost:8080/receive/");
                listener.Start();
                isListening = true;
                Log("Notification server active on port 8080");
                Task.Run(() => Listen());
            }
            catch (Exception ex)
            {
                Log("Server Error: " + ex.Message);
            }
        }

        private async Task Listen()
        {
            while (isListening)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    {
                        string xml = await reader.ReadToEndAsync();
                        string msg = ParseXml(xml);
                        Log("COMMAND RECEIVED: " + msg);
                    }
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                }
                catch { /* Handle listener abortion on form close */ }
            }
        }

        private string ParseXml(string xml)
        {
            if (xml.Contains("<content>"))
            {
                int s = xml.IndexOf("<content>") + 9;
                int e = xml.IndexOf("</content>");
                string content = xml.Substring(s, e - s);
                return WebUtility.HtmlDecode(content);
            }
            return "Empty Command";
        }

        private void Log(string m)
        {
            if (InvokeRequired) { Invoke(new Action<string>(Log), m); return; }
            listBoxLogs.Items.Add($"[{DateTime.Now:HH:mm:ss}] {m}");
            listBoxLogs.TopIndex = listBoxLogs.Items.Count - 1;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isListening = false;
            if (listener != null)
            {
                listener.Abort();
            }
        }
    }
}