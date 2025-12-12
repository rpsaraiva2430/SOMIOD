using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Http;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [RoutePrefix("api/somiod")]
    public class ApplicationController : ApiController
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SomiodDatabase"].ConnectionString;

        // GET ALL
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            if (Request.Headers.Contains("somiod-discovery"))
            {
                var resType = Request.Headers.GetValues("somiod-discovery").FirstOrDefault();
                if (resType == "application") return Ok(GetAllApplicationPaths());
                return BadRequest("Only 'application' discovery is supported on the base URL.");
            }
            return Ok(GetAllApplications());
        }

        // GET SINGLE APP ou DISCOVERY DE CONTENTORES
        [HttpGet, Route("{resourceName:regex(^(?!container|subscription).*$)}")]
        public IHttpActionResult GetSingle(string resourceName)
        {
            var app = GetApplication(resourceName);
            if (app == null) return NotFound();

            // Discovery de Contentores
            if (Request.Headers.Contains("somiod-discovery"))
            {
                var resType = Request.Headers.GetValues("somiod-discovery").FirstOrDefault();
                if (resType == "container")
                {
                    return Ok(GetContainersForApp(resourceName));
                }
                return BadRequest("Discovery type not supported for application context.");
            }

            return Ok(app);
        }

        // POST APPLICATION
        [HttpPost, Route("")]
        public IHttpActionResult Post([FromBody] Application app)
        {
            if (app == null) return BadRequest("Data required.");
            if (string.IsNullOrWhiteSpace(app.ResourceName))
                app.ResourceName = $"app-{Guid.NewGuid().ToString().Substring(0, 8)}";

            app.CreationDatetime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Application (resource_name, creation_datetime) VALUES (@Name, @Date)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", app.ResourceName);
                    cmd.Parameters.AddWithValue("@Date", app.CreationDatetime);
                    try { cmd.ExecuteNonQuery(); }
                    catch (SqlException ex) when (ex.Number == 2627) { return Conflict(); }
                }
            }
            return Created($"/api/somiod/{app.ResourceName}", app);
        }

        // POST CONTAINER (Cria Container na App)
        [HttpPost, Route("{appName}")]
        public IHttpActionResult PostContainer(string appName, [FromBody] Container container)
        {
            if (container == null) return BadRequest("Data required.");
            if (string.IsNullOrWhiteSpace(container.ResourceName))
                container.ResourceName = $"cont-{Guid.NewGuid().ToString().Substring(0, 8)}";

            if (container.ResourceName.ToLower() == "subscription" || container.ResourceName.ToLower() == "subs")
                return BadRequest("Invalid resource name.");

            container.ParentAppName = appName;
            container.CreationDatetime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Container (resource_name, creation_datetime, parent_app_name) VALUES (@Name, @Date, @ParentApp)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", container.ResourceName);
                    cmd.Parameters.AddWithValue("@Date", container.CreationDatetime);
                    cmd.Parameters.AddWithValue("@ParentApp", container.ParentAppName);
                    try { cmd.ExecuteNonQuery(); }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 2627) return Conflict();
                        if (ex.Number == 547) return NotFound();
                        throw;
                    }
                }
            }
            return Created($"/api/somiod/{appName}/{container.ResourceName}", container);
        }

        // DELETE APP
        [HttpDelete, Route("{resourceName:regex(^(?!container|subscription).*$)}")]
        public IHttpActionResult Delete(string resourceName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Application WHERE resource_name = @Name";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", resourceName);
                    if (cmd.ExecuteNonQuery() == 0) return NotFound();
                }
            }
            return StatusCode(HttpStatusCode.NoContent);
        }
        // UPDATE
        [HttpPut, Route("{resourceName:regex(^(?!container|subscription).*$)}")]
        public IHttpActionResult Put(string resourceName, [FromBody] Application app)
        {
            if (app == null) return BadRequest("Data required.");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Application SET resource_name = @NewName WHERE resource_name = @OldName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NewName", app.ResourceName);
                    cmd.Parameters.AddWithValue("@OldName", resourceName);
                    if (cmd.ExecuteNonQuery() == 0) return NotFound();
                }
            }
            return Ok(app);
        }

        // ---------------- HELPERS ----------------

        private IEnumerable<string> GetAllApplicationPaths()
        {
            List<string> paths = new List<string>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT resource_name FROM Application", conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) paths.Add($"/api/somiod/{reader["resource_name"]}");
                }
            }
            return paths;
        }

        private IEnumerable<string> GetContainersForApp(string appName)
        {
            List<string> paths = new List<string>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name FROM Container WHERE parent_app_name = @AppName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AppName", appName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            paths.Add($"/api/somiod/{appName}/{reader["resource_name"]}");
                    }
                }
            }
            return paths;
        }

        private IEnumerable<Application> GetAllApplications()
        {
            List<Application> apps = new List<Application>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT resource_name, creation_datetime FROM Application", conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        apps.Add(new Application
                        {
                            ResourceName = reader["resource_name"].ToString(),
                            CreationDatetime = reader["creation_datetime"].ToString()
                        });
                    }
                }
            }
            return apps;
        }

        private Application GetApplication(string name)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT resource_name, creation_datetime FROM Application WHERE resource_name = @Name", conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Application
                            {
                                ResourceName = reader["resource_name"].ToString(),
                                CreationDatetime = reader["creation_datetime"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }
    }
}