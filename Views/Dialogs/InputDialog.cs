namespace Lab4.Views.Dialogs;

using Gtk;

/// <summary>
/// Helper for creating input dialogs (rename, add, etc.)
/// </summary>
public static class InputDialog
{
    /// <summary>
    /// Shows a rename dialog with validation
    /// </summary>
    /// <param name="parent">Parent window</param>
    /// <param name="title">Dialog title</param>
    /// <param name="currentName">Current name to display</param>
    /// <param name="validateUnique">Function to validate uniqueness of the new name</param>
    /// <param name="onRenamed">Callback when rename is confirmed</param>
    public static void ShowRenameDialog(
        Window parent,
        string title,
        string currentName,
        Func<string, bool> validateUnique,
        Action<string> onRenamed)
    {
        var dialog = new Window();
        dialog.SetTitle(title);
        dialog.SetDefaultSize(300, 150);
        dialog.SetModal(true);
        dialog.SetTransientFor(parent);

        var contentBox = Box.New(Orientation.Vertical, 10);
        contentBox.AddCssClass("dialog-content");

        var label = Label.New("New name:");
        label.Halign = Align.Start;
        contentBox.Append(label);

        var entry = Entry.New();
        entry.SetText(currentName);
        contentBox.Append(entry);

        var buttonBox = Box.New(Orientation.Horizontal, 10);
        buttonBox.Halign = Align.End;
        buttonBox.AddCssClass("dialog-button-box");

        var cancelButton = Button.NewWithLabel("Cancel");
        cancelButton.OnClicked += (s, e) => dialog.Close();
        buttonBox.Append(cancelButton);

        var okButton = Button.NewWithLabel("OK");
        okButton.AddCssClass("suggested-action");
        okButton.OnClicked += (s, e) =>
        {
            var newName = entry.GetText();
            if (!string.IsNullOrWhiteSpace(newName))
            {
                // Validate uniqueness
                var isUnique = validateUnique(newName);
                if (!isUnique)
                {
                    ShowErrorDialog(
                        dialog,
                        "Duplicate Name",
                        $"A meal time with the name '{newName}' already exists.\nPlease choose a different name."
                    );
                    return;
                }

                onRenamed(newName);
                dialog.Close();
            }
        };
        buttonBox.Append(okButton);

        contentBox.Append(buttonBox);
        dialog.SetChild(contentBox);
        dialog.Show();
    }

    /// <summary>
    /// Shows an add dialog for creating new items
    /// </summary>
    /// <param name="parent">Parent window</param>
    /// <param name="title">Dialog title</param>
    /// <param name="labelText">Label for the input field</param>
    /// <param name="placeholderText">Placeholder text for the entry</param>
    /// <param name="validateUnique">Function to validate uniqueness</param>
    /// <param name="onAdded">Callback when add is confirmed</param>
    public static void ShowAddDialog(
        Window parent,
        string title,
        string labelText,
        string placeholderText,
        Func<string, bool> validateUnique,
        Action<string> onAdded)
    {
        var dialog = new Window();
        dialog.SetTitle(title);
        dialog.SetDefaultSize(300, 150);
        dialog.SetModal(true);
        dialog.SetTransientFor(parent);

        var contentBox = Box.New(Orientation.Vertical, 10);
        contentBox.AddCssClass("dialog-content");

        var label = Label.New(labelText);
        label.Halign = Align.Start;
        contentBox.Append(label);

        var entry = Entry.New();
        entry.SetPlaceholderText(placeholderText);
        contentBox.Append(entry);

        var buttonBox = Box.New(Orientation.Horizontal, 10);
        buttonBox.Halign = Align.End;
        buttonBox.AddCssClass("dialog-button-box");

        var cancelButton = Button.NewWithLabel("Cancel");
        cancelButton.OnClicked += (s, e) => dialog.Close();
        buttonBox.Append(cancelButton);

        var okButton = Button.NewWithLabel("Add");
        okButton.AddCssClass("suggested-action");
        okButton.OnClicked += (s, e) =>
        {
            var name = entry.GetText();
            if (!string.IsNullOrWhiteSpace(name))
            {
                // Validate uniqueness
                var isUnique = validateUnique(name);
                if (!isUnique)
                {
                    ShowErrorDialog(
                        dialog,
                        "Duplicate Name",
                        $"A meal time with the name '{name}' already exists.\nPlease choose a different name."
                    );
                    return;
                }

                onAdded(name);
                dialog.Close();
            }
        };
        buttonBox.Append(okButton);

        contentBox.Append(buttonBox);
        dialog.SetChild(contentBox);
        dialog.Show();
    }

    /// <summary>
    /// Shows a simple error dialog (used internally by InputDialog)
    /// </summary>
    private static void ShowErrorDialog(Window parent, string title, string message)
    {
        var errorDialog = new Window();
        errorDialog.SetTitle("Error");
        errorDialog.SetDefaultSize(300, 100);
        errorDialog.SetModal(true);
        errorDialog.SetTransientFor(parent);

        var errorBox = Box.New(Orientation.Vertical, 10);
        errorBox.AddCssClass("dialog-content");

        var errorLabel = Label.New(message);
        errorLabel.Halign = Align.Center;
        errorBox.Append(errorLabel);

        var errorOkButton = Button.NewWithLabel("OK");
        errorOkButton.Halign = Align.Center;
        errorOkButton.OnClicked += (s, e) => errorDialog.Close();
        errorBox.Append(errorOkButton);

        errorDialog.SetChild(errorBox);
        errorDialog.Show();
    }
}
