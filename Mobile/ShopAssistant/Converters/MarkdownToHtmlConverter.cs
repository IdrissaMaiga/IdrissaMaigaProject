using System.Globalization;
using Markdig;

namespace ShopAssistant.Converters;

/// <summary>
/// Converts markdown text to HTML for display in WebView
/// </summary>
public class MarkdownToHtmlConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string markdown || string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        try
        {
            // Convert markdown to HTML using Markdig
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var html = Markdown.ToHtml(markdown, pipeline);
            
            // Wrap in a styled HTML container for better display
            var styledHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            font-size: 15px;
            line-height: 1.5;
            color: #333;
            margin: 0;
            padding: 8px;
            word-wrap: break-word;
        }}
        p {{
            margin: 0 0 8px 0;
        }}
        ul, ol {{
            margin: 8px 0;
            padding-left: 24px;
        }}
        li {{
            margin: 4px 0;
        }}
        code {{
            background-color: #f4f4f4;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
            font-size: 0.9em;
        }}
        pre {{
            background-color: #f4f4f4;
            padding: 12px;
            border-radius: 6px;
            overflow-x: auto;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
        }}
        strong {{
            font-weight: 600;
        }}
        em {{
            font-style: italic;
        }}
        a {{
            color: #007AFF;
            text-decoration: none;
        }}
        a:active {{
            opacity: 0.7;
        }}
        h1, h2, h3, h4, h5, h6 {{
            margin: 12px 0 8px 0;
            font-weight: 600;
        }}
        h1 {{ font-size: 1.5em; }}
        h2 {{ font-size: 1.3em; }}
        h3 {{ font-size: 1.1em; }}
        blockquote {{
            border-left: 3px solid #ddd;
            margin: 8px 0;
            padding-left: 12px;
            color: #666;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 8px 0;
        }}
        th, td {{
            border: 1px solid #ddd;
            padding: 8px;
            text-align: left;
        }}
        th {{
            background-color: #f4f4f4;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    {html}
</body>
</html>";
            
            return styledHtml;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error converting markdown to HTML: {ex.Message}");
            // Return plain text wrapped in HTML if conversion fails
            // Return plain text wrapped in HTML if conversion fails
            var escaped = markdown
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            font-size: 15px;
            line-height: 1.5;
            color: #333;
            margin: 0;
            padding: 8px;
            white-space: pre-wrap;
            word-wrap: break-word;
        }}
    </style>
</head>
<body>
    {escaped}
</body>
</html>";
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

