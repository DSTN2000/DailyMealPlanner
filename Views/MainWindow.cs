namespace Lab4.Views;

using Gtk;
using Lab4.ViewModels;
using Lab4.Views.Dialogs;

public class MainWindow
{
    private readonly MainWindowViewModel _viewModel;
    private readonly SearchHandler _searchHandler;
    private readonly ApplicationWindow _window;
    private Box _contentBox = null!;
    private Box _mealPlanBox = null!;
    private DailyMealPlanView? _mealPlanView;

    public MainWindow(Gtk.Application app)
    {
        _viewModel = new MainWindowViewModel();
        _searchHandler = new SearchHandler(_viewModel);
        _window = ApplicationWindow.New(app);
        _window.Title = "Daily Meal Planner";
        _window.SetDefaultSize(900, 600);

        LoadCustomCSS();
        SetupActions();
        SetupSearchHandler();
        BuildUI();
    }

    private void SetupSearchHandler()
    {
        _searchHandler.ResultsUpdated += OnSearchResultsUpdated;
    }

    private void OnSearchResultsUpdated(object? sender, List<ProductViewModel> results)
    {
        // Update UI on main thread
        GLib.Functions.IdleAdd(0, () =>
        {
            UpdateSearchResultsUI(results);
            return false;
        });
    }

