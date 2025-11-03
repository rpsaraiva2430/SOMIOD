using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Http;

namespace WebAPI.Controllers
{
    [RoutePrefix("api/somiod")]
    public class ApplicationController : ApiController
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SomiodDatabase"].ConnectionString;

        // GET /api/somiod or /api/somiod/{resourceName}
        [HttpGet, Route("{resourceName?}")]
        public IHttpActionResult Get(string resourceName = null)
        {
            // Discovery
            if (Request.Headers.Contains("somiod-discovery"))
            {
                var resType = Request.Headers.GetValues("somiod-discovery").FirstOrDefault();
                if (resType == "application")
                    return Ok(GetAllApplicationPaths());
                else
                    return BadRequest("Only 'application' discovery is supported in this controller.");
            }

            // GET normal
            if (string.IsNullOrEmpty(resourceName))
                return Ok(GetAllApplications());
            else
            {
                var app = GetApplication(resourceName);
                if (app == null)
                    return NotFound();
                return Ok(app);
            }
        }

        // POST /api/somiod
        [HttpPost, Route("")]
        public IHttpActionResult Post([FromBody] ApplicationModel app)
        {
            if (app == null)
                return BadRequest("Application data is required.");

            // Gerar resource-name se não fornecido
            if (string.IsNullOrWhiteSpace(app.resource_name))
                app.resource_name = $"app-{Guid.NewGuid().ToString().Substring(0, 8)}";

            app.creation_datetime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Application (resource_name, creation_datetime) VALUES (@Name, @Datetime)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", app.resource_name);
                    cmd.Parameters.AddWithValue("@Datetime", app.creation_datetime);
                    cmd.ExecuteNonQuery();
                }
            }

            return Created($"/api/somiod/{app.resource_name}", app);
        }

        // DELETE /api/somiod/{resourceName}
        [HttpDelete, Route("{resourceName}")]
        public IHttpActionResult Delete(string resourceName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Application WHERE resource_name = @Name";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", resourceName);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0) return NotFound();
                }
            }
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // ---------------- Métodos privados ----------------

        private IEnumerable<string> GetAllApplicationPaths()
        {
            List<string> paths = new List<string>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name FROM Application";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        paths.Add($"/api/somiod/{reader["resource_name"]}");
                }
            }
            return paths;
        }

        private IEnumerable<ApplicationModel> GetAllApplications()
        {
            List<ApplicationModel> apps = new List<ApplicationModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name, creation_datetime FROM Application";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        apps.Add(new ApplicationModel
                        {
                            resource_name = reader["resource_name"].ToString(),
                            creation_datetime = reader["creation_datetime"].ToString()
                        });
                    }
                }
            }
            return apps;
        }

        private ApplicationModel GetApplication(string resourceName)
        {
            ApplicationModel app = null;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name, creation_datetime FROM Application WHERE resource_name = @Name";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", resourceName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            app = new ApplicationModel
                            {
                                resource_name = reader["resource_name"].ToString(),
                                creation_datetime = reader["creation_datetime"].ToString()
                            };
                        }
                    }
                }
            }
            return app;
        }
    }

    // ---------------- Modelo ----------------
    public class ApplicationModel
    {
        public string resource_name { get; set; }
        public string creation_datetime { get; set; }
    }
}
