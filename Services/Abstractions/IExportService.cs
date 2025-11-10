using Lab4.Models;

namespace Lab4.Services.Abstractions;

public interface IExportService
{
    void Export(DailyMealPlan mealPlan, string filePath);
}
