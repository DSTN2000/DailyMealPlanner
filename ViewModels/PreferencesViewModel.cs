namespace Lab4.ViewModels;

using System.ComponentModel;
using Lab4.Models;
using Lab4.Services;

public class PreferencesViewModel : INotifyPropertyChanged
{
    private User _user;

    public User User
    {
        get => _user;
        set
        {
            _user = value;
            OnPropertyChanged(nameof(User));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PreferencesViewModel(User user)
    {
        _user = user;
        UpdateCalculations();
    }

    public void UpdateCalculations()
    {
        NutritionCalculationService.CalculateNutritionalNeeds(_user);
        OnPropertyChanged(nameof(User));
    }

    public bool IsActivityLevel(int levelValue)
    {
        return (int)_user.ActivityLevel == levelValue;
    }

    public void SetActivityLevel(int levelValue)
    {
        _user.ActivityLevel = (ActivityLevel)levelValue;
    }

    // Expose activity level constants without exposing the enum
    public static int ActivityLevelSedentary => 0;
    public static int ActivityLevelModerate => 1;
    public static int ActivityLevelMedium => 2;
    public static int ActivityLevelHigh => 3;

    public (bool isValid, string errorTitle, string errorMessage) ValidateAndApply(
        double weight, double height, double age, int activityLevelIndex)
    {
        var validation = ValidationService.ValidateUserInput(weight, height, age);

        if (!validation.isValid)
        {
            return validation;
        }

        var activityLevel = (ActivityLevel)activityLevelIndex;

        // Update user data
        _user.Weight = weight;
        _user.Height = height;
        _user.Age = age;
        _user.ActivityLevel = activityLevel;

        // Recalculate
        UpdateCalculations();

        Logger.Instance.Information("User preferences updated: Weight={Weight}kg, Height={Height}cm, Age={Age}, Activity={Activity}",
            weight, height, age, activityLevel);

        return (true, string.Empty, string.Empty);
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
