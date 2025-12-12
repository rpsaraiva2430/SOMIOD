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

        // POST CONTENT-INSTANCE
        [HttpPost, Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult PostContentInstance(string appName, string containerName, [FromBody] ContentInstance model)
        {
            if (model == null) return BadRequest("Content instance data is required.");
            
            // Generate resource-name if not provided
            if (string.IsNullOrWhiteSpace(model.ResourceName))
                model.ResourceName = $"ci-{Guid.NewGuid().ToString().Substring(0, 8)}";

            model.ParentAppName = appName;
            model.ParentContainerName = containerName;
            model.CreationDatetime = DateTime.UtcNow;

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

                    try 
                    { 
                        cmd.ExecuteNonQuery(); 
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 547) return NotFound(); // Parent container doesn't exist
                        if (ex.Number == 2627) return Conflict(); // Duplicate resource name
                        throw;
                    }
                }
            }

            // Return Created with location header and full resource properties
            return Created($"/api/somiod/{appName}/{containerName}/{model.ResourceName}", model);
        }

        // POST SUBSCRIPTION (Creation at container level)
        [HttpPost, Route("api/somiod/{appName}/{containerName}/subs")]
        public IHttpActionResult PostSubscription(string appName, string containerName, [FromBody] Subscription subscription)
        {
            if (subscription == null) return BadRequest("Subscription data is required.");
            
            // Generate resource-name if not provided
            if (string.IsNullOrWhiteSpace(subscription.ResourceName))
                subscription.ResourceName = $"sub-{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Validate evt values (1 for creation, 2 for deletion)
            if (subscription.Evt != 1 && subscription.Evt != 2)
                return BadRequest("evt property must be 1 (creation) or 2 (deletion).");

            if (string.IsNullOrWhiteSpace(subscription.Endpoint))
                return BadRequest("endpoint is required.");

            subscription.ParentAppName = appName;
            subscription.ParentContainerName = containerName;
            subscription.CreationDatetime = DateTime.UtcNow;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Subscription (resource_name, creation_datetime, evt, endpoint, parent_container_name, parent_app_name) VALUES (@Name, @Date, @Evt, @Endpoint, @PCont, @PApp)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", subscription.ResourceName);
                    cmd.Parameters.AddWithValue("@Date", subscription.CreationDatetime.ToString("yyyy-MM-ddTHH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Evt", subscription.Evt);
                    cmd.Parameters.AddWithValue("@Endpoint", subscription.Endpoint);
                    cmd.Parameters.AddWithValue("@PCont", subscription.ParentContainerName);
                    cmd.Parameters.AddWithValue("@PApp", subscription.ParentAppName);

                    try 
                    { 
                        cmd.ExecuteNonQuery(); 
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 547) return NotFound(); // Parent container doesn't exist
                        if (ex.Number == 2627) return Conflict(); // Duplicate resource name
                        throw;
                    }
                }
            }

            // Return Created with location header and full resource properties
            return Created($"/api/somiod/{appName}/{containerName}/subs/{subscription.ResourceName}", subscription);
        }

        // PUT CONTAINER (Update existing container)
        [HttpPut, Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult Put(string appName, string containerName, [FromBody] Container container)
        {
            if (container == null) return BadRequest("Container data is required.");

            // First check if the container exists and get its current data
            Container existingContainer = null;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string selectQuery = "SELECT resource_name, creation_datetime FROM Container WHERE resource_name = @Name AND parent_app_name = @ParentApp";
                using (SqlCommand cmd = new SqlCommand(selectQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", containerName);
                    cmd.Parameters.AddWithValue("@ParentApp", appName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            existingContainer = new Container
                            {
                                ResourceName = reader["resource_name"].ToString(),
                                CreationDatetime = reader["creation_datetime"].ToString(),
                                ParentAppName = appName
                            };
                        }
                    }
                }
            }

            if (existingContainer == null)
                return NotFound();

            // For containers, there are limited updatable properties
            // In this implementation, we're essentially refreshing/confirming the container exists
            // If you have additional properties to update in the future, add them here
            
            // Since containers only have resource-name (identifier) and creation-datetime (immutable),
            // this PUT operation serves to verify the container exists and return its current state
            // You could extend this to update additional metadata fields if they exist in your database

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // For now, we don't actually need to update anything since containers have minimal properties
                // This query serves as a verification that the container still exists
                string updateQuery = "SELECT COUNT(*) FROM Container WHERE resource_name = @Name AND parent_app_name = @ParentApp";
                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", containerName);
                    cmd.Parameters.AddWithValue("@ParentApp", appName);
                    
                    int count = (int)cmd.ExecuteScalar();
                    if (count == 0) return NotFound();
                }
            }

            // Return the container with all its current properties
            var updatedContainer = new Container
            {
                ResourceName = containerName,
                CreationDatetime = existingContainer.CreationDatetime, // Keep original creation date
                ParentAppName = appName
            };

            return Ok(updatedContainer);
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
                            // Updated to use /subs/ virtual node as per requirements
                            paths.Add($"/api/somiod/{appName}/{containerName}/subs/{reader["resource_name"]}");
                    }
                }
            }
            return paths;
        }
    }
}