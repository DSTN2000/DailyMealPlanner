namespace Lab4.Services;

using System.Text.Json;
using Lab4.Models;
using Microsoft.Data.Sqlite;

public class CatalogService
{
    private static readonly string dbPath = Path.Combine("Resources", "opennutrition_foods.db");

    public static async Task<List<string>> GetCategoriesAsync()
    {
        Logger.Instance.Information("Querying categories from database");

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT DISTINCT type
            FROM ""opennutrition_foods.db""
            WHERE type IS NOT NULL AND type != ''
            ORDER BY type";

        var categories = new List<string>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            categories.Add(reader.GetString(0));
        }

        Logger.Instance.Information("Found {Count} categories", categories.Count);
        return categories;
    }

    public static async Task<int> GetProductCountByCategoryAsync(string category)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*)
            FROM ""opennutrition_foods.db""
            WHERE type = @category";
        command.Parameters.AddWithValue("@category", category);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count;
    }

    public static async Task<List<Product>> GetProductsByCategoryAsync(string category, int limit = -1, int offset = 0)
    {
        Logger.Instance.Information("Querying products for category {Category} (limit: {Limit}, offset: {Offset})",
            category, limit, offset);

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, description, type, labels, nutrition_100g
            FROM ""opennutrition_foods.db""
            WHERE type = @category
            ORDER BY name
            " + (limit > 0 ? "LIMIT @limit OFFSET @offset" : "");

        command.Parameters.AddWithValue("@category", category);
        if (limit > 0)
        {
            command.Parameters.AddWithValue("@limit", limit);
            command.Parameters.AddWithValue("@offset", offset);
        }

        var products = new List<Product>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(ParseProductFromReader(reader));
        }

        Logger.Instance.Information("Loaded {Count} products for category {Category}", products.Count, category);
        return products;
    }

    private static Product ParseProductFromReader(SqliteDataReader reader)
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

        return product;
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

    public static async Task<Product?> GetProductByIdAsync(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return null;
        }

        Logger.Instance.Information("Getting product by ID: {ProductId}", productId);

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, description, type, labels, nutrition_100g
            FROM ""opennutrition_foods.db""
            WHERE id = @productId
            LIMIT 1";

        command.Parameters.AddWithValue("@productId", productId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var product = ParseProductFromReader(reader);
            Logger.Instance.Information("Found product: {ProductName}", product.Name);
            return product;
        }

        Logger.Instance.Warning("Product not found: {ProductId}", productId);
        return null;
    }

    public static async Task<List<Product>> SearchProductsAsync(string searchQuery, int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return new List<Product>();
        }

        Logger.Instance.Information("Searching products with query: {Query}", searchQuery);

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, description, type, labels, nutrition_100g
            FROM ""opennutrition_foods.db""
            WHERE name LIKE @searchQuery OR description LIKE @searchQuery
            ORDER BY name
            LIMIT @limit";

        command.Parameters.AddWithValue("@searchQuery", $"%{searchQuery}%");
        command.Parameters.AddWithValue("@limit", limit);

        var products = new List<Product>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(ParseProductFromReader(reader));
        }

        Logger.Instance.Information("Found {Count} products matching query", products.Count);
        return products;
    }

}
