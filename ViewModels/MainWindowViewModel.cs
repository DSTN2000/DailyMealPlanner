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

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel()
    {
        CurrentUser = new User();
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
}
