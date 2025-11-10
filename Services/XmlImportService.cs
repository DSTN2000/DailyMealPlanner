using System.Xml;
using Lab4.Models;
using Lab4.Services.Abstractions;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Lab4.Services;

public class XmlImportService : IImportService
{
    public async Task<DailyMealPlan?> ImportAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Logger.Instance.Warning("File not found: {FilePath}", filePath);
                return null;
            }

            var mealPlan = new DailyMealPlan();
            var mealTimes = new List<MealTime>();

            using var reader = XmlReader.Create(filePath, new XmlReaderSettings { Async = true });

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "MealPlan":
                            var dateStr = reader.GetAttribute("Date");
                            if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
                            {
                                mealPlan.Date = date;
                            }
                            break;

                        case "MealTime":
                            var mealTime = await ParseMealTimeFromXml(reader);
                            if (mealTime != null)
                            {
                                mealTimes.Add(mealTime);
                            }
                            break;
                    }
                }
            }

            mealPlan.MealTimes = mealTimes;

            // Recalculate totals after loading
            NutritionCalculationService.RecalculateMealPlan(mealPlan);

            Logger.Instance.Information("Meal plan imported from XML: {FilePath}", filePath);
            return mealPlan;
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to import meal plan from XML");
            return null;
        }
    }

    private async Task<MealTime?> ParseMealTimeFromXml(XmlReader reader)
    {
        try
        {
            var typeStr = reader.GetAttribute("Type");
            var name = reader.GetAttribute("Name");

            if (string.IsNullOrEmpty(typeStr) || string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (!Enum.TryParse<MealTimeType>(typeStr, out var mealTimeType))
            {
                return null;
            }

            var mealTime = new MealTime(mealTimeType) { Name = name };
            var items = new List<MealPlanItem>();

            // Read until we find Items element
            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Item")
                {
                    var item = await ParseMealPlanItemFromXml(reader);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "MealTime")
                {
                    break;
                }
            }

            mealTime.Items = items;
            return mealTime;
        }
        catch
        {
            return null;
        }
    }

    private async Task<MealPlanItem?> ParseMealPlanItemFromXml(XmlReader reader)
    {
        try
        {
            string? productName = null;
            double weight = 100.0;

            // Read item elements
            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "ProductName":
                            productName = await reader.ReadElementContentAsStringAsync();
                            break;
                        case "Weight":
                            if (double.TryParse(await reader.ReadElementContentAsStringAsync(), out var w))
                            {
                                weight = w;
                            }
                            break;
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Item")
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(productName))
            {
                return null;
            }

            // Look up the product in the database by name
            var products = await CatalogService.SearchProductsAsync(productName);
            var product = products.FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));

            if (product == null)
            {
                Logger.Instance.Warning("Product not found during XML import: {ProductName}", productName);
                return null;
            }

            return new MealPlanItem(product, weight);
        }
        catch
        {
            return null;
        }
    }
}