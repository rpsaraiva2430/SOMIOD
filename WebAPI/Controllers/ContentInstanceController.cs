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
    public class ContentInstanceController : ApiController
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SomiodDatabase"].ConnectionString;

        // GET CONTENT INSTANCE
        [HttpGet, Route("api/somiod/{appName}/{containerName}/{contentInstanceName}")]
        public IHttpActionResult Get(string appName, string containerName, string contentInstanceName)
        {
            // DISCOVERY: Content instances are leaf nodes, so they don't have children to discover
            if (Request.Headers.Contains("somiod-discovery"))
            {
                // Content instances don't have child resources, return empty list
                return Ok(new List<string>());
            }

            // GET normal: Return the specific content instance
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name, creation_datetime, content, content_type FROM ContentInstance WHERE resource_name = @Name AND parent_container_name = @ContainerName AND parent_app_name = @AppName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", contentInstanceName);
                    cmd.Parameters.AddWithValue("@ContainerName", containerName);
                    cmd.Parameters.AddWithValue("@AppName", appName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new ContentInstance
                            {
                                ResourceName = reader["resource_name"].ToString(),
                                CreationDatetime = Convert.ToDateTime(reader["creation_datetime"]),
                                Content = reader["content"].ToString(),
                                ContentType = reader["content_type"].ToString(),
                                ParentContainerName = containerName,
                                ParentAppName = appName
                            });
                        }
                    }
                }
            }
            return NotFound();
        }

        // DELETE CONTENT INSTANCE
        [HttpDelete, Route("api/somiod/{appName}/{containerName}/{contentInstanceName}")]
        public IHttpActionResult Delete(string appName, string containerName, string contentInstanceName)
        {
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
            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}