    private void SetupActions()
    {
        // Create settings action
        var settingsAction = Gio.SimpleAction.New("settings", null);
        settingsAction.OnActivate += (sender, args) =>
        {
            var preferencesDialog = new PreferencesDialog(_window, _viewModel);
            preferencesDialog.Show();
        };
        _window.AddAction(settingsAction);

        // Create export action
        var exportAction = Gio.SimpleAction.New("export", null);
        exportAction.OnActivate += (sender, args) => ExportMealPlan();
        _window.AddAction(exportAction);

        // Create import action
        var importAction = Gio.SimpleAction.New("import", null);
        importAction.OnActivate += (sender, args) => ImportMealPlan();
        _window.AddAction(importAction);
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
            }
        }
        catch
        {
            // CSS loading failed - continue with default styling
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
        leftPanel.AddCssClass("left-panel");

        // Header
        var headerLabel = Label.New("Food Catalog");
        headerLabel.AddCssClass("title-2");
        headerLabel.Halign = Align.Start;
        leftPanel.Append(headerLabel);

        // Search box
        var searchBox = CreateSearchBox();
        leftPanel.Append(searchBox);

        // Content area (will be updated after data loads)
        _contentBox = Box.New(Orientation.Vertical, 5);
        _contentBox.Vexpand = true;

        var loadingLabel = Label.New("Loading categories...");
        _contentBox.Append(loadingLabel);

        leftPanel.Append(_contentBox);

        // Right panel: Meal Plan
        var rightPanel = Box.New(Orientation.Vertical, 10);
        rightPanel.AddCssClass("right-panel");

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

        // Create File submenu
        var fileMenu = Gio.Menu.New();
        fileMenu.Append("Export Meal Plan...", "win.export");
        fileMenu.Append("Import Meal Plan...", "win.import");
        menu.AppendSubmenu("File", fileMenu);

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

            // Update UI directly (we're already on main thread in GTK)
            UpdateCategoriesView();
        }
        catch (Exception ex)
        {
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
        // Clear loading message
        while (_contentBox.GetFirstChild() != null)
        {
            _contentBox.Remove(_contentBox.GetFirstChild()!);
        }

        // Create scrolled window
        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolledWindow.Vexpand = true;

        // Create a box to hold category views
        var categoriesBox = Box.New(Orientation.Vertical, 5);
        categoriesBox.AddCssClass("panel-content");

        // Create CategoryView for each CategoryViewModel
        foreach (var categoryVm in _viewModel.Categories)
        {
            var categoryView = new CategoryView(categoryVm);
            categoryView.ProductClicked += (s, productVm) =>
            {
                ShowAddProductDialog(productVm.Name);
            };
            categoriesBox.Append(categoryView.Widget);
        }

        scrolledWindow.Child = categoriesBox;
        _contentBox.Append(scrolledWindow);
    }

    private void ShowAddProductDialog(string productName)
    {
        var mealTimes = _viewModel.MealPlan.MealTimes.ToList();
        var dialogVm = new AddProductDialogViewModel(productName, mealTimes);
        var dialog = new AddProductDialog(_window, dialogVm);

        dialog.ProductAdded += async (s, viewModel) =>
        {
            var selectedMealTime = viewModel.GetSelectedMealTime();
            await _viewModel.AddProductToMealTimeAsync(viewModel.ProductName, selectedMealTime, viewModel.Weight);
            BuildMealPlanUI();
        };

        dialog.Show();
    }

    private void BuildMealPlanUI()
    {
        // Clear existing content
        while (_mealPlanBox.GetFirstChild() != null)
        {
            _mealPlanBox.Remove(_mealPlanBox.GetFirstChild()!);
        }

        // Create the DailyMealPlanView and add it to the container
        _mealPlanView = new DailyMealPlanView(_viewModel.MealPlan);
        _mealPlanBox.Append(_mealPlanView.Widget);
    }

    private Box CreateSearchBox()
    {
        var searchContainer = Box.New(Orientation.Vertical, 5);

        var searchEntry = SearchEntry.New();
        searchEntry.SetPlaceholderText("Search products...");
        searchEntry.Hexpand = true;

        // Handle search changed - delegate to SearchHandler
        searchEntry.OnSearchChanged += (sender, args) =>
        {
            var query = searchEntry.GetText();

            if (string.IsNullOrWhiteSpace(query))
            {
                // Show categories when search is cleared
                UpdateCategoriesView();
                return;
            }

            if (query.Length >= 2)
            {
                // Delegate search to SearchHandler
                _searchHandler.Search(query);
            }
        };

        searchContainer.Append(searchEntry);
        return searchContainer;
    }

    private void UpdateSearchResultsUI(List<ProductViewModel> results)
    {
        // Clear content box
        while (_contentBox.GetFirstChild() != null)
        {
            _contentBox.Remove(_contentBox.GetFirstChild()!);
        }

        // Display results
        if (results.Count == 0)
        {
            var noResultsLabel = Label.New("No products found");
            noResultsLabel.AddCssClass("dim-label");
            _contentBox.Append(noResultsLabel);
            return;
        }

        // Create scrolled window for results
        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolledWindow.Vexpand = true;

        // Create results box
        var resultsBox = Box.New(Orientation.Vertical, 5);
        resultsBox.AddCssClass("panel-content");

        // Add result count
        var totalResults = _searchHandler.CurrentResults.Count;
        var countLabel = Label.New($"Showing {results.Count} of {totalResults} products");
        countLabel.AddCssClass("dim-label");
        countLabel.Halign = Align.Start;
        resultsBox.Append(countLabel);

        // Display products
        foreach (var productVm in results)
        {
            var productButton = Button.New();
            productButton.AddCssClass("flat");
            productButton.Hexpand = true;

            var productBox = Box.New(Orientation.Vertical, 3);
            productBox.Halign = Align.Start;

            var nameLabel = Label.New(productVm.Name);
            nameLabel.AddCssClass("product-name");
            nameLabel.Halign = Align.Start;
            nameLabel.Wrap = true;
            productBox.Append(nameLabel);

            var infoLabel = Label.New($"{productVm.CaloriesDisplay} • {productVm.Category}");
            infoLabel.AddCssClass("dim-label");
            infoLabel.Halign = Align.Start;
            productBox.Append(infoLabel);

            productButton.Child = productBox;

            // Handle click
            var productName = productVm.Name;
            productButton.OnClicked += (s, e) => ShowAddProductDialog(productName);

            resultsBox.Append(productButton);
        }

        // Add "Load more" indicator if there are more results
        if (_searchHandler.CanLoadMore())
        {
            var loadMoreLabel = Label.New("Scroll down to load more...");
            loadMoreLabel.AddCssClass("dim-label");
            loadMoreLabel.Halign = Align.Start;
            resultsBox.Append(loadMoreLabel);
        }

        scrolledWindow.Child = resultsBox;

        // Add scroll event to load more results
        var scrollAdjustment = scrolledWindow.GetVadjustment();
        scrollAdjustment.OnValueChanged += async (sender, args) =>
        {
            var adj = scrolledWindow.GetVadjustment();
            var nearBottom = adj.GetValue() + adj.GetPageSize() >= adj.GetUpper() - 100;

            if (nearBottom && _searchHandler.CanLoadMore())
            {
                await _searchHandler.LoadMoreResultsAsync();
            }
        };

        _contentBox.Append(scrolledWindow);
    }

    private void ExportMealPlan()
    {
        var date = _viewModel.MealPlan.GetModel().Date.ToString("yyyy-MM-dd");
        var dialog = FileDialogHelper.CreateSaveDialog(
            "Export Meal Plan to XML",
            $"mealplan_{date}.xml",
            ("XML Files", new[] { "*.xml" }),
            ("All Files", new[] { "*" })
        );

        // Show save dialog
        _ = dialog.SaveAsync(_window).ContinueWith(task =>
        {
            try
            {
                var file = task.Result;
                if (file != null)
                {
                    var filePath = file.GetPath();
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        GLib.Functions.IdleAdd(0, () =>
                        {
                            ExportToFile(filePath);
                            return false;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Services.Logger.Instance.Error(ex, "Failed to export meal plan");
                GLib.Functions.IdleAdd(0, () =>
                        {
                    ShowErrorDialog("Export Failed", $"Failed to export meal plan: {ex.Message}");
                    return false;
                });
            }
        });
    }

    private void ExportToFile(string filePath)
    {
        try
        {
            // Ensure file has .xml extension
            if (!filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                filePath += ".xml";
            }

            var mealPlan = _viewModel.MealPlan.GetModel();
            Services.ExportService.ExportToXml(mealPlan, filePath);

            ShowInfoDialog("Export Successful", $"Meal plan exported to:\n{filePath}");
        }
        catch (Exception ex)
        {
            Services.Logger.Instance.Error(ex, "Failed to export to file: {FilePath}", filePath);
            ShowErrorDialog("Export Failed", $"Failed to export meal plan: {ex.Message}");
        }
    }

    private void ImportMealPlan()
    {
        var dialog = FileDialogHelper.CreateOpenDialog(
            "Import Meal Plan from XML",
            ("XML Files", new[] { "*.xml" }),
            ("All Files", new[] { "*" })
        );

        // Show open dialog
        _ = dialog.OpenAsync(_window).ContinueWith(task =>
        {
            try
            {
                var file = task.Result;
                if (file != null)
                {
                    var filePath = file.GetPath();
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        GLib.Functions.IdleAdd(0, () =>
                        {
                            ImportFromFile(filePath);
                            return false;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Services.Logger.Instance.Error(ex, "Failed to import meal plan");
                GLib.Functions.IdleAdd(0, () =>
                {
                    ShowErrorDialog("Import Failed", $"Failed to import meal plan: {ex.Message}");
                    return false;
                });
            }
        });
    }

    private void ImportFromFile(string filePath)
    {
        try
        {
            // Load the meal plan from XML file
            var loadedPlan = Services.ExportService.ImportFromXml(filePath);

            if (loadedPlan != null)
            {
                // Update the view model with the loaded plan
                _viewModel.UpdateMealPlan(loadedPlan);
                BuildMealPlanUI();

                ShowInfoDialog("Import Successful", $"Meal plan imported from:\n{filePath}");
            }
            else
            {
                ShowErrorDialog("Import Failed", "Failed to load meal plan from file.");
            }
        }
        catch (Exception ex)
        {
            Services.Logger.Instance.Error(ex, "Failed to import from file: {FilePath}", filePath);
            ShowErrorDialog("Import Failed", $"Failed to import meal plan: {ex.Message}");
        }
    }

    private void ShowInfoDialog(string title, string message)
    {
        Services.Logger.Instance.Information("{Title}: {Message}", title, message);
        Dialogs.MessageDialog.ShowInfo(_window, title, message);
    }

    private void ShowErrorDialog(string title, string message)
    {
        Services.Logger.Instance.Error("{Title}: {Message}", title, message);
        Dialogs.MessageDialog.ShowError(_window, title, message);
    }

}
