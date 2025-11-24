using System.Diagnostics;

namespace ShopAssistant.Services;

/// <summary>
/// Helper class for debug logging that automatically captures logs to DebugLogService
/// Use this instead of System.Diagnostics.Debug.WriteLine for logs to appear in the debug viewer
/// </summary>
public static class DebugHelper
{
    private static DebugLogService? _debugLogService;

    /// <summary>
    /// Initialize the debug helper with the debug log service
    /// </summary>
    internal static void Initialize(DebugLogService debugLogService)
    {
        _debugLogService = debugLogService;
    }

    /// <summary>
    /// Write a debug message that will appear in the debug log viewer
    /// </summary>
    public static void WriteLine(string message)
    {
        // Always write to standard debug output
        Debug.WriteLine(message);
        
        // Also capture to our service if available
        _debugLogService?.AddLog(message);
    }

    /// <summary>
    /// Write a formatted debug message
    /// </summary>
    public static void WriteLine(string format, params object[] args)
    {
        var message = string.Format(format, args);
        WriteLine(message);
    }
}

