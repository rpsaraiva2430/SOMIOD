using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GateStatus.Services
{
    public class SomiodClient
    {
        private readonly HttpClient httpClient;
        private readonly string baseUrl;
        private readonly Action<string> log;

        public SomiodClient(string somiodBaseUrl, Action<string> logAction)
        {
            // Ensures base URL does not end with a slash to avoid double slashes in paths
            baseUrl = somiodBaseUrl.TrimEnd('/');
            log = logAction;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        // 1. Creates the Application named 'gate'
        public async Task CreateApplicationAsync()
        {
            string json = @"{
                ""res-type"": ""application"",
                ""resource-name"": ""gate""
            }";

            // Endpoint: /api/somiod
            await PostAsync($"{baseUrl}/api/somiod", json, "Application");
        }

        // 2. Creates the Container named 'gate-status' inside 'gate' application
        public async Task CreateContainerAsync()
        {
            string json = @"{
                ""res-type"": ""container"",
                ""resource-name"": ""gate-status""
            }";

            // Endpoint: /api/somiod/gate
            await PostAsync($"{baseUrl}/api/somiod/gate", json, "Container");
        }

        // 3. Creates the Subscription inside 'gate-status' container
        public async Task CreateSubscriptionAsync()
        {
            string json = @"{
                ""res-type"": ""subscription"",
                ""resource-name"": ""sub-door"",
                ""evt"": 1,
                ""endpoint"": ""http://localhost:8080/receive/""
            }";

            // Endpoint: /api/somiod/gate/gate-status/subs
            await PostAsync($"{baseUrl}/api/somiod/gate/gate-status/subs", json, "Subscription");
        }

        private async Task PostAsync(string url, string json, string name)
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                    log($"{name} created successfully.");
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    log($"{name} already exists (Conflict).");
                else
                    log($"{name} error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                log($"{name} connection error: {ex.Message}");
            }
        }
    }
}