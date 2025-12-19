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
            AppendStatus("Creating application 'gate' (and container 'gate-status')...");
            try
            {
                var client = GetClient();
                // Keep UI context
                var (success, response) = await client.CreateApplicationBAsync();

                // Always append the raw response for debugging
                AppendStatus("CreateApplicationB: raw response -> " + (response ?? "<null>"));

                if (success)
                {
                    AppendStatus("CreateApplicationB: reported success.");
                }
                else
                {
                    AppendStatus("CreateApplicationB: reported failure -> " + response);
                }

                // Additional debug: try to detect application/container parts in server message
                if (!string.IsNullOrEmpty(response))
                {
                    if (response.IndexOf("Application", StringComparison.OrdinalIgnoreCase) >= 0)
                        AppendStatus("Debug: server message contains 'Application'.");

                    if (response.IndexOf("Container", StringComparison.OrdinalIgnoreCase) >= 0)
                        AppendStatus("Debug: server message contains 'Container'.");

                    if (response.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        response.IndexOf("HTTP 409", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        AppendStatus("Debug: resource already existed (idempotent create).");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendStatus("CreateApplicationB: exception -> " + ex.Message);
            }
            finally
            {
                // Keep app create button disabled after successful create attempt so user doesn't re-create repeatedly.
                // Re-enable control buttons so user can send commands.
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
