using System;
using System.Configuration;
using System.IO;
using System.Xml.Linq;

namespace GateStatus.Services
{
    /// <summary>
    /// Service for handling incoming XML notifications in the GateStatus application.
    /// Saves received notifications to XML files for auditing and processing.
    /// </summary>
    public class XmlNotificationHandler
    {
        private readonly string _xmlStoragePath;
        private readonly Action<string> _log;

        public XmlNotificationHandler(Action<string> logAction)
        {
            _log = logAction ?? throw new ArgumentNullException(nameof(logAction));
            
            // Get storage path from config or use default
            string configPath = ConfigurationManager.AppSettings["ReceivedNotificationsPath"];
            
            if (!string.IsNullOrEmpty(configPath))
            {
                // Use path from config (relative to exe directory)
                _xmlStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath);
            }
            else
            {
                // Default fallback
                _xmlStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReceivedNotifications");
            }
            
            _log($"XML storage path configured: {_xmlStoragePath}");
            EnsureStorageDirectory();
        }

        /// <summary>
        /// Processes and saves an incoming XML notification.
        /// </summary>
        /// <param name="xmlContent">The XML notification content</param>
        /// <returns>True if processing succeeded</returns>
        public bool ProcessIncomingNotification(string xmlContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xmlContent))
                {
                    _log("Empty notification received");
                    return false;
                }

                // Parse XML to validate structure
                XDocument xmlDoc = XDocument.Parse(xmlContent);
                
                // Extract basic info for filename
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                string notificationId = Guid.NewGuid().ToString().Substring(0, 8);
                string fileName = $"notification_{timestamp}_{notificationId}.xml";
                string fullPath = Path.Combine(_xmlStoragePath, fileName);

                // Add metadata to the XML
                XDocument enhancedDoc = EnhanceNotificationXml(xmlDoc);

                // Save to file
                enhancedDoc.Save(fullPath);
                
                _log($"Notification saved to: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                _log($"Error processing notification: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Enhances the notification XML with additional metadata.
        /// </summary>
        private XDocument EnhanceNotificationXml(XDocument originalXml)
        {
            // Clone the original document
            XDocument enhancedDoc = new XDocument(originalXml);
            
            // Add processing metadata
            XElement root = enhancedDoc.Root;
            if (root != null)
            {
                root.Add(new XElement("processing-timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")));
                root.Add(new XElement("received-by", "GateStatus-Application"));
            }
            
            return enhancedDoc;
        }

        /// <summary>
        /// Ensures the storage directory exists.
        /// </summary>
        private void EnsureStorageDirectory()
        {
            try
            {
                if (!Directory.Exists(_xmlStoragePath))
                {
                    Directory.CreateDirectory(_xmlStoragePath);
                    _log($"Created notifications directory: {_xmlStoragePath}");
                }
            }
            catch (Exception ex)
            {
                _log($"Failed to create notifications directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets count of stored notification files.
        /// </summary>
        public int GetStoredNotificationCount()
        {
            try
            {
                if (!Directory.Exists(_xmlStoragePath))
                    return 0;

                return Directory.GetFiles(_xmlStoragePath, "*.xml").Length;
            }
            catch (Exception ex)
            {
                _log($"Error counting notification files: {ex.Message}");
                return 0;
            }
        }
    }
}
