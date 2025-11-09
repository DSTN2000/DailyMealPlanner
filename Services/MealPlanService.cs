namespace Lab4.Services;

using System.Text.Json;
using Lab4.Models;

public class MealPlanService
{
    private static readonly string AppName = "DailyMealPlanner";
    private static readonly string MealPlansDirectoryName = "MealPlans";

    public static string GetMealPlansDirectory()
    {
        var configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appConfigDir = Path.Combine(configDir, AppName);
        var mealPlansDir = Path.Combine(appConfigDir, MealPlansDirectoryName);

        // Ensure directory exists
        if (!Directory.Exists(mealPlansDir))
        {
            Directory.CreateDirectory(mealPlansDir);
        }

        return mealPlansDir;
    }

    public static string GetMealPlanFilePath(DateTime date)
    {
        var mealPlansDir = GetMealPlansDirectory();
        var fileName = $"mealplan_{date:yyyy-MM-dd}.json";
        return Path.Combine(mealPlansDir, fileName);
    }

    public static void SaveMealPlan(DailyMealPlan mealPlan)
    {
        try
        {
            var filePath = GetMealPlanFilePath(mealPlan.Date);
            var json = JsonSerializer.Serialize(mealPlan, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
            Logger.Instance.Information("Meal plan saved to {Path}", filePath);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to save meal plan");
            throw;
        }
    }

    public static DailyMealPlan? LoadMealPlan(DateTime date)
    {
        try
        {
            var filePath = GetMealPlanFilePath(date);

            if (!File.Exists(filePath))
            {
                Logger.Instance.Information("No meal plan found for date {Date}", date);
                return null;
            }

            var json = File.ReadAllText(filePath);
            var mealPlan = JsonSerializer.Deserialize<DailyMealPlan>(json);

            if (mealPlan != null)
            {
                // Recalculate totals after loading
                NutritionCalculationService.RecalculateMealPlan(mealPlan);
                Logger.Instance.Information("Meal plan loaded from {Path}", filePath);
            }

            return mealPlan;
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to load meal plan");
            return null;
        }
    }

    public static List<DateTime> GetAvailableMealPlanDates()
    {
        try
        {
            var mealPlansDir = GetMealPlansDirectory();
            var files = Directory.GetFiles(mealPlansDir, "mealplan_*.json");
            var dates = new List<DateTime>();

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith("mealplan_") && fileName.Length > 9)
                {
                    var dateString = fileName.Substring(9); // Remove "mealplan_" prefix
                    if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
                    {
                        dates.Add(date);
                    }
                }
            }

            return dates.OrderByDescending(d => d).ToList();
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to get available meal plan dates");
            return new List<DateTime>();
        }
    }
}
