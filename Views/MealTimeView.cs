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
    public MealTimeViewModel ViewModel => _viewModel;
    public event EventHandler<MealTimeViewModel>? RemoveMealTimeRequested;
    public event EventHandler<string>? DragStarted;
    public event EventHandler? DragEnded;
    public event Func<string, MealTimeViewModel, bool>? ValidateNameUnique;

    public MealTimeView(MealTimeViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _container = Box.New(Orientation.Vertical, 5);
        _container.AddCssClass("card");
        _container.AddCssClass("mealtime-card");

        _itemsContainer = Box.New(Orientation.Vertical, 0);

        // Setup drag source for custom meal times
        if (_viewModel.IsCustom)
        {
            var dragSource = DragSource.New();
            dragSource.SetActions(Gdk.DragAction.Move);

            dragSource.OnPrepare += (source, args) =>
            {
                // Store the meal time name as drag data
                var value = new GObject.Value();
                value.Init(GObject.Type.String);
                value.SetString(_viewModel.Name);
                var content = Gdk.ContentProvider.NewForValue(value);
                return content;
            };

            dragSource.OnDragBegin += (source, args) =>
            {
                _container.AddCssClass("dragging");
                DragStarted?.Invoke(this, _viewModel.Name);
            };

            dragSource.OnDragEnd += (source, args) =>
            {
                _container.RemoveCssClass("dragging");
                DragEnded?.Invoke(this, EventArgs.Empty);
            };

            _container.AddController(dragSource);
        }

        BuildUI();

        // Subscribe to ViewModel changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MealTimeViewModel.Items) ||
                e.PropertyName == nameof(MealTimeViewModel.HasItems))
            {
                RebuildItems();
            }
            else if (e.PropertyName == nameof(MealTimeViewModel.NutritionSummary) ||
                     e.PropertyName == nameof(MealTimeViewModel.Name))
            {
                // Rebuild entire UI to update totals or name
                RebuildUI();
            }
        };
    }

    private void BuildUI()
    {
        // Header row with mealtime name and action buttons
        var headerRow = Box.New(Orientation.Horizontal, 10);
        headerRow.AddCssClass("card-content");

        // Add drag handle for custom meal times (left side)
        if (_viewModel.IsCustom)
        {
            var dragHandle = Label.New("≡");
            dragHandle.AddCssClass("drag-handle");
            dragHandle.SetTooltipText("Drag to reorder");
            headerRow.Append(dragHandle);
        }

        var header = Label.New(_viewModel.Name);
        header.AddCssClass("title-3");
        header.Halign = Align.Start;
        header.Hexpand = true;
        headerRow.Append(header);

        // Action buttons (only for custom meal times)
        if (_viewModel.CanRename)
        {
            var renameButton = Button.NewWithLabel("Rename");
            renameButton.OnClicked += OnRenameClicked;
            headerRow.Append(renameButton);
        }

        if (_viewModel.CanRemove)
        {
            var removeButton = Button.NewWithLabel("Remove");
            removeButton.AddCssClass("destructive-action");
            removeButton.OnClicked += OnRemoveMealTimeClicked;
            headerRow.Append(removeButton);
        }

        _container.Append(headerRow);

        // Items container
        _container.Append(_itemsContainer);
        RebuildItems();

        // Mealtime totals
        var totalsLabel = Label.New(_viewModel.NutritionSummary);
        totalsLabel.AddCssClass("calculated-value");
        totalsLabel.AddCssClass("card-content");
        totalsLabel.Halign = Align.Start;
        _container.Append(totalsLabel);
    }

    private void OnRenameClicked(Button sender, EventArgs args)
    {
        var dialog = new Window();
        dialog.SetTitle("Rename Meal Time");
        dialog.SetDefaultSize(300, 150);
        dialog.SetModal(true);

        var contentBox = Box.New(Orientation.Vertical, 10);
        contentBox.AddCssClass("dialog-content");

        var label = Label.New("New name:");
        label.Halign = Align.Start;
        contentBox.Append(label);

        var entry = Entry.New();
        entry.SetText(_viewModel.Name);
        contentBox.Append(entry);

        var buttonBox = Box.New(Orientation.Horizontal, 10);
        buttonBox.Halign = Align.End;
        buttonBox.AddCssClass("dialog-button-box");

        var cancelButton = Button.NewWithLabel("Cancel");
        cancelButton.OnClicked += (s, e) => dialog.Close();
        buttonBox.Append(cancelButton);

        var okButton = Button.NewWithLabel("OK");
        okButton.AddCssClass("suggested-action");
        okButton.OnClicked += (s, e) =>
        {
            var newName = entry.GetText();
            if (!string.IsNullOrWhiteSpace(newName))
            {
                // Validate uniqueness before renaming
                var isUnique = ValidateNameUnique?.Invoke(newName, _viewModel) ?? true;
                if (!isUnique)
                {
                    // Show error dialog for duplicate name
                    var errorDialog = new Window();
                    errorDialog.SetTitle("Error");
                    errorDialog.SetDefaultSize(300, 100);
                    errorDialog.SetModal(true);

                    var errorBox = Box.New(Orientation.Vertical, 10);
                    errorBox.AddCssClass("dialog-content");

                    var errorLabel = Label.New($"A meal time with the name '{newName}' already exists.\nPlease choose a different name.");
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

                _viewModel.Name = newName;
                dialog.Close();
            }
        };
        buttonBox.Append(okButton);

        contentBox.Append(buttonBox);
        dialog.SetChild(contentBox);
        dialog.Show();
    }

    private void OnRemoveMealTimeClicked(Button sender, EventArgs args)
    {
        RemoveMealTimeRequested?.Invoke(this, _viewModel);
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
            emptyLabel.AddCssClass("empty-state");
            emptyLabel.AddCssClass("card-content");
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
