using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace GateControl
{
    /// <summary>
    /// SOMIOD client that sends only JSON in HTTP bodies.
    /// The 'content' property of content-instances is a JSON-formatted string value and
    /// requests use header Content-Type: application/json.
    /// Uses the endpoints present in your project files (gate / gate-status).
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
        /// Create Application B and its container (idempotent).
        /// POST -> /api/somiod                (application)
        /// POST -> /api/somiod/{application}  (container)
        /// Creates application "gate" and container "gate-status".
        /// Treats HTTP 409 (already exists) as success.
        /// </summary>
        public async Task<(bool Success, string Response)> CreateApplicationBAsync()
        {
            var appUrl = $"{_baseUrl}/api/somiod";
            var appPayload = new Dictionary<string, object>
            {
                ["res-type"] = "application",
                ["resource-name"] = "gate"
            };

            var (appSuccess, appResponse) = await PostJsonAsync(appUrl, appPayload).ConfigureAwait(false);

            // If application creation failed for a reason other than "already exists", return failure
            if (!appSuccess && !appResponse.StartsWith("HTTP 409"))
                return (false, $"Create application failed: {appResponse}");

            // If 409 or success, treat application as present — proceed to create container
            string appResult = appSuccess ? $"Application created: {appResponse}" : "Application already exists (HTTP 409).";

            // Create container under the application: POST -> /api/somiod/gate
            var containerUrl = $"{_baseUrl}/api/somiod/gate";
            var containerPayload = new Dictionary<string, object>
            {
                ["res-type"] = "container",
                ["resource-name"] = "gate-status"
            };

            var (containerSuccess, containerResponse) = await PostJsonAsync(containerUrl, containerPayload).ConfigureAwait(false);

            if (!containerSuccess && !containerResponse.StartsWith("HTTP 409"))
            {
                return (false, $"{appResult}; Create container failed: {containerResponse}");
            }

            string containerResult = containerSuccess ? $"Container created: {containerResponse}" : "Container already exists (HTTP 409).";

            return (true, $"{appResult}; {containerResult}");
        }

        /// <summary>
        /// Send OPEN command as JSON (string) inside the content property.
        /// POST -> /api/somiod/gate/gate-status
        /// Uses a unique resource-name per content-instance to avoid HTTP 409 conflicts.
        /// </summary>
        public async Task<(bool Success, string Response)> OpenGateAsync()
        {
            var url = $"{_baseUrl}/api/somiod/gate/gate-status";

            var commandObj = new Dictionary<string, object>
            {
                ["command"] = new Dictionary<string, string> { ["action"] = "open" }
            };

            var contentString = _serializer.Serialize(commandObj);

            var uniqueResourceName = "cmd-open-" + Guid.NewGuid().ToString("N");

            var payload = new Dictionary<string, object>
            {
                ["res-type"] = "content-instance",
                ["resource-name"] = uniqueResourceName,
                ["content-type"] = "application/json",
                ["content"] = contentString
            };

            return await PostJsonAsync(url, payload).ConfigureAwait(false);
        }

        /// <summary>
        /// Send CLOSE command as JSON (string) inside the content property.
        /// POST -> /api/somiod/gate/gate-status
        /// Uses a unique resource-name per content-instance to avoid HTTP 409 conflicts.
        /// </summary>
        public async Task<(bool Success, string Response)> CloseGateAsync()
        {
            var url = $"{_baseUrl}/api/somiod/gate/gate-status";

            var commandObj = new Dictionary<string, object>
            {
                ["command"] = new Dictionary<string, string> { ["action"] = "close" }
            };

            var contentString = _serializer.Serialize(commandObj);

            var uniqueResourceName = "cmd-close-" + Guid.NewGuid().ToString("N");

            var payload = new Dictionary<string, object>
            {
                ["res-type"] = "content-instance",
                ["resource-name"] = uniqueResourceName,
                ["content-type"] = "application/json",
                ["content"] = contentString
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
