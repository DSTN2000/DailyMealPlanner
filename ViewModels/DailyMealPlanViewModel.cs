namespace Lab4.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using Lab4.Models;
using Lab4.Services;

public class DailyMealPlanViewModel : INotifyPropertyChanged
{
    private readonly DailyMealPlan _model;
    private ObservableCollection<MealTimeViewModel> _mealTimes;
    private bool _isRecalculating = false;
    private User? _currentUser;

    // View-friendly properties (NO Model exposure!)
    public DateTime Date
    {
        get => _model.Date;
        set
        {
            if (_model.Date != value)
            {
                _model.Date = value;
                OnPropertyChanged(nameof(Date));
                OnPropertyChanged(nameof(DateDisplay));
            }
        }
    }

    public string DateDisplay => Date.ToString("dddd, MMMM dd, yyyy");

    public ObservableCollection<MealTimeViewModel> MealTimes
    {
        get => _mealTimes;
        private set
        {
            _mealTimes = value;
            OnPropertyChanged(nameof(MealTimes));
        }
    }

    // Nutritional totals
    public double TotalCalories => _model.TotalCalories;
    public double TotalProtein => _model.TotalProtein;
    public double TotalFat => _model.TotalFat;
    public double TotalCarbohydrates => _model.TotalCarbohydrates;

    public string TotalCaloriesDisplay => $"{TotalCalories:F0} kcal";
    public string TotalProteinDisplay => $"P: {TotalProtein:F1}g";
    public string TotalFatDisplay => $"F: {TotalFat:F1}g";
    public string TotalCarbsDisplay => $"C: {TotalCarbohydrates:F1}g";

    // Goal values from User (for progress tracking)
    public double GoalCalories => _currentUser?.DailyCalories ?? 2000;
    public double GoalProtein => _currentUser?.DailyProtein ?? 150;
    public double GoalFat => _currentUser?.DailyFat ?? 67;
    public double GoalCarbohydrates => _currentUser?.DailyCarbohydrates ?? 225;

    // Progress calculations
    public double CalorieProgressPercentage => GoalCalories > 0 ? (TotalCalories / GoalCalories) * 100 : 0;
    public double CalorieProgressFraction => GoalCalories > 0 ? Math.Min(TotalCalories / GoalCalories, 1.0) : 0;
    public string CalorieProgressDisplay => $"{TotalCalories:F0} / {GoalCalories:F0} kcal ({CalorieProgressPercentage:F0}%)";
    public string CalorieProgressColorClass => GetProgressColorClass(CalorieProgressPercentage);

    public double ProteinProgressPercentage => GoalProtein > 0 ? (TotalProtein / GoalProtein) * 100 : 0;
    public double ProteinProgressFraction => GoalProtein > 0 ? Math.Min(TotalProtein / GoalProtein, 1.0) : 0;
    public string ProteinProgressDisplay => $"P: {TotalProtein:F0}/{GoalProtein:F0}g";
    public string ProteinProgressColorClass => GetProgressColorClass(ProteinProgressPercentage);

    public double FatProgressPercentage => GoalFat > 0 ? (TotalFat / GoalFat) * 100 : 0;
    public double FatProgressFraction => GoalFat > 0 ? Math.Min(TotalFat / GoalFat, 1.0) : 0;
    public string FatProgressDisplay => $"F: {TotalFat:F0}/{GoalFat:F0}g";
    public string FatProgressColorClass => GetProgressColorClass(FatProgressPercentage);

    public double CarbsProgressPercentage => GoalCarbohydrates > 0 ? (TotalCarbohydrates / GoalCarbohydrates) * 100 : 0;
    public double CarbsProgressFraction => GoalCarbohydrates > 0 ? Math.Min(TotalCarbohydrates / GoalCarbohydrates, 1.0) : 0;
    public string CarbsProgressDisplay => $"C: {TotalCarbohydrates:F0}/{GoalCarbohydrates:F0}g";
    public string CarbsProgressColorClass => GetProgressColorClass(CarbsProgressPercentage);

    public event PropertyChangedEventHandler? PropertyChanged;

    public DailyMealPlanViewModel() : this(new DailyMealPlan(), null)
    {
    }

    public DailyMealPlanViewModel(DailyMealPlan model, User? currentUser = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _currentUser = currentUser;

        // Wrap all model mealtimes in ViewModels
        _mealTimes = new ObservableCollection<MealTimeViewModel>(
            model.MealTimes.Select(mt => new MealTimeViewModel(mt))
        );

        // Subscribe to child ViewModel changes
        foreach (var mealTimeVm in _mealTimes)
        {
            mealTimeVm.ItemsChanged += (s, e) => Recalculate();
        }
    }

