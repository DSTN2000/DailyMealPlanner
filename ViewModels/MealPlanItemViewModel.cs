namespace Lab4.ViewModels;

using System.ComponentModel;
using Lab4.Models;

public class MealPlanItemViewModel : INotifyPropertyChanged
{
    private readonly MealPlanItem _model;
    private double _weight;

    // View-friendly properties (NO Model exposure!)
    public string ProductName => _model.Product.Name;
    public string ProductId => _model.Product.Id;

    public double Weight
    {
        get => _weight;
        set
        {
            if (_weight != value && value > 0)
            {
                _weight = value;
                OnPropertyChanged(nameof(Weight));
                OnPropertyChanged(nameof(WeightDisplay));
                // Notify parent that recalculation is needed
                WeightChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string WeightDisplay => $"{Weight:F0}g";

    // Nutritional displays
    public double Calories => _model.Calories;
    public double Protein => _model.Protein;
    public double TotalFat => _model.TotalFat;
    public double Carbohydrates => _model.Carbohydrates;

    public string CaloriesDisplay => $"{Calories:F0} kcal";
    public string ProteinDisplay => $"{Protein:F1}g";
    public string FatDisplay => $"{TotalFat:F1}g";
    public string CarbsDisplay => $"{Carbohydrates:F1}g";
    public string NutritionSummary => $"{CaloriesDisplay} | P: {ProteinDisplay} | F: {FatDisplay} | C: {CarbsDisplay}";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? WeightChanged;

    public MealPlanItemViewModel(MealPlanItem model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _weight = model.Weight;
    }

    /// <summary>
    /// Updates the underlying model with current weight (called by parent ViewModel)
    /// </summary>
    internal void UpdateModel(double newWeight)
    {
        _model.Weight = newWeight;
        // Model nutritional values are recalculated by the service
    }

    /// <summary>
    /// Refreshes display properties after model recalculation (called by parent ViewModel)
    /// </summary>
    internal void RefreshNutrition()
    {
        OnPropertyChanged(nameof(Calories));
        OnPropertyChanged(nameof(Protein));
        OnPropertyChanged(nameof(TotalFat));
        OnPropertyChanged(nameof(Carbohydrates));
        OnPropertyChanged(nameof(CaloriesDisplay));
        OnPropertyChanged(nameof(ProteinDisplay));
        OnPropertyChanged(nameof(FatDisplay));
        OnPropertyChanged(nameof(CarbsDisplay));
        OnPropertyChanged(nameof(NutritionSummary));
    }

    /// <summary>
    /// Gets the underlying model (internal - only for parent ViewModel/Service access)
    /// </summary>
    internal MealPlanItem GetModel() => _model;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
