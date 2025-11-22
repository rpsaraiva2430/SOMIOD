using System;
using System.Collections.Generic; // Necessário para List<string>
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Http;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    public class ContainerController : ApiController
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SomiodDatabase"].ConnectionString;

        // GET CONTAINER (Ou Discovery de filhos)
        [HttpGet, Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult Get(string appName, string containerName)
        {
            // 1. DISCOVERY: Se o header pedir, lista os filhos (ContentInstances)
            if (Request.Headers.Contains("somiod-discovery"))
            {
                var resType = Request.Headers.GetValues("somiod-discovery").FirstOrDefault();

                // Se pedir 'content-instance', devolve a lista de URLs
                if (resType == "content-instance")
                {
                    return Ok(GetContentInstancesForContainer(appName, containerName));
                }
                // Se pedir 'subscription', devolve a lista de URLs
                if (resType == "subscription")
                {
                    return Ok(GetSubscriptionsForContainer(appName, containerName));
                }
            }

            // 2. GET NORMAL: Devolve os dados do Contentor
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

        // POST CONTENT-INSTANCE (Este método está ÓTIMO, mantive igual)
        [HttpPost, Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult PostContentInstance(string appName, string containerName, [FromBody] ContentInstance model)
        {
            if (model == null) return BadRequest("Data required.");
            if (string.IsNullOrWhiteSpace(model.ResourceName))
                model.ResourceName = $"ci-{Guid.NewGuid().ToString().Substring(0, 8)}";

            model.ParentAppName = appName;
            model.ParentContainerName = containerName;
            model.CreationDatetime = DateTime.UtcNow;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // Inserção preparada para a BD sem IDs
                string query = "INSERT INTO ContentInstance (resource_name, creation_datetime, content, content_type, parent_container_name, parent_app_name) VALUES (@Name, @Date, @Content, @Type, @PCont, @PApp)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", model.ResourceName);
                    cmd.Parameters.AddWithValue("@Date", model.CreationDatetime.ToString("yyyy-MM-ddTHH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Content", model.Content);
                    cmd.Parameters.AddWithValue("@Type", model.ContentType ?? "application/json");
                    cmd.Parameters.AddWithValue("@PCont", model.ParentContainerName);
                    cmd.Parameters.AddWithValue("@PApp", model.ParentAppName);

                    try { cmd.ExecuteNonQuery(); }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 547) return NotFound(); // Erro se a app/container pai não existirem
                        throw;
                    }
                }
            }
            return Ok(model);
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

        // ---------------- HELPER METHODS PARA DISCOVERY ----------------

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
                            // Subscrições podem ser acedidas via '/subscription/{nome}' ou '/subs/{nome}' dependendo da tua preferência
                            paths.Add($"/api/somiod/{appName}/{containerName}/subscription/{reader["resource_name"]}");
                    }
                }
            }
            return paths;
        }
    }
}