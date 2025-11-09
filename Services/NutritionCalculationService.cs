namespace Lab4.Services;

using Lab4.Models;

public class NutritionCalculationService
{
    public static void CalculateNutritionalNeeds(User user)
    {
        var heightM = user.Height / 100.0;
        user.BMI = user.Weight / (heightM * heightM);

        user.ARM = user.ActivityLevel switch
        {
            ActivityLevel.Sedentary => 1.2,
            ActivityLevel.Moderate => 1.375,
            ActivityLevel.Medium => 1.55,
            ActivityLevel.High => 1.725,
            _ => 1.2
        };

        var bmr = 447.593 + 9.247 * user.Weight + 3.098 * user.Height - 4.330 * user.Age;

        // Calculate TDEE (Total Daily Energy Expenditure)
        user.DailyCalories = bmr * user.ARM;

        // Protein: 30% of calories, 4 cal/g
        user.DailyProtein = (user.DailyCalories * 0.30) / 4;

        // Fat: 25% of calories, 9 cal/g
        user.DailyFat = (user.DailyCalories * 0.25) / 9;

        // Carbs: 45% of calories, 4 cal/g
        user.DailyCarbohydrates = (user.DailyCalories * 0.45) / 4;
    }

    /// <summary>
    /// Recalculates nutrition for a single meal plan item based on its weight
    /// </summary>
    public static void RecalculateItemNutrition(MealPlanItem item)
    {
        if (item?.Product == null) return;

        var multiplier = item.Weight / 100.0;
        item.Calories = item.Product.Calories * multiplier;
        item.Protein = item.Product.Protein * multiplier;
        item.TotalFat = item.Product.TotalFat * multiplier;
        item.Carbohydrates = item.Product.Carbohydrates * multiplier;
        item.Sodium = item.Product.Sodium * multiplier;
        item.Fiber = item.Product.Fiber * multiplier;
        item.Sugar = item.Product.Sugar * multiplier;
    }

    /// <summary>
    /// Recalculates all nutritional totals for the entire daily meal plan
    /// </summary>
    public static void RecalculateMealPlan(DailyMealPlan mealPlan)
    {
        if (mealPlan == null) return;

        // Reset daily totals
        mealPlan.TotalCalories = 0;
        mealPlan.TotalProtein = 0;
        mealPlan.TotalFat = 0;
        mealPlan.TotalCarbohydrates = 0;

        // Calculate totals for each mealtime
        foreach (var mealTime in mealPlan.MealTimes)
        {
            // Reset mealtime totals
            mealTime.TotalCalories = 0;
            mealTime.TotalProtein = 0;
            mealTime.TotalFat = 0;
            mealTime.TotalCarbohydrates = 0;

            // Recalculate each item and sum
            foreach (var item in mealTime.Items)
            {
                RecalculateItemNutrition(item);

                mealTime.TotalCalories += item.Calories;
                mealTime.TotalProtein += item.Protein;
                mealTime.TotalFat += item.TotalFat;
                mealTime.TotalCarbohydrates += item.Carbohydrates;
            }

            // Add mealtime totals to daily totals
            mealPlan.TotalCalories += mealTime.TotalCalories;
            mealPlan.TotalProtein += mealTime.TotalProtein;
            mealPlan.TotalFat += mealTime.TotalFat;
            mealPlan.TotalCarbohydrates += mealTime.TotalCarbohydrates;
        }
    }
}
