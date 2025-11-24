using Microsoft.Maui.Storage;

namespace ShopAssistant.Services;

public class SettingsService
{
    private const string GeminiApiKeyKey = "gemini_api_key";

    public async Task<string?> GetGeminiApiKeyAsync()
    {
        try
        {
            return await SecureStorage.GetAsync(GeminiApiKeyKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveGeminiApiKeyAsync(string apiKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                SecureStorage.Remove(GeminiApiKeyKey);
            }
            else
            {
                await SecureStorage.SetAsync(GeminiApiKeyKey, apiKey);
            }
        }
        catch
        {
            // Handle error silently or log
        }
    }

    public async Task<bool> HasApiKeyAsync()
    {
        var key = await GetGeminiApiKeyAsync();
        return !string.IsNullOrWhiteSpace(key);
    }

    /// <summary>
    /// Sets a default API key if none exists. Useful for development/testing.
    /// </summary>
    public async Task SetDefaultApiKeyIfNotExistsAsync(string defaultApiKey)
    {
        if (string.IsNullOrWhiteSpace(defaultApiKey))
            return;

        var existingKey = await GetGeminiApiKeyAsync();
        if (string.IsNullOrWhiteSpace(existingKey))
        {
            await SaveGeminiApiKeyAsync(defaultApiKey);
        }
    }
}



