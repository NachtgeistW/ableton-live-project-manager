using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbletonProjectManager
{
    public partial class MainWindow : Window
    {
        private Button _selectFolderButton;
        private DataGrid _projectsGrid;
        private TextBlock _statusText;
        private Grid _dragDropOverlay;
        private ObservableCollection<AbletonProject> _projects;
        
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            
            _selectFolderButton = this.FindControl<Button>("SelectFolderButton");
            _projectsGrid = this.FindControl<DataGrid>("ProjectsGrid");
            _statusText = this.FindControl<TextBlock>("StatusText");
            _dragDropOverlay = this.FindControl<Grid>("DragDropOverlay");
            
            _projects = new ObservableCollection<AbletonProject>();
            _projectsGrid.ItemsSource = _projects;
            
            // Set up drag and drop
            AddHandler(DragDrop.DragOverEvent, DragOver);
            AddHandler(DragDrop.DropEvent, Drop);
            
            _selectFolderButton.Click += SelectFolder;
            
            // Update visibility of overlay
            UpdateOverlayVisibility();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private async void SelectFolder(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Ableton Project Folder"
            };
            
            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                await LoadProjectsFromFolder(result);
            }
        }
        
        private void DragOver(object sender, DragEventArgs e)
        {
            // Only allow if the dragged data contains filenames
            e.DragEffects = e.Data.Contains(DataFormats.FileNames) 
                ? DragDropEffects.Copy 
                : DragDropEffects.None;
        }
        
        private async void Drop(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.FileNames))
            {
                var files = e.Data.GetFileNames();
                foreach (var file in files)
                {
                    // Check if it's a directory
                    if (Directory.Exists(file))
                    {
                        await LoadProjectsFromFolder(file);
                    }
                }
            }
        }
        
        private async Task LoadProjectsFromFolder(string folderPath)
        {
            try
            {
                // Ensure proper encoding for path handling
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                try
                {
                    Console.OutputEncoding = Encoding.UTF8;
                }
                catch (IOException)
                {
                    // Running without a console, which is normal for GUI apps
                }
                
                _statusText.Text = $"Loading projects from {folderPath}...";
                
                // Check if the directory exists and is accessible
                if (!Directory.Exists(folderPath))
                {
                    throw new DirectoryNotFoundException($"Directory not found: {folderPath}");
                }
                
                var loadedProjects = await AbletonProject.LoadProjects(folderPath);
                
                foreach (var project in loadedProjects)
                {
                    _projects.Add(project);
                }
                
                _statusText.Text = $"Loaded {loadedProjects.Count} projects from {folderPath}";
                
                // Update visibility of overlay
                UpdateOverlayVisibility();
            }
            catch (Exception ex)
            {
                _statusText.Text = $"Error: {ex.Message}";
                
                // Get the full exception details for debugging
                string errorDetails = $"Error loading projects: {ex.Message}\n\nStack Trace: {ex.StackTrace}";
                if (ex.InnerException != null)
                {
                    errorDetails += $"\n\nInner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
                }
                
                Console.WriteLine(errorDetails);
                
                await MessageBox.Show(this, $"Error loading projects: {ex.Message}", "Error", 
                    MessageBox.MessageBoxButtons.Ok);
            }
        }
        
        private void UpdateOverlayVisibility()
        {
            _dragDropOverlay.IsVisible = _projects.Count == 0;
        }
    }
}