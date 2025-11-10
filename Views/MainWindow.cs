namespace Lab4.Views;

using Gtk;
using Lab4.ViewModels;

public class MainWindow
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ApplicationWindow _window;
    private Box _contentBox = null!;
    private Box _mealPlanBox = null!;
    private DailyMealPlanView? _mealPlanView;
    private System.Timers.Timer? _searchDebounceTimer;
    private string _lastSearchQuery = string.Empty;
    private List<ProductViewModel> _currentSearchResults = new();
    private int _loadedResultsCount = 0;
    private const int ResultsPageSize = 50;

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

        // Search entry
        var searchEntry = SearchEntry.New();
        searchEntry.SetPlaceholderText("Search products...");
        searchEntry.Hexpand = true;

        // Initialize debounce timer (300ms delay)
        _searchDebounceTimer = new System.Timers.Timer(300)
        {
            AutoReset = false
        };
        _searchDebounceTimer.Elapsed += (sender, args) =>
        {
            var query = searchEntry.GetText();

            // Execute on main thread
            GLib.Functions.IdleAdd(0, () =>
            {
                _ = PerformSearchAsync(query);
                return false;
            });
        };

        // Handle search activation (when user presses Enter)
        searchEntry.OnActivate += async (sender, args) =>
        {
            // Stop debounce timer and search immediately
            _searchDebounceTimer?.Stop();

            var query = searchEntry.GetText();
            await PerformSearchAsync(query);
        };

        // Handle search changed (debounced real-time search)
        searchEntry.OnSearchChanged += (sender, args) =>
        {
            var query = searchEntry.GetText();

            // Stop any previous timer
            _searchDebounceTimer?.Stop();

            // Only search if query is at least 2 characters
            if (string.IsNullOrWhiteSpace(query))
            {
                // Reset last search query and show categories immediately if search is cleared
                _lastSearchQuery = string.Empty;
                UpdateCategoriesView();
                return;
            }

            if (query.Length < 2)
            {
                return;
            }

            // Start debounce timer
            _searchDebounceTimer?.Start();
        };

        searchContainer.Append(searchEntry);
        return searchContainer;
    }

    private async Task PerformSearchAsync(string query)
    {
        // Prevent duplicate searches with the same query
        if (query == _lastSearchQuery)
        {
            return;
        }

        _lastSearchQuery = query;

        if (string.IsNullOrWhiteSpace(query))
        {
            _currentSearchResults.Clear();
            _loadedResultsCount = 0;
            UpdateCategoriesView();
            return;
        }

        try
        {
            // Clear content box
            while (_contentBox.GetFirstChild() != null)
            {
                _contentBox.Remove(_contentBox.GetFirstChild()!);
            }

            // Show loading
            var loadingLabel = Label.New("Searching...");
            _contentBox.Append(loadingLabel);

            // Perform search
            var results = await _viewModel.SearchProductsAsync(query);

            // Store results for lazy loading
            _currentSearchResults = results;
            _loadedResultsCount = 0;

            // Clear loading
            while (_contentBox.GetFirstChild() != null)
            {
                _contentBox.Remove(_contentBox.GetFirstChild()!);
            }

            // Display results
            if (results.Count == 0)
            {
                var noResultsLabel = Label.New($"No products found for '{query}'");
                noResultsLabel.AddCssClass("dim-label");
                _contentBox.Append(noResultsLabel);
            }
            else
            {
                DisplaySearchResults();
            }
        }
        catch (Exception ex)
        {
            // Clear content and show error
            while (_contentBox.GetFirstChild() != null)
            {
                _contentBox.Remove(_contentBox.GetFirstChild()!);
            }

            var errorLabel = Label.New($"Search failed: {ex.Message}");
            errorLabel.AddCssClass("error");
            _contentBox.Append(errorLabel);
        }
    }

    private void DisplaySearchResults()
    {
        // Create scrolled window for results
        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolledWindow.Vexpand = true;

        // Create results box
        var resultsBox = Box.New(Orientation.Vertical, 5);
        resultsBox.AddCssClass("panel-content");

        // Add result count
        var countLabel = Label.New($"Found {_currentSearchResults.Count} products");
        countLabel.AddCssClass("dim-label");
        countLabel.Halign = Align.Start;
        resultsBox.Append(countLabel);

        // Load initial page of results
        LoadMoreResults(resultsBox);

        scrolledWindow.Child = resultsBox;

        // Add scroll event to detect when user reaches bottom
        var scrollAdjustment = scrolledWindow.GetVadjustment();
        scrollAdjustment.OnValueChanged += (sender, args) =>
        {
            var adj = scrolledWindow.GetVadjustment();
            var nearBottom = adj.GetValue() + adj.GetPageSize() >= adj.GetUpper() - 100;

            if (nearBottom && _loadedResultsCount < _currentSearchResults.Count)
            {
                LoadMoreResults(resultsBox);
            }
        };

        _contentBox.Append(scrolledWindow);
    }

    private void LoadMoreResults(Box resultsBox)
    {
        var itemsToLoad = Math.Min(ResultsPageSize, _currentSearchResults.Count - _loadedResultsCount);

        if (itemsToLoad <= 0)
        {
            return;
        }

        var startIndex = _loadedResultsCount;
        var endIndex = startIndex + itemsToLoad;

        for (int i = startIndex; i < endIndex; i++)
        {
            var productVm = _currentSearchResults[i];

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
            var productName = productVm.Name; // Capture for closure
            productButton.OnClicked += (s, e) => ShowAddProductDialog(productName);

            resultsBox.Append(productButton);
        }

        _loadedResultsCount += itemsToLoad;

        // Add "Loading more..." indicator if there are more results
        if (_loadedResultsCount < _currentSearchResults.Count)
        {
            var loadingMoreLabel = Label.New($"Showing {_loadedResultsCount} of {_currentSearchResults.Count} results. Scroll down for more...");
            loadingMoreLabel.AddCssClass("dim-label");
            loadingMoreLabel.Halign = Align.Start;
            resultsBox.Append(loadingMoreLabel);
        }
        else if (_currentSearchResults.Count > ResultsPageSize)
        {
            var allLoadedLabel = Label.New($"All {_currentSearchResults.Count} results loaded");
            allLoadedLabel.AddCssClass("dim-label");
            allLoadedLabel.Halign = Align.Start;
            resultsBox.Append(allLoadedLabel);
        }
    }

}
