namespace Lab4.Models;

public class MealPlanItem
{
    public Product Product { get; set; }
    public double Weight { get; set; } = 100.0; // in grams

    // Calculated nutritional values (based on weight)
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double TotalFat { get; set; }
    public double Carbohydrates { get; set; }
    public double Sodium { get; set; }
    public double Fiber { get; set; }
    public double Sugar { get; set; }

    public MealPlanItem()
    {
        Product = new Product();
    }

    public MealPlanItem(Product product, double weight = 100.0)
    {
        Product = product;
        Weight = weight;
    }
}
