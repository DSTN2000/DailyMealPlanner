namespace Lab4.ViewModels;

using System.ComponentModel;
using Lab4.Models;
using Lab4.Services;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private List<CategoryGroup> _categories = new();
    private bool _isLoading;

    public List<CategoryGroup> Categories
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

    public event PropertyChangedEventHandler? PropertyChanged;

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
}
