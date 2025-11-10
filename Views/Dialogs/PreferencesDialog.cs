namespace Lab4.Views.Dialogs;

using Gtk;
using Lab4.ViewModels;
using static Lab4.ViewModels.PreferencesViewModel;

public class PreferencesDialog
{
    private readonly Window _dialog;
    private readonly PreferencesViewModel _viewModel;

    // Anthropometry fields
    private Entry _weightEntry = null!;
    private Entry _heightEntry = null!;
    private Entry _ageEntry = null!;
    private Label _bmiLabel = null!;

    // Activity level radio buttons
    private CheckButton _sedentaryRadio = null!;
    private CheckButton _moderateRadio = null!;
    private CheckButton _mediumRadio = null!;
    private CheckButton _highRadio = null!;

    // ARM field
    private Label _armLabel = null!;

    // Daily norm fields
    private Label _caloriesLabel = null!;
    private Label _proteinLabel = null!;
    private Label _fatLabel = null!;
    private Label _carbsLabel = null!;

    public PreferencesDialog(Window parent, MainWindowViewModel mainViewModel)
    {
        _viewModel = new PreferencesViewModel(
            mainViewModel.CurrentUser,
            () => mainViewModel.SaveUserConfiguration()
        );

        _dialog = Window.New();
        _dialog.Title = "Preferences";
        _dialog.SetDefaultSize(350, 600);
        _dialog.SetTransientFor(parent);
        _dialog.Modal = true;

        BuildUI();
    }

    private void BuildUI()
    {
        var contentBox = Box.New(Orientation.Vertical, 0);

        var mainBox = Box.New(Orientation.Vertical, 15);
        mainBox.AddCssClass("dialog-content");

        // Anthropometry section
        mainBox.Append(CreateAnthropometrySection());

        // Activity section
        mainBox.Append(CreateActivitySection());

        // ARM section
        mainBox.Append(CreateArmSection());

        // Daily norm section
        mainBox.Append(CreateDailyNormSection());

        // Scrolled window for the content
        var scrolled = ScrolledWindow.New();
        scrolled.SetPolicy(PolicyType.Never, PolicyType.Automatic);
        scrolled.Vexpand = true;
        scrolled.Child = mainBox;

        contentBox.Append(scrolled);

        // Button box
        var buttonBox = Box.New(Orientation.Horizontal, 10);
        buttonBox.AddCssClass("dialog-button-box");
        buttonBox.Halign = Align.End;

        var applyButton = Button.NewWithLabel("Apply");
        applyButton.OnClicked += OnApplyClicked;
        buttonBox.Append(applyButton);

        contentBox.Append(buttonBox);

        _dialog.Child = contentBox;
    }

    private Box CreateAnthropometrySection()
    {
        var section = Box.New(Orientation.Vertical, 10);

        // Section title
        var titleLabel = Label.New("Anthropometry");
        titleLabel.Halign = Align.Start;
        titleLabel.AddCssClass("heading");
        section.Append(titleLabel);

        // Weight
        section.Append(CreateLabeledEntry("Weight:", _viewModel.WeightText, "kg", out _weightEntry));
        _weightEntry.SetInputPurpose(Gtk.InputPurpose.Number);

        // Height
        section.Append(CreateLabeledEntry("Height:", _viewModel.HeightText, "cm", out _heightEntry));
        _heightEntry.SetInputPurpose(Gtk.InputPurpose.Number);

        // Age
        section.Append(CreateLabeledEntry("Age:", _viewModel.AgeText, "years", out _ageEntry));
        _ageEntry.SetInputPurpose(Gtk.InputPurpose.Number);

        // BMI (read-only)
        section.Append(CreateLabeledValue("BMI:", "", out _bmiLabel));

        // Connect calculation
        _weightEntry.OnChanged += (s, e) => UpdateCalculatedValues();
        _heightEntry.OnChanged += (s, e) => UpdateCalculatedValues();
        _ageEntry.OnChanged += (s, e) => UpdateCalculatedValues();

        return section;
    }

    private Box CreateActivitySection()
    {
        var section = Box.New(Orientation.Vertical, 10);

        // Section title
        var titleLabel = Label.New("Activity Level");
        titleLabel.Halign = Align.Start;
        titleLabel.AddCssClass("heading");
        section.Append(titleLabel);

        // Radio buttons - create using factory
        _sedentaryRadio = CreateActivityRadioButton("Sedentary lifestyle", ActivityLevelSedentary, null);
        section.Append(_sedentaryRadio);

        _moderateRadio = CreateActivityRadioButton("Moderate activity", ActivityLevelModerate, _sedentaryRadio);
        section.Append(_moderateRadio);

        _mediumRadio = CreateActivityRadioButton("Medium activity", ActivityLevelMedium, _sedentaryRadio);
        section.Append(_mediumRadio);

        _highRadio = CreateActivityRadioButton("High activity", ActivityLevelHigh, _sedentaryRadio);
        section.Append(_highRadio);

        // Connect to calculation
        _sedentaryRadio.OnToggled += (s, e) => UpdateCalculatedValues();
        _moderateRadio.OnToggled += (s, e) => UpdateCalculatedValues();
        _mediumRadio.OnToggled += (s, e) => UpdateCalculatedValues();
        _highRadio.OnToggled += (s, e) => UpdateCalculatedValues();

        return section;
    }

