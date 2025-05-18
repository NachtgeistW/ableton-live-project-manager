using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace AbletonProjectManager
{
    public class AbletonParser
    {
        /// <summary>
        /// Uncompresses and parses an Ableton project file (.als)
        /// </summary>
        public async Task<(XDocument XmlData, JObject JsonData)> UnpackAndCreateJson(byte[] compressedData)
        {
            try
            {
                // Decompress the gzipped data
                string decompressedXml;
                using (var compressedStream = new MemoryStream(compressedData))
                using (var decompressStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    await decompressStream.CopyToAsync(resultStream);
                    decompressedXml = Encoding.UTF8.GetString(resultStream.ToArray());
                }

                // Parse as XML
                var xmlDoc = XDocument.Parse(decompressedXml);
                
                // Remove the XML declaration to avoid JSON parsing issues
                xmlDoc.Declaration = null;

                var jsonText = JsonConvert.SerializeXNode(xmlDoc, Formatting.None, true);
                
                // Parse JSON with specific reader settings
                using (var stringReader = new StringReader(jsonText))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    var jsonData = JObject.Load(jsonReader);
                    return (xmlDoc, jsonData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Ableton project: {ex.Message}");
                throw new ApplicationException($"Failed to process Ableton project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses XMP files for keywords/tags in the Ableton project folder
        /// </summary>
        public static async Task<List<Dictionary<string, object>>> ParseXmpFiles(string projectDirectory)
        {
            var folderPath = Path.Combine(projectDirectory, "Ableton Folder Info");
            
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"Directory {folderPath} does not exist.");
                return new List<Dictionary<string, object>>();
            }
            
            var xmpFiles = Directory.GetFiles(folderPath, "*.xmp");
            var allKeywords = new List<Dictionary<string, object>>();
            
            foreach (var filePath in xmpFiles)
            {
                var xmpContent = await File.ReadAllTextAsync(filePath);
                var xmlDoc = XDocument.Parse(xmpContent);
                
                // Extract keywords using LINQ to XML
                var ns = new XmlNamespaceManager(new NameTable());
                ns.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                ns.AddNamespace("ablFR", "http://ns.ableton.com/xmp/folderinfo/1.0/");
                
                try 
                {
                    var keywords = from desc in xmlDoc.Descendants(XName.Get("Description", ns.LookupNamespace("rdf")))
                                  from bag in desc.Descendants(XName.Get("Bag", ns.LookupNamespace("ablFR") + ":items"))
                                  from li in bag.Descendants(XName.Get("li", ns.LookupNamespace("rdf") + ":Bag"))
                                  let keywordBag = li.Descendants(XName.Get("Bag", ns.LookupNamespace("ablFR") + ":keywords")).FirstOrDefault()
                                  where keywordBag != null
                                  from keyword in keywordBag.Descendants(XName.Get("li", ns.LookupNamespace("rdf") + ":Bag"))
                                  let keywordValue = keyword.Value
                                  let parts = keywordValue.Split('|')
                                  let groups = parts.Length > 0 ? parts[0] : string.Empty
                                  let tag = parts.Length > 1 ? parts[1] : string.Empty
                                  select new Dictionary<string, object>
                                  {
                                      { 
                                          keywordValue, 
                                          new Dictionary<string, object>
                                          {
                                              { "label", tag },
                                              { "variant", "outline" },
                                              { "value", keywordValue },
                                              { "group", groups }
                                          }
                                      }
                                  };
                    
                    allKeywords.AddRange(keywords);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing XMP file {filePath}: {ex.Message}");
                }
            }
            
            return allKeywords;
        }

        /// <summary>
        /// Removes specified devices from an Ableton project
        /// </summary>
        public static string RemoveDevices(XDocument doc, List<string> pluginsToRemove)
        {
            var modifiedDoc = new XDocument(doc);
            
            // Find and remove devices
            var deviceElements = modifiedDoc.Descendants()
                .Where(e => e.Attributes().Any(a => a.Name == "Value" && pluginsToRemove.Contains(a.Value)))
                .ToList();
            
            foreach (var device in deviceElements)
            {
                // Find parent Devices element
                var devicesElement = device.Ancestors().FirstOrDefault(a => a.Name.LocalName == "Devices");
                if (devicesElement != null)
                {
                    device.Remove();
                }
            }
            
            // Convert back to XML string with proper encoding
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings 
            { 
                Indent = true,
                Encoding = Encoding.UTF8
            }))
            {
                modifiedDoc.Save(xmlWriter);
                return stringWriter.ToString();
            }
        }
    }
}