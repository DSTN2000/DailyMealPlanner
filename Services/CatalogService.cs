namespace Lab4.Services;

using System.Text.Json;
using Lab4.Models;
using Microsoft.Data.Sqlite;

public class CatalogService
{
    private static readonly string dbPath = Path.Combine("Resources", "opennutrition_foods.db");
    private static List<Product>? _cachedProducts;
    private static List<CategoryGroup>? _cachedCategories;

    public static async Task<List<Product>> GetAllProductsAsync()
    {
        if (_cachedProducts != null) return _cachedProducts;

        Logger.Instance.Information("Loading products from database: {Path}", dbPath);
        _cachedProducts = await LoadProductsFromDatabaseAsync();
        Logger.Instance.Information("Loaded {Count} products", _cachedProducts.Count);

        return _cachedProducts;
    }

    public static async Task<List<CategoryGroup>> GetCategoriesAsync()
    {
        if (_cachedCategories != null) return _cachedCategories;

        var products = await GetAllProductsAsync();

        _cachedCategories = products
            .GroupBy(p => p.Category)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new CategoryGroup(g.Key) { Products = g.ToList() })
            .OrderBy(c => c.Name)
            .ToList();

        Logger.Instance.Information("Grouped into {Count} categories", _cachedCategories.Count);
        return _cachedCategories;
    }

    private static async Task<List<Product>> LoadProductsFromDatabaseAsync()
    {
        var products = new List<Product>();

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, description, type, labels, nutrition_100g
            FROM ""opennutrition_foods.db"" ";

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var product = new Product
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Category = reader.IsDBNull(3) ? "Uncategorized" : reader.GetString(3)
            };

            // Parse labels (subcategories)
            if (!reader.IsDBNull(4))
            {
                var labelsJson = reader.GetString(4);
                try
                {
                    var labels = JsonSerializer.Deserialize<List<string>>(labelsJson);
                    if (labels != null) product.Labels = labels;
                }
                catch { /* ignore errors */ }
            }

            // Parse nutrition data
            if (!reader.IsDBNull(5))
            {
                var nutritionJson = reader.GetString(5);
                try
                {
                    using var doc = JsonDocument.Parse(nutritionJson);
                    var root = doc.RootElement;

                    product.Calories = GetJsonDouble(root, "calories");
                    product.Protein = GetJsonDouble(root, "protein");
                    product.TotalFat = GetJsonDouble(root, "total_fat");
                    product.Carbohydrates = GetJsonDouble(root, "carbohydrates");
                    product.Sodium = GetJsonDouble(root, "sodium");
                    product.Fiber = GetJsonDouble(root, "dietary_fiber");
                    product.Sugar = GetJsonDouble(root, "total_sugars");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug("Failed to parse nutrition for {ProductId}: {Error}", product.Id, ex.Message);
                }
            }

            products.Add(product);
        }

        return products;
    }

    private static double GetJsonDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDouble();
        }
        return 0;
    }

    public static void ClearCache()
    {
        _cachedProducts = null;
        _cachedCategories = null;
        Logger.Instance.Debug("Cache cleared");
    }
}
