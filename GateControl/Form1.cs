using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GateControl
{
    public partial class Form1 : Form
    {
        private SomiodClient _client;

        public Form1()
        {
            InitializeComponent();
        }

        private SomiodClient GetClient()
        {
            var baseUrl = txtBaseUrl.Text?.Trim();
            if (string.IsNullOrEmpty(baseUrl))
                throw new InvalidOperationException("Base URL is required.");
            if (_client == null)
            {
                _client = new SomiodClient(baseUrl);
            }
            return _client;
        }

        private void AppendStatus(string text)
        {
            if (txtStatus.InvokeRequired)
            {
                txtStatus.Invoke(new Action(() => AppendStatus(text)));
                return;
            }
            txtStatus.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        }

        private async void BtnCreateApp_Click(object sender, EventArgs e)
        {
            btnCreateApp.Enabled = btnOpenGate.Enabled = btnCloseGate.Enabled = false;
            AppendStatus("Creating application 'gate'...");
            try
            {
                var client = GetClient();
                // Do NOT use ConfigureAwait(false) here - keep the UI synchronization context
                var (success, response) = await client.CreateApplicationBAsync();
                if (success)
                    AppendStatus("CreateApplicationB: success.");
                else
                    AppendStatus("CreateApplicationB: failed -> " + response);
            }
            catch (Exception ex)
            {
                AppendStatus("CreateApplicationB: exception -> " + ex.Message);
            }
            finally
            {
                // This runs on the UI thread because we didn't use ConfigureAwait(false)
                btnOpenGate.Enabled = btnCloseGate.Enabled = true;
                btnCreateApp.Enabled = false;
            }
        }

        private async void BtnOpenGate_Click(object sender, EventArgs e)
        {
            btnCreateApp.Enabled = btnOpenGate.Enabled = btnCloseGate.Enabled = false;
            AppendStatus("Sending OPEN command...");
            try
            {
                var client = GetClient();
                // Keep context so UI updates in finally run on UI thread
                var (success, response) = await client.OpenGateAsync();
                if (success)
                    AppendStatus("OpenGate: success.");
                else
                    AppendStatus("OpenGate: failed -> " + response);
            }
            catch (Exception ex)
            {
                AppendStatus("OpenGate: exception -> " + ex.Message);
            }
            finally
            {
                btnCloseGate.Enabled = true;
                btnOpenGate.Enabled = false;
            }
        }

        private async void BtnCloseGate_Click(object sender, EventArgs e)
        {
            btnCreateApp.Enabled = btnOpenGate.Enabled = btnCloseGate.Enabled = false;
            AppendStatus("Sending CLOSE command...");
            try
            {
                var client = GetClient();
                // Keep context so UI updates in finally run on UI thread
                var (success, response) = await client.CloseGateAsync();
                if (success)
                    AppendStatus("CloseGate: success.");
                else
                    AppendStatus("CloseGate: failed -> " + response);
            }
            catch (Exception ex)
            {
                AppendStatus("CloseGate: exception -> " + ex.Message);
            }
            finally
            {
                btnOpenGate.Enabled = true;
                btnCloseGate.Enabled = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _client?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
