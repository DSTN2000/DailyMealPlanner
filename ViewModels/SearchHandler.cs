namespace Lab4.ViewModels;

using System.Timers;

/// <summary>
/// Handles search debouncing and pagination logic
/// </summary>
public class SearchHandler
{
    private readonly MainWindowViewModel _viewModel;
    private System.Timers.Timer? _debounceTimer;
    private string _lastSearchQuery = string.Empty;
    private List<ProductViewModel> _currentResults = new();
    private int _loadedResultsCount = 0;
    private bool _isSearching = false;

    public const int ResultsPageSize = 50;
    public const int DebounceDelayMs = 300;

    public List<ProductViewModel> CurrentResults => _currentResults;
    public int LoadedResultsCount => _loadedResultsCount;

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

    /// <summary>
    /// Loads more results for the current search query
    /// </summary>
    public async Task LoadMoreResultsAsync()
    {
        if (string.IsNullOrWhiteSpace(_lastSearchQuery) || _loadedResultsCount >= _currentResults.Count)
        {
            return;
        }

        // Simulate loading more results (already have them, just reveal more)
        var remainingCount = _currentResults.Count - _loadedResultsCount;
        var countToLoad = Math.Min(ResultsPageSize, remainingCount);

        // Small delay to simulate async operation
        await Task.Delay(50);

        _loadedResultsCount += countToLoad;

        // Notify subscribers
        var visibleResults = _currentResults.Take(_loadedResultsCount).ToList();
        ResultsUpdated?.Invoke(this, visibleResults);
    }

    /// <summary>
    /// Checks if more results can be loaded
    /// </summary>
    public bool CanLoadMore()
    {
        return _loadedResultsCount < _currentResults.Count;
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
            _loadedResultsCount = Math.Min(ResultsPageSize, _currentResults.Count);

            // Notify subscribers with first page of results
            var visibleResults = _currentResults.Take(_loadedResultsCount).ToList();
            ResultsUpdated?.Invoke(this, visibleResults);
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
        _loadedResultsCount = 0;
        ResultsUpdated?.Invoke(this, new List<ProductViewModel>());
    }

    public void Dispose()
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
    }
}
