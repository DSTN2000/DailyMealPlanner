namespace Lab4.ViewModels;

using System.ComponentModel;
using Lab4.Models;

/// <summary>
/// ViewModel for the Add Product dialog
/// </summary>
public class AddProductDialogViewModel : INotifyPropertyChanged
{
    private string _productName = string.Empty;
    private int _selectedMealTimeIndex = 0;
    private double _weight = 100.0;
    private readonly List<MealTimeViewModel> _mealTimes;

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

    public string[] MealTimeOptions { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AddProductDialogViewModel(string productName, List<MealTimeViewModel> mealTimes)
    {
        ProductName = productName;
        _mealTimes = mealTimes ?? throw new ArgumentNullException(nameof(mealTimes));

        // Build meal time options from actual meal times
        MealTimeOptions = _mealTimes.Select(mt => mt.Name).ToArray();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Internal method to get selected meal time ViewModel
    internal MealTimeViewModel GetSelectedMealTime()
    {
        if (SelectedMealTimeIndex >= 0 && SelectedMealTimeIndex < _mealTimes.Count)
        {
            return _mealTimes[SelectedMealTimeIndex];
        }
        return _mealTimes[0]; // Default to first meal time
    }
}
