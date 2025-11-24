namespace ProductAssistant.Core.Configuration;

/// <summary>
/// Configuration options for database settings
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";
    
    public string Path { get; set; } = "productassistant.db";
    public string? ConnectionString { get; set; }
}

