namespace Lab4.Models;

public class CategoryGroup
{
    public string Name { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = new();

    public CategoryGroup() { }

    public CategoryGroup(string name)
    {
        Name = name;
    }
}
