namespace Lab4.ViewModels;

using Lab4.Models;

public class ProductViewModel
{
    private readonly Product _model;

    // View-friendly properties (NO Model exposure!)
    public string Name => _model.Name;
    public string Id => _model.Id;
    public string Category => _model.Category;
    public List<string> Labels => _model.Labels;
    public double Calories => _model.Calories;
    public double Protein => _model.Protein;
    public double TotalFat => _model.TotalFat;
    public double Carbohydrates => _model.Carbohydrates;
    public double Sodium => _model.Sodium;
    public double Fiber => _model.Fiber;
    public double Sugar => _model.Sugar;

    public string CaloriesDisplay => Calories > 0 ? $"{Calories:F0} kcal" : string.Empty;
    public string ProteinDisplay => Protein > 0 ? $"P: {Protein:F1}g" : string.Empty;
    public string FatDisplay => TotalFat > 0 ? $"F: {TotalFat:F1}g" : string.Empty;
    public string CarbsDisplay => Carbohydrates > 0 ? $"C: {Carbohydrates:F1}g" : string.Empty;
    public string NutritionSummary => $"{CaloriesDisplay} | {ProteinDisplay} | {FatDisplay} | {CarbsDisplay}";

    public ProductViewModel(Product product)
    {
        _model = product ?? throw new ArgumentNullException(nameof(product));
    }

    /// <summary>
    /// Gets the underlying model (internal - only for parent ViewModel access)
    /// </summary>
    internal Product GetModel() => _model;
}
