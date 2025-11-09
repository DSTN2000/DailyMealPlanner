namespace Lab4.Views;

using Gtk;
using Lab4.ViewModels;

/// <summary>
/// View for a single meal plan item (product with weight)
/// </summary>
public class MealPlanItemView
{
    private readonly MealPlanItemViewModel _viewModel;
    private readonly Box _container;
    private SpinButton? _weightSpin;
    private uint _updateTimerId = 0;
    private double _pendingWeight = 0;

    public Widget Widget => _container;
    public event EventHandler? RemoveRequested;

    public MealPlanItemView(MealPlanItemViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _container = Box.New(Orientation.Horizontal, 10);
        _container.AddCssClass("meal-item");

        BuildUI();
    }

    private void BuildUI()
    {
        // Product name
        var nameLabel = Label.New(_viewModel.ProductName);
        nameLabel.Halign = Align.Start;
        nameLabel.Hexpand = true;
        nameLabel.SetEllipsize(Pango.EllipsizeMode.End);
        _container.Append(nameLabel);

        // Weight spinner
        _weightSpin = SpinButton.NewWithRange(1, 10000, 1);
        _weightSpin.SetValue(_viewModel.Weight);
        _weightSpin.OnValueChanged += OnWeightChanged;
        _container.Append(_weightSpin);

        var gramsLabel = Label.New("g");
        _container.Append(gramsLabel);

        // Remove button
        var removeButton = Button.NewWithLabel("Remove");
        removeButton.AddCssClass("destructive-action");
        removeButton.OnClicked += (sender, args) =>
        {
            RemoveRequested?.Invoke(this, EventArgs.Empty);
        };
        _container.Append(removeButton);
    }

    private void OnWeightChanged(SpinButton sender, EventArgs args)
    {
        // Get the new weight value
        _pendingWeight = _weightSpin!.GetValue();

        // Cancel existing timer if any
        if (_updateTimerId != 0)
        {
            GLib.Functions.SourceRemove(_updateTimerId);
        }

        // Schedule update after 300ms of no changes (debounce)
        _updateTimerId = GLib.Functions.TimeoutAdd(0, 300, () =>
        {
            _updateTimerId = 0;
            // Use tolerance for floating point comparison
            if (Math.Abs(_viewModel.Weight - _pendingWeight) > 0.01)
            {
                _viewModel.Weight = _pendingWeight;
            }
            return false; // Don't repeat
        });
    }
}
