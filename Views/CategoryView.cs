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
        _expander.AddCssClass("category-expander");

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
        _productsContainer.AddCssClass("expander-content");

        // Use subcategory groups from ViewModel (already sorted)
        foreach (var (subcategory, products) in _viewModel.SubcategoryGroups)
        {
            var subcategoryExpander = CreateSubcategoryExpander(subcategory, products);
            _productsContainer.Append(subcategoryExpander);
        }

        // Add to scrolled window for long lists
        var scrolled = ScrolledWindow.New();
        scrolled.SetPolicy(PolicyType.Never, PolicyType.Automatic);
        scrolled.SetMaxContentHeight(400);
        scrolled.SetPropagateNaturalHeight(true);
        scrolled.SetPlacement(CornerType.TopLeft); // Scrollbar on left
        scrolled.AddCssClass("product-list-scroll");
        scrolled.Child = _productsContainer;

        _expander.SetChild(scrolled);
    }

    private Expander CreateSubcategoryExpander(string subcategoryName, List<ProductViewModel> products)
    {
        var expander = Expander.New($"{subcategoryName} ({products.Count})");
        expander.AddCssClass("subcategory-expander");

        var placeholderBox = Box.New(Orientation.Vertical, 2);
        placeholderBox.AddCssClass("subcategory-content");
        var isLoaded = false;

        // Lazy load products when subcategory is expanded
        // Use OnNotify to monitor "expanded" property changes
        expander.OnNotify += (sender, args) =>
        {
            if (args.Pspec.GetName() == "expanded" && expander.GetExpanded() && !isLoaded)
            {
                var productListView = new ProductListView(products, showCount: true);
                productListView.ProductClicked += (s, productVm) =>
                {
                    ProductClicked?.Invoke(this, productVm);
                };

                var contentBox = Box.New(Orientation.Vertical, 2);
                contentBox.AddCssClass("subcategory-content");
                contentBox.Append(productListView.Widget);

                expander.SetChild(contentBox);
                isLoaded = true;
            }
        };

        expander.SetChild(placeholderBox);
        return expander;
    }
}
