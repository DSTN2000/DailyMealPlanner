namespace Lab4.Services;

public class ValidationService
{
    public static (bool isValid, string errorTitle, string errorMessage) ValidateUserInput(
        double weight, double height, double age)
    {
        if (weight <= 0)
        {
            return (false, "Invalid Weight", "Please enter a valid positive number for weight.");
        }

        if (height <= 0)
        {
            return (false, "Invalid Height", "Please enter a valid positive number for height.");
        }

        if (age <= 0)
        {
            return (false, "Invalid Age", "Please enter a valid positive number for age.");
        }

        return (true, string.Empty, string.Empty);
    }
}
