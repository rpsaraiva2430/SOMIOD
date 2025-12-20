using System;
using System.Configuration;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using WebAPI.Models;

namespace WebAPI.Services
{
    /// <summary>
    /// Service responsible for serializing notifications to XML files and validating them against schema.
    /// </summary>
    public class XmlNotificationSerializer
    {
        private readonly string _xmlStoragePath;
        private readonly string _schemaPath;
        private XmlSchemaSet _schemaSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlNotificationSerializer"/> class.
        /// </summary>
        public XmlNotificationSerializer()
        {
            // Get storage path from config or use default
            string configPath = ConfigurationManager.AppSettings["XmlNotificationPath"];
            
            if (!string.IsNullOrEmpty(configPath) && configPath.StartsWith("~/"))
            {
                // Handle relative path with ~/
                _xmlStoragePath = System.Web.HttpContext.Current.Server.MapPath(configPath);
            }
            else if (!string.IsNullOrEmpty(configPath))
            {
                // Use absolute path from config
                _xmlStoragePath = configPath;
            }
            else
            {
                // Default to App_Data/Notifications
                _xmlStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Notifications");
            }
            
            _schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", "notification-schema.xsd");
            
            InitializeSchema();
            EnsureStorageDirectory();
        }

        /// <summary>
        /// Serializes a notification to XML file and validates it against the schema.
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="containerName">Container name</param>
        /// <param name="data">Content instance data</param>
        /// <param name="eventType">Event type (1=creation, 2=deletion)</param>
        /// <returns>True if serialization and validation succeeded</returns>
        public bool SerializeAndValidateNotification(string appName, string containerName, ContentInstance data, int eventType)
        {
            try
            {
                string notificationId = Guid.NewGuid().ToString();
                string fileName = GenerateFileName(appName, containerName, notificationId);
                string fullPath = Path.Combine(_xmlStoragePath, fileName);

                // Add detailed logging for debugging
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Storage path: {_xmlStoragePath}");
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Full file path: {fullPath}");
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Directory exists: {Directory.Exists(_xmlStoragePath)}");

                // Create XML document
                XDocument xmlDoc = CreateNotificationXml(appName, containerName, data, eventType, notificationId);

                // Validate against schema
                bool isValid = ValidateXmlDocument(xmlDoc);
                if (!isValid)
                {
                    System.Diagnostics.Debug.WriteLine($"[XML Serializer] Validation failed for notification {notificationId}");
                    return false;
                }

                // Save to file with additional error checking
                xmlDoc.Save(fullPath);
                
                // Verify file was actually created
                bool fileExists = File.Exists(fullPath);
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Notification saved: {fileName}");
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] File verification: {fileExists}");
                
                if (fileExists)
                {
                    var fileInfo = new FileInfo(fullPath);
                    System.Diagnostics.Debug.WriteLine($"[XML Serializer] File size: {fileInfo.Length} bytes");
                }
                
                return fileExists;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Creates the XML document for the notification.
        /// </summary>
        private XDocument CreateNotificationXml(string appName, string containerName, ContentInstance data, int eventType, string notificationId)
        {
            XNamespace ns = "http://somiod.local/notification";
            
            return new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "notification",
                    new XAttribute("id", notificationId),
                    new XElement(ns + "event", eventType == 1 ? "creation" : "deletion"),
                    new XElement(ns + "resource", data.ResourceName ?? ""),
                    new XElement(ns + "content", data.Content ?? ""),
                    new XElement(ns + "original-path", $"/api/somiod/{appName}/{containerName}/{data.ResourceName}"),
                    new XElement(ns + "timestamp", data.CreationDatetime.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                    new XElement(ns + "application", appName),
                    new XElement(ns + "container", containerName)
                )
            );
        }

        /// <summary>
        /// Validates the XML document against the predefined schema.
        /// </summary>
        private bool ValidateXmlDocument(XDocument xmlDoc)
        {
            if (_schemaSet == null)
            {
                System.Diagnostics.Debug.WriteLine("[XML Serializer] Schema not loaded, skipping validation");
                return true; // Continue without validation if schema not available
            }

            bool isValid = true;
            
            try
            {
                xmlDoc.Validate(_schemaSet, (sender, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[XML Validation] {e.Severity}: {e.Message}");
                    if (e.Severity == XmlSeverityType.Error)
                        isValid = false;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XML Validation] Exception: {ex.Message}");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Initializes the XML schema for validation.
        /// </summary>
        private void InitializeSchema()
        {
            try
            {
                if (!File.Exists(_schemaPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[XML Serializer] Schema file not found: {_schemaPath}");
                    return;
                }

                _schemaSet = new XmlSchemaSet();
                _schemaSet.Add("http://somiod.local/notification", _schemaPath);
                _schemaSet.Compile();
                
                System.Diagnostics.Debug.WriteLine("[XML Serializer] Schema loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Failed to load schema: {ex.Message}");
                _schemaSet = null;
            }
        }

        /// <summary>
        /// Ensures the storage directory exists.
        /// </summary>
        private void EnsureStorageDirectory()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Configured path: {_xmlStoragePath}");
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Path exists: {Directory.Exists(_xmlStoragePath)}");
                
                if (!Directory.Exists(_xmlStoragePath))
                {
                    Directory.CreateDirectory(_xmlStoragePath);
                    System.Diagnostics.Debug.WriteLine($"[XML Serializer] Created directory: {_xmlStoragePath}");
                }
                
                // Test write permissions
                string testFile = Path.Combine(_xmlStoragePath, "test_write.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Write permissions verified");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Directory setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a unique filename for the XML notification.
        /// </summary>
        private string GenerateFileName(string appName, string containerName, string notificationId)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            return $"{appName}_{containerName}_{timestamp}_{notificationId.Substring(0, 8)}.xml";
        }

        /// <summary>
        /// Gets all stored notification XML files for a specific application.
        /// </summary>
        public string[] GetNotificationFiles(string appName = null)
        {
            try
            {
                if (!Directory.Exists(_xmlStoragePath))
                    return new string[0];

                string searchPattern = string.IsNullOrEmpty(appName) ? "*.xml" : $"{appName}_*.xml";
                return Directory.GetFiles(_xmlStoragePath, searchPattern);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XML Serializer] Error getting files: {ex.Message}");
                return new string[0];
            }
        }
    }
}