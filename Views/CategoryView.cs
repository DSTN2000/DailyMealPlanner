namespace Lab4.Views;

using Gtk;
using Lab4.ViewModels;

/// <summary>
/// View for displaying a category with its products
/// </summary>
public class CategoryView
{
    private readonly CategoryViewModel _viewModel;
    private readonly Expander _expander;
    private Box? _productsContainer;
    private bool _isLoaded = false;

    public Widget Widget => _expander;
    public event EventHandler<ProductViewModel>? ProductClicked;

    public CategoryView(CategoryViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _expander = Expander.New(_viewModel.Name);
        _expander.MarginTop = 5;
        _expander.MarginBottom = 5;

        // Lazy loading: load products when expanded
        // Use OnNotify to monitor "expanded" property changes
        _expander.OnNotify += async (sender, args) =>
        {
            if (args.Pspec.GetName() == "expanded" && _expander.GetExpanded() && !_isLoaded)
            {
                await LoadProductsAsync();
                _isLoaded = true;
            }
        };

        // Subscribe to ViewModel events
        _viewModel.ProductsLoaded += (s, e) => BuildProductsList();
    }

    private async Task LoadProductsAsync()
    {
        // Show loading indicator
        var loadingLabel = Label.New("Loading products...");
        _expander.SetChild(loadingLabel);

        try
        {
            await _viewModel.LoadProductsAsync();
        }
        catch (Exception ex)
        {
            var errorLabel = Label.New($"Error: {ex.Message}");
            errorLabel.AddCssClass("error");
            _expander.SetChild(errorLabel);
        }
    }

    private void BuildProductsList()
    {
        if (_productsContainer != null) return;

        _productsContainer = Box.New(Orientation.Vertical, 0);
        _productsContainer.MarginStart = 10;
        _productsContainer.MarginEnd = 10;

        // Use subcategory groups from ViewModel (already sorted)
        foreach (var (subcategory, products) in _viewModel.SubcategoryGroups)
        {
            var subcategoryExpander = CreateSubcategoryExpander(subcategory, products);
            _productsContainer.Append(subcategoryExpander);
        }

        // Add to scrolled window for long lists
        var scrolled = ScrolledWindow.New();
        scrolled.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolled.SetMaxContentHeight(400);
        scrolled.SetPropagateNaturalHeight(true);
        scrolled.Child = _productsContainer;

        _expander.SetChild(scrolled);
    }

    private Expander CreateSubcategoryExpander(string subcategoryName, List<ProductViewModel> products)
    {
        var expander = Expander.New($"{subcategoryName} ({products.Count})");
        expander.MarginTop = 3;
        expander.MarginBottom = 3;

        var productsBox = Box.New(Orientation.Vertical, 2);
        productsBox.MarginStart = 10;
        var isLoaded = false;

        // Lazy load products when subcategory is expanded
        // Use OnNotify to monitor "expanded" property changes
        expander.OnNotify += (sender, args) =>
        {
            if (args.Pspec.GetName() == "expanded" && expander.GetExpanded() && !isLoaded)
            {
                LoadProducts(productsBox, products);
                isLoaded = true;
            }
        };

        expander.SetChild(productsBox);
        return expander;
    }

    private void LoadProducts(Box container, List<ProductViewModel> products)
    {
        // Use ListView for virtualization if many products (delegate to ViewModel)
        if (_viewModel.ShouldUseVirtualization(products.Count))
        {
            var listView = CreateVirtualizedProductList(products);
            container.Append(listView);
        }
        else
        {
            // For smaller lists, create ProductView instances
            foreach (var productVm in products)
            {
                var productView = new ProductView(productVm);
                productView.ProductClicked += (s, e) =>
                {
                    ProductClicked?.Invoke(this, productVm);
                };
                container.Append(productView.Widget);
            }
        }
    }

    private Widget CreateVirtualizedProductList(List<ProductViewModel> products)
    {
        var scrolled = ScrolledWindow.New();
        scrolled.SetPolicy(PolicyType.Never, PolicyType.Automatic);
        scrolled.SetMaxContentHeight(300);

        var listBox = ListBox.New();
        listBox.SetSelectionMode(SelectionMode.None);

        foreach (var productVm in products)
        {
            var productView = new ProductView(productVm);
            productView.ProductClicked += (s, e) =>
            {
                ProductClicked?.Invoke(this, productVm);
            };

            var row = ListBoxRow.New();
            row.SetChild(productView.Widget);
            listBox.Append(row);
        }

        scrolled.SetChild(listBox);
        return scrolled;
    }
}
