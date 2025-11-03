using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Http;

namespace WebAPI.Controllers
{
    [RoutePrefix("api/somiod/container")]
    public class ContainerController : ApiController
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SomiodDatabase"].ConnectionString;

        // GET /api/somiod/container or /api/somiod/container/{resourceName}
        [HttpGet, Route("{resourceName?}")]
        public IHttpActionResult Get(string resourceName = null)
        {
            // Discovery
            if (Request.Headers.Contains("somiod-discovery"))
            {
                var resType = Request.Headers.GetValues("somiod-discovery").FirstOrDefault();
                if (resType == "container")
                    return Ok(GetAllContainerPaths());
                else
                    return BadRequest("Only 'container' discovery is supported in this controller.");
            }

            // GET normal
            if (string.IsNullOrEmpty(resourceName))
                return Ok(GetAllContainers());
            else
            {
                var container = GetContainer(resourceName);
                if (container == null)
                    return NotFound();
                return Ok(container);
            }
        }

        // POST /api/somiod/container
        [HttpPost, Route("")]
        public IHttpActionResult Post([FromBody] ContainerModel container)
        {
            if (container == null)
                return BadRequest("Container data is required.");

            // Generate resource-name if not provided
            if (string.IsNullOrWhiteSpace(container.resource_name))
                container.resource_name = $"container-{Guid.NewGuid().ToString().Substring(0, 8)}";

            container.creation_datetime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Container (resource_name, creation_datetime, application_id) VALUES (@Name, @Datetime, @AppId)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", container.resource_name);
                    cmd.Parameters.AddWithValue("@Datetime", container.creation_datetime);
                    cmd.Parameters.AddWithValue("@AppId", container.application_id);
                    cmd.ExecuteNonQuery();
                }
            }

            return Created($"/api/somiod/container/{container.resource_name}", container);
        }

        // DELETE /api/somiod/container/{resourceName}
        [HttpDelete, Route("{resourceName}")]
        public IHttpActionResult Delete(string resourceName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Container WHERE resource_name = @Name";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", resourceName);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0) return NotFound();
                }
            }
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // ---------------- Private helpers ----------------

        private IEnumerable<string> GetAllContainerPaths()
        {
            List<string> paths = new List<string>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name FROM Container";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        paths.Add($"/api/somiod/container/{reader["resource_name"]}");
                }
            }
            return paths;
        }

        private IEnumerable<ContainerModel> GetAllContainers()
        {
            List<ContainerModel> containers = new List<ContainerModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name, creation_datetime, application_id FROM Container";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        containers.Add(new ContainerModel
                        {
                            resource_name = reader["resource_name"].ToString(),
                            creation_datetime = reader["creation_datetime"].ToString(),
                            application_id = Convert.ToInt32(reader["application_id"])
                        });
                    }
                }
            }
            return containers;
        }

        private ContainerModel GetContainer(string resourceName)
        {
            ContainerModel container = null;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name, creation_datetime, application_id FROM Container WHERE resource_name = @Name";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", resourceName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            container = new ContainerModel
                            {
                                resource_name = reader["resource_name"].ToString(),
                                creation_datetime = reader["creation_datetime"].ToString(),
                                application_id = Convert.ToInt32(reader["application_id"])
                            };
                        }
                    }
                }
            }
            return container;
        }
    }

    // ---------------- Model used by this controller (keeps naming consistent with existing ApplicationModel) ----------------
    public class ContainerModel
    {
        public string resource_name { get; set; }
        public string creation_datetime { get; set; }
        public int application_id { get; set; }
    }
}
