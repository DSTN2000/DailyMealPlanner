namespace Lab4.ViewModels;

using System.ComponentModel;
using Lab4.Models;
using Lab4.Services;

public class PreferencesViewModel : INotifyPropertyChanged
{
    private readonly UserViewModel _userViewModel;
    private readonly Action _onConfigurationSaved;

    // Wrapped UserViewModel properties - NO direct Model exposure
    public double Weight
    {
        get => _userViewModel.Weight;
        set
        {
            if (_userViewModel.Weight != value)
            {
                _userViewModel.Weight = value;
                OnPropertyChanged(nameof(Weight));
                OnPropertyChanged(nameof(WeightText));
            }
        }
    }

    public double Height
    {
        get => _userViewModel.Height;
        set
        {
            if (_userViewModel.Height != value)
            {
                _userViewModel.Height = value;
                OnPropertyChanged(nameof(Height));
                OnPropertyChanged(nameof(HeightText));
            }
        }
    }

    public double Age
    {
        get => _userViewModel.Age;
        set
        {
            if (_userViewModel.Age != value)
            {
                _userViewModel.Age = value;
                OnPropertyChanged(nameof(Age));
                OnPropertyChanged(nameof(AgeText));
            }
        }
    }

    public int ActivityLevelIndex
    {
        get => (int)_userViewModel.ActivityLevel;
        set
        {
            if ((int)_userViewModel.ActivityLevel != value)
            {
                _userViewModel.ActivityLevel = (ActivityLevel)value;
                OnPropertyChanged(nameof(ActivityLevelIndex));
            }
        }
    }

    // Text representations for Entry fields
    public string WeightText
    {
        get => Weight > 0 ? Weight.ToString("F1") : string.Empty;
        set => TryParseAndSetWeight(value);
    }

    public string HeightText
    {
        get => Height > 0 ? Height.ToString("F1") : string.Empty;
        set => TryParseAndSetHeight(value);
    }

    public string AgeText
    {
        get => Age > 0 ? Age.ToString("F0") : string.Empty;
        set => TryParseAndSetAge(value);
    }

    // Calculated values (read-only)
    public double DailyCalories => _userViewModel.DailyCalories;
    public double DailyProtein => _userViewModel.DailyProtein;
    public double DailyFat => _userViewModel.DailyFat;
    public double DailyCarbohydrates => _userViewModel.DailyCarbohydrates;
    public double BMI => _userViewModel.BMI;
    public double ARM => _userViewModel.ARM;

    // Display properties
    public string DailyCaloriesDisplay => $"{DailyCalories:F0} kcal";
    public string DailyProteinDisplay => $"Protein: {DailyProtein:F1}g";
    public string DailyFatDisplay => $"Fat: {DailyFat:F1}g";
    public string DailyCarbsDisplay => $"Carbs: {DailyCarbohydrates:F1}g";
    public string BMIDisplay => $"BMI: {BMI:F1}";
    public string ARMDisplay => $"{ARM:F3}";

    public event PropertyChangedEventHandler? PropertyChanged;

    public PreferencesViewModel(UserViewModel userViewModel, Action onConfigurationSaved)
    {
        _userViewModel = userViewModel ?? throw new ArgumentNullException(nameof(userViewModel));
        _onConfigurationSaved = onConfigurationSaved;
        UpdateCalculations();
    }

    private void TryParseAndSetWeight(string value)
    {
        if (double.TryParse(value, out var weight) && weight > 0)
        {
            Weight = weight;
        }
    }

    private void TryParseAndSetHeight(string value)
    {
        if (double.TryParse(value, out var height) && height > 0)
        {
            Height = height;
        }
    }

    private void TryParseAndSetAge(string value)
    {
        if (double.TryParse(value, out var age) && age > 0)
        {
            Age = age;
        }
    }

    public void UpdateCalculations()
    {
        var userModel = _userViewModel.GetModel();
        NutritionCalculationService.CalculateNutritionalNeeds(userModel);
        _userViewModel.RefreshCalculatedProperties();

        // Notify all calculated properties
        OnPropertyChanged(nameof(DailyCalories));
        OnPropertyChanged(nameof(DailyProtein));
        OnPropertyChanged(nameof(DailyFat));
        OnPropertyChanged(nameof(DailyCarbohydrates));
        OnPropertyChanged(nameof(BMI));
        OnPropertyChanged(nameof(ARM));
        OnPropertyChanged(nameof(DailyCaloriesDisplay));
        OnPropertyChanged(nameof(DailyProteinDisplay));
        OnPropertyChanged(nameof(DailyFatDisplay));
        OnPropertyChanged(nameof(DailyCarbsDisplay));
        OnPropertyChanged(nameof(BMIDisplay));
        OnPropertyChanged(nameof(ARMDisplay));
    }

    // Preview calculations without saving (for real-time feedback)
    public (bool isValid, string errorMessage, string previewCalories, string previewProtein, string previewFat, string previewCarbs, string previewBMI, string previewARM)
        PreviewCalculations(string weightText, string heightText, string ageText, int activityLevelIndex)
    {
        if (!double.TryParse(weightText, out var weight) || weight <= 0)
        {
            return (false, "Invalid weight", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        if (!double.TryParse(heightText, out var height) || height <= 0)
        {
            return (false, "Invalid height", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        if (!double.TryParse(ageText, out var age) || age <= 0)
        {
            return (false, "Invalid age", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        // Create temporary user for preview
        var tempUser = new User
        {
            Weight = weight,
            Height = height,
            Age = age,
            ActivityLevel = (ActivityLevel)activityLevelIndex
        };

        NutritionCalculationService.CalculateNutritionalNeeds(tempUser);

        return (true, string.Empty,
            $"{tempUser.DailyCalories:F0} kcal",
            $"Protein: {tempUser.DailyProtein:F1}g",
            $"Fat: {tempUser.DailyFat:F1}g",
            $"Carbs: {tempUser.DailyCarbohydrates:F1}g",
            $"BMI: {tempUser.BMI:F1}",
            $"ARM: {tempUser.ARM:F3}");
    }

    // Expose activity level constants without exposing the enum
    public static int ActivityLevelSedentary => 0;
    public static int ActivityLevelModerate => 1;
    public static int ActivityLevelMedium => 2;
    public static int ActivityLevelHigh => 3;

    public (bool isValid, string errorTitle, string errorMessage) ValidateAndApply(
        string weightText, string heightText, string ageText, int activityLevelIndex)
    {
        if (!double.TryParse(weightText, out var weight) || weight <= 0)
        {
            return (false, "Invalid Input", "Weight must be a positive number");
        }

        if (!double.TryParse(heightText, out var height) || height <= 0)
        {
            return (false, "Invalid Input", "Height must be a positive number");
        }

        if (!double.TryParse(ageText, out var age) || age <= 0)
        {
            return (false, "Invalid Input", "Age must be a positive number");
        }

        var validation = ValidationService.ValidateUserInput(weight, height, age);

        if (!validation.isValid)
        {
            return validation;
        }

        // Update user data
        Weight = weight;
        Height = height;
        Age = age;
        ActivityLevelIndex = activityLevelIndex;

        // Recalculate
        UpdateCalculations();

        Logger.Instance.Information("User preferences updated: Weight={Weight}kg, Height={Height}cm, Age={Age}, Activity={Activity}",
            weight, height, age, (ActivityLevel)activityLevelIndex);

        // Save configuration to disk
        _onConfigurationSaved?.Invoke();

        return (true, string.Empty, string.Empty);
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
