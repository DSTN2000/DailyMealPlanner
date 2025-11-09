namespace Lab4.Views;

using Gtk;
using Lab4.ViewModels;

/// <summary>
/// View for a meal time section (e.g., Breakfast, Lunch, Dinner)
/// </summary>
public class MealTimeView
{
    private readonly MealTimeViewModel _viewModel;
    private readonly Box _container;
    private readonly Box _itemsContainer;

    public Widget Widget => _container;

    public MealTimeView(MealTimeViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _container = Box.New(Orientation.Vertical, 5);
        _container.AddCssClass("card");
        _container.AddCssClass("mealtime-card");

        _itemsContainer = Box.New(Orientation.Vertical, 0);

        BuildUI();

        // Subscribe to ViewModel changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MealTimeViewModel.Items) ||
                e.PropertyName == nameof(MealTimeViewModel.HasItems))
            {
                RebuildItems();
            }
            else if (e.PropertyName == nameof(MealTimeViewModel.NutritionSummary))
            {
                // Rebuild entire UI to update totals
                RebuildUI();
            }
        };
    }

    private void BuildUI()
    {
        // Header with mealtime name
        var header = Label.New(_viewModel.Name);
        header.AddCssClass("title-3");
        header.AddCssClass("mealtime-header");
        header.Halign = Align.Start;
        _container.Append(header);

        // Items container
        _container.Append(_itemsContainer);
        RebuildItems();

        // Mealtime totals
        var totalsLabel = Label.New(_viewModel.NutritionSummary);
        totalsLabel.AddCssClass("calculated-value");
        totalsLabel.AddCssClass("mealtime-totals");
        totalsLabel.Halign = Align.Start;
        _container.Append(totalsLabel);
    }

    private void RebuildItems()
    {
        // Clear existing items
        while (_itemsContainer.GetFirstChild() != null)
        {
            _itemsContainer.Remove(_itemsContainer.GetFirstChild()!);
        }

        if (_viewModel.HasItems)
        {
            // Create a view for each item ViewModel
            foreach (var itemVm in _viewModel.Items)
            {
                var itemView = new MealPlanItemView(itemVm);

                // Handle remove request
                itemView.RemoveRequested += (s, e) =>
                {
                    _viewModel.RemoveItem(itemVm);
                };

                _itemsContainer.Append(itemView.Widget);
            }
        }
        else
        {
            var emptyLabel = Label.New("No items yet");
            emptyLabel.AddCssClass("dim-label");
            emptyLabel.AddCssClass("mealtime-empty");
            emptyLabel.Halign = Align.Start;
            _itemsContainer.Append(emptyLabel);
        }
    }

    private void RebuildUI()
    {
        // Clear and rebuild entire UI
        while (_container.GetFirstChild() != null)
        {
            _container.Remove(_container.GetFirstChild()!);
        }
        BuildUI();
    }
}
