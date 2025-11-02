namespace Lab4.Views;

using Gtk;
using Lab4.Models;
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
        _window = ApplicationWindow.New(app);
        _window.Title = "Daily Meal Planner";
        _window.SetDefaultSize(900, 600);

        BuildUI();
    }

    public void Show()
    {
        _window.Show();
        // Load data after window is visible
        LoadDataAsync();
    }

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

            // Update UI directly (we're already on main thread in GTK)
            UpdateCategoriesView();
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to load data");

            // Show error
            var errorLabel = Label.New($"Error loading data: {ex.Message}");
            errorLabel.AddCssClass("error");
            while (_contentBox.GetFirstChild() != null)
            {
                _contentBox.Remove(_contentBox.GetFirstChild()!);
            }
            _contentBox.Append(errorLabel);
        }
    }

    private void UpdateCategoriesView()
    {
        Logger.Instance.Information("UpdateCategoriesView called with {Count} categories", _viewModel.Categories.Count);

        // Clear loading message
        while (_contentBox.GetFirstChild() != null)
        {
            _contentBox.Remove(_contentBox.GetFirstChild()!);
        }

        // Create scrolled window
        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolledWindow.Vexpand = true;

        // Create a box to hold expandable category groups
        var categoriesBox = Box.New(Orientation.Vertical, 5);
        categoriesBox.MarginStart = 10;
        categoriesBox.MarginEnd = 10;
        categoriesBox.MarginTop = 10;
        categoriesBox.MarginBottom = 10;

        foreach (var category in _viewModel.Categories)
        {
            var expander = CreateLazyExpander(category);
            categoriesBox.Append(expander);
        }

        scrolledWindow.Child = categoriesBox;
        _contentBox.Append(scrolledWindow);

        Logger.Instance.Information("UI created with lazy-loading expanders");
    }

    private Expander CreateLazyExpander(string categoryName)
    {
        var expander = Expander.New(categoryName);
        expander.MarginTop = 5;
        expander.MarginBottom = 5;

        var isLoading = false;

        // Load products when expander is expanded
        expander.OnNotify += async (sender, args) =>
        {
            if (args.Pspec.GetName() == "expanded" && expander.Expanded && expander.Child == null && !isLoading)
            {
                isLoading = true;
                Logger.Instance.Information("Loading products for category: {Category}", categoryName);

                // Show loading placeholder
                var loadingLabel = Label.New("Loading products...");
                expander.Child = loadingLabel;

                try
                {
                    var products = await CatalogService.GetProductsByCategoryAsync(categoryName);

                    // Create virtualized ListView
                    var listView = CreateProductListView(products);

                    expander.Child = listView;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex, "Failed to load products for {Category}", categoryName);
                    var errorLabel = Label.New($"Error: {ex.Message}");
                    expander.Child = errorLabel;
                }
                finally
                {
                    isLoading = false;
                }
            }
        };

        return expander;
    }

    private ScrolledWindow CreateProductListView(List<Product> products)
    {
        // Create StringList as a simple model - we'll display product info in the factory
        var stringList = Gtk.StringList.New(null);
        foreach (var product in products)
        {
            // Store product data as JSON string (simple approach)
            var productData = $"{product.Name}|{product.Calories}|{product.Protein}|{product.TotalFat}|{product.Carbohydrates}";
            stringList.Append(productData);
        }

        // Create selection model (no selection)
        var selectionModel = NoSelection.New(stringList);

        // Create factory for list items
        var factory = SignalListItemFactory.New();

        factory.OnSetup += (sender, args) =>
        {
            if (args.Object is ListItem listItem)
            {
                var row = CreateEmptyProductRow();
                listItem.Child = row;
            }
        };

        factory.OnBind += (sender, args) =>
        {
            if (args.Object is ListItem listItem)
            {
                var stringObject = listItem.Item as StringObject;
                if (stringObject != null && listItem.Child is Box row)
                {
                    var productData = stringObject.String?.Split('|');
                    if (productData != null && productData.Length >= 5)
                    {
                        UpdateProductRow(row, productData[0],
                            double.Parse(productData[1]),
                            double.Parse(productData[2]),
                            double.Parse(productData[3]),
                            double.Parse(productData[4]));
                    }
                }
            }
        };

        // Create ListView with virtualization
        var listView = ListView.New(selectionModel, factory);

        // Wrap in ScrolledWindow
        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolledWindow.SetSizeRequest(-1, 400); // Max height
        scrolledWindow.Child = listView;

        return scrolledWindow;
    }

    private Box CreateEmptyProductRow()
    {
        var row = Box.New(Orientation.Horizontal, 10);
        row.MarginStart = 10;
        row.MarginEnd = 10;
        row.MarginTop = 3;
        row.MarginBottom = 3;

        // Product name
        var nameLabel = Label.New("");
        nameLabel.Halign = Align.Start;
        nameLabel.Hexpand = true;
        nameLabel.SetEllipsize(Pango.EllipsizeMode.End);
        row.Append(nameLabel);

        // Nutritional info box
        var nutritionBox = Box.New(Orientation.Horizontal, 15);

        var caloriesLabel = Label.New("");
        caloriesLabel.SetSizeRequest(80, -1);
        nutritionBox.Append(caloriesLabel);

        var proteinLabel = Label.New("");
        proteinLabel.SetSizeRequest(60, -1);
        nutritionBox.Append(proteinLabel);

        var fatLabel = Label.New("");
        fatLabel.SetSizeRequest(60, -1);
        nutritionBox.Append(fatLabel);

        var carbsLabel = Label.New("");
        carbsLabel.SetSizeRequest(60, -1);
        nutritionBox.Append(carbsLabel);

        row.Append(nutritionBox);

        return row;
    }

    private void UpdateProductRow(Box row, string name, double calories, double protein, double fat, double carbs)
    {
        var nameLabel = row.GetFirstChild() as Label;
        if (nameLabel != null)
        {
            nameLabel.SetText(name);
        }

        var nutritionBox = row.GetLastChild() as Box;
        if (nutritionBox != null)
        {
            var child = nutritionBox.GetFirstChild();
            int index = 0;

            while (child != null)
            {
                if (child is Label label)
                {
                    switch (index)
                    {
                        case 0: // Calories
                            label.SetText(calories > 0 ? $"{calories:F0} kcal" : "");
                            label.AddCssClass("dim-label");
                            break;
                        case 1: // Protein
                            label.SetText(protein > 0 ? $"P: {protein:F1}g" : "");
                            label.AddCssClass("dim-label");
                            break;
                        case 2: // Fat
                            label.SetText(fat > 0 ? $"F: {fat:F1}g" : "");
                            label.AddCssClass("dim-label");
                            break;
                        case 3: // Carbs
                            label.SetText(carbs > 0 ? $"C: {carbs:F1}g" : "");
                            label.AddCssClass("dim-label");
                            break;
                    }
                    index++;
                }
                child = child.GetNextSibling();
            }
        }
    }

    private Box CreateProductRow(Product product)
    {
        var row = Box.New(Orientation.Horizontal, 10);
        row.MarginStart = 10;
        row.MarginEnd = 10;
        row.MarginTop = 3;
        row.MarginBottom = 3;

        // Product name
        var nameLabel = Label.New(product.Name);
        nameLabel.Halign = Align.Start;
        nameLabel.Hexpand = true;
        nameLabel.SetEllipsize(Pango.EllipsizeMode.End);
        row.Append(nameLabel);

        // Nutritional info
        var nutritionBox = Box.New(Orientation.Horizontal, 15);

        if (product.Calories > 0)
        {
            var caloriesLabel = Label.New($"{product.Calories:F0} kcal");
            caloriesLabel.AddCssClass("dim-label");
            caloriesLabel.SetSizeRequest(80, -1);
            nutritionBox.Append(caloriesLabel);
        }

        if (product.Protein > 0)
        {
            var proteinLabel = Label.New($"P: {product.Protein:F1}g");
            proteinLabel.AddCssClass("dim-label");
            proteinLabel.SetSizeRequest(60, -1);
            nutritionBox.Append(proteinLabel);
        }

        if (product.TotalFat > 0)
        {
            var fatLabel = Label.New($"F: {product.TotalFat:F1}g");
            fatLabel.AddCssClass("dim-label");
            fatLabel.SetSizeRequest(60, -1);
            nutritionBox.Append(fatLabel);
        }

        if (product.Carbohydrates > 0)
        {
            var carbsLabel = Label.New($"C: {product.Carbohydrates:F1}g");
            carbsLabel.AddCssClass("dim-label");
            carbsLabel.SetSizeRequest(60, -1);
            nutritionBox.Append(carbsLabel);
        }

        row.Append(nutritionBox);

        return row;
    }
}
