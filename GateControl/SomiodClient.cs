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
    /// Uses the endpoints present in your project files (gate-control / gate-status).
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
        /// Create Application B as specified:
        /// POST -> /api/somiod
        /// { "res-type": "application", "resource-name": "gate-remote" }
        /// </summary>
        public async Task<(bool Success, string Response)> CreateApplicationBAsync()
        {
            var url = $"{_baseUrl}/api/somiod";
            var payload = new Dictionary<string, object>
            {
                ["res-type"] = "application",
                ["resource-name"] = "gate"
            };

            var (success, response) = await PostJsonAsync(url, payload).ConfigureAwait(false);

            // Treat HTTP 409 (already exists) as success for idempotence
            if (!success && response.StartsWith("HTTP 409"))
                return (true, "Application already exists (HTTP 409).");

            return (success, response);
        }

        /// <summary>
        /// Send OPEN command as JSON (string) inside the content property:
        /// POST -> /api/somiod/gate-control/gate-status
        /// content: serialized JSON string (e.g. "{\"command\":{\"action\":\"open\"}}")
        /// Uses a unique resource-name per content-instance to avoid HTTP 409 conflicts.
        /// </summary>
        public async Task<(bool Success, string Response)> OpenGateAsync()
        {
            var url = $"{_baseUrl}/api/somiod/gate/gate-status";

            var commandObj = new Dictionary<string, object>
            {
                ["command"] = new Dictionary<string, string> { ["action"] = "open" }
            };

            // serialize the inner command to a JSON string so 'content' is a string value
            var contentString = _serializer.Serialize(commandObj);

            // unique resource-name to avoid conflict on repeated posts
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
        /// Send CLOSE command as JSON (string) inside the content property:
        /// POST -> /api/somiod/gate-control/gate-status
        /// content: serialized JSON string (e.g. "{\"command\":{\"action\":\"close\"}}")
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
