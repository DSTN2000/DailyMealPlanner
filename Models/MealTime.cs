namespace Lab4.Models;

public class MealTime
{
    public MealTimeType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MealPlanItem> Items { get; set; } = new();

    // Calculated totals for this mealtime
    public double TotalCalories { get; set; }
    public double TotalProtein { get; set; }
    public double TotalFat { get; set; }
    public double TotalCarbohydrates { get; set; }

    public MealTime()
    {
    }

    public MealTime(MealTimeType type, string? customName = null)
    {
        Type = type;
        Name = type switch
        {
            MealTimeType.Breakfast => "Breakfast",
            MealTimeType.Lunch => "Lunch",
            MealTimeType.Dinner => "Dinner",
            MealTimeType.Custom => customName ?? "Custom Meal",
            _ => "Meal"
        };
    }
}