    /// <summary>
    /// Adds a product to a specific mealtime
    /// </summary>
    public void AddProduct(ProductViewModel productVm, MealTimeType mealTimeType, double weight = 100.0)
    {
        if (productVm == null) throw new ArgumentNullException(nameof(productVm));

        var mealTimeVm = MealTimes.FirstOrDefault(mt => mt.Type == mealTimeType);
        if (mealTimeVm != null)
        {
            // Get the underlying product (internal access only)
            var product = productVm.GetModel();
            mealTimeVm.AddItem(product, weight);

            Logger.Instance.Information("Added {Product} ({Weight}g) to {MealTime}", product.Name, weight, mealTimeVm.Name);
            Recalculate();
        }
    }

    /// <summary>
    /// Removes an item from a mealtime
    /// </summary>
    public void RemoveItem(MealTimeViewModel mealTimeVm, MealPlanItemViewModel itemVm)
    {
        if (mealTimeVm == null || itemVm == null) return;

        mealTimeVm.RemoveItem(itemVm);
        Logger.Instance.Information("Removed item from {MealTime}", mealTimeVm.Name);
        Recalculate();
    }

    /// <summary>
    /// Updates the weight of an item
    /// </summary>
    public void UpdateItemWeight(MealPlanItemViewModel itemVm, double newWeight)
    {
        if (itemVm == null || newWeight <= 0) return;

        itemVm.UpdateModel(newWeight);
        itemVm.Weight = newWeight;  // Triggers WeightChanged event → ItemsChanged → Recalculate
        Logger.Instance.Information("Updated item weight to {Weight}g", newWeight);
    }

    /// <summary>
    /// Adds a custom mealtime
    /// </summary>
    public void AddCustomMealTime(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var modelMealTime = new MealTime(MealTimeType.Custom, name);
        _model.MealTimes.Add(modelMealTime);

        var mealTimeVm = new MealTimeViewModel(modelMealTime);
        mealTimeVm.ItemsChanged += (s, e) => Recalculate();

        MealTimes.Add(mealTimeVm);
        Logger.Instance.Information("Added custom mealtime: {Name}", name);

        Recalculate();
    }

    /// <summary>
    /// Removes a mealtime (only custom ones can be removed)
    /// </summary>
    public void RemoveMealTime(MealTimeViewModel mealTimeVm)
    {
        if (mealTimeVm == null || !mealTimeVm.CanRemove) return;

        var modelMealTime = mealTimeVm.GetModel();
        _model.MealTimes.Remove(modelMealTime);
        MealTimes.Remove(mealTimeVm);

        Logger.Instance.Information("Removed custom mealtime: {Name}", mealTimeVm.Name);
        Recalculate();
    }

    /// <summary>
    /// Recalculates all nutritional totals (with re-entrancy guard)
    /// </summary>
    private void Recalculate()
    {
        if (_isRecalculating) return;

        try
        {
            _isRecalculating = true;

            // Recalculate using service
            NutritionCalculationService.RecalculateMealPlan(_model);

            // Refresh all child ViewModels
            foreach (var mealTimeVm in MealTimes)
            {
                mealTimeVm.RefreshNutrition();
            }

            // Notify property changes
            OnPropertyChanged(nameof(TotalCalories));
            OnPropertyChanged(nameof(TotalProtein));
            OnPropertyChanged(nameof(TotalFat));
            OnPropertyChanged(nameof(TotalCarbohydrates));
            OnPropertyChanged(nameof(TotalCaloriesDisplay));
            OnPropertyChanged(nameof(TotalProteinDisplay));
            OnPropertyChanged(nameof(TotalFatDisplay));
            OnPropertyChanged(nameof(TotalCarbsDisplay));

            // Notify progress properties
            OnPropertyChanged(nameof(CalorieProgressPercentage));
            OnPropertyChanged(nameof(CalorieProgressFraction));
            OnPropertyChanged(nameof(CalorieProgressDisplay));
            OnPropertyChanged(nameof(CalorieProgressColorClass));
            OnPropertyChanged(nameof(ProteinProgressPercentage));
            OnPropertyChanged(nameof(ProteinProgressFraction));
            OnPropertyChanged(nameof(ProteinProgressDisplay));
            OnPropertyChanged(nameof(ProteinProgressColorClass));
            OnPropertyChanged(nameof(FatProgressPercentage));
            OnPropertyChanged(nameof(FatProgressFraction));
            OnPropertyChanged(nameof(FatProgressDisplay));
            OnPropertyChanged(nameof(FatProgressColorClass));
            OnPropertyChanged(nameof(CarbsProgressPercentage));
            OnPropertyChanged(nameof(CarbsProgressFraction));
            OnPropertyChanged(nameof(CarbsProgressDisplay));
            OnPropertyChanged(nameof(CarbsProgressColorClass));
        }
        finally
        {
            _isRecalculating = false;
        }
    }

    /// <summary>
    /// Determines CSS color class based on progress percentage
    /// </summary>
    private static string GetProgressColorClass(double percentage)
    {
        if (percentage < 80 || percentage > 120)
            return "error";
        else if (percentage < 90 || percentage > 110)
            return "warning";
        else
            return "success";
    }

    /// <summary>
    /// Gets the underlying model (internal - only for Service access when saving)
    /// </summary>
    internal DailyMealPlan GetModel() => _model;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
