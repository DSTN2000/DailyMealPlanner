namespace Lab4.Views;

using Gtk;
using Lab4.Services;
using Lab4.ViewModels;

public class MainWindow
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ApplicationWindow _window;
    private Box _contentBox = null!;
    private Box _mealPlanBox = null!;
    private DailyMealPlanView? _mealPlanView;

    public MainWindow(Gtk.Application app)
    {
        _viewModel = new MainWindowViewModel();
        _window = ApplicationWindow.New(app);
        _window.Title = "Daily Meal Planner";
        _window.SetDefaultSize(900, 600);

        LoadCustomCSS();
        SetupActions();
        BuildUI();
    }

    private void SetupActions()
    {
        // Create settings action
        var settingsAction = Gio.SimpleAction.New("settings", null);
        settingsAction.OnActivate += (sender, args) =>
        {
            Logger.Instance.Information("Opening preferences dialog");
            var preferencesDialog = new PreferencesDialog(_window, _viewModel);
            preferencesDialog.Show();
        };

        _window.AddAction(settingsAction);
    }

    private void LoadCustomCSS()
    {
        try
        {
            var cssProvider = CssProvider.New();
            var cssPath = Path.Combine(AppContext.BaseDirectory, "Views", "styles.css");

            if (File.Exists(cssPath))
            {
                var css = File.ReadAllText(cssPath);
                cssProvider.LoadFromData(css, -1);

                Gtk.StyleContext.AddProviderForDisplay(
                    Gdk.Display.GetDefault()!,
                    cssProvider,
                    800 // GTK_STYLE_PROVIDER_PRIORITY_APPLICATION
                );

                Logger.Instance.Information("CSS loaded from {CssPath}", cssPath);
            }
            else
            {
                Logger.Instance.Warning("CSS file not found at {CssPath}", cssPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to load CSS");
        }
    }

    public void Show()
    {
        _window.Show();
        // Load data after window is visible
        LoadDataAsync();
    }

    private void BuildUI()
    {
        // Main container box
        var mainBox = Box.New(Orientation.Vertical, 0);

        // Create menubar
        var menuBar = CreateMenuBar();
        mainBox.Append(menuBar);

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

        // Right panel: Meal Plan
        var rightPanel = Box.New(Orientation.Vertical, 10);
        rightPanel.MarginTop = 10;
        rightPanel.MarginBottom = 10;
        rightPanel.MarginStart = 5;
        rightPanel.MarginEnd = 10;

        // Header
        var mealPlanHeader = Label.New("Daily Meal Plan");
        mealPlanHeader.AddCssClass("title-2");
        mealPlanHeader.Halign = Align.Start;
        rightPanel.Append(mealPlanHeader);

        // Meal plan content area
        _mealPlanBox = Box.New(Orientation.Vertical, 5);
        _mealPlanBox.Vexpand = true;
        rightPanel.Append(_mealPlanBox);

        // Build meal plan UI
        BuildMealPlanUI();

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

        mainBox.Append(paned);
        _window.Child = mainBox;
    }

    private Gtk.PopoverMenuBar CreateMenuBar()
    {
        // Create menu model
        var menu = Gio.Menu.New();

        // Create Preferences submenu
        var preferencesMenu = Gio.Menu.New();
        preferencesMenu.Append("Edit Preferences", "win.settings");

        menu.AppendSubmenu("Preferences", preferencesMenu);

        // Create PopoverMenuBar from model
        var menuBar = Gtk.PopoverMenuBar.NewFromModel(menu);

        return menuBar;
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
                    var products = await _viewModel.LoadProductsForCategoryAsync(categoryName);

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

    private Dictionary<string, List<ProductViewModel>> GroupBySubcategories(List<ProductViewModel> products)
    {
        var grouped = new Dictionary<string, List<ProductViewModel>>();

        foreach (var product in products)
        {
            if (product.Labels.Count > 0)
            {
                // Use first label as subcategory
                var subcategory = product.Labels[0];
                if (!grouped.ContainsKey(subcategory))
                {
                    grouped[subcategory] = new List<ProductViewModel>();
                }
                grouped[subcategory].Add(product);
            }
            else
            {
                // No labels - use "other"
                if (!grouped.ContainsKey("other"))
                {
                    grouped["other"] = new List<ProductViewModel>();
                }
                grouped["other"].Add(product);
            }
        }

        return grouped;
    }

    private Expander CreateSubcategoryExpander(string subcategoryName, List<ProductViewModel> products)
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

    private ScrolledWindow CreateProductListView(List<ProductViewModel> products)
    {
        // For small lists, use Box with ProductView instances
        if (products.Count <= 100)
        {
            var box = Box.New(Orientation.Vertical, 2);
            box.MarginStart = 10;
            box.MarginEnd = 10;

            foreach (var productVm in products)
            {
                var productView = new ProductView(productVm);
                productView.ProductClicked += (s, e) =>
                {
                    ShowAddProductDialog(productVm.Name);
                };
                box.Append(productView.Widget);
            }

            var scrolledWindow = ScrolledWindow.New();
            scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            scrolledWindow.SetSizeRequest(-1, 400);
            scrolledWindow.Child = box;

            return scrolledWindow;
        }

        // For large lists, use virtualized ListBox
        var listBox = ListBox.New();
        listBox.SetSelectionMode(SelectionMode.None);

        foreach (var productVm in products)
        {
            var productView = new ProductView(productVm);
            productView.ProductClicked += (s, e) =>
            {
                ShowAddProductDialog(productVm.Name);
            };

            var row = ListBoxRow.New();
            row.SetChild(productView.Widget);
            listBox.Append(row);
        }

        var scrolled = ScrolledWindow.New();
        scrolled.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolled.SetSizeRequest(-1, 400);
        scrolled.SetChild(listBox);

        return scrolled;
    }

    private void ShowAddProductDialog(string productName)
    {
        var dialog = Gtk.Dialog.New();
        dialog.SetTransientFor(_window);
        dialog.SetModal(true);
        dialog.Title = $"Add {productName}";
        dialog.SetDefaultSize(400, 200);

        var contentArea = (Box)dialog.GetContentArea();
        var box = Box.New(Orientation.Vertical, 10);
        box.MarginStart = 20;
        box.MarginEnd = 20;
        box.MarginTop = 20;
        box.MarginBottom = 20;

        // Mealtime selection
        var mealtimeLabel = Label.New("Select Mealtime:");
        mealtimeLabel.Halign = Align.Start;
        box.Append(mealtimeLabel);

        var mealtimeDropdown = DropDown.NewFromStrings(new[] { "Breakfast", "Lunch", "Dinner" });
        box.Append(mealtimeDropdown);

        // Weight input
        var weightLabel = Label.New("Weight (grams):");
        weightLabel.Halign = Align.Start;
        box.Append(weightLabel);

        var weightSpin = SpinButton.NewWithRange(1, 10000, 1);
        weightSpin.SetValue(100);
        box.Append(weightSpin);

        contentArea.Append(box);

        // Add buttons
        dialog.AddButton("Cancel", (int)ResponseType.Cancel);
        dialog.AddButton("Add", (int)ResponseType.Accept);

        dialog.OnResponse += async (sender, args) =>
        {
            if (args.ResponseId == (int)ResponseType.Accept)
            {
                var selectedIndex = mealtimeDropdown.GetSelected();
                var mealTimeType = selectedIndex switch
                {
                    0 => Lab4.Models.MealTimeType.Breakfast,
                    1 => Lab4.Models.MealTimeType.Lunch,
                    2 => Lab4.Models.MealTimeType.Dinner,
                    _ => Lab4.Models.MealTimeType.Breakfast
                };

                var weight = weightSpin.GetValue();

                // Find the product by name and add it
                await AddProductToMealPlanAsync(productName, mealTimeType, weight);
            }
            dialog.Close();
        };

        dialog.Show();
    }

    private async Task AddProductToMealPlanAsync(string productName, Lab4.Models.MealTimeType mealTimeType, double weight)
    {
        try
        {
            // Search for the product by name
            var products = await _viewModel.SearchProductsAsync(productName);
            var productVm = products.FirstOrDefault(p => p.Name == productName);

            if (productVm != null)
            {
                _viewModel.MealPlan.AddProduct(productVm, mealTimeType, weight);
                BuildMealPlanUI();
                Logger.Instance.Information("Added {Product} to {MealTime}", productName, mealTimeType);
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to add product to meal plan");
        }
    }

    private void BuildMealPlanUI()
    {
        // Clear existing content
        while (_mealPlanBox.GetFirstChild() != null)
        {
            _mealPlanBox.Remove(_mealPlanBox.GetFirstChild()!);
        }

        // Create the DailyMealPlanView and add it to the container
        _mealPlanView = new DailyMealPlanView(_viewModel.MealPlan, _viewModel.CurrentUser);
        _mealPlanBox.Append(_mealPlanView.Widget);
    }

}
