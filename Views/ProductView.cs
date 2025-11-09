namespace Lab4.Views;

using Gtk;
using Lab4.ViewModels;

/// <summary>
/// View for displaying a single product in the catalog
/// </summary>
public class ProductView
{
    private readonly ProductViewModel _viewModel;
    private readonly Box _container;

    public Widget Widget => _container;
    public event EventHandler? ProductClicked;

    public ProductView(ProductViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _container = Box.New(Orientation.Horizontal, 10);
        _container.AddCssClass("card");
        _container.MarginTop = 2;
        _container.MarginBottom = 2;
        _container.MarginStart = 5;
        _container.MarginEnd = 5;

        BuildUI();
    }

    private void BuildUI()
    {
        // Make the whole card clickable
        var gesture = GestureClick.New();
        gesture.OnPressed += (sender, args) =>
        {
            ProductClicked?.Invoke(this, EventArgs.Empty);
        };
        _container.AddController(gesture);

        // Product info box
        var infoBox = Box.New(Orientation.Vertical, 4);
        infoBox.Hexpand = true;
        infoBox.MarginTop = 8;
        infoBox.MarginBottom = 8;
        infoBox.MarginStart = 12;
        infoBox.MarginEnd = 12;

        // Product name
        var nameLabel = Label.New(_viewModel.Name);
        nameLabel.Halign = Align.Start;
        nameLabel.Xalign = 0;
        nameLabel.SetEllipsize(Pango.EllipsizeMode.End);
        infoBox.Append(nameLabel);

        // Nutritional info
        var nutritionBox = Box.New(Orientation.Horizontal, 10);

        if (!string.IsNullOrEmpty(_viewModel.CaloriesDisplay))
        {
            var caloriesLabel = Label.New(_viewModel.CaloriesDisplay);
            caloriesLabel.AddCssClass("dim-label");
            nutritionBox.Append(caloriesLabel);
        }

        if (!string.IsNullOrEmpty(_viewModel.ProteinDisplay))
        {
            var proteinLabel = Label.New(_viewModel.ProteinDisplay);
            proteinLabel.AddCssClass("dim-label");
            nutritionBox.Append(proteinLabel);
        }

        if (!string.IsNullOrEmpty(_viewModel.FatDisplay))
        {
            var fatLabel = Label.New(_viewModel.FatDisplay);
            fatLabel.AddCssClass("dim-label");
            nutritionBox.Append(fatLabel);
        }

        if (!string.IsNullOrEmpty(_viewModel.CarbsDisplay))
        {
            var carbsLabel = Label.New(_viewModel.CarbsDisplay);
            carbsLabel.AddCssClass("dim-label");
            nutritionBox.Append(carbsLabel);
        }

        infoBox.Append(nutritionBox);
        _container.Append(infoBox);
    }
}
