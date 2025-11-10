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
        scrolled.Child = _productsContainer;

        _expander.SetChild(scrolled);
    }

    private Expander CreateSubcategoryExpander(string subcategoryName, List<ProductViewModel> products)
    {
        var expander = Expander.New($"{subcategoryName} ({products.Count})");
        expander.AddCssClass("subcategory-expander");

        var productsBox = Box.New(Orientation.Vertical, 2);
        productsBox.AddCssClass("subcategory-content");
        var isLoaded = false;
        var state = new PaginationState { LoadedCount = 0, PageSize = 50 };

        // Lazy load products when subcategory is expanded
        // Use OnNotify to monitor "expanded" property changes
        expander.OnNotify += (sender, args) =>
        {
            if (args.Pspec.GetName() == "expanded" && expander.GetExpanded() && !isLoaded)
            {
                LoadProductsPage(productsBox, products, state);
                isLoaded = true;
            }
        };

        expander.SetChild(productsBox);
        return expander;
    }

    private class PaginationState
    {
        public int LoadedCount { get; set; }
        public int PageSize { get; set; }
    }

    private void LoadProductsPage(Box container, List<ProductViewModel> products, PaginationState state)
    {
        var itemsToLoad = Math.Min(state.PageSize, products.Count - state.LoadedCount);

        if (itemsToLoad <= 0)
        {
            return;
        }

        var startIndex = state.LoadedCount;
        var endIndex = startIndex + itemsToLoad;

        // Remove old "Load More" button if exists
        var lastChild = container.GetLastChild();
        if (lastChild != null && lastChild is Button)
        {
            container.Remove(lastChild);
        }

        // Create product buttons (similar to search results)
        for (int i = startIndex; i < endIndex; i++)
        {
            var productVm = products[i];

            var productButton = Button.New();
            productButton.AddCssClass("flat");
            productButton.Hexpand = true;

            var productBox = Box.New(Orientation.Vertical, 3);
            productBox.Halign = Align.Start;

            var nameLabel = Label.New(productVm.Name);
            nameLabel.AddCssClass("product-name");
            nameLabel.Halign = Align.Start;
            nameLabel.Wrap = true;
            productBox.Append(nameLabel);

            var infoLabel = Label.New($"{productVm.CaloriesDisplay} • {productVm.Category}");
            infoLabel.AddCssClass("dim-label");
            infoLabel.Halign = Align.Start;
            productBox.Append(infoLabel);

            productButton.Child = productBox;

            // Handle click
            var capturedProductVm = productVm;
            productButton.OnClicked += (s, e) =>
            {
                ProductClicked?.Invoke(this, capturedProductVm);
            };

            container.Append(productButton);
        }

        state.LoadedCount += itemsToLoad;

        // Add "Load More" button if there are more products
        if (state.LoadedCount < products.Count)
        {
            var loadMoreButton = Button.NewWithLabel($"Load More ({state.LoadedCount}/{products.Count} shown)");
            loadMoreButton.AddCssClass("flat");
            loadMoreButton.OnClicked += (s, e) =>
            {
                LoadProductsPage(container, products, state);
            };
            container.Append(loadMoreButton);
        }
        else if (products.Count > state.PageSize)
        {
            var allLoadedLabel = Label.New($"All {products.Count} products loaded");
            allLoadedLabel.AddCssClass("dim-label");
            allLoadedLabel.Halign = Align.Start;
            container.Append(allLoadedLabel);
        }
    }
}
