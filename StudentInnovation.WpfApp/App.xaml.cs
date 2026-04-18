using System.Windows;
using System.IO;

namespace StudentInnovation.WpfApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        base.OnStartup(e);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var msg = e.ExceptionObject?.ToString() ?? "Unknown unhandled exception";
            WriteLog("CurrentDomain", msg);
        }
        catch
        {
        }
    }

    private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            WriteLog("DispatcherUnhandledException", e.Exception.ToString());
            MessageBox.Show("程序发生未捕获异常，已记录日志到本地。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
        catch
        {
        }
    }

    private static void WriteLog(string where, string message)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StudentInnovationSystem", "logs");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"crash-{DateTime.Now:yyyyMMdd}.log");
        File.AppendAllText(file, $"[{DateTime.Now:HH:mm:ss}] {where}: {message}{Environment.NewLine}{Environment.NewLine}");
    }
}
