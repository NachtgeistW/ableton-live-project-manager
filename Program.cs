using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System;
using System.IO;
using System.Text;

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
            Styles.Add(new Avalonia.Themes.Default.DefaultTheme());
            Styles.Add(new Avalonia.Themes.Default.DefaultTheme());
            base.Initialize();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}