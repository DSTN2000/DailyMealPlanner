namespace Lab4.Views.Dialogs;

using Gtk;
using Lab4.ViewModels;

/// <summary>
/// Dialog for adding a product to a meal plan
/// </summary>
public class AddProductDialog
{
    private readonly AddProductDialogViewModel _viewModel;
    private readonly Dialog _dialog;
    private readonly Window _parentWindow;

    public event EventHandler<AddProductDialogViewModel>? ProductAdded;

    public AddProductDialog(Window parentWindow, AddProductDialogViewModel viewModel)
    {
        _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _dialog = Dialog.New();
        _dialog.SetTransientFor(_parentWindow);
        _dialog.SetModal(true);
        _dialog.Title = $"Add {_viewModel.ProductName}";
        _dialog.SetDefaultSize(400, 200);

        BuildUI();
    }

    private void BuildUI()
    {
        var contentArea = (Box)_dialog.GetContentArea();
        var box = Box.New(Orientation.Vertical, 10);
        box.AddCssClass("dialog-content");

        // Mealtime selection
        var mealtimeLabel = Label.New("Select Mealtime:");
        mealtimeLabel.Halign = Align.Start;
        box.Append(mealtimeLabel);

        var mealtimeDropdown = DropDown.NewFromStrings(_viewModel.MealTimeOptions);
        mealtimeDropdown.SetSelected((uint)_viewModel.SelectedMealTimeIndex);
        box.Append(mealtimeDropdown);

        // Weight input
        var weightLabel = Label.New("Weight (grams):");
        weightLabel.Halign = Align.Start;
        box.Append(weightLabel);

        var weightSpin = SpinButton.NewWithRange(1, 10000, 1);
        weightSpin.SetValue(_viewModel.Weight);
        box.Append(weightSpin);

        contentArea.Append(box);

        // Add buttons
        _dialog.AddButton("Cancel", (int)ResponseType.Cancel);
        _dialog.AddButton("Add", (int)ResponseType.Accept);

        _dialog.OnResponse += (sender, args) =>
        {
            if (args.ResponseId == (int)ResponseType.Accept)
            {
                _viewModel.SelectedMealTimeIndex = (int)mealtimeDropdown.GetSelected();
                _viewModel.Weight = weightSpin.GetValue();

                ProductAdded?.Invoke(this, _viewModel);
            }
            _dialog.Close();
        };
    }

    public void Show()
    {
        _dialog.Show();
    }
}
