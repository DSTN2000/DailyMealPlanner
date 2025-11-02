namespace Lab4.Services;

using Lab4.Models;

public class NutritionCalculationService
{
    public static void CalculateNutritionalNeeds(User user)
    {
        // Calculate BMI
        var heightM = user.Height / 100.0;
        user.BMI = user.Weight / (heightM * heightM);

        // Determine ARM based on activity level
        user.ARM = user.ActivityLevel switch
        {
            ActivityLevel.Sedentary => 1.2,
            ActivityLevel.Moderate => 1.375,
            ActivityLevel.Medium => 1.55,
            ActivityLevel.High => 1.725,
            _ => 1.2
        };

        // Calculate BMR using Mifflin-St Jeor equation (assuming male for now)
        // BMR = 10 * weight(kg) + 6.25 * height(cm) - 5 * age(years) + 5
        var bmr = (10 * user.Weight) + (6.25 * user.Height) - (5 * user.Age) + 5;

        // Calculate TDEE (Total Daily Energy Expenditure)
        user.DailyCalories = bmr * user.ARM;

        // Calculate macronutrient distribution (standard ratios)
        // Protein: 30% of calories, 4 cal/g
        user.DailyProtein = (user.DailyCalories * 0.30) / 4;

        // Fat: 25% of calories, 9 cal/g
        user.DailyFat = (user.DailyCalories * 0.25) / 9;

        // Carbs: 45% of calories, 4 cal/g
        user.DailyCarbohydrates = (user.DailyCalories * 0.45) / 4;
    }
}
