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
            var baseUri = txtBaseUri.Text?.Trim();
            if (string.IsNullOrEmpty(baseUri))
                throw new InvalidOperationException("Base URL is required.");
            if (_client == null)
            {
                _client = new SomiodClient(baseUri);
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
            AppendStatus("Creating application 'gate-remote'...");
            try
            {
                var client = GetClient();
                var (success, response) = await client.CreateApplicationBAsync().ConfigureAwait(false);
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
                btnCreateApp.Enabled = btnOpenGate.Enabled = btnCloseGate.Enabled = true;
            }
        }

        private async void BtnOpenGate_Click(object sender, EventArgs e)
        {
            btnCreateApp.Enabled = btnOpenGate.Enabled = btnCloseGate.Enabled = false;
            AppendStatus("Sending OPEN command...");
            try
            {
                var client = GetClient();
                var (success, response) = await client.OpenGateAsync().ConfigureAwait(false);
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
                btnCreateApp.Enabled = btnOpenGate.Enabled = btnCloseGate.Enabled = true;
            }
        }

        private async void BtnCloseGate_Click(object sender, EventArgs e)
        {
            btnCreateApp.Enabled = btnOpenGate.Enabled = btnCloseGate.Enabled = false;
            AppendStatus("Sending CLOSE command...");
            try
            {
                var client = GetClient();
                var (success, response) = await client.CloseGateAsync().ConfigureAwait(false);
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
                btnCreateApp.Enabled = btnOpenGate.Enabled = btnCloseGate.Enabled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _client?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
