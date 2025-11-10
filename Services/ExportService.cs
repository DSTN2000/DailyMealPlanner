namespace Lab4.Services;

using System.Text;
using System.Xml;
using Lab4.Models;

public class ExportService
{
    public static void ExportToXml(DailyMealPlan mealPlan, string filePath)
    {
        try
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                Encoding = Encoding.UTF8
            };

            using var writer = XmlWriter.Create(filePath, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("MealPlan");
            writer.WriteAttributeString("Date", mealPlan.Date.ToString("yyyy-MM-dd"));

            // Write totals
            writer.WriteStartElement("DailyTotals");
            writer.WriteElementString("Calories", mealPlan.TotalCalories.ToString("F2"));
            writer.WriteElementString("Protein", mealPlan.TotalProtein.ToString("F2"));
            writer.WriteElementString("Fat", mealPlan.TotalFat.ToString("F2"));
            writer.WriteElementString("Carbohydrates", mealPlan.TotalCarbohydrates.ToString("F2"));
            writer.WriteEndElement();

            // Write meal times
            writer.WriteStartElement("MealTimes");
            foreach (var mealTime in mealPlan.MealTimes)
            {
                writer.WriteStartElement("MealTime");
                writer.WriteAttributeString("Type", mealTime.Type.ToString());
                writer.WriteAttributeString("Name", mealTime.Name);

                writer.WriteStartElement("Totals");
                writer.WriteElementString("Calories", mealTime.TotalCalories.ToString("F2"));
                writer.WriteElementString("Protein", mealTime.TotalProtein.ToString("F2"));
                writer.WriteElementString("Fat", mealTime.TotalFat.ToString("F2"));
                writer.WriteElementString("Carbohydrates", mealTime.TotalCarbohydrates.ToString("F2"));
                writer.WriteEndElement();

                writer.WriteStartElement("Items");
                foreach (var item in mealTime.Items)
                {
                    writer.WriteStartElement("Item");
                    writer.WriteElementString("ProductName", item.Product.Name);
                    writer.WriteElementString("Weight", item.Weight.ToString("F0"));
                    writer.WriteElementString("Calories", item.Calories.ToString("F2"));
                    writer.WriteElementString("Protein", item.Protein.ToString("F2"));
                    writer.WriteElementString("Fat", item.TotalFat.ToString("F2"));
                    writer.WriteElementString("Carbohydrates", item.Carbohydrates.ToString("F2"));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement(); // Items

                writer.WriteEndElement(); // MealTime
            }
            writer.WriteEndElement(); // MealTimes

            writer.WriteEndElement(); // MealPlan
            writer.WriteEndDocument();

            Logger.Instance.Information("Meal plan exported to XML: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to export meal plan to XML");
            throw;
        }
    }

    public static DailyMealPlan? ImportFromXml(string filePath)
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

            using var reader = XmlReader.Create(filePath);

            while (reader.Read())
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
                            var mealTime = ParseMealTimeFromXml(reader);
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

