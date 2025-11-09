namespace Lab4.Views;

using Gtk;
using Lab4.ViewModels;

/// <summary>
/// View for the daily meal plan (shows all meal times with progress card)
/// </summary>
public class DailyMealPlanView
{
    private readonly DailyMealPlanViewModel _viewModel;
    private readonly Box _container;
    private readonly Box _dailyTotalsCard;
    private readonly Box _mealTimesContainer;

    public Widget Widget => _container;

    public DailyMealPlanView(DailyMealPlanViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _container = Box.New(Orientation.Vertical, 0);
        _dailyTotalsCard = Box.New(Orientation.Vertical, 10);
        _mealTimesContainer = Box.New(Orientation.Vertical, 10);

        BuildUI();

        // Subscribe to ViewModel changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DailyMealPlanViewModel.MealTimes))
            {
                RebuildMealTimes();
            }
            else if (e.PropertyName?.StartsWith("Total") == true)
            {
                RebuildDailyTotals();
            }
        };
    }

    private void BuildUI()
    {
        // Daily totals card (sticky at top)
        BuildDailyTotalsCard();
        _container.Append(_dailyTotalsCard);

        // Create scrolled window for meal times
        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolledWindow.Vexpand = true;

        _mealTimesContainer.AddCssClass("meal-times-container");

        scrolledWindow.Child = _mealTimesContainer;
        _container.Append(scrolledWindow);

        RebuildMealTimes();
    }

    private void BuildDailyTotalsCard()
    {
        // Clear existing
        while (_dailyTotalsCard.GetFirstChild() != null)
        {
            _dailyTotalsCard.Remove(_dailyTotalsCard.GetFirstChild()!);
        }

        _dailyTotalsCard.AddCssClass("progress-card");

        var header = Label.New("Daily Progress");
        header.AddCssClass("title-3");
        header.Halign = Align.Start;
        _dailyTotalsCard.Append(header);

        // Current vs Goal - use ViewModel property
        var statusLabel = Label.New(_viewModel.CalorieProgressDisplay);
        statusLabel.AddCssClass("progress-status");
        statusLabel.Halign = Align.Start;
        _dailyTotalsCard.Append(statusLabel);

        // Main calorie progress bar - use ViewModel properties
        var progressBar = ProgressBar.New();
        progressBar.SetFraction(_viewModel.CalorieProgressFraction);
        progressBar.AddCssClass(_viewModel.CalorieProgressColorClass);
        _dailyTotalsCard.Append(progressBar);

        // Macronutrient progress bars in a single row - use ViewModel properties
        var macrosBox = Box.New(Orientation.Horizontal, 8);
        macrosBox.AddCssClass("progress-card-section");
        macrosBox.Homogeneous = true;

        macrosBox.Append(CreateMacroProgressBar(_viewModel.ProteinProgressDisplay, _viewModel.ProteinProgressFraction, _viewModel.ProteinProgressColorClass));
        macrosBox.Append(CreateMacroProgressBar(_viewModel.FatProgressDisplay, _viewModel.FatProgressFraction, _viewModel.FatProgressColorClass));
        macrosBox.Append(CreateMacroProgressBar(_viewModel.CarbsProgressDisplay, _viewModel.CarbsProgressFraction, _viewModel.CarbsProgressColorClass));

        _dailyTotalsCard.Append(macrosBox);
    }

    private static Box CreateMacroProgressBar(string displayText, double progressFraction, string colorClass)
    {
        var box = Box.New(Orientation.Vertical, 2);

        var macroLabel = Label.New(displayText);
        macroLabel.AddCssClass("caption");
        macroLabel.Halign = Align.Center;
        box.Append(macroLabel);

        var progressBar = ProgressBar.New();
        progressBar.SetFraction(progressFraction);
        progressBar.AddCssClass("macro-progress");
        progressBar.AddCssClass(colorClass);
        box.Append(progressBar);

        return box;
    }

    private void RebuildMealTimes()
    {
        // Clear existing meal time views
        while (_mealTimesContainer.GetFirstChild() != null)
        {
            _mealTimesContainer.Remove(_mealTimesContainer.GetFirstChild()!);
        }

        // Create a view for each meal time ViewModel
        foreach (var mealTimeVm in _viewModel.MealTimes)
        {
            var mealTimeView = new MealTimeView(mealTimeVm);
            _mealTimesContainer.Append(mealTimeView.Widget);
        }
    }

    private void RebuildDailyTotals()
    {
        BuildDailyTotalsCard();
    }
}
