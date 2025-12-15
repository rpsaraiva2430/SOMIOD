using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace GateControl
{
    /// <summary>
    /// SOMIOD client for creating application resource and posting JSON commands inside JSON bodies.
    /// All requests use JSON bodies and set Content-Type: application/json.
    /// The 'content' property of content-instances is now JSON (content-type = application/json).
    /// </summary>
    public class SomiodClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public SomiodClient(string baseUrl, HttpMessageHandler handler = null)
        {
            _baseUrl = (baseUrl ?? throw new ArgumentNullException(nameof(baseUrl))).TrimEnd('/');
            _http = handler == null ? new HttpClient() : new HttpClient(handler);
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Create Application B (resource-name = "gate-remote").
        /// Optionally create a container under the newly created application by passing containerName.
        /// POST -> /api/somiod         (for application)
        /// POST -> /api/somiod/{app}   (for container)
        /// </summary>
        public async Task<(bool Success, string Response)> CreateApplicationBAsync(string containerName = "commands")
        {
            var url = $"{_baseUrl}/api/somiod";
            // Create application payload
            var appPayload = new Dictionary<string, object>
            { 
                { "resource-name", "gate" }
            };

            var (appSuccess, appResponse) = await PostJsonAsync(url, appPayload).ConfigureAwait(false);
            if (!appSuccess)
            {
                // Return failure immediately if application creation failed
                return (false, $"Create application failed: {appResponse}");
            }

            // If caller requested no container creation, exit successfully
            if (string.IsNullOrWhiteSpace(containerName))
            {
                return (true, $"Application created: {appResponse} (no container created)");
            }

            // Create container under the application: POST -> /api/somiod/gate
            var containerUrl = $"{_baseUrl}/api/somiod/gate";
            var containerPayload = new Dictionary<string, object>
            {
                { "resource-name", "gate-status" }
            };

            var (containerSuccess, containerResponse) = await PostJsonAsync(containerUrl, containerPayload).ConfigureAwait(false);
            if (!containerSuccess)
            {
                return (false, $"Application created but container creation failed: {containerResponse}");
            }

            return (true, $"Application created: {appResponse}; Container created: {containerResponse}");
        }

        /// <summary>
        /// Post content-instance with JSON content to gate/gate-status to OPEN gate
        /// content-type set to application/json and content is a JSON object (nested).
        /// </summary>
        public async Task<(bool Success, string Response)> OpenGateAsync()
        {
            var url = $"{_baseUrl}/api/somiod/gate/gate-status";

            // The content field is now an object that will be serialized as JSON.
            var jsonContent = new Dictionary<string, object>
            {
                { "command", new Dictionary<string, string> { { "action", "open" } } }
            };

            var payload = new Dictionary<string, object>
            {
                ["resource-name"] = "cmd-open",
                ["content-type"] = "application/json",
                ["content"] = jsonContent
            };

            return await PostJsonAsync(url, payload).ConfigureAwait(false);
        }

        /// <summary>
        /// Post content-instance with JSON content to gate-control/gate-status to CLOSE gate
        /// content-type set to application/json and content is a JSON object (nested).
        /// </summary>
        public async Task<(bool Success, string Response)> CloseGateAsync()
        {
            var url = $"{_baseUrl}/api/somiod/gate-control/gate-status";

            var jsonContent = new Dictionary<string, object>
            {
                { "command", new Dictionary<string, string> { { "action", "close" } } }
            };

            var payload = new Dictionary<string, object>
            {
                ["resource-name"] = "cmd-close",
                ["content-type"] = "application/json",
                ["content"] = jsonContent
            };
            return await PostJsonAsync(url, payload).ConfigureAwait(false);
        }

        private async Task<(bool Success, string Response)> PostJsonAsync(string url, object payload)
        {
            var json = _serializer.Serialize(payload);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                try
                {
                    var resp = await _http.PostAsync(url, content).ConfigureAwait(false);
                    var respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode)
                        return (true, respBody);
                    return (false, $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}: {respBody}");
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}
