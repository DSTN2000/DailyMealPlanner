namespace Lab4.ViewModels;

using System.ComponentModel;
using Lab4.Models;
using Lab4.Services;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private List<CategoryViewModel> _categories = new();
    private bool _isLoading;

    public List<CategoryViewModel> Categories
    {
        get => _categories;
        set
        {
            _categories = value;
            OnPropertyChanged(nameof(Categories));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public User CurrentUser { get; private set; }
    public DailyMealPlanViewModel MealPlan { get; private set; }

    public void UpdateMealPlan(DailyMealPlan newPlan)
    {
        MealPlan = new DailyMealPlanViewModel(newPlan, CurrentUser);
        OnPropertyChanged(nameof(MealPlan));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel()
    {
        // Load user config from file, or create new if not exists
        CurrentUser = ConfigurationService.LoadUserConfig() ?? new User();

        // Calculate nutritional needs on startup
        NutritionCalculationService.CalculateNutritionalNeeds(CurrentUser);

        // Initialize meal plan for today
        var savedPlan = MealPlanService.LoadMealPlan(DateTime.Today);
        MealPlan = new DailyMealPlanViewModel(savedPlan ?? new DailyMealPlan(), CurrentUser);
    }

    public void SaveUserConfiguration()
    {
        ConfigurationService.SaveUserConfig(CurrentUser);

        // Refresh meal plan goals to update progress displays
        MealPlan.RefreshGoals();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async Task LoadCategoriesAsync()
    {
        IsLoading = true;
        try
        {
            Logger.Instance.Information("Loading categories...");
            var categoryNames = await CatalogService.GetCategoriesAsync();
            Categories = categoryNames.Select(name => new CategoryViewModel(name)).ToList();
            Logger.Instance.Information("Categories loaded successfully");
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to load categories");
        }
        finally
        {
            IsLoading = false;
        }
    }


    public void SaveMealPlan()
    {
        try
        {
            MealPlanService.SaveMealPlan(MealPlan.GetModel());
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to save meal plan");
        }
    }

    public void LoadMealPlan(DateTime date)
    {
        try
        {
            var plan = MealPlanService.LoadMealPlan(date);
            if (plan != null)
            {
                MealPlan = new DailyMealPlanViewModel(plan, CurrentUser);
                OnPropertyChanged(nameof(MealPlan));
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to load meal plan");
        }
    }

    public async Task<List<ProductViewModel>> SearchProductsAsync(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return new List<ProductViewModel>();
        }

        Logger.Instance.Information("Searching products with query: {Query}", searchQuery);
        var products = await CatalogService.SearchProductsAsync(searchQuery);
        return products.Select(p => new ProductViewModel(p)).ToList();
    }

    public async Task AddProductToMealPlanAsync(string productName, MealTimeType mealTimeType, double weight)
    {
        try
        {
            // Search for the product by name
            var products = await SearchProductsAsync(productName);
            var productVm = products.FirstOrDefault(p => p.Name == productName);

            if (productVm != null)
            {
                MealPlan.AddProduct(productVm, mealTimeType, weight);
                Logger.Instance.Information("Added {Product} ({Weight}g) to {MealTime}", productName, weight, mealTimeType);
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to add product to meal plan");
            throw;
        }
    }

    public async Task AddProductToMealTimeAsync(string productName, MealTimeViewModel mealTimeVm, double weight)
    {
        try
        {
            // Search for the product by name
            var products = await SearchProductsAsync(productName);
            var productVm = products.FirstOrDefault(p => p.Name == productName);

            if (productVm != null)
            {
                // Get the underlying product model
                var product = productVm.GetModel();

                // Add directly to the meal time
                mealTimeVm.AddItem(product, weight);
                Logger.Instance.Information("Added {Product} ({Weight}g) to {MealTime}", productName, weight, mealTimeVm.Name);
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to add product to meal time");
            throw;
        }
    }
}
