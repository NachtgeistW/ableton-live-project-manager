using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AbletonProjectManager;

/// <summary>
/// Handles persisting Ableton projects between application sessions
/// </summary>
public class ProjectStore
{
    private readonly string _storageFilePath;
    
    public ProjectStore()
    {
        // Get the app data folder
        string appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AbletonProjectManager");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }
        
        // Define the storage file path
        _storageFilePath = Path.Combine(appDataFolder, "projects.json");
    }
    
    /// <summary>
    /// Saves the collection of Ableton projects to persistent storage
    /// </summary>
    public async Task SaveProjectsAsync(IEnumerable<AbletonProjectModel> projects)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        
        // Serialize and save to file
        using var stream = File.Create(_storageFilePath);
        await JsonSerializer.SerializeAsync(stream, projects, options);
    }
    
    /// <summary>
    /// Loads previously saved projects from persistent storage
    /// </summary>
    public async Task<List<AbletonProjectModel>> LoadProjectsAsync()
    {
        // If the file doesn't exist, return an empty list
        if (!File.Exists(_storageFilePath))
        {
            return new List<AbletonProjectModel>();
        }
        
        try
        {
            // Read and deserialize the file
            using var stream = File.OpenRead(_storageFilePath);
            var projects = await JsonSerializer.DeserializeAsync<List<AbletonProjectModel>>(stream);
            return projects ?? new List<AbletonProjectModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading saved projects: {ex.Message}");
            return new List<AbletonProjectModel>();
        }
    }
    
    /// <summary>
    /// Updates existing projects and adds new projects to storage
    /// </summary>
    public async Task UpdateProjectsAsync(ObservableCollection<AbletonProjectModel> currentProjects)
    {
        // Load existing projects
        var savedProjects = await LoadProjectsAsync();
        
        // Track existing project paths to avoid duplicates
        var existingPaths = new HashSet<string>();
        foreach (var project in savedProjects)
        {
            existingPaths.Add(project.ProjectFolder);
            
            // Check if project still exists on disk
            if (!Directory.Exists(project.ProjectFolder))
            {
                continue; // Skip projects that no longer exist
            }
            
            // Add to current projects if not already present
            if (!ContainsProjectPath(currentProjects, project.ProjectFolder))
            {
                // Load the current state of the project (could have been modified)
                try
                {
                    var updatedProject = await AbletonProjectModel.LoadProjects(project.ProjectFolder);
                    if (updatedProject.Count > 0)
                    {
                        foreach (var p in updatedProject)
                        {
                            if (!ContainsProjectPath(currentProjects, p.ProjectFolder))
                            {
                                currentProjects.Add(p);
                            }
                        }
                    }
                    else
                    {
                        currentProjects.Add(project);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading project {project.Title}: {ex.Message}");
                    currentProjects.Add(project); // Add the saved version as fallback
                }
            }
        }
        
        await SaveProjectsAsync(currentProjects);
    }
    
    /// <summary>
    /// Helper method to check if a project path exists in the collection
    /// </summary>
    private bool ContainsProjectPath(IEnumerable<AbletonProjectModel> projects, string path)
    {
        foreach (var project in projects)
        {
            if (project.ProjectFolder.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}