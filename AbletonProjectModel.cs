using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AbletonProjectManager;

public class AbletonProjectModel
{
    [JsonProperty("title")]
    public string Title { get; set; }
    
    [JsonProperty("bpm")]
    public double Bpm { get; set; }
    
    [JsonProperty("scale")]
    public string Scale { get; set; }
    
    [JsonProperty("projectFolder")]
    public string ProjectFolder { get; set; }
    
    [JsonProperty("lastModified")]
    public DateTime LastModified { get; set; }
        
    // Additional project properties can be added here

    /// <summary>
    /// Loads an Ableton project from a folder path
    /// </summary>
    private static async Task<AbletonProjectModel> LoadProject(string projectPath)
    {
        try
        {
            var project = new AbletonProjectModel
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
            var tempo = jsonData["LiveSet"]?["MainTrack"]?["DeviceChain"]?["Mixer"]?["Tempo"];
            var bpmElement = tempo?["Manual"];
            if (bpmElement != null && double.TryParse(bpmElement["@Value"]?.ToString(), out var bpmValue))
            {
                project.Bpm = bpmValue;
            }

            // Extract Scale
            var scaleInfo = jsonData["LiveSet"]?["ScaleInformation"];
            if (scaleInfo != null)
            {
                var rootElement = scaleInfo["Root"]?["@Value"];
                var nameElement = scaleInfo["Name"]?["@Value"];

                string[] noteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
                string[] scaleNames =
                [
                    "Major", "Minor", "Dorian", "Mixolydian", "Lydian", "Phrygian", "Locrian", "Whole Tone",
                    "Half-whole Dim.", "Whole-half Dim.", "Minor Blues", "Minor Pentatonic", "Major Pentatonic",
                    "Harmonic Minor", "Harmonic Major", "Dorian #4", "Phrygian Dominant", "Melodic Minor",
                    "Lydian Augmented", "Lydian Dominant", "Super Locrian", "8-Tone Spanish", "Bhairav",
                    "Hungarian Minor", "Hirajoshi", "In-Sen", "Iwato", "Kumoi", "Pelog Selisir", "Pelog Tembung",
                    "Messiaen 3", "Messiaen 4", "Messiaen 5", "Messiaen 6", "Messiaen 7"
                ];
                if (rootElement != null && nameElement != null)
                {
                    var root = int.Parse(rootElement.ToString());
                    var scaleNameIndex = nameElement.ToString();
                        
                    // Convert root number to note name
                    var rootNote = noteNames[root % 12];
                    var scaleName = scaleNames[int.Parse(scaleNameIndex)];
                        
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
    public static async Task<List<AbletonProjectModel>> LoadProjects(string rootFolder)
    {
        var projects = new List<AbletonProjectModel>();
            
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