namespace Lab4.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using Lab4.Models;

public class MealTimeViewModel : INotifyPropertyChanged
{
    private readonly MealTime _model;
    private ObservableCollection<MealPlanItemViewModel> _items;

    // View-friendly properties (NO Model exposure!)
    public string Name
    {
        get => _model.Name;
        set
        {
            if (_model.Name != value && !string.IsNullOrWhiteSpace(value))
            {
                _model.Name = value;
                OnPropertyChanged(nameof(Name));
                NameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public MealTimeType Type => _model.Type;
    public bool IsCustom => Type == MealTimeType.Custom;
    public bool CanRemove => IsCustom;
    public bool CanRename => IsCustom;

    public ObservableCollection<MealPlanItemViewModel> Items
    {
        get => _items;
        private set
        {
            _items = value;
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(ItemCount));
        }
    }

    public bool HasItems => Items.Count > 0;
    public int ItemCount => Items.Count;

    // Nutritional totals (from model)
    public double TotalCalories => _model.TotalCalories;
    public double TotalProtein => _model.TotalProtein;
    public double TotalFat => _model.TotalFat;
    public double TotalCarbohydrates => _model.TotalCarbohydrates;

    public string TotalCaloriesDisplay => $"{TotalCalories:F0} kcal";
    public string TotalProteinDisplay => $"P: {TotalProtein:F1}g";
    public string TotalFatDisplay => $"F: {TotalFat:F1}g";
    public string TotalCarbsDisplay => $"C: {TotalCarbohydrates:F1}g";
    public string NutritionSummary => $"{TotalCaloriesDisplay} | {TotalProteinDisplay} | {TotalFatDisplay} | {TotalCarbsDisplay}";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ItemsChanged;
    public event EventHandler? NameChanged;

    public MealTimeViewModel(MealTime model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));

        // Wrap all model items in ViewModels
        _items = new ObservableCollection<MealPlanItemViewModel>(
            model.Items.Select(item => new MealPlanItemViewModel(item))
        );

        // Subscribe to item weight changes
        foreach (var itemVm in _items)
        {
            itemVm.WeightChanged += (s, e) => ItemsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Adds a new item (called by parent ViewModel)
    /// </summary>
    public void AddItem(Product product, double weight = 100.0)
    {
        var modelItem = new MealPlanItem(product, weight);
        _model.Items.Add(modelItem);

        var itemVm = new MealPlanItemViewModel(modelItem);
        itemVm.WeightChanged += (s, e) => ItemsChanged?.Invoke(this, EventArgs.Empty);

        Items.Add(itemVm);
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ItemCount));

        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes an item (called by parent ViewModel)
    /// </summary>
    public void RemoveItem(MealPlanItemViewModel itemVm)
    {
        if (itemVm == null) return;

        var modelItem = itemVm.GetModel();
        _model.Items.Remove(modelItem);
        Items.Remove(itemVm);

        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ItemCount));

        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Refreshes all nutritional displays after recalculation (called by parent ViewModel)
    /// </summary>
    internal void RefreshNutrition()
    {
        // Refresh totals
        OnPropertyChanged(nameof(TotalCalories));
        OnPropertyChanged(nameof(TotalProtein));
        OnPropertyChanged(nameof(TotalFat));
        OnPropertyChanged(nameof(TotalCarbohydrates));
        OnPropertyChanged(nameof(TotalCaloriesDisplay));
        OnPropertyChanged(nameof(TotalProteinDisplay));
        OnPropertyChanged(nameof(TotalFatDisplay));
        OnPropertyChanged(nameof(TotalCarbsDisplay));
        OnPropertyChanged(nameof(NutritionSummary));

        // Refresh all item nutrition
        foreach (var itemVm in Items)
        {
            itemVm.RefreshNutrition();
        }
    }

    /// <summary>
    /// Gets the underlying model (internal - only for parent ViewModel/Service access)
    /// </summary>
    internal MealTime GetModel() => _model;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
