namespace Lab4.ViewModels;

using System.ComponentModel;
using Lab4.Models;
using Lab4.Services;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private List<string> _categories = new();
    private bool _isLoading;

    public List<string> Categories
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel()
    {
        // Load user config from file, or create new if not exists
        CurrentUser = ConfigurationService.LoadUserConfig() ?? new User();

        // Calculate nutritional needs on startup
        NutritionCalculationService.CalculateNutritionalNeeds(CurrentUser);

        // Initialize meal plan for today
        var savedPlan = MealPlanService.LoadMealPlan(DateTime.Today);
        MealPlan = new DailyMealPlanViewModel(savedPlan ?? new DailyMealPlan());
    }

    public void SaveUserConfiguration()
    {
        ConfigurationService.SaveUserConfig(CurrentUser);
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
            Categories = await CatalogService.GetCategoriesAsync();
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

    public async Task<List<ProductViewModel>> LoadProductsForCategoryAsync(string categoryName)
    {
        Logger.Instance.Information("Loading products for category: {Category}", categoryName);
        var products = await CatalogService.GetProductsByCategoryAsync(categoryName);
        return products.Select(p => new ProductViewModel(p)).ToList();
    }

    public async Task<Product?> GetProductByIdAsync(string productId)
    {
        return await CatalogService.GetProductByIdAsync(productId);
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
                MealPlan = new DailyMealPlanViewModel(plan);
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
}
