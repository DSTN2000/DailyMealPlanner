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
    private string? _currentlyDraggedMealTimeName;

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

        // Add visual separator between progress and meal times
        var separator = Separator.New(Orientation.Horizontal);
        separator.AddCssClass("horizontal");
        _container.Append(separator);

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

        // Create a view for each meal time ViewModel with drop zones between them
        for (int i = 0; i < _viewModel.MealTimes.Count; i++)
        {
            var mealTimeVm = _viewModel.MealTimes[i];
            var targetIndex = i; // Capture for closure

            // Add drop zone before this meal time
            var dropZoneBefore = CreateDropZone(targetIndex);
            _mealTimesContainer.Append(dropZoneBefore);

            // Create the meal time view
            var mealTimeView = new MealTimeView(mealTimeVm);

            // Handle remove meal time request
            mealTimeView.RemoveMealTimeRequested += (s, vm) =>
            {
                _viewModel.RemoveMealTime(vm);
            };

            // Handle name validation for rename
            mealTimeView.ValidateNameUnique += (name, vm) =>
            {
                return _viewModel.IsNameUnique(name, vm);
            };

            // Track drag state
            mealTimeView.DragStarted += (s, name) =>
            {
                _currentlyDraggedMealTimeName = name;
            };

            mealTimeView.DragEnded += (s, e) =>
            {
                _currentlyDraggedMealTimeName = null;
            };

            _mealTimesContainer.Append(mealTimeView.Widget);
        }

        // Add final drop zone at the end
        var dropZoneEnd = CreateDropZone(_viewModel.MealTimes.Count);
        _mealTimesContainer.Append(dropZoneEnd);

        // Add button to add custom meal time
        var addMealTimeButton = Button.NewWithLabel("+ Add Custom Meal Time");
        addMealTimeButton.Halign = Align.Center;
        addMealTimeButton.AddCssClass("suggested-action");
        addMealTimeButton.OnClicked += OnAddMealTimeClicked;
        _mealTimesContainer.Append(addMealTimeButton);
    }

    private Box CreateDropZone(int targetIndex)
    {
        var dropZone = Box.New(Orientation.Horizontal, 0);
        dropZone.AddCssClass("drop-zone");
        dropZone.SetSizeRequest(-1, 20);

        var dropTarget = DropTarget.New(GObject.Type.String, Gdk.DragAction.Move);

        dropTarget.OnDrop += (target, args) =>
        {
            var value = args.Value;
            if (value == null) return false;

            try
            {
                // Get the dragged meal time name
                var draggedName = value.GetString();
                if (string.IsNullOrEmpty(draggedName)) return false;

                // Find the source meal time
                var sourceMealTime = _viewModel.MealTimes.FirstOrDefault(mt => mt.Name == draggedName && mt.IsCustom);
                if (sourceMealTime == null) return false;

                // Get the current index of the source
                var sourceIndex = _viewModel.MealTimes.IndexOf(sourceMealTime);
                if (sourceIndex == -1) return false;

                // Don't allow dropping in the same position or immediately adjacent
                // (dropping above or below itself would do nothing)
                if (targetIndex == sourceIndex || targetIndex == sourceIndex + 1)
                    return false;

                // Reorder to the target index
                _viewModel.ReorderMealTime(sourceMealTime, targetIndex);
                return true;
            }
            catch
            {
                return false;
            }
        };

        dropTarget.OnEnter += (target, args) =>
        {
            // Check if this is a valid drop target using the currently dragged meal time
            if (!string.IsNullOrEmpty(_currentlyDraggedMealTimeName))
            {
                var sourceMealTime = _viewModel.MealTimes.FirstOrDefault(mt => mt.Name == _currentlyDraggedMealTimeName && mt.IsCustom);
                if (sourceMealTime != null)
                {
                    var sourceIndex = _viewModel.MealTimes.IndexOf(sourceMealTime);

                    // Only highlight if this is a valid drop position (not adjacent to source)
                    if (targetIndex != sourceIndex && targetIndex != sourceIndex + 1)
                    {
                        dropZone.AddCssClass("drop-zone-active");
                    }
                }
            }
            return Gdk.DragAction.Move;
        };

        dropTarget.OnLeave += (target, args) =>
        {
            dropZone.RemoveCssClass("drop-zone-active");
        };

        dropZone.AddController(dropTarget);
        return dropZone;
    }

    private void OnAddMealTimeClicked(Button sender, EventArgs args)
    {
        var dialog = new Window();
        dialog.SetTitle("Add Custom Meal Time");
        dialog.SetDefaultSize(300, 150);
        dialog.SetModal(true);

        var contentBox = Box.New(Orientation.Vertical, 10);
        contentBox.AddCssClass("dialog-content");

        var label = Label.New("Meal time name:");
        label.Halign = Align.Start;
        contentBox.Append(label);

        var entry = Entry.New();
        entry.SetPlaceholderText("e.g., Snack, Second Breakfast");
        contentBox.Append(entry);

        var buttonBox = Box.New(Orientation.Horizontal, 10);
        buttonBox.Halign = Align.End;
        buttonBox.AddCssClass("dialog-button-box");

        var cancelButton = Button.NewWithLabel("Cancel");
        cancelButton.OnClicked += (s, e) => dialog.Close();
        buttonBox.Append(cancelButton);

        var okButton = Button.NewWithLabel("Add");
        okButton.AddCssClass("suggested-action");
        okButton.OnClicked += (s, e) =>
        {
            var name = entry.GetText();
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (!_viewModel.AddCustomMealTime(name))
                {
                    // Show error dialog for duplicate name
                    var errorDialog = new Window();
                    errorDialog.SetTitle("Error");
                    errorDialog.SetDefaultSize(300, 100);
                    errorDialog.SetModal(true);

                    var errorBox = Box.New(Orientation.Vertical, 10);
                    errorBox.AddCssClass("dialog-content");

                    var errorLabel = Label.New($"A meal time with the name '{name}' already exists.\nPlease choose a different name.");
                    errorLabel.Halign = Align.Center;
                    errorBox.Append(errorLabel);

                    var errorOkButton = Button.NewWithLabel("OK");
                    errorOkButton.Halign = Align.Center;
                    errorOkButton.OnClicked += (s2, e2) => errorDialog.Close();
                    errorBox.Append(errorOkButton);

                    errorDialog.SetChild(errorBox);
                    errorDialog.Show();
                    return;
                }
                dialog.Close();
            }
        };
        buttonBox.Append(okButton);

        contentBox.Append(buttonBox);
        dialog.SetChild(contentBox);
        dialog.Show();
    }

    private void RebuildDailyTotals()
    {
        BuildDailyTotalsCard();
    }
}
