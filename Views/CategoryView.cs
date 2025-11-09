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

        // Lazy loading: only build UI when expanded
        _expander.OnActivate += (sender, args) =>
        {
            if (!_isLoaded && _expander.GetExpanded())
            {
                BuildProductsList();
                _isLoaded = true;
            }
        };
    }

    private void BuildProductsList()
    {
        if (_productsContainer != null) return;

        _productsContainer = Box.New(Orientation.Vertical, 0);
        _productsContainer.MarginStart = 10;
        _productsContainer.MarginEnd = 10;

        // Group products by subcategory (labels)
        var grouped = _viewModel.Products
            .GroupBy(p => p.Labels.FirstOrDefault() ?? "Other")
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var subcategoryExpander = CreateSubcategoryExpander(group.Key, group.ToList());
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

        // Lazy load products when subcategory is expanded
        expander.OnActivate += (sender, args) =>
        {
            if (expander.GetExpanded() && productsBox.GetFirstChild() == null)
            {
                LoadProducts(productsBox, products);
            }
        };

        expander.SetChild(productsBox);
        return expander;
    }

    private void LoadProducts(Box container, List<ProductViewModel> products)
    {
        // Use ListView for virtualization if many products
        if (products.Count > 50)
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
