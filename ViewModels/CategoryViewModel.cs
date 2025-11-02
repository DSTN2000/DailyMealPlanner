namespace Lab4.ViewModels;

using System.Collections.ObjectModel;
using Lab4.Models;

public class CategoryViewModel
{
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public ObservableCollection<ProductViewModel> Products { get; set; } = new();

    public CategoryViewModel() { }

    public CategoryViewModel(CategoryGroup categoryGroup)
    {
        Name = categoryGroup.Name;
        ProductCount = categoryGroup.Products.Count;
        Products = new ObservableCollection<ProductViewModel>(
            categoryGroup.Products.Select(p => new ProductViewModel(p))
        );
    }
}
