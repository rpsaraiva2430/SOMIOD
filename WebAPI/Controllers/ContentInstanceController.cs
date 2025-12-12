using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Http;
using uPLibrary.Networking.M2Mqtt;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    public class ContentInstanceController : ApiController
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SomiodDatabase"].ConnectionString;

        // GET CONTENT INSTANCE
        [HttpGet, Route("api/somiod/{appName}/{containerName}/{contentInstanceName:regex(^(?!subs|subscription).*$)}")]
        public IHttpActionResult Get(string appName, string containerName, string contentInstanceName)
        {
            if (Request.Headers.Contains("somiod-discovery")) return Ok(new List<string>());

            var instance = GetContentInstanceModel(appName, containerName, contentInstanceName);
            if (instance == null) return NotFound();

            return Ok(instance);
        }

        // DELETE CONTENT INSTANCE (Com Notificação)
        [HttpDelete, Route("api/somiod/{appName}/{containerName}/{contentInstanceName:regex(^(?!subs|subscription).*$)}")]
        public IHttpActionResult Delete(string appName, string containerName, string contentInstanceName)
        {
            // 1. Obter dados antes de apagar (para a notificação)
            var instanceToDelete = GetContentInstanceModel(appName, containerName, contentInstanceName);

            if (instanceToDelete == null) return NotFound();

            // 2. Apagar da BD
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM ContentInstance WHERE resource_name = @Name AND parent_container_name = @ContainerName AND parent_app_name = @AppName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", contentInstanceName);
                    cmd.Parameters.AddWithValue("@ContainerName", containerName);
                    cmd.Parameters.AddWithValue("@AppName", appName);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected == 0) return NotFound();
                }
            }

            // 3. Disparar Notificação (evt: 2 = Deletion)
            DispatchNotifications(appName, containerName, instanceToDelete, 2);

            return StatusCode(HttpStatusCode.NoContent);
        }

        // ---------------- HELPERS ----------------

        private ContentInstance GetContentInstanceModel(string appName, string containerName, string name)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name, creation_datetime, content, content_type FROM ContentInstance WHERE resource_name = @Name AND parent_container_name = @ContainerName AND parent_app_name = @AppName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@ContainerName", containerName);
                    cmd.Parameters.AddWithValue("@AppName", appName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ContentInstance
                            {
                                ResourceName = reader["resource_name"].ToString(),
                                CreationDatetime = Convert.ToDateTime(reader["creation_datetime"]),
                                Content = reader["content"].ToString(),
                                ContentType = reader["content_type"].ToString(),
                                ParentContainerName = containerName,
                                ParentAppName = appName
                            };
                        }
                    }
                }
            }
            return null;
        }

        // ---------------- MOTOR DE NOTIFICAÇÕES ----------------

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
                    SendMqttNotification(sub.Endpoint, appName, containerName, messageXML);
                else if (sub.Endpoint.ToLower().StartsWith("http://") || sub.Endpoint.ToLower().StartsWith("https://"))
                    SendHttpNotification(sub.Endpoint, messageXML);
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
                            subs.Add(new Subscription { Endpoint = reader["endpoint"].ToString() });
                    }
                }
            }
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
                // ISTO É O IMPORTANTE: Ver qual é o erro real
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