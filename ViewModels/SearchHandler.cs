namespace Lab4.ViewModels;

using System.Timers;

/// <summary>
/// Handles search debouncing logic
/// </summary>
public class SearchHandler
{
    private readonly MainWindowViewModel _viewModel;
    private System.Timers.Timer? _debounceTimer;
    private string _lastSearchQuery = string.Empty;
    private List<ProductViewModel> _currentResults = new();
    private bool _isSearching = false;

    public const int DebounceDelayMs = 300;

    public List<ProductViewModel> CurrentResults => _currentResults;

    public event EventHandler<List<ProductViewModel>>? ResultsUpdated;

    public SearchHandler(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    /// <summary>
    /// Initiates a debounced search
    /// </summary>
    public void Search(string query)
    {
        // Stop existing timer
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();

        if (string.IsNullOrWhiteSpace(query))
        {
            ClearResults();
            return;
        }

        // Prevent duplicate searches
        if (query == _lastSearchQuery)
        {
            return;
        }

        // Create new timer for debouncing
        _debounceTimer = new System.Timers.Timer(DebounceDelayMs);
        _debounceTimer.Elapsed += async (sender, e) =>
        {
            _debounceTimer?.Stop();
            await PerformSearchAsync(query);
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    private async Task PerformSearchAsync(string query)
    {
        // Check if this search was already performed (race condition protection)
        if (query == _lastSearchQuery)
        {
            return;
        }

        // Prevent concurrent searches
        if (_isSearching)
        {
            return;
        }

        try
        {
            _isSearching = true;
            _lastSearchQuery = query;
            _currentResults = await _viewModel.SearchProductsAsync(query);

            // Notify subscribers with all results (ProductListView handles pagination)
            ResultsUpdated?.Invoke(this, _currentResults);
        }
        finally
        {
            _isSearching = false;
        }
    }

    private void ClearResults()
    {
        _lastSearchQuery = string.Empty;
        _currentResults.Clear();
        ResultsUpdated?.Invoke(this, new List<ProductViewModel>());
    }

    public void Dispose()
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
    }
}
