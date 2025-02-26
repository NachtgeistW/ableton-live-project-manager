using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AbletonProjectManager
{
    public class AbletonProject
    {
        public string Title { get; set; }
        public double Bpm { get; set; }
        public string Scale { get; set; }
        public string ProjectFolder { get; set; }
        public DateTime LastModified { get; set; }
        
        // Additional project properties can be added here

        /// <summary>
        /// Loads an Ableton project from a folder path
        /// </summary>
        public static async Task<AbletonProject> LoadProject(string projectPath)
        {
            try
            {
                var project = new AbletonProject
                {
                    ProjectFolder = projectPath,
                    Title = Path.GetFileName(projectPath)
                };
                
                // Ensure proper encoding for console output
                try
                {
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.WriteLine($"Loading project from: {projectPath}");
                }
                catch (IOException)
                {
                    // Running without a console, which is normal for GUI apps
                }
                
                // Look for .als file
                var alsFiles = Directory.GetFiles(projectPath, "*.als");
                if (alsFiles.Length == 0)
                {
                    throw new FileNotFoundException("No Ableton Live Set (.als) files found in the project folder.");
                }
                
                var alsFilePath = alsFiles[0]; // Use first .als file found
                Console.WriteLine($"Found .als file: {alsFilePath}");
                
                var fileInfo = new FileInfo(alsFilePath);
                project.LastModified = fileInfo.LastWriteTime;

                // Read file with proper encoding handling
                var compressedData = await File.ReadAllBytesAsync(alsFilePath);
                
                var parser = new AbletonParser();
                var (xmlData, jsonData) = await parser.UnpackAndCreateJson(compressedData);
                
                // Extract BPM
                var tempo = xmlData.Descendants("Tempo").FirstOrDefault();
                if (tempo != null)
                {
                    var bpmElement = tempo.Element("Manual");
                    if (bpmElement != null && double.TryParse(bpmElement.Value, out double bpmValue))
                    {
                        project.Bpm = bpmValue;
                    }
                }
                
                // Extract Scale
                var scaleInfo = xmlData.Descendants("ScaleInformation").FirstOrDefault();
                if (scaleInfo != null)
                {
                    var rootElement = scaleInfo.Element("Root");
                    var nameElement = scaleInfo.Element("Name");
                    
                    if (rootElement != null && nameElement != null)
                    {
                        int root = int.Parse(rootElement.Value);
                        string scaleName = nameElement.Value;
                        
                        // Convert root number to note name
                        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
                        string rootNote = noteNames[root % 12];
                        
                        project.Scale = $"{rootNote} {scaleName}";
                    }
                }
                
                return project;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading project from {projectPath}: {ex.Message}");
                throw new ApplicationException($"Error loading project from {projectPath}: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Loads all Ableton projects from a root folder
        /// </summary>
        public static async Task<List<AbletonProject>> LoadProjects(string rootFolder)
        {
            var projects = new List<AbletonProject>();
            
            // Check if the rootFolder itself is a project
            if (Directory.GetFiles(rootFolder, "*.als").Length > 0)
            {
                try
                {
                    var project = await LoadProject(rootFolder);
                    projects.Add(project);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading project from {rootFolder}: {ex.Message}");
                }
            }
            
            // Check subdirectories
            foreach (var directory in Directory.GetDirectories(rootFolder))
            {
                if (Directory.GetFiles(directory, "*.als").Length > 0)
                {
                    try
                    {
                        var project = await LoadProject(directory);
                        projects.Add(project);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading project from {directory}: {ex.Message}");
                    }
                }
                else
                {
                    // Recursively check subdirectories for projects
                    var subProjects = await LoadProjects(directory);
                    projects.AddRange(subProjects);
                }
            }
            
            return projects;
        }
    }
}