namespace Lab4.ViewModels;

using System.ComponentModel;
using Lab4.Models;

/// <summary>
/// ViewModel wrapper for User Model
/// Provides view-friendly properties and prevents direct Model exposure
/// </summary>
public class UserViewModel : INotifyPropertyChanged
{
    private readonly User _model;

    // Basic properties
    public double Weight
    {
        get => _model.Weight;
        set
        {
            if (_model.Weight != value)
            {
                _model.Weight = value;
                OnPropertyChanged(nameof(Weight));
            }
        }
    }

    public double Height
    {
        get => _model.Height;
        set
        {
            if (_model.Height != value)
            {
                _model.Height = value;
                OnPropertyChanged(nameof(Height));
            }
        }
    }

    public double Age
    {
        get => _model.Age;
        set
        {
            if (_model.Age != value)
            {
                _model.Age = value;
                OnPropertyChanged(nameof(Age));
            }
        }
    }

    public ActivityLevel ActivityLevel
    {
        get => _model.ActivityLevel;
        set
        {
            if (_model.ActivityLevel != value)
            {
                _model.ActivityLevel = value;
                OnPropertyChanged(nameof(ActivityLevel));
            }
        }
    }

    // Calculated properties (read-only)
    public double DailyCalories => _model.DailyCalories;
    public double DailyProtein => _model.DailyProtein;
    public double DailyFat => _model.DailyFat;
    public double DailyCarbohydrates => _model.DailyCarbohydrates;
    public double BMI => _model.BMI;
    public double ARM => _model.ARM;

    // Display properties
    public string WeightDisplay => $"{Weight:F1} kg";
    public string HeightDisplay => $"{Height:F0} cm";
    public string AgeDisplay => $"{Age:F0} years";
    public string DailyCaloriesDisplay => $"{DailyCalories:F0} kcal";
    public string DailyProteinDisplay => $"{DailyProtein:F1}g";
    public string DailyFatDisplay => $"{DailyFat:F1}g";
    public string DailyCarbohydratesDisplay => $"{DailyCarbohydrates:F1}g";
    public string BMIDisplay => $"{BMI:F1}";
    public string ARMDisplay => $"{ARM:F3}";

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserViewModel(User model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Refreshes all calculated properties after nutrition recalculation
    /// </summary>
    public void RefreshCalculatedProperties()
    {
        OnPropertyChanged(nameof(DailyCalories));
        OnPropertyChanged(nameof(DailyProtein));
        OnPropertyChanged(nameof(DailyFat));
        OnPropertyChanged(nameof(DailyCarbohydrates));
        OnPropertyChanged(nameof(BMI));
        OnPropertyChanged(nameof(ARM));
        OnPropertyChanged(nameof(DailyCaloriesDisplay));
        OnPropertyChanged(nameof(DailyProteinDisplay));
        OnPropertyChanged(nameof(DailyFatDisplay));
        OnPropertyChanged(nameof(DailyCarbohydratesDisplay));
        OnPropertyChanged(nameof(BMIDisplay));
        OnPropertyChanged(nameof(ARMDisplay));
    }

    /// <summary>
    /// Gets the underlying model (internal - only for Service access)
    /// </summary>
    internal User GetModel() => _model;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
