using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Threading;
using System;
using System.IO;
using System.Text;
using Avalonia.Markup.Xaml;

namespace AbletonProjectManager
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Configure encoding support
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // Set console encoding only if running with a console
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch (IOException)
            {
                // Running without a console, which is normal for GUI apps
            }
            
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
    
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Remove data validation to improve performance when not needed
            BindingPlugins.DataValidators.RemoveAt(0);
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}