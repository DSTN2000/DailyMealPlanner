namespace Lab4.Views;

using Gtk;
using Lab4.Services;

public class PreferencesDialog
{
    private readonly Window _dialog;

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

    public PreferencesDialog(Window parent)
    {
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
        mainBox.MarginTop = 20;
        mainBox.MarginBottom = 20;
        mainBox.MarginStart = 20;
        mainBox.MarginEnd = 20;

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
        buttonBox.MarginTop = 10;
        buttonBox.MarginBottom = 10;
        buttonBox.MarginStart = 20;
        buttonBox.MarginEnd = 20;
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
        section.Append(CreateLabeledEntry("Weight:", "75", "kg", out _weightEntry));
        _weightEntry.SetInputPurpose(Gtk.InputPurpose.Number);

        // Height
        section.Append(CreateLabeledEntry("Height:", "170", "cm", out _heightEntry));
        _heightEntry.SetInputPurpose(Gtk.InputPurpose.Number);

        // Age
        section.Append(CreateLabeledEntry("Age:", "30", "years", out _ageEntry));
        _ageEntry.SetInputPurpose(Gtk.InputPurpose.Number);

        // BMI (read-only)
        section.Append(CreateLabeledValue("BMI:", "", out _bmiLabel));

        // Connect calculation
        _weightEntry.OnChanged += (s, e) => CalculateAll();
        _heightEntry.OnChanged += (s, e) => CalculateAll();
        _ageEntry.OnChanged += (s, e) => CalculateAll();

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

        // Radio buttons
        _sedentaryRadio = CheckButton.NewWithLabel("Sedentary lifestyle");
        section.Append(_sedentaryRadio);

        _moderateRadio = CheckButton.NewWithLabel("Moderate activity");
        _moderateRadio.SetGroup(_sedentaryRadio);
        _moderateRadio.Active = true; // Default selection
        section.Append(_moderateRadio);

        _mediumRadio = CheckButton.NewWithLabel("Medium activity");
        _mediumRadio.SetGroup(_sedentaryRadio);
        section.Append(_mediumRadio);

        _highRadio = CheckButton.NewWithLabel("High activity");
        _highRadio.SetGroup(_sedentaryRadio);
        section.Append(_highRadio);

        // Connect to calculation
        _sedentaryRadio.OnToggled += (s, e) => CalculateAll();
        _moderateRadio.OnToggled += (s, e) => CalculateAll();
        _mediumRadio.OnToggled += (s, e) => CalculateAll();
        _highRadio.OnToggled += (s, e) => CalculateAll();

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

    private Box CreateLabeledEntry(string labelText, string defaultValue, string unit, out Entry entry)
    {
        var row = Box.New(Orientation.Horizontal, 10);

        var label = Label.New(labelText);
        label.SetSizeRequest(100, -1);
        label.Halign = Align.Start;
        row.Append(label);

        entry = Entry.New();
        entry.SetText(defaultValue);
        entry.Hexpand = true;
        row.Append(entry);

        if (!string.IsNullOrEmpty(unit))
        {
            var unitLabel = Label.New(unit);
            unitLabel.SetSizeRequest(40, -1);
            row.Append(unitLabel);
        }

        return row;
    }

    private Box CreateLabeledValue(string labelText, string unit, out Label valueLabel)
    {
        var row = Box.New(Orientation.Horizontal, 10);

        var label = Label.New(labelText);
        label.SetSizeRequest(100, -1);
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
            unitLabel.SetSizeRequest(40, -1);
            row.Append(unitLabel);
        }

        return row;
    }

    private void CalculateAll()
    {
        var weightText = _weightEntry.GetBuffer().GetText();
        var heightText = _heightEntry.GetBuffer().GetText();
        var ageText = _ageEntry.GetBuffer().GetText();

        if (!double.TryParse(weightText, out var weight) || weight <= 0) return;
        if (!double.TryParse(heightText, out var height) || height <= 0) return;
        if (!double.TryParse(ageText, out var age) || age <= 0) return;

        // Calculate BMI
        var heightM = height / 100.0;
        var bmi = weight / (heightM * heightM);
        _bmiLabel.SetLabel(bmi.ToString("F1"));

        // Determine ARM based on activity level
        double armValue = 1.2; // Sedentary
        if (_moderateRadio.Active)
            armValue = 1.375;
        else if (_mediumRadio.Active)
            armValue = 1.55;
        else if (_highRadio.Active)
            armValue = 1.725;

        _armLabel.SetLabel(armValue.ToString("F3"));

        // Calculate BMR using Mifflin-St Jeor equation (assuming male for now)
        // BMR = 10 * weight(kg) + 6.25 * height(cm) - 5 * age(years) + 5
        var bmr = (10 * weight) + (6.25 * height) - (5 * age) + 5;

        // Calculate TDEE (Total Daily Energy Expenditure)
        var tdee = bmr * armValue;
        _caloriesLabel.SetLabel(tdee.ToString("F0"));

        // Calculate macronutrient distribution (standard ratios)
        // Protein: 30% of calories, 4 cal/g = tdee * 0.30 / 4
        var protein = (tdee * 0.30) / 4;
        _proteinLabel.SetLabel(protein.ToString("F1"));

        // Fat: 25% of calories, 9 cal/g = tdee * 0.25 / 9
        var fat = (tdee * 0.25) / 9;
        _fatLabel.SetLabel(fat.ToString("F1"));

        // Carbs: 45% of calories, 4 cal/g = tdee * 0.45 / 4
        var carbs = (tdee * 0.45) / 4;
        _carbsLabel.SetLabel(carbs.ToString("F1"));

        Logger.Instance.Information("Calculated: BMI={BMI}, ARM={ARM}, TDEE={TDEE}, Protein={Protein}g, Fat={Fat}g, Carbs={Carbs}g",
            bmi, armValue, tdee, protein, fat, carbs);
    }

    private void OnApplyClicked(Button sender, EventArgs args)
    {
        if (!ValidateInputs())
        {
            return;
        }

        // If validation passes, close the dialog
        Logger.Instance.Information("Preferences applied successfully");
        _dialog.Close();
    }

    private bool ValidateInputs()
    {
        var weightText = _weightEntry.GetBuffer().GetText();
        var heightText = _heightEntry.GetBuffer().GetText();
        var ageText = _ageEntry.GetBuffer().GetText();

        // Validate weight
        if (!double.TryParse(weightText, out var weight) || weight <= 0)
        {
            ShowWarningDialog("Invalid Weight", "Please enter a valid positive number for weight.");
            return false;
        }

        // Validate height
        if (!double.TryParse(heightText, out var height) || height <= 0)
        {
            ShowWarningDialog("Invalid Height", "Please enter a valid positive number for height.");
            return false;
        }

        // Validate age
        if (!double.TryParse(ageText, out var age) || age <= 0)
        {
            ShowWarningDialog("Invalid Age", "Please enter a valid positive number for age.");
            return false;
        }

        return true;
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
        CalculateAll();
        _dialog.Show();
    }
}
