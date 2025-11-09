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

    public Widget Widget => _container;
    public event EventHandler? RemoveRequested;

    public MealPlanItemView(MealPlanItemViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _container = Box.New(Orientation.Horizontal, 10);
        _container.MarginStart = 12;
        _container.MarginEnd = 12;
        _container.MarginTop = 4;
        _container.MarginBottom = 4;

        BuildUI();

        // Subscribe to ViewModel changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MealPlanItemViewModel.Weight) ||
                e.PropertyName == nameof(MealPlanItemViewModel.WeightDisplay))
            {
                // Weight changed externally, no rebuild needed (spinner is bound)
            }
        };
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
        var weightSpin = SpinButton.NewWithRange(1, 10000, 1);
        weightSpin.SetValue(_viewModel.Weight);
        weightSpin.OnValueChanged += (sender, args) =>
        {
            var newWeight = weightSpin.GetValue();
            _viewModel.Weight = newWeight;  // Update ViewModel (triggers recalculation)
        };
        _container.Append(weightSpin);

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
}
