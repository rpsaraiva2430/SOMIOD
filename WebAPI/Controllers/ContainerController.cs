using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Http;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    public class ContainerController : ApiController
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SomiodDatabase"].ConnectionString;

        // GET CONTAINER (Discovery e Dados)
        [HttpGet, Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult Get(string appName, string containerName)
        {
            // 1. DISCOVERY
            if (Request.Headers.Contains("somiod-discovery"))
            {
                var resType = Request.Headers.GetValues("somiod-discovery").FirstOrDefault();

                if (resType == "content-instance")
                    return Ok(GetContentInstancesForContainer(appName, containerName));

                if (resType == "subscription")
                    return Ok(GetSubscriptionsForContainer(appName, containerName));
            }

            // 2. GET NORMAL
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name, creation_datetime FROM Container WHERE resource_name = @Name AND parent_app_name = @ParentApp";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", containerName);
                    cmd.Parameters.AddWithValue("@ParentApp", appName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new Container
                            {
                                ResourceName = reader["resource_name"].ToString(),
                                CreationDatetime = reader["creation_datetime"].ToString(),
                                ParentAppName = appName
                            });
                        }
                    }
                }
            }
            return NotFound();
        }

        // POST CONTENT-INSTANCE (Cria dados e envia Notificações)
        [HttpPost, Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult PostContentInstance(string appName, string containerName, [FromBody] ContentInstance model)
        {
            if (model == null) return BadRequest("Data required.");

            // Gerar nome se não existir
            if (string.IsNullOrWhiteSpace(model.ResourceName))
                model.ResourceName = $"ci-{Guid.NewGuid().ToString().Substring(0, 8)}";

            model.ParentAppName = appName;
            model.ParentContainerName = containerName;
            model.CreationDatetime = DateTime.UtcNow;

            // 1. Guardar na Base de Dados
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO ContentInstance (resource_name, creation_datetime, content, content_type, parent_container_name, parent_app_name) VALUES (@Name, @Date, @Content, @Type, @PCont, @PApp)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", model.ResourceName);
                    cmd.Parameters.AddWithValue("@Date", model.CreationDatetime.ToString("yyyy-MM-ddTHH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Content", model.Content ?? "");
                    cmd.Parameters.AddWithValue("@Type", model.ContentType ?? "application/json");
                    cmd.Parameters.AddWithValue("@PCont", model.ParentContainerName);
                    cmd.Parameters.AddWithValue("@PApp", model.ParentAppName);

                    try { cmd.ExecuteNonQuery(); }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 547) return NotFound(); // App ou Container pai não existem
                        if (ex.Number == 2627) return Conflict();
                        throw;
                    }
                }
            }

            // 2. MOTOR DE NOTIFICAÇÕES: Dispara evento de Criação (evt: 1)
            DispatchNotifications(appName, containerName, model, 1);

            return Created($"/api/somiod/{appName}/{containerName}/{model.ResourceName}", model);
        }

        // DELETE CONTAINER
        [HttpDelete, Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult Delete(string appName, string containerName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Container WHERE resource_name = @Name AND parent_app_name = @ParentApp";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", containerName);
                    cmd.Parameters.AddWithValue("@ParentApp", appName);
                    if (cmd.ExecuteNonQuery() == 0) return NotFound();
                }
            }
            return StatusCode(HttpStatusCode.NoContent);
        }

        // UPDATE CONTAINER (PUT)
        [HttpPut, Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult Put(string appName, string containerName, [FromBody] Container model)
        {
            if (model == null) return BadRequest("Data required.");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Container SET resource_name = @NewName WHERE resource_name = @Name AND parent_app_name = @ParentApp";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NewName", model.ResourceName ?? containerName);
                    cmd.Parameters.AddWithValue("@Name", containerName);
                    cmd.Parameters.AddWithValue("@ParentApp", appName);

                    try
                    {
                        if (cmd.ExecuteNonQuery() == 0) return NotFound();
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 2627) return Conflict();
                        if (ex.Number == 547) return BadRequest("Cannot rename container due to existing dependencies.");
                        throw;
                    }
                }
            }
            return Ok(model);
        }

        // ---------------- MÉTODOS AUXILIARES (Discovery) ----------------

        private IEnumerable<string> GetContentInstancesForContainer(string appName, string containerName)
        {
            List<string> paths = new List<string>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name FROM ContentInstance WHERE parent_container_name = @ContName AND parent_app_name = @AppName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ContName", containerName);
                    cmd.Parameters.AddWithValue("@AppName", appName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            paths.Add($"/api/somiod/{appName}/{containerName}/{reader["resource_name"]}");
                    }
                }
            }
            return paths;
        }

        private IEnumerable<string> GetSubscriptionsForContainer(string appName, string containerName)
        {
            List<string> paths = new List<string>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name FROM Subscription WHERE parent_container_name = @ContName AND parent_app_name = @AppName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ContName", containerName);
                    cmd.Parameters.AddWithValue("@AppName", appName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            paths.Add($"/api/somiod/{appName}/{containerName}/subs/{reader["resource_name"]}");
                    }
                }
            }
            return paths;
        }

        // ---------------- MOTOR DE NOTIFICAÇÕES (Lógica MQTT e HTTP) ----------------

        private void DispatchNotifications(string appName, string containerName, ContentInstance data, int eventType)
        {
            List<Subscription> subs = GetMatchingSubscriptions(appName, containerName, eventType);

            if (subs.Count == 0) return;

            string messageXML = $@"<notification>
<event>{(eventType == 1 ? "creation" : "deletion")}</event>
<resource>{data.ResourceName}</resource>
<content>{data.Content}</content>
<original-path>/api/somiod/{appName}/{containerName}/{data.ResourceName}</original-path>
</notification>";

            foreach (var sub in subs)
            {
                if (sub.Endpoint.ToLower().StartsWith("mqtt://"))
                {
                    SendMqttNotification(sub.Endpoint, appName, containerName, messageXML);
                }
                else if (sub.Endpoint.ToLower().StartsWith("http://") || sub.Endpoint.ToLower().StartsWith("https://"))
                {
                    SendHttpNotification(sub.Endpoint, messageXML);
                }
            }
        }

        private List<Subscription> GetMatchingSubscriptions(string appName, string containerName, int eventType)
        {
            List<Subscription> subs = new List<Subscription>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT endpoint FROM Subscription WHERE parent_app_name = @AppName AND parent_container_name = @ContName AND evt = @Evt";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AppName", appName);
                    cmd.Parameters.AddWithValue("@ContName", containerName);
                    cmd.Parameters.AddWithValue("@Evt", eventType);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            subs.Add(new Subscription { Endpoint = reader["endpoint"].ToString() });
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine($"[Quantidade de subs]: {subs.Count}");
            return subs;
        }

        private void SendMqttNotification(string endpoint, string appName, string containerName, string message)
        {
            try
            {
                // Debug: Escreve na janela de Output do Visual Studio
                System.Diagnostics.Debug.WriteLine($"[MQTT] A tentar ligar a: {endpoint}");

                Uri uri = new Uri(endpoint);
                // Tenta forçar o IP se for localhost
                string brokerIp = (uri.Host == "localhost") ? "127.0.0.1" : uri.Host;

                MqttClient client = new MqttClient(brokerIp);
                string clientId = Guid.NewGuid().ToString();

                client.Connect(clientId);

                if (client.IsConnected)
                {
                    string channel = $"api/somiod/{appName}/{containerName}";
                    System.Diagnostics.Debug.WriteLine($"[MQTT] A enviar para o canal: {channel}");

                    client.Publish(channel, Encoding.UTF8.GetBytes(message));
                    System.Threading.Thread.Sleep(200); // 200ms de pausa
                    client.Disconnect();
                    System.Diagnostics.Debug.WriteLine("[MQTT] Enviado com sucesso!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MQTT] Falha: Não conseguiu conectar.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MQTT ERRO CRÍTICO]: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MQTT STACK]: {ex.StackTrace}");
            }
        }

        private void SendHttpNotification(string url, string message)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/xml";
                    client.UploadString(url, message);
                }
            }
            catch (Exception) { }
        }
    }
}