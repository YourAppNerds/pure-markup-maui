using Microsoft.UI.Dispatching;

namespace WindowsNoAppXaml.WinUI;

/// <summary>
/// Hand-written entry point. Mirrors the Main that the XAML compiler generates in App.g.i.cs
/// when an ApplicationDefinition (App.xaml) exists.
/// </summary>
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
