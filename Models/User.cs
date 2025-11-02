namespace Lab4.Models;

public class User
{
    // Input values
    public double Weight { get; set; } // in kg
    public double Height { get; set; } // in cm
    public double Age { get; set; } // in years
    public ActivityLevel ActivityLevel { get; set; }

    // Calculated values
    public double BMI { get; set; }
    public double ARM { get; set; }
    public double DailyCalories { get; set; }
    public double DailyProtein { get; set; } // in grams
    public double DailyFat { get; set; } // in grams
    public double DailyCarbohydrates { get; set; } // in grams

    public User()
    {
        Weight = 75;
        Height = 170;
        Age = 30;
        ActivityLevel = ActivityLevel.Moderate;
    }
}

public enum ActivityLevel
{
    Sedentary,
    Moderate,
    Medium,
    High
}
