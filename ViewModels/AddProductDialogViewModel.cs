namespace Lab4.ViewModels;

using System.ComponentModel;
using Lab4.Models;

/// <summary>
/// ViewModel for the Add Product dialog
/// </summary>
public class AddProductDialogViewModel : INotifyPropertyChanged
{
    private string _productName = string.Empty;
    private int _selectedMealTimeIndex = 0; // 0=Breakfast, 1=Lunch, 2=Dinner
    private double _weight = 100.0;

    public string ProductName
    {
        get => _productName;
        set
        {
            _productName = value;
            OnPropertyChanged(nameof(ProductName));
        }
    }

    public int SelectedMealTimeIndex
    {
        get => _selectedMealTimeIndex;
        set
        {
            _selectedMealTimeIndex = value;
            OnPropertyChanged(nameof(SelectedMealTimeIndex));
        }
    }

    public double Weight
    {
        get => _weight;
        set
        {
            if (value > 0)
            {
                _weight = value;
                OnPropertyChanged(nameof(Weight));
            }
        }
    }

    public string[] MealTimeOptions { get; } = { "Breakfast", "Lunch", "Dinner" };

    public event PropertyChangedEventHandler? PropertyChanged;

    public AddProductDialogViewModel(string productName)
    {
        ProductName = productName;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Internal method to convert index to Model enum (only for ViewModel-to-Model communication)
    internal MealTimeType GetMealTimeType()
    {
        return SelectedMealTimeIndex switch
        {
            0 => MealTimeType.Breakfast,
            1 => MealTimeType.Lunch,
            2 => MealTimeType.Dinner,
            _ => MealTimeType.Breakfast
        };
    }
}
