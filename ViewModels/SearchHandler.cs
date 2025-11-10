namespace Lab4.ViewModels;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

/// <summary>
/// Handles search debouncing logic
/// </summary>
public class SearchHandler : IDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private System.Timers.Timer? _debounceTimer;
    private List<ProductViewModel> _currentResults = new();
    private int _searchVersion = 0;

    public const int DebounceDelayMs = 300;

    public List<ProductViewModel> CurrentResults => _currentResults;

    public event EventHandler<(string Query, List<ProductViewModel> Results)>? ResultsUpdated;

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

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            ClearResults();
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
        var version = Interlocked.Increment(ref _searchVersion);

        try
        {
            var results = await _viewModel.SearchProductsAsync(query);

            // If another search has been started while we were searching,
            // our results are obsolete. Don't update the UI.
            if (version == _searchVersion)
            {
                _currentResults = results;
                ResultsUpdated?.Invoke(this, (query, _currentResults));
            }
        }
        catch (Exception ex)
        {
            Services.Logger.Instance.Error(ex, "An error occurred during search for query {Query}", query);
            // Only show error if this is the latest search
            if (version == _searchVersion)
            {
                // Optionally, clear results or show an error state in the UI
                _currentResults = new List<ProductViewModel>();
                ResultsUpdated?.Invoke(this, (query, _currentResults));
            }
        }
    }

    private void ClearResults()
    {
        Interlocked.Increment(ref _searchVersion); // Invalidate any ongoing searches
        _currentResults.Clear();
        ResultsUpdated?.Invoke(this, (string.Empty, new List<ProductViewModel>()));
    }

    public void Dispose()
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
