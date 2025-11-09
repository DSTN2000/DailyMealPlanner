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

            .calculated-value {
                color: #3584e4;
                font-weight: bold;
            }

            /* Progress bar colors */
            progressbar.success trough progress {
                background-color: #26a269;
                background-image: linear-gradient(to bottom, #33d17a, #26a269);
            }

            progressbar.warning trough progress {
                background-color: #f57900;
                background-image: linear-gradient(to bottom, #ff9e00, #f57900);
            }

            progressbar.error trough progress {
                background-color: #c01c28;
                background-image: linear-gradient(to bottom, #e01b24, #c01c28);
            }

            progressbar trough {
                background-color: alpha(@theme_fg_color, 0.15);
                min-height: 16px;
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
            nameLabel.SetText($"{name} ({calories:F0} kcal)");
        }

        // Make product clickable to add to meal plan
        var gesture = GestureClick.New();
        gesture.OnReleased += (sender, args) =>
        {
            // Show dialog to select mealtime and weight
            ShowAddProductDialog(name);
        };
        card.AddController(gesture);
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
            var product = products.FirstOrDefault(p => p.Name == productName);

            if (product?.Product != null)
            {
                _viewModel.MealPlan.AddProduct(product.Product, mealTimeType, weight);
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

        // Daily totals card (sticky at top)
        var dailyTotalsBox = CreateDailyTotalsSection();
        _mealPlanBox.Append(dailyTotalsBox);

        // Create scrolled window for meal plan
        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolledWindow.Vexpand = true;

        // Create container for mealtimes
        var mealPlanContainer = Box.New(Orientation.Vertical, 10);
        mealPlanContainer.MarginStart = 5;
        mealPlanContainer.MarginEnd = 5;
        mealPlanContainer.MarginTop = 5;
        mealPlanContainer.MarginBottom = 5;

        // Create section for each mealtime
        foreach (var mealTime in _viewModel.MealPlan.MealTimes)
        {
            var mealTimeBox = CreateMealTimeSection(mealTime);
            mealPlanContainer.Append(mealTimeBox);
        }

        scrolledWindow.Child = mealPlanContainer;
        _mealPlanBox.Append(scrolledWindow);
    }

    private Box CreateMealTimeSection(Lab4.Models.MealTime mealTime)
    {
        var section = Box.New(Orientation.Vertical, 5);
        section.AddCssClass("card");
        section.MarginTop = 5;
        section.MarginBottom = 5;

        // Header with mealtime name
        var header = Label.New(mealTime.Name);
        header.AddCssClass("title-3");
        header.Halign = Align.Start;
        header.MarginStart = 12;
        header.MarginTop = 8;
        section.Append(header);

        // Items list
        if (mealTime.Items.Count > 0)
        {
            foreach (var item in mealTime.Items)
            {
                var itemBox = CreateMealPlanItemRow(mealTime, item);
                section.Append(itemBox);
            }
        }
        else
        {
            var emptyLabel = Label.New("No items yet");
            emptyLabel.AddCssClass("dim-label");
            emptyLabel.Halign = Align.Start;
            emptyLabel.MarginStart = 12;
            emptyLabel.MarginBottom = 8;
            section.Append(emptyLabel);
        }

        // Mealtime totals
        var totalsLabel = Label.New($"{mealTime.TotalCalories:F0} kcal | P: {mealTime.TotalProtein:F1}g | F: {mealTime.TotalFat:F1}g | C: {mealTime.TotalCarbohydrates:F1}g");
        totalsLabel.AddCssClass("calculated-value");
        totalsLabel.Halign = Align.Start;
        totalsLabel.MarginStart = 12;
        totalsLabel.MarginBottom = 8;
        section.Append(totalsLabel);

        return section;
    }

    private Box CreateMealPlanItemRow(Lab4.Models.MealTime mealTime, Lab4.Models.MealPlanItem item)
    {
        var row = Box.New(Orientation.Horizontal, 10);
        row.MarginStart = 12;
        row.MarginEnd = 12;
        row.MarginTop = 4;
        row.MarginBottom = 4;

        // Product name
        var nameLabel = Label.New(item.Product.Name);
        nameLabel.Halign = Align.Start;
        nameLabel.Hexpand = true;
        nameLabel.SetEllipsize(Pango.EllipsizeMode.End);
        row.Append(nameLabel);

        // Weight spinner
        var weightSpin = SpinButton.NewWithRange(1, 10000, 1);
        weightSpin.SetValue(item.Weight);
        weightSpin.OnValueChanged += (sender, args) =>
        {
            var newWeight = weightSpin.GetValue();
            _viewModel.MealPlan.UpdateItemWeight(item, newWeight);
            BuildMealPlanUI(); // Rebuild to update totals
        };
        row.Append(weightSpin);

        var gramsLabel = Label.New("g");
        row.Append(gramsLabel);

        // Remove button
        var removeButton = Button.NewWithLabel("Remove");
        removeButton.AddCssClass("destructive-action");
        removeButton.OnClicked += (sender, args) =>
        {
            _viewModel.MealPlan.RemoveItem(mealTime, item);
            BuildMealPlanUI();
        };
        row.Append(removeButton);

        return row;
    }

    private Box CreateDailyTotalsSection()
    {
        var section = Box.New(Orientation.Vertical, 10);
        section.AddCssClass("card");
        section.MarginStart = 5;
        section.MarginEnd = 5;
        section.MarginTop = 5;
        section.MarginBottom = 10;

        var header = Label.New("Daily Progress");
        header.AddCssClass("title-3");
        header.Halign = Align.Start;
        header.MarginStart = 12;
        header.MarginTop = 8;
        section.Append(header);

        // Current vs Goal
        var actualCalories = _viewModel.MealPlan.MealPlan.TotalCalories;
        var goalCalories = _viewModel.CurrentUser.DailyCalories;
        var percentage = goalCalories > 0 ? (actualCalories / goalCalories) * 100 : 0;

        var statusLabel = Label.New($"{actualCalories:F0} / {goalCalories:F0} kcal ({percentage:F0}%)");
        statusLabel.AddCssClass("title-4");
        statusLabel.Halign = Align.Start;
        statusLabel.MarginStart = 12;
        section.Append(statusLabel);

        // Progress bar with dynamic color
        var progressBar = ProgressBar.New();
        progressBar.SetFraction(Math.Min(actualCalories / goalCalories, 1.0));
        progressBar.MarginStart = 12;
        progressBar.MarginEnd = 12;
        progressBar.MarginBottom = 5;

        // Determine color based on how close to goal
        // Red if too far (< 80% or > 120%), yellow if close (80-90% or 110-120%), green if optimal (90-110%)
        string colorClass;
        if (percentage < 80 || percentage > 120)
        {
            colorClass = "error"; // Red
        }
        else if (percentage < 90 || percentage > 110)
        {
            colorClass = "warning"; // Yellow/Orange
        }
        else
        {
            colorClass = "success"; // Green
        }
        progressBar.AddCssClass(colorClass);
        section.Append(progressBar);

        // Nutritional breakdown
        var macrosLabel = Label.New($"{_viewModel.MealPlan.TotalProteinDisplay} | {_viewModel.MealPlan.TotalFatDisplay} | {_viewModel.MealPlan.TotalCarbsDisplay}");
        macrosLabel.AddCssClass("dim-label");
        macrosLabel.Halign = Align.Start;
        macrosLabel.MarginStart = 12;
        macrosLabel.MarginBottom = 8;
        section.Append(macrosLabel);

        return section;
    }
}
