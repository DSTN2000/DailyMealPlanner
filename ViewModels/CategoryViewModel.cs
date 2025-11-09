namespace Lab4.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using Lab4.Models;
using Lab4.Services;

public class CategoryViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _isLoading;
    private bool _isLoaded;
    private List<ProductViewModel> _allProducts = new();

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        private set
        {
            _isLoaded = value;
            OnPropertyChanged(nameof(IsLoaded));
        }
    }

    public List<ProductViewModel> Products => _allProducts;

    // Grouped products by subcategory (sorted by key)
    private Dictionary<string, List<ProductViewModel>> _subcategoryGroups = new();
    public IOrderedEnumerable<KeyValuePair<string, List<ProductViewModel>>> SubcategoryGroups =>
        _subcategoryGroups.OrderBy(kvp => kvp.Key);

    // Presentation logic property
    public bool ShouldUseVirtualization(int productCount) => productCount > 50;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ProductsLoaded;

    public CategoryViewModel(string categoryName)
    {
        Name = categoryName;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async Task LoadProductsAsync()
    {
        if (IsLoaded || IsLoading)
            return;

        IsLoading = true;
        try
        {
            Logger.Instance.Information("Loading products for category: {Category}", Name);
            var products = await CatalogService.GetProductsByCategoryAsync(Name);
            _allProducts = products.Select(p => new ProductViewModel(p)).ToList();

            // Group by subcategories
            _subcategoryGroups = GroupBySubcategories(_allProducts);

            IsLoaded = true;
            OnPropertyChanged(nameof(Products));
            OnPropertyChanged(nameof(SubcategoryGroups));
            ProductsLoaded?.Invoke(this, EventArgs.Empty);

            Logger.Instance.Information("Loaded {Count} products in {SubcategoryCount} subcategories for {Category}",
                _allProducts.Count, _subcategoryGroups.Count, Name);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to load products for {Category}", Name);
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Dictionary<string, List<ProductViewModel>> GroupBySubcategories(List<ProductViewModel> products)
    {
        var grouped = new Dictionary<string, List<ProductViewModel>>();

        foreach (var product in products)
        {
            string subcategory = "other";
            if (product.Labels.Count > 0)
            {
                subcategory = product.Labels[0];
            }

            if (!grouped.ContainsKey(subcategory))
            {
                grouped[subcategory] = new List<ProductViewModel>();
            }
            grouped[subcategory].Add(product);
        }

        return grouped;
    }
}
