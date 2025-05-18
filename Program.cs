using Avalonia;
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
}