    private static MealTime? ParseMealTimeFromXml(XmlReader reader)
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
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Item")
                {
                    var item = ParseMealPlanItemFromXml(reader);
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

    private static MealPlanItem? ParseMealPlanItemFromXml(XmlReader reader)
    {
        try
        {
            string? productName = null;
            double weight = 100.0;

            // Read item elements
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "ProductName":
                            productName = reader.ReadElementContentAsString();
                            break;
                        case "Weight":
                            if (double.TryParse(reader.ReadElementContentAsString(), out var w))
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
            var product = CatalogService.SearchProductsAsync(productName).Result
                .FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));

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

    public static void ExportToHtml(DailyMealPlan mealPlan, string filePath)
    {
        try
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine($"    <title>Meal Plan - {mealPlan.Date:yyyy-MM-dd}</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; max-width: 1200px; margin: 20px auto; padding: 0 20px; }");
            html.AppendLine("        h1 { color: #333; }");
            html.AppendLine("        h2 { color: #666; margin-top: 30px; }");
            html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
            html.AppendLine("        th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }");
            html.AppendLine("        th { background-color: #4CAF50; color: white; }");
            html.AppendLine("        tr:nth-child(even) { background-color: #f2f2f2; }");
            html.AppendLine("        .totals { background-color: #e8f5e9; font-weight: bold; }");
            html.AppendLine("        .section { margin-bottom: 40px; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine($"    <h1>Daily Meal Plan - {mealPlan.Date:dddd, MMMM d, yyyy}</h1>");

            // Daily totals
            html.AppendLine("    <div class=\"section\">");
            html.AppendLine("        <h2>Daily Totals</h2>");
            html.AppendLine("        <table>");
            html.AppendLine("            <tr>");
            html.AppendLine("                <th>Calories</th>");
            html.AppendLine("                <th>Protein</th>");
            html.AppendLine("                <th>Fat</th>");
            html.AppendLine("                <th>Carbohydrates</th>");
            html.AppendLine("            </tr>");
            html.AppendLine("            <tr class=\"totals\">");
            html.AppendLine($"                <td>{mealPlan.TotalCalories:F0} kcal</td>");
            html.AppendLine($"                <td>{mealPlan.TotalProtein:F1} g</td>");
            html.AppendLine($"                <td>{mealPlan.TotalFat:F1} g</td>");
            html.AppendLine($"                <td>{mealPlan.TotalCarbohydrates:F1} g</td>");
            html.AppendLine("            </tr>");
            html.AppendLine("        </table>");
            html.AppendLine("    </div>");

            // Meal times
            foreach (var mealTime in mealPlan.MealTimes)
            {
                html.AppendLine("    <div class=\"section\">");
                html.AppendLine($"        <h2>{mealTime.Name}</h2>");
                html.AppendLine("        <table>");
                html.AppendLine("            <tr>");
                html.AppendLine("                <th>Food</th>");
                html.AppendLine("                <th>Weight</th>");
                html.AppendLine("                <th>Calories</th>");
                html.AppendLine("                <th>Protein</th>");
                html.AppendLine("                <th>Fat</th>");
                html.AppendLine("                <th>Carbs</th>");
                html.AppendLine("            </tr>");

                foreach (var item in mealTime.Items)
                {
                    html.AppendLine("            <tr>");
                    html.AppendLine($"                <td>{item.Product.Name}</td>");
                    html.AppendLine($"                <td>{item.Weight:F0} g</td>");
                    html.AppendLine($"                <td>{item.Calories:F0} kcal</td>");
                    html.AppendLine($"                <td>{item.Protein:F1} g</td>");
                    html.AppendLine($"                <td>{item.TotalFat:F1} g</td>");
                    html.AppendLine($"                <td>{item.Carbohydrates:F1} g</td>");
                    html.AppendLine("            </tr>");
                }

                html.AppendLine("            <tr class=\"totals\">");
                html.AppendLine("                <td>Totals</td>");
                html.AppendLine("                <td></td>");
                html.AppendLine($"                <td>{mealTime.TotalCalories:F0} kcal</td>");
                html.AppendLine($"                <td>{mealTime.TotalProtein:F1} g</td>");
                html.AppendLine($"                <td>{mealTime.TotalFat:F1} g</td>");
                html.AppendLine($"                <td>{mealTime.TotalCarbohydrates:F1} g</td>");
                html.AppendLine("            </tr>");
                html.AppendLine("        </table>");
                html.AppendLine("    </div>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            File.WriteAllText(filePath, html.ToString());
            Logger.Instance.Information("Meal plan exported to HTML: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to export meal plan to HTML");
            throw;
        }
    }

    public static void ExportToText(DailyMealPlan mealPlan, string filePath)
    {
        try
        {
            var text = new StringBuilder();
            text.AppendLine("========================================");
            text.AppendLine($"Daily Meal Plan - {mealPlan.Date:dddd, MMMM d, yyyy}");
            text.AppendLine("========================================");
            text.AppendLine();

            text.AppendLine("DAILY TOTALS:");
            text.AppendLine($"  Calories:       {mealPlan.TotalCalories:F0} kcal");
            text.AppendLine($"  Protein:        {mealPlan.TotalProtein:F1} g");
            text.AppendLine($"  Fat:            {mealPlan.TotalFat:F1} g");
            text.AppendLine($"  Carbohydrates:  {mealPlan.TotalCarbohydrates:F1} g");
            text.AppendLine();

            foreach (var mealTime in mealPlan.MealTimes)
            {
                text.AppendLine("----------------------------------------");
                text.AppendLine($"{mealTime.Name.ToUpper()}");
                text.AppendLine("----------------------------------------");

                if (mealTime.Items.Count == 0)
                {
                    text.AppendLine("  (No items)");
                }
                else
                {
                    foreach (var item in mealTime.Items)
                    {
                        text.AppendLine($"  {item.Product.Name} ({item.Weight:F0}g)");
                        text.AppendLine($"    Calories: {item.Calories:F0} kcal");
                        text.AppendLine($"    Protein: {item.Protein:F1}g | Fat: {item.TotalFat:F1}g | Carbs: {item.Carbohydrates:F1}g");
                        text.AppendLine();
                    }

                    text.AppendLine($"  {mealTime.Name} Totals:");
                    text.AppendLine($"    Calories: {mealTime.TotalCalories:F0} kcal");
                    text.AppendLine($"    Protein: {mealTime.TotalProtein:F1}g | Fat: {mealTime.TotalFat:F1}g | Carbs: {mealTime.TotalCarbohydrates:F1}g");
                }
                text.AppendLine();
            }

            File.WriteAllText(filePath, text.ToString());
            Logger.Instance.Information("Meal plan exported to Text: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to export meal plan to Text");
            throw;
        }
    }
}
