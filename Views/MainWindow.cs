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

        LoadCustomCSS();
        BuildUI();
    }

    private void LoadCustomCSS()
    {
        var cssProvider = CssProvider.New();
        var css = @"
            .card {
                background-color: @theme_bg_color;
                border-radius: 8px;
                border: 1px solid alpha(@theme_fg_color, 0.15);
                transition: all 200ms ease-in-out;
            }

            .card:hover {
                background-color: alpha(@theme_selected_bg_color, 0.1);
                box-shadow: 0 2px 4px alpha(black, 0.1);
            }

            listview {
                background-color: transparent;
            }

            scrolledwindow {
                background-color: transparent;
            }
        ";

        cssProvider.LoadFromData(css, -1);

        Gtk.StyleContext.AddProviderForDisplay(
            Gdk.Display.GetDefault()!,
            cssProvider,
            800 // GTK_STYLE_PROVIDER_PRIORITY_APPLICATION
        );
    }

    public void Show()
    {
        _window.Show();
        // Load data after window is visible
        LoadDataAsync();
    }

    private void BuildUI()
    {
        // Use Paned for proportional split
        var paned = Paned.New(Orientation.Horizontal);

        // Left panel: Product tree view
        var leftPanel = Box.New(Orientation.Vertical, 10);
        leftPanel.MarginTop = 10;
        leftPanel.MarginBottom = 10;
        leftPanel.MarginStart = 10;
        leftPanel.MarginEnd = 5;

        // Header
        var headerLabel = Label.New("Food Catalog");
        headerLabel.AddCssClass("title-2");
        headerLabel.Halign = Align.Start;
        leftPanel.Append(headerLabel);

        // Content area (will be updated after data loads)
        _contentBox = Box.New(Orientation.Vertical, 5);
        _contentBox.Vexpand = true;

        var loadingLabel = Label.New("Loading categories...");
        _contentBox.Append(loadingLabel);

        leftPanel.Append(_contentBox);

        // Right panel: Empty for now
        var rightPanel = Box.New(Orientation.Vertical, 0);
        rightPanel.MarginTop = 10;
        rightPanel.MarginBottom = 10;
        rightPanel.MarginStart = 5;
        rightPanel.MarginEnd = 10;

        var placeholderLabel = Label.New("Select a product...");
        placeholderLabel.AddCssClass("dim-label");
        placeholderLabel.Valign = Align.Center;
        placeholderLabel.Halign = Align.Center;
        rightPanel.Append(placeholderLabel);

        // Set start child (left - 1/3)
        paned.SetStartChild(leftPanel);
        paned.SetResizeStartChild(false);
        paned.SetShrinkStartChild(false);

        // Set end child (right - 2/3)
        paned.SetEndChild(rightPanel);
        paned.SetResizeEndChild(true);
        paned.SetShrinkEndChild(false);

        // Set initial position to 1/3 of window width (300px as default for 900px window)
        paned.SetPosition(300);

        _window.Child = paned;
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

                    // Group by subcategories (labels)
                    var grouped = GroupBySubcategories(products);

                    // Create content box
                    var contentBox = Box.New(Orientation.Vertical, 5);

                    if (grouped.Count > 1)
                    {
                        // Multiple subcategories - create nested expanders
                        foreach (var (subcategory, subcategoryProducts) in grouped)
                        {
                            var subcategoryExpander = CreateSubcategoryExpander(subcategory, subcategoryProducts);
                            contentBox.Append(subcategoryExpander);
                        }

                        var scrolledWindow = ScrolledWindow.New();
                        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
                        scrolledWindow.SetSizeRequest(-1, 400);
                        scrolledWindow.Child = contentBox;

                        expander.Child = scrolledWindow;
                    }
                    else
                    {
                        // Single subcategory or no subcategories - show products directly
                        var listView = CreateProductListView(products);
                        expander.Child = listView;
                    }

                    Logger.Instance.Information("Loaded {Count} products in {SubcategoryCount} subcategories for {Category}",
                        products.Count, grouped.Count, categoryName);
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

    private Dictionary<string, List<Product>> GroupBySubcategories(List<Product> products)
    {
        var grouped = new Dictionary<string, List<Product>>();

        foreach (var product in products)
        {
            if (product.Labels.Count > 0)
            {
                // Use first label as subcategory
                var subcategory = product.Labels[0];
                if (!grouped.ContainsKey(subcategory))
                {
                    grouped[subcategory] = new List<Product>();
                }
                grouped[subcategory].Add(product);
            }
            else
            {
                // No labels - use "other"
                if (!grouped.ContainsKey("other"))
                {
                    grouped["other"] = new List<Product>();
                }
                grouped["other"].Add(product);
            }
        }

        return grouped;
    }

    private Expander CreateSubcategoryExpander(string subcategoryName, List<Product> products)
    {
        var expander = Expander.New($"{subcategoryName} ({products.Count} items)");
        expander.MarginTop = 3;
        expander.MarginBottom = 3;
        expander.MarginStart = 20; // Indent subcategories

        var isLoaded = false;

        expander.OnNotify += (sender, args) =>
        {
            if (args.Pspec.GetName() == "expanded" && expander.Expanded && !isLoaded)
            {
                isLoaded = true;
                Logger.Instance.Information("Creating virtualized list for subcategory: {Subcategory} ({Count} products)",
                    subcategoryName, products.Count);

                var listView = CreateProductListView(products);
                expander.Child = listView;
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
            // Store product data as JSON string
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

        // Wrap in ScrolledWindow with scrollbar on the left
        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolledWindow.SetSizeRequest(-1, 400); // Max height
        scrolledWindow.SetPlacement(CornerType.TopRight); // Place scrollbar on the left
        scrolledWindow.Child = listView;

        return scrolledWindow;
    }

    private Box CreateEmptyProductRow()
    {
        // Create card container
        var card = Box.New(Orientation.Vertical, 0);
        card.MarginStart = 10;
        card.MarginEnd = 30; // Extra margin for scrollbar space
        card.MarginTop = 4;
        card.MarginBottom = 4;

        // Add CSS classes for card styling
        card.AddCssClass("card");

        // Product name label
        var nameLabel = Label.New("");
        nameLabel.Halign = Align.Start;
        nameLabel.SetEllipsize(Pango.EllipsizeMode.End);
        nameLabel.MarginStart = 12;
        nameLabel.MarginEnd = 12;
        nameLabel.MarginTop = 8;
        nameLabel.MarginBottom = 8;

        card.Append(nameLabel);

        return card;
    }

    private void UpdateProductRow(Box card, string name, double calories, double protein, double fat, double carbs)
    {
        var nameLabel = card.GetFirstChild() as Label;
        if (nameLabel != null)
        {
            nameLabel.SetText(name);
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