    private Box CreateArmSection()
    {
        var section = Box.New(Orientation.Vertical, 5);

        section.Append(CreateLabeledValue("Activity Multiplier (ARM):", "", out _armLabel));

        return section;
    }

    private Box CreateDailyNormSection()
    {
        var section = Box.New(Orientation.Vertical, 10);

        // Section title
        var titleLabel = Label.New("Daily Nutritional Needs");
        titleLabel.Halign = Align.Start;
        titleLabel.AddCssClass("heading");
        section.Append(titleLabel);

        // Calories
        section.Append(CreateLabeledValue("Calories:", "kcal", out _caloriesLabel));

        // Proteins
        section.Append(CreateLabeledValue("Protein:", "g", out _proteinLabel));

        // Fats
        section.Append(CreateLabeledValue("Fat:", "g", out _fatLabel));

        // Carbohydrates
        section.Append(CreateLabeledValue("Carbohydrates:", "g", out _carbsLabel));

        return section;
    }

    private CheckButton CreateActivityRadioButton(string label, int activityLevelValue, CheckButton? group)
    {
        var radioButton = CheckButton.NewWithLabel(label);

        if (group != null)
        {
            radioButton.SetGroup(group);
        }

        radioButton.Active = _viewModel.ActivityLevelIndex == activityLevelValue;

        return radioButton;
    }

    private Box CreateLabeledEntry(string labelText, string defaultValue, string unit, out Entry entry)
    {
        var row = Box.New(Orientation.Horizontal, 10);

        var label = Label.New(labelText);
        label.AddCssClass("form-label");
        label.Halign = Align.Start;
        row.Append(label);

        entry = Entry.New();
        entry.SetText(defaultValue);
        entry.Hexpand = true;
        row.Append(entry);

        if (!string.IsNullOrEmpty(unit))
        {
            var unitLabel = Label.New(unit);
            unitLabel.AddCssClass("form-unit-label");
            row.Append(unitLabel);
        }

        return row;
    }

    private Box CreateLabeledValue(string labelText, string unit, out Label valueLabel)
    {
        var row = Box.New(Orientation.Horizontal, 10);

        var label = Label.New(labelText);
        label.AddCssClass("form-label");
        label.Halign = Align.Start;
        row.Append(label);

        valueLabel = Label.New("");
        valueLabel.Halign = Align.End;
        valueLabel.Hexpand = true;
        valueLabel.AddCssClass("calculated-value");
        row.Append(valueLabel);

        if (!string.IsNullOrEmpty(unit))
        {
            var unitLabel = Label.New(unit);
            unitLabel.AddCssClass("form-unit-label");
            row.Append(unitLabel);
        }

        return row;
    }

    private void UpdateCalculatedValues()
    {
        var weightText = _weightEntry.GetBuffer().GetText();
        var heightText = _heightEntry.GetBuffer().GetText();
        var ageText = _ageEntry.GetBuffer().GetText();
        var activityLevelIndex = GetSelectedActivityLevelIndex();

        // Get preview from ViewModel (handles all parsing and calculations)
        var preview = _viewModel.PreviewCalculations(weightText, heightText, ageText, activityLevelIndex);

        if (!preview.isValid)
        {
            // Don't update if invalid input
            return;
        }

        // Update UI with preview values from ViewModel
        _bmiLabel.SetLabel(preview.previewBMI.Replace("BMI: ", ""));
        _armLabel.SetLabel(preview.previewARM.Replace("ARM: ", ""));
        _caloriesLabel.SetLabel(preview.previewCalories.Replace(" kcal", ""));
        _proteinLabel.SetLabel(preview.previewProtein.Replace("Protein: ", "").Replace("g", ""));
        _fatLabel.SetLabel(preview.previewFat.Replace("Fat: ", "").Replace("g", ""));
        _carbsLabel.SetLabel(preview.previewCarbs.Replace("Carbs: ", "").Replace("g", ""));
    }

    private int GetSelectedActivityLevelIndex()
    {
        if (_sedentaryRadio.Active) return ActivityLevelSedentary;
        if (_moderateRadio.Active) return ActivityLevelModerate;
        if (_mediumRadio.Active) return ActivityLevelMedium;
        if (_highRadio.Active) return ActivityLevelHigh;
        return ActivityLevelModerate; // Default to Moderate
    }

    private void OnApplyClicked(Button sender, EventArgs args)
    {
        var weightText = _weightEntry.GetBuffer().GetText();
        var heightText = _heightEntry.GetBuffer().GetText();
        var ageText = _ageEntry.GetBuffer().GetText();
        var activityLevelIndex = GetSelectedActivityLevelIndex();

        // Validate and apply through ViewModel (passes strings, ViewModel handles parsing)
        var result = _viewModel.ValidateAndApply(weightText, heightText, ageText, activityLevelIndex);

        if (!result.isValid)
        {
            ShowWarningDialog(result.errorTitle, result.errorMessage);
            return;
        }

        // If validation passes, close the dialog
        _dialog.Close();
    }

    private void ShowWarningDialog(string title, string message)
    {
        var dialog = new AlertDialog();
        dialog.Message = title;
        dialog.SetDetail(message);
        dialog.SetButtons(new[] { "OK" });
        dialog.SetDefaultButton(0);
        dialog.SetCancelButton(0);

        dialog.Show(_dialog);
    }

    public void Show()
    {
        // Perform initial calculation with default values
        UpdateCalculatedValues();
        _dialog.Show();
    }
}
