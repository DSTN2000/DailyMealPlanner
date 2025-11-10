using System.Text;
using System.Xml;
using Lab4.Models;
using Lab4.Services.Abstractions;

namespace Lab4.Services;

public class XmlExportService : IExportService
{
    public void Export(DailyMealPlan mealPlan, string filePath)
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
}
