using Lab4.Models;
using System.Threading.Tasks;

namespace Lab4.Services.Abstractions;

public interface IImportService
{
    Task<DailyMealPlan?> ImportAsync(string filePath);
}