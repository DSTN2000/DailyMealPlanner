using Gtk;
using Lab4.Views;
using Lab4.Services;

public class Program
{
    public static int Main(string[] args)
    {
        var application = Application.New("org.lab4.mealplanner", Gio.ApplicationFlags.DefaultFlags);

        application.OnActivate += (sender, eventArgs) =>
        {
            var window = new MainWindow((Application)sender);
            window.Show();
        };

        var result = application.RunWithSynchronizationContext(args);

        Logger.CloseAndFlush();
        return result;
    }
}
