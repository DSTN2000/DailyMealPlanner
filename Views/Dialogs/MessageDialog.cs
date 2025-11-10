namespace Lab4.Views.Dialogs;

using Gtk;

/// <summary>
/// Helper for creating simple message dialogs
/// </summary>
public static class MessageDialog
{
    public static void ShowInfo(Window parent, string title, string message)
    {
        var dialog = new AlertDialog();
        dialog.Message = title;
        dialog.SetDetail(message);
        dialog.SetButtons(new[] { "OK" });
        dialog.SetDefaultButton(0);
        dialog.SetCancelButton(0);
        dialog.Show(parent);
    }

    public static void ShowError(Window parent, string title, string message)
    {
        var dialog = new AlertDialog();
        dialog.Message = title;
        dialog.SetDetail(message);
        dialog.SetButtons(new[] { "OK" });
        dialog.SetDefaultButton(0);
        dialog.SetCancelButton(0);
        dialog.Show(parent);
    }
}
