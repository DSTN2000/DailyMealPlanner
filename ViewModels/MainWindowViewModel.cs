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

    public UserViewModel CurrentUser { get; private set; }
    public DailyMealPlanViewModel MealPlan { get; private set; }

    public void UpdateMealPlan(DailyMealPlan newPlan)
    {
        MealPlan = new DailyMealPlanViewModel(newPlan, CurrentUser.GetModel());
        OnPropertyChanged(nameof(MealPlan));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel()
    {
        // Load user config from file, or create new if not exists
        var userModel = ConfigurationService.LoadUserConfig() ?? new User();
        CurrentUser = new UserViewModel(userModel);

        // Calculate nutritional needs on startup
        NutritionCalculationService.CalculateNutritionalNeeds(userModel);
        CurrentUser.RefreshCalculatedProperties();

        // Initialize empty meal plan for today
        MealPlan = new DailyMealPlanViewModel(new DailyMealPlan(), userModel);
    }

    public void SaveUserConfiguration()
    {
        var userModel = CurrentUser.GetModel();
        NutritionCalculationService.CalculateNutritionalNeeds(userModel);
        CurrentUser.RefreshCalculatedProperties();
        ConfigurationService.SaveUserConfig(userModel);

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

    public void AddProductToMealTime(ProductViewModel productVm, MealTimeViewModel mealTimeVm, double weight)
    {
        try
        {
            // Get the underlying product model
            var product = productVm.GetModel();

            // Add directly to the meal time
            mealTimeVm.AddItem(product, weight);
            Logger.Instance.Information("Added {Product} ({Weight}g) to {MealTime}", productVm.Name, weight, mealTimeVm.Name);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to add product to meal time");
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
                AddProductToMealTime(productVm, mealTimeVm, weight);
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to add product to meal time");
            throw;
        }
    }
}
