namespace Lab4.Views;

using Gtk;
using Lab4.ViewModels;

/// <summary>
/// Reusable view for displaying a paginated list of products
/// </summary>
public class ProductListView
{
    private readonly Box _container;
    private readonly List<ProductViewModel> _products;
    private readonly PaginationState _state;
    private readonly bool _showCategory;
    private const int PageSize = 50;

    private Label? _countLabel;

    public event EventHandler<ProductViewModel>? ProductClicked;

    public Widget Widget => _container;

    public ProductListView(List<ProductViewModel> products, bool showCount = true, bool showCategory = false)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _container = Box.New(Orientation.Vertical, 5);
        _container.AddCssClass("panel-content");
        _state = new PaginationState { StartIndex = 0, EndIndex = Math.Min(PageSize, _products.Count) };
        _showCategory = showCategory;

        if (showCount)
        {
            _countLabel = Label.New(string.Empty);
            _countLabel.AddCssClass("dim-label");
            _countLabel.Halign = Align.Start;
            _container.Append(_countLabel);
        }

        RenderProducts();
    }

    private void RenderProducts()
    {
        // Clear all children except count label
        Widget? firstChild = _countLabel != null ? _countLabel : null;
        Widget? child = _container.GetFirstChild();

        while (child != null)
        {
            var next = child.GetNextSibling();
            if (child != firstChild)
            {
                _container.Remove(child);
            }
            child = next;
        }

        // Update count label
        if (_countLabel != null)
        {
            _countLabel.SetLabel($"Showing {_state.StartIndex + 1}-{_state.EndIndex} of {_products.Count} products");
        }

        // Add "Load Previous 50" button at top if not at start
        if (_state.StartIndex > 0)
        {
            var loadPrevButton = Button.NewWithLabel("← Load Previous 50");
            loadPrevButton.AddCssClass("flat");
            loadPrevButton.OnClicked += (s, e) => LoadPrevious();
            _container.Append(loadPrevButton);
        }

        // Create product buttons
        for (int i = _state.StartIndex; i < _state.EndIndex; i++)
        {
            var productVm = _products[i];

            var productButton = Button.New();
            productButton.AddCssClass("flat");
            productButton.AddCssClass("product-list-item");
            productButton.Hexpand = true;

            var productBox = Box.New(Orientation.Vertical, 3);
            productBox.Halign = Align.Start;

            var nameLabel = Label.New(productVm.Name);
            nameLabel.AddCssClass("product-name");
            nameLabel.Halign = Align.Start;
            nameLabel.Wrap = true;
            productBox.Append(nameLabel);

            // Show category only if showCategory is true
            var infoText = _showCategory
                ? $"{productVm.CaloriesDisplay} • {productVm.Category}"
                : productVm.CaloriesDisplay;

            var infoLabel = Label.New(infoText);
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

            _container.Append(productButton);
        }

        // Add "Load 50 More" button at bottom if there are more products
        if (_state.EndIndex < _products.Count)
        {
            var loadMoreButton = Button.NewWithLabel($"Load 50 More →");
            loadMoreButton.AddCssClass("flat");
            loadMoreButton.OnClicked += (s, e) => LoadMore();
            _container.Append(loadMoreButton);
        }
        else if (_products.Count > PageSize && _state.EndIndex == _products.Count)
        {
            var allLoadedLabel = Label.New($"All {_products.Count} products loaded");
            allLoadedLabel.AddCssClass("dim-label");
            allLoadedLabel.Halign = Align.Start;
            _container.Append(allLoadedLabel);
        }
    }

    private void LoadMore()
    {
        // Move window forward by PageSize
        _state.StartIndex += PageSize;
        _state.EndIndex = Math.Min(_state.StartIndex + PageSize, _products.Count);
        RenderProducts();
    }

    private void LoadPrevious()
    {
        // Move window backward by PageSize
        _state.StartIndex = Math.Max(0, _state.StartIndex - PageSize);
        _state.EndIndex = Math.Min(_state.StartIndex + PageSize, _products.Count);
        RenderProducts();
    }

    private class PaginationState
    {
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }
}
