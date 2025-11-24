using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;
using System.Text.RegularExpressions;
using System.Web;

namespace ProductAssistant.ScrapingService.Services;

public class DirectArukeresoScrapingService : IArukeresoScrapingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DirectArukeresoScrapingService> _logger;
    private const string BaseSearchUrl = "https://www.arukereso.hu/CategorySearch.php?st=";

    public DirectArukeresoScrapingService(HttpClient httpClient, ILogger<DirectArukeresoScrapingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure HttpClient to simulate a real browser
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "hu-HU,hu;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<List<Product>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullUrl = BaseSearchUrl + Uri.EscapeDataString(searchTerm);
            _logger.LogInformation("Scraping: {Url}", fullUrl);

            // Fetch HTML from the web
            var htmlContent = await GetHtmlFromWebAsync(fullUrl);
            if (string.IsNullOrEmpty(htmlContent))
            {
                _logger.LogWarning("Failed to retrieve HTML for search term: {SearchTerm}", searchTerm);
                return new List<Product>();
            }

            // Parse the HTML
            var products = ParseHtml(htmlContent);
            _logger.LogInformation("Found {Count} products for: {SearchTerm}", products.Count, searchTerm);
            
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products for: {SearchTerm}", searchTerm);
            return new List<Product>();
        }
    }

    public async Task<Product?> GetProductDetailsAsync(string productUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching product details from: {Url}", productUrl);
            
            var htmlContent = await GetHtmlFromWebAsync(productUrl);
            if (string.IsNullOrEmpty(htmlContent))
            {
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var product = new Product
            {
                ProductUrl = productUrl
            };

            // Extract product details (basic implementation)
            var nameNode = doc.DocumentNode.SelectSingleNode("//h1[@class='product-title']");
            if (nameNode != null)
            {
                product.Name = HtmlEntity.DeEntitize(nameNode.InnerText.Trim());
            }

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product details");
            return null;
        }
    }

    public async Task<List<Product>> ScrapeCategoryAsync(string categoryUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scraping category: {Url}", categoryUrl);
            
            var htmlContent = await GetHtmlFromWebAsync(categoryUrl);
            if (string.IsNullOrEmpty(htmlContent))
            {
                return new List<Product>();
            }

            var products = ParseHtml(htmlContent);
            _logger.LogInformation("Scraped {Count} products from category", products.Count);
            
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping category");
            return new List<Product>();
        }
    }

    private async Task<string?> GetHtmlFromWebAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network error fetching: {Url}", url);
            return null;
        }
    }

    private List<Product> ParseHtml(string html)
    {
        var results = new List<Product>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Select product boxes
        var productNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-box')]");

        if (productNodes == null)
        {
            _logger.LogWarning("No product nodes found in HTML");
            return results;
        }

        foreach (var node in productNodes)
        {
            try
            {
                var product = new Product
                {
                    CreatedAt = DateTime.UtcNow
                    // Currency will be extracted dynamically from price text
                };

                // 1. Name and URL
                var nameNode = node.SelectSingleNode(".//div[contains(@class, 'name')]//a");
                if (nameNode != null)
                {
                    product.Name = HtmlEntity.DeEntitize(nameNode.InnerText.Trim());
                    var href = nameNode.GetAttributeValue("href", "");
                    product.ProductUrl = href.StartsWith("http") ? href : $"https://www.arukereso.hu{href}";
                }

                // 2. Price and Currency (Try desktop link first, then mobile div)
                var priceNode = node.SelectSingleNode(".//a[contains(@class, 'price')]") 
                             ?? node.SelectSingleNode(".//div[contains(@class, 'price')]");
                
                if (priceNode != null)
                {
                    var priceText = HtmlEntity.DeEntitize(priceNode.InnerText.Trim());
                    
                    // Extract numeric price (e.g., "149 990 Ft" -> 149990)
                    var priceMatch = Regex.Match(priceText, @"[\d\s]+");
                    if (priceMatch.Success)
                    {
                        var priceString = priceMatch.Value.Replace(" ", "").Replace("\u00a0", "");
                        if (decimal.TryParse(priceString, out var price))
                        {
                            product.Price = price;
                        }
                    }
                    
                    // Extract currency dynamically from price text (e.g., "Ft", "EUR", "USD")
                    // Look for currency symbols or codes after the price
                    var currencyMatch = Regex.Match(priceText, @"[\d\s]+[\s]*([A-Za-z]{2,4}|[€$£¥₹])", RegexOptions.IgnoreCase);
                    if (currencyMatch.Success && currencyMatch.Groups.Count > 1)
                    {
                        var currency = currencyMatch.Groups[1].Value.Trim();
                        // Normalize common currency symbols
                        if (currency == "€") currency = "EUR";
                        else if (currency == "$") currency = "USD";
                        else if (currency == "£") currency = "GBP";
                        else if (currency == "¥") currency = "JPY";
                        else if (currency == "₹") currency = "INR";
                        
                        product.Currency = currency;
                    }
                    else
                    {
                        // Fallback: check if text contains "Ft" (Hungarian Forint)
                        if (priceText.Contains("Ft", StringComparison.OrdinalIgnoreCase))
                        {
                            product.Currency = "Ft";
                        }
                        else
                        {
                            // Default fallback if no currency found
                            product.Currency = "HUF";
                        }
                    }
                }
                else
                {
                    // No price node found, set default currency
                    product.Currency = "HUF";
                }

                // 3. Image (Lazy load handling)
                var imgNode = node.SelectSingleNode(".//div[contains(@class, 'image-link-container')]//img");
                if (imgNode != null)
                {
                    // Arukereso uses 'data-lazy-src' for images not yet scrolled into view
                    string lazySrc = imgNode.GetAttributeValue("data-lazy-src", "");
                    string src = imgNode.GetAttributeValue("src", "");
                    var imageUrl = !string.IsNullOrEmpty(lazySrc) ? lazySrc : src;
                    
                    if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.Contains("placeholder"))
                    {
                        product.ImageUrl = imageUrl.StartsWith("http") ? imageUrl : $"https:{imageUrl}";
                    }
                }

                // 4. Store Name (from offer info)
                var offerNode = node.SelectSingleNode(".//a[contains(@class, 'offer-num')]");
                if (offerNode != null)
                {
                    var offerText = HtmlEntity.DeEntitize(offerNode.InnerText.Trim());
                    product.StoreName = offerText;
                }

                // Filter out empty items (ads or layout spacers)
                if (!string.IsNullOrEmpty(product.Name) && product.Price > 0)
                {
                    results.Add(product);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing individual product node");
                // Ignore parsing errors for individual items
            }
        }

        return results;
    }
}
