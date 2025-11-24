using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopAssistant.Services;
using System.Collections.ObjectModel;

namespace ShopAssistant.ViewModels;

public partial class DebugLogViewModel : BaseViewModel
{
    private readonly DebugLogService _debugLogService;
    private System.Timers.Timer? _updateTimer;
    private bool _updatePending = false;
    private bool _isPageActive = false;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string logsText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogs))]
    private bool hasNoLogs = true;

    public bool HasLogs => !HasNoLogs;

    public ReadOnlyObservableCollection<string> AllLogs => _debugLogService.Logs;
    
    public void OnPageAppearing()
    {
        _isPageActive = true;
        // Refresh logs when page becomes visible
        _updateTimer?.Stop();
        _updatePending = false;
        // Force immediate update when page appears
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_isPageActive)
            {
                UpdateFilteredLogsInternal();
            }
        });
    }
    
    public void OnPageDisappearing()
    {
        _isPageActive = false;
        // Stop timer and clear pending updates
        _updateTimer?.Stop();
        _updatePending = false;
    }

    public DebugLogViewModel(DebugLogService debugLogService)
    {
        _debugLogService = debugLogService;
        Title = "Debug Logs";
        
        // Initialize update timer for batching rapid updates
        _updateTimer = new System.Timers.Timer(300); // 300ms debounce
        _updateTimer.Elapsed += (sender, e) =>
        {
            _updateTimer.Stop();
            if (_updatePending)
            {
                _updatePending = false;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateFilteredLogs();
                });
            }
        };
        _updateTimer.AutoReset = false;
        
        // Subscribe to log changes via the exposed event
        _debugLogService.LogsChanged += (sender, e) =>
        {
            // Only update if page is active
            if (!_isPageActive)
                return;
                
            // Batch rapid updates to avoid RecyclerView errors
            _updatePending = true;
            _updateTimer?.Stop();
            _updateTimer?.Start();
        };
    }

    partial void OnSearchTextChanged(string value)
    {
        // Only update if page is active
        if (!_isPageActive)
            return;
            
        // Debounce search updates
        _updatePending = true;
        _updateTimer?.Stop();
        _updateTimer?.Start();
    }

    private void UpdateFilteredLogs()
    {
        // Don't update if page is not active
        if (!_isPageActive)
            return;
            
        // Postpone updates until after layout pass to prevent RecyclerView errors
        // Use Task.Delay to ensure we're not updating during scroll/layout
        Task.Delay(50).ContinueWith(_ =>
        {
            // Check again after delay in case page became inactive
            if (_isPageActive)
            {
                MainThread.BeginInvokeOnMainThread(UpdateFilteredLogsInternal);
            }
        }, TaskScheduler.Default);
    }

    private void UpdateFilteredLogsInternal()
    {
        // Ensure we're on the main thread
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(UpdateFilteredLogsInternal);
            return;
        }

        try
        {
            var logs = _debugLogService.Logs;
            var filteredLogs = new List<string>();
            
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Show all logs
                filteredLogs.AddRange(logs);
            }
            else
            {
                // Filter logs
                var searchLower = SearchText.ToLowerInvariant();
                foreach (var log in logs)
                {
                    if (log.ToLowerInvariant().Contains(searchLower))
                    {
                        filteredLogs.Add(log);
                    }
                }
            }
            
            // Update as simple text string - avoids RecyclerView issues completely
            var newLogsText = string.Join(Environment.NewLine, filteredLogs);
            if (LogsText != newLogsText)
            {
                LogsText = newLogsText;
                HasNoLogs = filteredLogs.Count == 0;
            }
        }
        catch (Exception ex)
        {
            // Silently handle errors during UI updates
            System.Diagnostics.Debug.WriteLine($"Error updating filtered logs: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CopyLogsAsync()
    {
        try
        {
            // Ensure we're on main thread for clipboard operations
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var logsToCopy = string.IsNullOrWhiteSpace(SearchText)
                        ? _debugLogService.GetAllLogsText()
                        : LogsText;
                    
                    if (string.IsNullOrWhiteSpace(logsToCopy))
                    {
                        await Shell.Current.DisplayAlert("No Logs", "There are no logs to copy.", "OK");
                        return;
                    }
                    
                    await Clipboard.SetTextAsync(logsToCopy);
                    await Shell.Current.DisplayAlert("Copied", "Debug logs copied to clipboard!", "OK");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", $"Failed to copy logs: {ex.Message}", "OK");
                }
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to copy logs: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _debugLogService.ClearLogs();
        LogsText = string.Empty;
        HasNoLogs = true;
    }

    [RelayCommand]
    private void RefreshLogs()
    {
        _updateTimer?.Stop();
        _updatePending = false;
        UpdateFilteredLogs();
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        try
        {
            // Navigate back to the main app (Chat page)
            await Shell.Current.GoToAsync("//Chat");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
        }
    }
}

