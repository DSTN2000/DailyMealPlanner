namespace Lab4.ViewModels;

using Lab4.Models;

public class ProductViewModel
{
    public string Name { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double TotalFat { get; set; }
    public double Carbohydrates { get; set; }

    public string CaloriesDisplay => Calories > 0 ? $"{Calories:F0} kcal" : string.Empty;
    public string ProteinDisplay => Protein > 0 ? $"P: {Protein:F1}g" : string.Empty;
    public string FatDisplay => TotalFat > 0 ? $"F: {TotalFat:F1}g" : string.Empty;
    public string CarbsDisplay => Carbohydrates > 0 ? $"C: {Carbohydrates:F1}g" : string.Empty;

    public ProductViewModel() { }

    public ProductViewModel(Product product)
    {
        Name = product.Name;
        Labels = product.Labels;
        Calories = product.Calories;
        Protein = product.Protein;
        TotalFat = product.TotalFat;
        Carbohydrates = product.Carbohydrates;
    }
}
