using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace ShopAssistant.Services;

/// <summary>
/// Service to capture and store debug log messages from System.Diagnostics.Debug
/// </summary>
public class DebugLogService : IDisposable
{
    private readonly ObservableCollection<string> _logs = new();
    private readonly object _lockObject = new();
    private readonly int _maxLogEntries = 1000;
    private bool _isDisposed = false;
    private DebugLogTraceListener? _traceListener;

    public ReadOnlyObservableCollection<string> Logs { get; }
    
    /// <summary>
    /// Event that fires when the logs collection changes
    /// </summary>
    public event NotifyCollectionChangedEventHandler? LogsChanged;

    public DebugLogService()
    {
        Logs = new ReadOnlyObservableCollection<string>(_logs);
        
        // Subscribe to collection changes to expose them via event
        _logs.CollectionChanged += (sender, e) =>
        {
            LogsChanged?.Invoke(sender, e);
        };
        
        // Try to hook into Trace.Listeners (available in .NET Core/MAUI)
        try
        {
            _traceListener = new DebugLogTraceListener(this);
            Trace.Listeners.Add(_traceListener);
        }
        catch
        {
            // Trace.Listeners might not be available, that's okay
            // Users can call AddLog directly or use DebugHelper
        }
    }

    public void AddLog(string message)
    {
        if (_isDisposed || string.IsNullOrWhiteSpace(message))
            return;

        lock (_lockObject)
        {
            // Filter out non-app messages
            if (ShouldIncludeLog(message))
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}";
                
                _logs.Add(logEntry);
                
                // Limit log size
                while (_logs.Count > _maxLogEntries)
                {
                    _logs.RemoveAt(0);
                }
            }
        }
    }

    private bool ShouldIncludeLog(string message)
    {
        // Filter out system/framework messages
        var lowerMessage = message.ToLowerInvariant();
        
        // Exclude common system messages
        var excludePatterns = new[]
        {
            "monodroid",
            "loaded assembly",
            "egl_emulation",
            "hwui",
            "appcompatdelegate",
            "ashmem",
            "thread started",
            "open_from_bundles",
            "[choreographer]",
            "gc ",
            "dalvik",
            "art"
        };
        
        // Include if it doesn't match exclude patterns
        return !excludePatterns.Any(pattern => lowerMessage.Contains(pattern));
    }

    public void ClearLogs()
    {
        lock (_lockObject)
        {
            _logs.Clear();
        }
    }

    public string GetAllLogsText()
    {
        lock (_lockObject)
        {
            return string.Join(Environment.NewLine, _logs);
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            if (_traceListener != null)
            {
                try
                {
                    Trace.Listeners.Remove(_traceListener);
                }
                catch
                {
                    // Ignore errors during disposal
                }
                _traceListener = null;
            }
        }
    }

    private class DebugLogTraceListener : TraceListener
    {
        private readonly DebugLogService _service;

        public DebugLogTraceListener(DebugLogService service)
        {
            _service = service;
        }

        public override void Write(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _service.AddLog(message);
            }
        }

        public override void WriteLine(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _service.AddLog(message);
            }
        }
    }
}

