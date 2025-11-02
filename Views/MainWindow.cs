namespace Lab4.Views;

using Gtk;
using Lab4.Services;
using Lab4.ViewModels;

public class MainWindow
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ApplicationWindow _window;
    private Box _contentBox = null!;

    public MainWindow(Gtk.Application app)
    {
        _viewModel = new MainWindowViewModel();
        _window = Gtk.ApplicationWindow.New(app);
        _window.Title = "Daily Meal Planner";
        _window.SetDefaultSize(900, 600);

        BuildUI();
        LoadDataAsync();
    }

    public void Show() => _window.Show();

    private void BuildUI()
    {
        var mainBox = Box.New(Orientation.Vertical, 10);
        mainBox.MarginTop = 10;
        mainBox.MarginBottom = 10;
        mainBox.MarginStart = 10;
        mainBox.MarginEnd = 10;

        // Header
        var headerLabel = Label.New("Food Catalog");
        headerLabel.AddCssClass("title-1");
        mainBox.Append(headerLabel);

        // Content area (will be updated after data loads)
        _contentBox = Box.New(Orientation.Vertical, 5);
        _contentBox.Vexpand = true;

        var loadingLabel = Label.New("Loading categories...");
        _contentBox.Append(loadingLabel);

        mainBox.Append(_contentBox);

        _window.Child = mainBox;
    }

    private async void LoadDataAsync()
    {
        try
        {
            await _viewModel.LoadCategoriesAsync();
            Logger.Instance.Information("Data loaded: {Count} categories", _viewModel.Categories.Count);

            // Update UI
            UpdateCategoriesView();
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to load data");

            // Show error
            GLib.Functions.IdleAdd(0, () =>
            {
                var errorLabel = Label.New($"Error loading data: {ex.Message}");
                errorLabel.AddCssClass("error");
                while (_contentBox.GetFirstChild() != null)
                {
                    _contentBox.Remove(_contentBox.GetFirstChild()!);
                }
                _contentBox.Append(errorLabel);
                return false;
            });
        }
    }

    private void UpdateCategoriesView()
    {
        // Clear loading message
        while (_contentBox.GetFirstChild() != null)
        {
            _contentBox.Remove(_contentBox.GetFirstChild()!);
        }

        // Create scrolled window
        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolledWindow.Vexpand = true;

        // Create a box to hold category groups
        var categoriesBox = Box.New(Orientation.Vertical, 10);

        foreach (var category in _viewModel.Categories)
        {
            // Category header
            var categoryLabel = Label.New(null);
            categoryLabel.SetMarkup($"<b>{category.Name}</b> ({category.Products.Count} items)");
            categoryLabel.Halign = Align.Start;
            categoriesBox.Append(categoryLabel);

            // Show first few products as example
            var productsToShow = category.Products.Take(10);
            foreach (var product in productsToShow)
            {
                var productLabel = Label.New($"  • {product.Name} - {product.Calories:F0} kcal");
                productLabel.Halign = Align.Start;
                categoriesBox.Append(productLabel);
            }

            if (category.Products.Count > 10)
            {
                var moreLabel = Label.New($"  ... and {category.Products.Count - 10} more");
                moreLabel.Halign = Align.Start;
                moreLabel.AddCssClass("dim-label");
                categoriesBox.Append(moreLabel);
            }
        }

        scrolledWindow.Child = categoriesBox;
        _contentBox.Append(scrolledWindow);

        Logger.Instance.Information("UI updated with category data");
    }
}
