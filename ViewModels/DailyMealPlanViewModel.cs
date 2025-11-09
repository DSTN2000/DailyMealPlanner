namespace Lab4.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using Lab4.Models;
using Lab4.Services;

public class DailyMealPlanViewModel : INotifyPropertyChanged
{
    private DailyMealPlan _mealPlan;
    private bool _isRecalculating = false;

    public DailyMealPlan MealPlan
    {
        get => _mealPlan;
        set
        {
            _mealPlan = value;
            OnPropertyChanged(nameof(MealPlan));
            OnPropertyChanged(nameof(Date));
            Recalculate();
        }
    }

    public DateTime Date
    {
        get => MealPlan?.Date ?? DateTime.Today;
        set
        {
            if (MealPlan != null)
            {
                MealPlan.Date = value;
                OnPropertyChanged(nameof(Date));
            }
        }
    }

    public ObservableCollection<MealTime> MealTimes => new(MealPlan?.MealTimes ?? new List<MealTime>());

    // Display properties
    public double TotalCalories => MealPlan?.TotalCalories ?? 0;
    public double TotalProtein => MealPlan?.TotalProtein ?? 0;
    public double TotalFat => MealPlan?.TotalFat ?? 0;
    public double TotalCarbohydrates => MealPlan?.TotalCarbohydrates ?? 0;

    public string TotalCaloriesDisplay => $"{TotalCalories:F0} kcal";
    public string TotalProteinDisplay => $"P: {TotalProtein:F1}g";
    public string TotalFatDisplay => $"F: {TotalFat:F1}g";
    public string TotalCarbsDisplay => $"C: {TotalCarbohydrates:F1}g";

    public event PropertyChangedEventHandler? PropertyChanged;

    public DailyMealPlanViewModel()
    {
        _mealPlan = new DailyMealPlan();
    }

    public DailyMealPlanViewModel(DailyMealPlan mealPlan)
    {
        _mealPlan = mealPlan;
    }

    /// <summary>
    /// Adds a product to a specific mealtime
    /// </summary>
    public void AddProduct(Product product, MealTimeType mealTimeType, double weight = 100.0)
    {
        var mealTime = MealPlan.MealTimes.FirstOrDefault(mt => mt.Type == mealTimeType);
        if (mealTime != null)
        {
            var item = new MealPlanItem(product, weight);
            mealTime.Items.Add(item);
            Logger.Instance.Information("Added {Product} ({Weight}g) to {MealTime}", product.Name, weight, mealTime.Name);
            Recalculate();
        }
    }

    /// <summary>
    /// Removes an item from a mealtime
    /// </summary>
    public void RemoveItem(MealTime mealTime, MealPlanItem item)
    {
        mealTime.Items.Remove(item);
        Logger.Instance.Information("Removed item from {MealTime}", mealTime.Name);
        Recalculate();
    }

    /// <summary>
    /// Updates the weight of an item
    /// </summary>
    public void UpdateItemWeight(MealPlanItem item, double newWeight)
    {
        if (newWeight > 0)
        {
            item.Weight = newWeight;
            Logger.Instance.Information("Updated item weight to {Weight}g", newWeight);
            Recalculate();
        }
    }

    /// <summary>
    /// Adds a custom mealtime
    /// </summary>
    public void AddCustomMealTime(string name)
    {
        var mealTime = new MealTime(MealTimeType.Custom, name);
        MealPlan.MealTimes.Add(mealTime);
        Logger.Instance.Information("Added custom mealtime: {Name}", name);
        OnPropertyChanged(nameof(MealTimes));
        Recalculate();
    }

    /// <summary>
    /// Removes a mealtime (only custom ones can be removed)
    /// </summary>
    public void RemoveMealTime(MealTime mealTime)
    {
        if (mealTime.Type == MealTimeType.Custom)
        {
            MealPlan.MealTimes.Remove(mealTime);
            Logger.Instance.Information("Removed custom mealtime: {Name}", mealTime.Name);
            OnPropertyChanged(nameof(MealTimes));
            Recalculate();
        }
    }

    /// <summary>
    /// Recalculates all nutritional totals (with re-entrancy guard)
    /// </summary>
    public void Recalculate()
    {
        if (_isRecalculating) return;

        try
        {
            _isRecalculating = true;
            NutritionCalculationService.RecalculateMealPlan(MealPlan);

            OnPropertyChanged(nameof(MealTimes));
            OnPropertyChanged(nameof(TotalCalories));
            OnPropertyChanged(nameof(TotalProtein));
            OnPropertyChanged(nameof(TotalFat));
            OnPropertyChanged(nameof(TotalCarbohydrates));
            OnPropertyChanged(nameof(TotalCaloriesDisplay));
            OnPropertyChanged(nameof(TotalProteinDisplay));
            OnPropertyChanged(nameof(TotalFatDisplay));
            OnPropertyChanged(nameof(TotalCarbsDisplay));
        }
        finally
        {
            _isRecalculating = false;
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
