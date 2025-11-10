namespace Lab4.Views.Dialogs;

using Gtk;

/// <summary>
/// Helper for creating file chooser dialogs
/// </summary>
public static class FileDialogHelper
{
    public static FileDialog CreateSaveDialog(string title, string defaultName, params (string name, string[] patterns)[] filters)
    {
        var dialog = FileDialog.New();
        dialog.SetTitle(title);
        dialog.SetInitialName(defaultName);

        if (filters.Length > 0)
        {
            var filterList = Gio.ListStore.New(Gtk.FileFilter.GetGType());

            foreach (var (name, patterns) in filters)
            {
                var filter = Gtk.FileFilter.New();
                filter.SetName(name);
                foreach (var pattern in patterns)
                {
                    filter.AddPattern(pattern);
                }
                filterList.Append(filter);
            }

            dialog.SetFilters(filterList);
        }

        return dialog;
    }

    public static FileDialog CreateOpenDialog(string title, params (string name, string[] patterns)[] filters)
    {
        var dialog = FileDialog.New();
        dialog.SetTitle(title);

        if (filters.Length > 0)
        {
            var filterList = Gio.ListStore.New(Gtk.FileFilter.GetGType());

            foreach (var (name, patterns) in filters)
            {
                var filter = Gtk.FileFilter.New();
                filter.SetName(name);
                foreach (var pattern in patterns)
                {
                    filter.AddPattern(pattern);
                }
                filterList.Append(filter);
            }

            dialog.SetFilters(filterList);
        }

        return dialog;
    }
}
