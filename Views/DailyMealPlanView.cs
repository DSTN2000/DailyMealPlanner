namespace Lab4.Views;

using Gtk;
using Lab4.ViewModels;
using Lab4.Models;

/// <summary>
/// View for the daily meal plan (shows all meal times with progress card)
/// </summary>
public class DailyMealPlanView
{
    private readonly DailyMealPlanViewModel _viewModel;
    private readonly User _currentUser;
    private readonly Box _container;
    private readonly Box _dailyTotalsCard;
    private readonly Box _mealTimesContainer;

    public Widget Widget => _container;

    public DailyMealPlanView(DailyMealPlanViewModel viewModel, User currentUser)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

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

        _mealTimesContainer.MarginStart = 5;
        _mealTimesContainer.MarginEnd = 5;
        _mealTimesContainer.MarginTop = 5;
        _mealTimesContainer.MarginBottom = 5;

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

        _dailyTotalsCard.AddCssClass("card");
        _dailyTotalsCard.MarginStart = 5;
        _dailyTotalsCard.MarginEnd = 5;
        _dailyTotalsCard.MarginTop = 5;
        _dailyTotalsCard.MarginBottom = 10;

        var header = Label.New("Daily Progress");
        header.AddCssClass("title-3");
        header.Halign = Align.Start;
        header.MarginStart = 12;
        header.MarginTop = 8;
        _dailyTotalsCard.Append(header);

        // Current vs Goal
        var actualCalories = _viewModel.TotalCalories;
        var goalCalories = _currentUser.DailyCalories;
        var percentage = goalCalories > 0 ? (actualCalories / goalCalories) * 100 : 0;

        var statusLabel = Label.New($"{actualCalories:F0} / {goalCalories:F0} kcal ({percentage:F0}%)");
        statusLabel.AddCssClass("title-4");
        statusLabel.Halign = Align.Start;
        statusLabel.MarginStart = 12;
        _dailyTotalsCard.Append(statusLabel);

        // Main calorie progress bar
        var progressBar = ProgressBar.New();
        progressBar.SetFraction(Math.Min(actualCalories / goalCalories, 1.0));
        progressBar.MarginStart = 12;
        progressBar.MarginEnd = 12;
        progressBar.MarginBottom = 5;
        progressBar.AddCssClass(GetProgressColorClass(percentage));
        _dailyTotalsCard.Append(progressBar);

        // Macronutrient progress bars in a single row
        var macrosBox = Box.New(Orientation.Horizontal, 8);
        macrosBox.MarginStart = 12;
        macrosBox.MarginEnd = 12;
        macrosBox.MarginBottom = 8;
        macrosBox.Homogeneous = true;

        macrosBox.Append(CreateMacroProgressBar("P", _viewModel.TotalProtein, _currentUser.DailyProtein));
        macrosBox.Append(CreateMacroProgressBar("F", _viewModel.TotalFat, _currentUser.DailyFat));
        macrosBox.Append(CreateMacroProgressBar("C", _viewModel.TotalCarbohydrates, _currentUser.DailyCarbohydrates));

        _dailyTotalsCard.Append(macrosBox);
    }

    private static Box CreateMacroProgressBar(string label, double actual, double goal)
    {
        var box = Box.New(Orientation.Vertical, 2);

        var percentage = goal > 0 ? (actual / goal) * 100 : 0;
        var macroLabel = Label.New($"{label}: {actual:F0}/{goal:F0}g");
        macroLabel.AddCssClass("caption");
        macroLabel.Halign = Align.Center;
        box.Append(macroLabel);

        var progressBar = ProgressBar.New();
        progressBar.SetFraction(Math.Min(actual / goal, 1.0));
        progressBar.AddCssClass("macro-progress");
        progressBar.AddCssClass(GetProgressColorClass(percentage));
        box.Append(progressBar);

        return box;
    }

    private static string GetProgressColorClass(double percentage)
    {
        if (percentage < 80 || percentage > 120)
            return "error";
        else if (percentage < 90 || percentage > 110)
            return "warning";
        else
            return "success";
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
