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
        var dialogVm = new AddProductDialogViewModel(productName);
        var dialog = new AddProductDialog(_window, dialogVm);

        dialog.ProductAdded += async (s, viewModel) =>
        {
            var mealTimeType = viewModel.GetMealTimeType();
            await _viewModel.AddProductToMealPlanAsync(viewModel.ProductName, mealTimeType, viewModel.Weight);
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

}
