using Microsoft.UI.Dispatching;

namespace ScreenshotPage.WinUI;

/// <summary>Hand-written entry point, mirroring the Main the XAML compiler generates for App.xaml.</summary>
public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Microsoft.UI.Xaml.Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
