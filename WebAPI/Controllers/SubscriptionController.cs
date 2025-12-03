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
    public class SubscriptionController : ApiController
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SomiodDatabase"].ConnectionString;

        // GET SUBSCRIPTION
        [HttpGet, Route("api/somiod/{appName}/{containerName}/subs/{subscriptionName}")]
        public IHttpActionResult Get(string appName, string containerName, string subscriptionName)
        {
            // DISCOVERY: Subscriptions are leaf nodes, so they don't have children to discover
            if (Request.Headers.Contains("somiod-discovery"))
            {
                // Subscriptions don't have child resources, return empty list
                return Ok(new List<string>());
            }

            // GET normal: Return the specific subscription
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT resource_name, creation_datetime, evt, endpoint FROM Subscription WHERE resource_name = @Name AND parent_container_name = @ContainerName AND parent_app_name = @AppName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", subscriptionName);
                    cmd.Parameters.AddWithValue("@ContainerName", containerName);
                    cmd.Parameters.AddWithValue("@AppName", appName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new Subscription
                            {
                                ResourceName = reader["resource_name"].ToString(),
                                CreationDatetime = Convert.ToDateTime(reader["creation_datetime"]),
                                Evt = Convert.ToInt32(reader["evt"]),
                                Endpoint = reader["endpoint"].ToString(),
                                ParentContainerName = containerName,
                                ParentAppName = appName
                            });
                        }
                    }
                }
            }
            return NotFound();
        }

        // POST SUBSCRIPTION (Creation at container level)
        [HttpPost, Route("api/somiod/{appName}/{containerName}/subs")]
        public IHttpActionResult Post(string appName, string containerName, [FromBody] Subscription subscription)
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
                        if (ex.Number == 2627) return Conflict(); // Duplicate key - subscription with this name already exists
                        throw;
                    }
                }
            }

            return Created($"/api/somiod/{appName}/{containerName}/subs/{subscription.ResourceName}", subscription);
        }

        // DELETE SUBSCRIPTION
        [HttpDelete, Route("api/somiod/{appName}/{containerName}/subs/{subscriptionName}")]
        public IHttpActionResult Delete(string appName, string containerName, string subscriptionName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Subscription WHERE resource_name = @Name AND parent_container_name = @ContainerName AND parent_app_name = @AppName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", subscriptionName);
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


