namespace Lab4.Models;

public class DailyMealPlan
{
    public DateTime Date { get; set; } = DateTime.Today;
    public List<MealTime> MealTimes { get; set; } = new();

    // Calculated daily totals
    public double TotalCalories { get; set; }
    public double TotalProtein { get; set; }
    public double TotalFat { get; set; }
    public double TotalCarbohydrates { get; set; }

    public DailyMealPlan()
    {
        // Initialize with default mealtimes
        MealTimes.Add(new MealTime(MealTimeType.Breakfast));
        MealTimes.Add(new MealTime(MealTimeType.Lunch));
        MealTimes.Add(new MealTime(MealTimeType.Dinner));
    }
}
