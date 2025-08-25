using Mood_Ring.Views;

namespace Mood_Ring;

public partial class App : System.Windows.Application
{
    private RingWindow? _window;
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        _window = new RingWindow();
        _window.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
