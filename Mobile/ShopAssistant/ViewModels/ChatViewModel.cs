using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Markdig;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;
using ShopAssistant.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ShopAssistant.ViewModels;

/// <summary>
/// ViewModel for the unified Chat/Search interface.
/// Handles AI conversations with automatic grounding search for products.
/// Products are displayed inline in the conversation, and users can add them to their collection.
/// </summary>
public partial class ChatViewModel : BaseViewModel
{
    private readonly IConversationalAIService _aiService;
    private readonly IProductService _productService;
    private readonly IAuthService _authService;
    private readonly IConversationMemoryService _memoryService;
    private readonly INetworkService _networkService = null!;
    private readonly SettingsService _settingsService;
    private CancellationTokenSource? _loadMessagesCancellation;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> messages = new();

    [ObservableProperty]
    private ObservableCollection<Conversation> conversations = new();

    [ObservableProperty]
    private Conversation? selectedConversation;
    
    partial void OnSelectedConversationChanged(Conversation? value)
    {
        // Don't auto-load here - let SelectConversationAsync handle loading
        // This prevents double-loading when user taps a conversation
        if (value != null)
        {
            // Verbose logging removed
        }
    }

    [ObservableProperty]
    private string userMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Product> selectedProducts = new();

    [ObservableProperty]
    private bool isOnline = true;

    [ObservableProperty]
    private bool isSidebarVisible = true; // Visible by default, will auto-hide on small screens

    // UI Text Properties (no static values)
    [ObservableProperty]
    private string headerTitle = "Search & Chat";

    [ObservableProperty]
    private string offlineText = "Offline";

    [ObservableProperty]
    private string emptyStateTitle = "Start searching for products";

    [ObservableProperty]
    private string emptyStateDescription = "Ask me to find products, compare prices, or get recommendations. I'll search for you and show results here in our conversation!";

    [ObservableProperty]
    private string productsHeaderText = "Products";

    [ObservableProperty]
    private string inputPlaceholder = "Ask me to find products, compare prices...";

    [ObservableProperty]
    private string sendButtonText = "Send";

    [ObservableProperty]
    private string conversationsHeaderText = "Conversations";

    [ObservableProperty]
    private string noConversationsText = "No conversations";

    [ObservableProperty]
    private string newChatHintText = "Tap + to start new chat";

    [ObservableProperty]
    private string saveButtonText = "Save";

    [ObservableProperty]
    private string renameText = "Rename";

    [ObservableProperty]
    private string deleteText = "Delete";

    [ObservableProperty]
    private string noImageText = "No Image";

    public ChatViewModel(
        IConversationalAIService aiService,
        IProductService productService,
        IAuthService authService,
        IConversationMemoryService memoryService,
        INetworkService networkService,
        SettingsService settingsService,
        IHttpClientFactory httpClientFactory)
    {
        _aiService = aiService;
        _productService = productService;
        _authService = authService;
        _memoryService = memoryService;
        _networkService = networkService;
        _settingsService = settingsService;
        // httpClientFactory parameter kept for DI compatibility but not used - we use ConversationMemoryClientService instead
        Title = HeaderTitle;
        
        // Subscribe to network changes
        _networkService.ConnectivityChanged += OnConnectivityChanged;
        UpdateNetworkStatus();

        // Load conversations (fire and forget - constructor cannot be async)
        LoadConversationsAsync();
    }

    /// <summary>
    /// Refreshes the conversations list from the API.
    /// This ensures the list is always in sync with the backend.
    /// </summary>
    private async Task RefreshConversationsFromApiAsync()
    {
        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                System.Diagnostics.Debug.WriteLine("RefreshConversationsFromApiAsync: UserId is empty, skipping");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"RefreshConversationsFromApiAsync: Refreshing conversations from API for user: {userId}");
            
            // Use memory service (ConversationMemoryClientService) which calls API
            // It handles online/offline and errors automatically
            var convos = await _memoryService.GetConversationsAsync(userId);
            
            System.Diagnostics.Debug.WriteLine($"RefreshConversationsFromApiAsync: Retrieved {convos?.Count ?? 0} conversations from API");
            
            if (convos != null && convos.Any())
            {
                foreach (var convo in convos)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Conversation {convo.Id}: {convo.Title} (Updated: {convo.UpdatedAt})");
                }
            }
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var currentSelectedId = SelectedConversation?.Id;
                Conversations.Clear();
                foreach (var convo in convos)
                {
                    Conversations.Add(convo);
                }
                System.Diagnostics.Debug.WriteLine($"RefreshConversationsFromApiAsync: Refreshed {Conversations.Count} conversations from API");
                
                // Restore selection if conversation still exists
                if (currentSelectedId.HasValue)
                {
                    var restoredConvo = Conversations.FirstOrDefault(c => c.Id == currentSelectedId.Value);
                    if (restoredConvo != null)
                    {
                        SelectedConversation = restoredConvo;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshConversationsFromApiAsync ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"RefreshConversationsFromApiAsync ERROR Stack trace: {ex.StackTrace}");
        }
    }

    private async void LoadConversationsAsync()
    {
        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                System.Diagnostics.Debug.WriteLine("LoadConversationsAsync: UserId is empty, skipping");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"LoadConversationsAsync: Loading conversations from API for user: {userId}");
            
            // Refresh conversations from API
            await RefreshConversationsFromApiAsync();

            // Select the most recent conversation or create a new one
            if (Conversations.Any())
            {
                var firstConversation = Conversations.First();
                System.Diagnostics.Debug.WriteLine($"LoadConversationsAsync: Selecting conversation {firstConversation.Id}: {firstConversation.Title}");
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SelectedConversation = firstConversation;
                });
                
                // Small delay to ensure SelectedConversation is set
                await Task.Delay(100);
                
                // Load messages for the first conversation
                System.Diagnostics.Debug.WriteLine($"LoadConversationsAsync: Loading messages for conversation {firstConversation.Id}");
                await LoadConversationMessagesAsync(firstConversation.Id, clearMessages: true);
            }
            else
            {
                // Create first conversation using memory service (calls API)
                System.Diagnostics.Debug.WriteLine("LoadConversationsAsync: No conversations found, creating new conversation via API");
                var newConvo = await _memoryService.CreateConversationAsync(userId, "Main Conversation");
                
                System.Diagnostics.Debug.WriteLine($"LoadConversationsAsync: Created new conversation {newConvo.Id} via API");
                
                // Refresh list from API to include the new conversation
                await RefreshConversationsFromApiAsync();
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var createdConvo = Conversations.FirstOrDefault(c => c.Id == newConvo.Id);
                    if (createdConvo != null)
                    {
                        SelectedConversation = createdConvo;
                    }
                    else
                    {
                        SelectedConversation = newConvo;
                    }
                });
                
                // Don't add welcome message - conversation will start naturally when user sends first message
            }
        }
        catch (Exception ex)
        {
            // Log error but don't show to user - empty state will be shown
            System.Diagnostics.Debug.WriteLine($"LoadConversationsAsync ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"LoadConversationsAsync ERROR Stack trace: {ex.StackTrace}");
        }
    }

    private async Task LoadConversationMessagesAsync(int conversationId, bool clearMessages = true)
    {
        // Cancel any in-flight load operation
        _loadMessagesCancellation?.Cancel();
        _loadMessagesCancellation?.Dispose();
        _loadMessagesCancellation = new CancellationTokenSource();
        var cancellationToken = _loadMessagesCancellation.Token;
        
        try
        {
            // Verbose logging removed - only log errors
            
            // Only clear messages if explicitly requested (e.g., when switching conversations)
            // Don't clear when reloading after sending a message to avoid blank screen
            if (clearMessages)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Messages.Clear();
                    SelectedProducts.Clear(); // Clear selection when switching conversations
                });
            }
            
            // Load messages from API via memory service (which uses ConversationMemoryClientService)
            System.Diagnostics.Debug.WriteLine($"LoadConversationMessagesAsync: Loading messages from API for conversation {conversationId}");
            var history = await _memoryService.GetConversationHistoryAsync(conversationId, 200); // Increased limit to 200
            System.Diagnostics.Debug.WriteLine($"LoadConversationMessagesAsync: Retrieved {history?.Count ?? 0} messages from API for conversation {conversationId}");
            
            if (history == null)
            {
                System.Diagnostics.Debug.WriteLine($"LoadConversationMessagesAsync: WARNING - history is null for conversation {conversationId}");
                history = new List<ConversationMessage>();
            }
            
            if (history.Any())
            {
                System.Diagnostics.Debug.WriteLine($"LoadConversationMessagesAsync: Found {history.Count} messages, logging first 5:");
                foreach (var msg in history.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"  - Message {msg.Id}: IsUser={msg.IsUserMessage}, Message='{msg.Message?.Substring(0, Math.Min(50, msg.Message?.Length ?? 0))}', Response='{msg.Response?.Substring(0, Math.Min(50, msg.Response?.Length ?? 0))}', HasProductsJson={!string.IsNullOrWhiteSpace(msg.ProductsJson)}, HasProductIdsJson={!string.IsNullOrWhiteSpace(msg.ProductIdsJson)}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"LoadConversationMessagesAsync: WARNING - No messages found in API response for conversation {conversationId}. This might indicate messages weren't saved or there's a timing issue.");
            }
            
            // Check for cancellation after API call
            cancellationToken.ThrowIfCancellationRequested();
            
            if (history.Count == 0)
            {
                // Verbose logging removed
                // Don't return early - let the UI show empty state naturally
                // Messages are already cleared above
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged(nameof(Messages));
                });
                return;
            }
            
            // Group messages by conversation flow - user message followed by assistant response
            // Sort by creation time to maintain chronological order
            var sortedHistory = history.OrderBy(m => m.CreatedAt).ToList();
            // Verbose logging removed
            
            // Build list of chat messages first (async operations outside UI thread)
            var chatMessagesToAdd = new List<ChatMessage>();
            
            foreach (var msg in sortedHistory)
            {
                // Add user message if it exists and is not empty
                if (msg.IsUserMessage && !string.IsNullOrWhiteSpace(msg.Message))
                {
                    chatMessagesToAdd.Add(new ChatMessage
                    {
                        Text = msg.Message,
                        IsUserMessage = true,
                        Timestamp = msg.CreatedAt
                    });
                }
                
                // Add assistant response if available
                if (!msg.IsUserMessage && !string.IsNullOrWhiteSpace(msg.Response))
                {
                    List<Product> messageProducts = new();
                    
                    // First, try to load products from ProductsJson (stores full product data from AI searches)
                    if (!string.IsNullOrWhiteSpace(msg.ProductsJson))
                    {
                        try
                        {
                            var products = System.Text.Json.JsonSerializer.Deserialize<List<Product>>(msg.ProductsJson);
                            if (products != null && products.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"Deserialized {products.Count} products from ProductsJson for message {msg.Id}");
                                
                                // Ensure products have all required fields populated and unique IDs
                                int tempIdCounter = -1; // Use negative IDs for temporary products
                                foreach (var product in products)
                                {
                                    // Generate unique ID if missing (for scraped products that aren't in DB yet)
                                    if (product.Id == 0)
                                    {
                                        // Generate a stable negative ID based on product name and URL hash
                                        var idSource = $"{product.Name}_{product.ProductUrl}_{product.Price}";
                                        product.Id = Math.Abs(idSource.GetHashCode()) * -1; // Negative ID to avoid conflicts
                                        tempIdCounter--;
                                    }
                                    
                                    // Ensure required fields are populated
                                    if (string.IsNullOrWhiteSpace(product.Name))
                                    {
                                        product.Name = "Unknown Product";
                                    }
                                    if (string.IsNullOrWhiteSpace(product.Currency))
                                    {
                                        product.Currency = "HUF";
                                    }
                                    // Ensure price is set (default to 0 if missing)
                                    if (product.Price < 0)
                                    {
                                        product.Price = 0;
                                    }
                                    
                                    // Ensure CreatedAt is set
                                    if (product.CreatedAt == default)
                                    {
                                        product.CreatedAt = DateTime.UtcNow;
                                    }
                                    
                                    System.Diagnostics.Debug.WriteLine($"Product: Id={product.Id}, Name={product.Name}, Price={product.Price}, Currency={product.Currency}, ImageUrl={product.ImageUrl ?? "NULL"}");
                                }
                                
                                messageProducts.AddRange(products);
                                System.Diagnostics.Debug.WriteLine($"Successfully loaded {messageProducts.Count} products from ProductsJson for message {msg.Id}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"ProductsJson was empty or null for message {msg.Id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ERROR parsing ProductsJson for message {msg.Id}: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"ERROR Stack trace: {ex.StackTrace}");
                        }
                    }
                    
                    // If no products from ProductsJson, try loading by IDs from ProductIdsJson
                    if (messageProducts.Count == 0 && !string.IsNullOrWhiteSpace(msg.ProductIdsJson))
                    {
                        try
                        {
                            var productIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(msg.ProductIdsJson);
                            if (productIds != null && productIds.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"Loading {productIds.Count} products by IDs in parallel");
                                
                                // Load all products in parallel for better performance
                                var productTasks = productIds.Select(id => _productService.GetProductByIdAsync(id));
                                var products = await Task.WhenAll(productTasks);
                                var loadedProducts = products.Where(p => p != null).ToList();
                                
                                // Ensure products have all required fields
                                foreach (var product in loadedProducts)
                                {
                                    if (product != null)
                                    {
                                        // Set default currency if missing
                                        if (string.IsNullOrWhiteSpace(product.Currency))
                                        {
                                            product.Currency = "HUF";
                                        }
                                        // Ensure price is set
                                        if (product.Price <= 0)
                                        {
                                            product.Price = 0;
                                        }
                                    }
                                }
                                
                                messageProducts.AddRange(loadedProducts!);
                                System.Diagnostics.Debug.WriteLine($"Loaded {messageProducts.Count} products by IDs for message {msg.Id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ERROR parsing ProductIdsJson for message {msg.Id}: {ex.Message}");
                        }
                    }
                    
                    // Legacy: Try to load single product if ProductIdsJson is empty
                    if (messageProducts.Count == 0 && msg.ProductId.HasValue)
                    {
                        try
                        {
                            var product = await _productService.GetProductByIdAsync(msg.ProductId.Value);
                            if (product != null)
                            {
                                // Ensure product has all required fields
                                if (string.IsNullOrWhiteSpace(product.Currency))
                                {
                                    product.Currency = "HUF";
                                }
                                if (product.Price <= 0)
                                {
                                    product.Price = 0;
                                }
                                messageProducts.Add(product);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ERROR: Could not load product {msg.ProductId.Value} for message {msg.Id}: {ex.Message}");
                        }
                    }
                    
                    // Check for cancellation before continuing
                    cancellationToken.ThrowIfCancellationRequested();

                    var chatMessage = new ChatMessage
                    {
                        Text = msg.Response,
                        IsUserMessage = false,
                        Timestamp = msg.CreatedAt
                    };
                    
                    // Add products to ObservableCollection for proper UI binding
                    // Must add products before adding message to collection to ensure UI updates
                    foreach (var product in messageProducts)
                    {
                        if (product != null && !string.IsNullOrWhiteSpace(product.Name))
                        {
                            chatMessage.Products.Add(product);
                            System.Diagnostics.Debug.WriteLine($"Added product to chat message: Id={product.Id}, Name={product.Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipped invalid product: Id={product?.Id}, Name={product?.Name}");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Chat message has {chatMessage.Products.Count} products attached");
                    chatMessagesToAdd.Add(chatMessage);
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"Built {chatMessagesToAdd.Count} chat messages to add to UI");
            
            // Now add all messages to the UI collection on the UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Check if we're still loading the same conversation (might have switched)
                var currentSelectedId = SelectedConversation?.Id;
                if (currentSelectedId != conversationId)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Conversation changed during load (Selected: {currentSelectedId}, Loading: {conversationId}), discarding {chatMessagesToAdd.Count} messages");
                    return;
                }
                
                // If not clearing messages, merge intelligently to avoid duplicates
                if (!clearMessages)
                {
                    // Create a set of existing message texts and timestamps for duplicate detection
                    var existingMessages = new HashSet<(string text, DateTime timestamp, bool isUser)>();
                    foreach (var existingMsg in Messages)
                    {
                        existingMessages.Add((existingMsg.Text, existingMsg.Timestamp, existingMsg.IsUserMessage));
                    }
                    
                    // Only add messages that don't already exist
                    foreach (var chatMessage in chatMessagesToAdd)
                    {
                        var messageKey = (chatMessage.Text, chatMessage.Timestamp, chatMessage.IsUserMessage);
                        if (!existingMessages.Contains(messageKey))
                        {
                            Messages.Add(chatMessage);
                            existingMessages.Add(messageKey);
                        }
                    }
                }
                else
                {
                    // Clear and add all messages
                    foreach (var chatMessage in chatMessagesToAdd)
                    {
                        Messages.Add(chatMessage);
                        // Products collection changes are automatically handled by ChatMessage's CollectionChanged subscription
                    }
                }
                
                // Force property change notification for Messages collection
                OnPropertyChanged(nameof(Messages));
            });
            
            System.Diagnostics.Debug.WriteLine($"Successfully loaded {Messages.Count} chat messages for conversation {conversationId}");
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled - this is expected when switching conversations quickly
            System.Diagnostics.Debug.WriteLine($"Loading conversation {conversationId} was cancelled");
        }
        catch (Exception ex)
        {
            // Log error with full details
            System.Diagnostics.Debug.WriteLine($"ERROR loading conversation messages for conversation {conversationId}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ERROR Stack trace: {ex.StackTrace}");
            
            // Ensure messages collection is accessible even on error
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (Messages.Count == 0)
                {
                    // Add error message to UI so user knows something went wrong
                    Messages.Add(new ChatMessage
                    {
                        Text = "Failed to load conversation history. Please try again.",
                        IsUserMessage = false,
                        Timestamp = DateTime.Now
                    });
                }
            });
            
            // Show error to user if we're in a user-initiated action
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (IsBusy)
                {
                    await Shell.Current.DisplayAlert("Error", $"Failed to load conversation messages: {ex.Message}", "OK");
                }
            });
        }
    }

    private void OnConnectivityChanged(object? sender, bool isConnected)
    {
        IsOnline = isConnected;
        // Don't add message automatically - let SendMessageAsync handle it
        // This prevents spam messages when connection flickers
    }

    private void UpdateNetworkStatus()
    {
        IsOnline = _networkService.IsConnected;
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserMessage) || IsBusy)
            return;

        // Ensure we have a conversation selected
        if (SelectedConversation == null)
        {
            try
            {
                var userId = await _authService.GetUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    await Shell.Current.DisplayAlert("Error", "Please login to send messages.", "OK");
                    return;
                }
                
                Conversation? newConvo = null;
                
                // Use memory service to create conversation (calls API)
                newConvo = await _memoryService.CreateConversationAsync(userId, "New Conversation");
                System.Diagnostics.Debug.WriteLine($"Created new conversation {newConvo.Id} via API for sending message");
                
                // Refresh conversations list from API to ensure it's in sync
                await RefreshConversationsFromApiAsync();
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Find the conversation in the refreshed list
                    var createdConvo = Conversations.FirstOrDefault(c => c.Id == newConvo.Id);
                    if (createdConvo != null)
                    {
                        SelectedConversation = createdConvo;
                    }
                    else
                    {
                        // Fallback: if not found, use the one we just created
                        SelectedConversation = newConvo;
                    }
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to create conversation: {ex.Message}", "OK");
                return;
            }
        }

        // Check network connectivity
        if (!_networkService.IsConnected)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(new ChatMessage
                {
                    Text = "No internet connection. Please check your network settings.",
                    IsUserMessage = false,
                    Timestamp = DateTime.Now
                });
                OnPropertyChanged(nameof(Messages));
            });
            return;
        }

        var messageText = UserMessage.Trim();
        UserMessage = string.Empty;

            // Add user message on UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var userMsg = new ChatMessage
                {
                    Text = messageText,
                    IsUserMessage = true,
                    Timestamp = DateTime.Now
                };
                Messages.Add(userMsg);
                OnPropertyChanged(nameof(Messages));
            });

        try
        {
            IsBusy = true;

            // Get context products
            var userId = await _authService.GetUserIdAsync();
            var contextProducts = await _productService.GetAllProductsAsync(userId);

            // Get API key
            var apiKey = await _settingsService.GetGeminiApiKeyAsync();
            
            // Log API key status (without exposing the actual key)
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                System.Diagnostics.Debug.WriteLine("WARNING: API key is not set. AI responses may be limited.");
            }
            else
            {
                // Verbose logging removed
            }

            // Store conversation in local variable to avoid null reference warning
            var conversation = SelectedConversation;
            if (conversation == null)
            {
                await Shell.Current.DisplayAlert("Error", "No conversation selected. Please try again.", "OK");
                return;
            }

            // Save user message to backend first (before calling AI service)
            // This matches the backend test pattern and ensures the message is saved even if AI call fails
            if (_networkService.IsConnected)
            {
                try
                {
                    var userMessageRecord = new ConversationMessage
                    {
                        ConversationId = conversation.Id,
                        UserId = userId ?? string.Empty,
                        Message = messageText,
                        Response = string.Empty,
                        IsUserMessage = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _memoryService.SaveMessageAsync(userMessageRecord);
                    System.Diagnostics.Debug.WriteLine($"Saved user message to backend for conversation {conversation.Id}, MessageId: {userMessageRecord.Id}, Message: '{messageText.Substring(0, Math.Min(50, messageText.Length))}'");
                    
                    // Small delay to ensure save is committed to database
                    await Task.Delay(300);
                }
                catch (Exception saveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save user message to backend: {saveEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Save error stack trace: {saveEx.StackTrace}");
                    // Continue anyway - backend service will also try to save it
                }
            }

            // Get AI response with conversationId for context
            // The backend ConversationalAIService will also save the user message and assistant response
            var response = await _aiService.GetResponseAsync(
                messageText,
                userId ?? string.Empty,
                apiKey,
                contextProducts,
                conversation.Id,
                CancellationToken.None);

            // Add AI response with products inline
            // Ensure products are distinct to avoid duplicates in UI
            // Normalize URLs for better deduplication (remove trailing slashes, query params for comparison)
            var products = response.Products?
                .GroupBy(p => new { 
                    Name = p.Name?.Trim().ToLowerInvariant() ?? string.Empty, 
                    Url = NormalizeUrlForComparison(p.ProductUrl)
                })
                .Select(g => g.First())
                .ToList() ?? new List<Product>();
            
            // Ensure all products from AI response have required fields populated and unique IDs
            int tempIdCounter = -1; // Use negative IDs for temporary products
            foreach (var product in products)
            {
                // Generate unique ID if missing (for scraped products that aren't in DB yet)
                if (product.Id == 0)
                {
                    // Generate a stable negative ID based on product name and URL hash
                    var idSource = $"{product.Name}_{product.ProductUrl}_{product.Price}";
                    product.Id = Math.Abs(idSource.GetHashCode()) * -1; // Negative ID to avoid conflicts
                    tempIdCounter--;
                }
                
                // Ensure required fields are populated
                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    product.Name = "Unknown Product";
                }
                if (string.IsNullOrWhiteSpace(product.Currency))
                {
                    product.Currency = "HUF";
                }
                // Ensure price is set (default to 0 if missing, but don't override valid 0 prices)
                if (product.Price < 0)
                {
                    product.Price = 0;
                }
                
                // Ensure CreatedAt is set
                if (product.CreatedAt == default)
                {
                    product.CreatedAt = DateTime.UtcNow;
                }
                
                System.Diagnostics.Debug.WriteLine($"AI Response Product: Id={product.Id}, Name={product.Name}, Price={product.Price}, Currency={product.Currency}, ImageUrl={product.ImageUrl ?? "NULL"}");
            }
            
            // Verbose logging removed
            
            var aiMsg = new ChatMessage
            {
                Text = response.Response,
                IsUserMessage = false,
                Timestamp = DateTime.Now
            };
            
            // Add message to UI first, then add products on UI thread for proper binding
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(aiMsg);
                OnPropertyChanged(nameof(Messages));
            });
            
            // Add products to ObservableCollection on UI thread for proper UI binding and updates
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var product in products)
                {
                    if (product != null && !string.IsNullOrWhiteSpace(product.Name))
                    {
                        aiMsg.Products.Add(product);
                        System.Diagnostics.Debug.WriteLine($"Added product to AI message: Id={product.Id}, Name={product.Name}, Price={product.Price}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipped invalid product in AI response: Id={product?.Id}, Name={product?.Name}");
                    }
                }
                System.Diagnostics.Debug.WriteLine($"AI message now has {aiMsg.Products.Count} products");
                // Products collection changes are automatically handled by ChatMessage's CollectionChanged subscription
            });

            // Reload messages from API after a delay to ensure backend has committed
            // The ConversationalAIService saves both user message and assistant response,
            // but we reload from API to ensure messages are persisted and available when switching conversations
            // Use a longer delay to allow backend to commit the transaction and AI service to finish saving
            // Only reload if we still have the same conversation selected (user might have switched)
            await Task.Delay(2000); // Increased delay to ensure backend has committed and AI service has saved response
            if (conversation != null && SelectedConversation?.Id == conversation.Id)
            {
                System.Diagnostics.Debug.WriteLine($"Reloading messages for conversation {conversation.Id} after sending message");
                // Reload messages from API to ensure we have the latest saved messages
                // This ensures messages are persisted and available when switching conversations
                // Use clearMessages: true to get fresh data from API (merge logic will handle duplicates if needed)
                await LoadConversationMessagesAsync(conversation.Id, clearMessages: true);
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.Message.Contains("Timeout"))
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(new ChatMessage
                {
                    Text = "The request took too long to complete. The AI service might be processing a complex query. Please try again with a simpler request or wait a moment.",
                    IsUserMessage = false,
                    Timestamp = DateTime.Now
                });
                OnPropertyChanged(nameof(Messages));
            });
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Timeout"))
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(new ChatMessage
                {
                    Text = "The request timed out. The AI service might be busy. Please try again in a moment.",
                    IsUserMessage = false,
                    Timestamp = DateTime.Now
                });
                OnPropertyChanged(nameof(Messages));
            });
        }
        catch (HttpRequestException)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(new ChatMessage
                {
                    Text = "Cannot connect to the server. Please check your network connection and ensure backend services are running.",
                    IsUserMessage = false,
                    Timestamp = DateTime.Now
                });
                OnPropertyChanged(nameof(Messages));
            });
        }
        catch (Exception ex)
        {
            // Provide user-friendly error message
            var errorMessage = ex.Message.Contains("Timeout") || ex.Message.Contains("canceled")
                ? "The request took too long. Please try again with a simpler query."
                : $"An error occurred: {ex.Message}";
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(new ChatMessage
                {
                    Text = errorMessage,
                    IsUserMessage = false,
                    Timestamp = DateTime.Now
                });
                OnPropertyChanged(nameof(Messages));
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleProductSelection(Product product)
    {
        if (product == null)
            return;

        var existingProduct = SelectedProducts.FirstOrDefault(p => p.Id == product.Id);
        if (existingProduct != null)
        {
            SelectedProducts.Remove(existingProduct);
        }
        else
        {
            SelectedProducts.Add(product);
        }
    }

    [RelayCommand]
    private async Task SaveSelectedToCollectionAsync()
    {
        if (SelectedProducts.Count == 0)
        {
            await Shell.Current.DisplayAlert("No Selection", "Please select products to add to your collection.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                await Shell.Current.DisplayAlert("Error", "Please login to save products.", "OK");
                return;
            }

            // Get user's existing products to check for duplicates
            var allUserProducts = await _productService.GetAllProductsAsync(userId);
            var existingProductKeys = allUserProducts
                .Where(p => !string.IsNullOrEmpty(p.ProductUrl))
                .Select(p => (p.Name?.Trim().ToLowerInvariant(), NormalizeUrlForComparison(p.ProductUrl)))
                .Where(k => !string.IsNullOrEmpty(k.Item1) && !string.IsNullOrEmpty(k.Item2))
                .ToHashSet();
            
            int savedCount = 0;
            int skippedCount = 0;
            
            // Remove duplicates from selection first
            var uniqueProducts = SelectedProducts
                .GroupBy(p => (p.Name?.Trim().ToLowerInvariant(), NormalizeUrlForComparison(p.ProductUrl)))
                .Select(g => g.First())
                .ToList();
            
            foreach (var product in uniqueProducts)
            {
                try
                {
                    // Check if product already exists in user's collection (by name and URL, case-insensitive)
                    var productName = product.Name?.Trim().ToLowerInvariant() ?? string.Empty;
                    var productUrl = NormalizeUrlForComparison(product.ProductUrl);
                    
                    if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(productUrl))
                    {
                        skippedCount++; // Skip products without name or URL
                        continue;
                    }
                    
                    var productKey = (productName, productUrl);
                    if (existingProductKeys.Contains(productKey))
                    {
                        skippedCount++;
                        continue;
                    }
                    
                    // Create new product with user ID for collection
                    var productToSave = new Product
                    {
                        Name = product.Name?.Trim() ?? string.Empty,
                        Price = product.Price,
                        Currency = product.Currency ?? "HUF",
                        ImageUrl = product.ImageUrl,
                        ProductUrl = product.ProductUrl,
                        StoreName = product.StoreName,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        ScrapedAt = product.ScrapedAt == default ? DateTime.UtcNow : product.ScrapedAt
                    };
                    
                    await _productService.CreateProductAsync(productToSave);
                    savedCount++;
                    existingProductKeys.Add(productKey); // Track newly added
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR saving product '{product.Name}': {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"ERROR Stack trace: {ex.StackTrace}");
                    skippedCount++; // Count as skipped if error occurs
                }
            }

            // Clear selection after saving and notify UI
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                SelectedProducts.Clear();
                OnPropertyChanged(nameof(SelectedProducts));
            });

            // Show appropriate message
            if (savedCount > 0 && skippedCount > 0)
            {
                await Shell.Current.DisplayAlert(
                    "Collection Updated", 
                    $"{savedCount} product(s) added to your collection. {skippedCount} product(s) were already in your collection.", 
                    "OK");
            }
            else if (savedCount > 0)
            {
                await Shell.Current.DisplayAlert(
                    "Success", 
                    $"{savedCount} product(s) added to your collection!", 
                    "OK");
            }
            else if (skippedCount > 0)
            {
                await Shell.Current.DisplayAlert(
                    "Already in Collection", 
                    "All selected products are already in your collection.", 
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "No Products Saved", 
                    "No products were saved. Please try again.", 
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to save products: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RemoveFromSelection(Product product)
    {
        if (product == null)
            return;

        var toRemove = SelectedProducts.FirstOrDefault(p => p.Id == product.Id);
        if (toRemove != null)
        {
            SelectedProducts.Remove(toRemove);
        }
    }

    public bool IsProductSelected(Product product)
    {
        if (product == null)
            return false;
        return SelectedProducts.Any(p => p.Id == product.Id);
    }

    [RelayCommand]
    private async Task CreateNewConversationAsync()
    {
        try
        {
            IsBusy = true;
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                return;

            Conversation? newConvo = null;
            
            // Use memory service to create conversation (calls API)
            newConvo = await _memoryService.CreateConversationAsync(userId, $"Conversation {Conversations.Count + 1}");
            
            System.Diagnostics.Debug.WriteLine($"Created new conversation via API: {newConvo.Id}");
            
            // Refresh conversations list from API to ensure it's in sync
            await RefreshConversationsFromApiAsync();
            
            // Select the newly created conversation
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Find the conversation in the refreshed list (it should be first as it's newest)
                var createdConvo = Conversations.FirstOrDefault(c => c.Id == newConvo.Id);
                if (createdConvo != null)
                {
                    SelectedConversation = createdConvo;
                }
                else
                {
                    // Fallback: if not found, use the one we just created
                    SelectedConversation = newConvo;
                }
                Messages.Clear();
                SelectedProducts.Clear(); // Clear selection when creating new conversation
                OnPropertyChanged(nameof(Messages));
                OnPropertyChanged(nameof(SelectedConversation));
            });
            
            // Don't add welcome message - conversation will start naturally when user sends first message
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to create conversation: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectConversationAsync(Conversation conversation)
    {
        if (conversation == null)
            return;

        // Always allow switching - refresh even if same conversation
        try
        {
            IsBusy = true;
            
            System.Diagnostics.Debug.WriteLine($"Selecting conversation: {conversation.Id} - {conversation.Title}");
            
            // Clear selected products when switching conversations
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                SelectedProducts.Clear();
            });
            
            // Set selected conversation FIRST so LoadConversationMessagesAsync can verify it
            SelectedConversation = conversation;
            
            // Load messages for the selected conversation (this will clear messages internally)
            // Use the conversation parameter directly to avoid race condition
            var conversationIdToLoad = conversation.Id;
            await LoadConversationMessagesAsync(conversationIdToLoad, clearMessages: true);
            
            // Ensure UI updates by forcing property change notification on UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                OnPropertyChanged(nameof(Messages));
                OnPropertyChanged(nameof(SelectedConversation));
                OnPropertyChanged(nameof(SelectedProducts));
            });
            
            // Close sidebar after selecting conversation (especially useful on mobile)
            IsSidebarVisible = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR selecting conversation: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ERROR Stack trace: {ex.StackTrace}");
            await Shell.Current.DisplayAlert("Error", $"Failed to load conversation: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteConversationAsync(Conversation conversation)
    {
        if (conversation == null)
            return;

        var confirm = await Shell.Current.DisplayAlert(
            "Delete Conversation",
            $"Are you sure you want to delete '{conversation.Title}'?",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;
            var deletedId = conversation.Id;
            
            // Delete via API
            await _memoryService.DeleteConversationAsync(conversation.Id);
            System.Diagnostics.Debug.WriteLine($"Deleted conversation {deletedId} via API");
            
            // Refresh conversations list from API to ensure it's in sync
            await RefreshConversationsFromApiAsync();

            // Select another conversation or create new one
            if (SelectedConversation?.Id == deletedId)
            {
                if (Conversations.Any())
                {
                    SelectedConversation = Conversations.First();
                    await LoadConversationMessagesAsync(SelectedConversation.Id);
                }
                else
                {
                    // Create a new conversation if this was the last one
                    await CreateNewConversationAsync();
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to delete conversation: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    [RelayCommand]
    private async Task RenameConversationAsync(Conversation conversation)
    {
        if (conversation == null)
            return;

        var newTitle = await Shell.Current.DisplayPromptAsync(
            "Rename Conversation",
            "Enter new name:",
            initialValue: conversation.Title,
            maxLength: 50);

        if (string.IsNullOrWhiteSpace(newTitle))
            return;

        try
        {
            // Update via API
            await _memoryService.UpdateConversationTitleAsync(conversation.Id, newTitle);
            System.Diagnostics.Debug.WriteLine($"Renamed conversation {conversation.Id} via API to: {newTitle}");
            
            // Refresh conversations list from API to ensure it's in sync
            await RefreshConversationsFromApiAsync();
            
            // Update selected conversation if it's the one we renamed
            if (SelectedConversation?.Id == conversation.Id)
            {
                var updatedConvo = Conversations.FirstOrDefault(c => c.Id == conversation.Id);
                if (updatedConvo != null)
                {
                    SelectedConversation = updatedConvo;
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to rename conversation: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void LoadWebViewContent(ChatMessage? message)
    {
        if (message == null || message.IsUserMessage || string.IsNullOrWhiteSpace(message.HtmlContent))
            return;

        // The WebView Source is already bound to HtmlContent, but we can trigger a refresh if needed
        // This command is called when the WebView loads, ensuring content is displayed
        // Verbose logging removed
    }
    
    /// <summary>
    /// Normalizes a URL for comparison by removing trailing slashes and query parameters.
    /// This helps with product deduplication when URLs differ only by these elements.
    /// </summary>
    private static string NormalizeUrlForComparison(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;
            
        try
        {
            var normalized = url.Trim().ToLowerInvariant();
            
            // Remove trailing slash
            if (normalized.EndsWith("/"))
            {
                normalized = normalized.TrimEnd('/');
            }
            
            // Remove query parameters for comparison (keep base URL only)
            var uri = new Uri(normalized);
            normalized = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
            
            return normalized;
        }
        catch
        {
            // If URL parsing fails, just return trimmed lowercase version
            return url.Trim().ToLowerInvariant();
        }
    }

    private class ConversationMessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int? ProductId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool IsUserMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProductIdsJson { get; set; }
        public string? ProductsJson { get; set; }
    }

}

/// <summary>
/// Represents a message in the chat conversation.
/// Can contain text and optionally products that are displayed inline.
/// Products are automatically included when AI performs grounding searches.
/// </summary>
public class ChatMessage : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private string _text = string.Empty;
    private bool _isUserMessage;
    private string? _htmlContent;
    
    public ChatMessage()
    {
        // Subscribe to Products collection changes to notify UI
        Products.CollectionChanged += (sender, e) =>
        {
            OnPropertyChanged(nameof(Products));
        };
    }
    
    public string Text 
    { 
        get => _text;
        set 
        {
            if (SetProperty(ref _text, value))
            {
                _htmlContent = null; // Invalidate cached HTML
                OnPropertyChanged(nameof(HtmlContent));
            }
        }
    }
    
    public bool IsUserMessage 
    { 
        get => _isUserMessage;
        set 
        {
            if (SetProperty(ref _isUserMessage, value))
            {
                _htmlContent = null; // Invalidate cached HTML
                OnPropertyChanged(nameof(HtmlContent));
            }
        }
    }
    
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// HTML content for markdown rendering in WebView (assistant messages only)
    /// Computed lazily when accessed
    /// </summary>
    public string HtmlContent 
    { 
        get 
        {
            if (_htmlContent != null)
                return _htmlContent;
                
            // Convert markdown to HTML for assistant messages
            if (!IsUserMessage && !string.IsNullOrWhiteSpace(Text))
            {
                _htmlContent = ConvertMarkdownToHtml(Text);
            }
            else
            {
                _htmlContent = string.Empty;
            }
            return _htmlContent;
        }
    }
    
    /// <summary>
    /// Products displayed inline with this message.
    /// Populated when AI performs grounding search and finds products.
    /// Using ObservableCollection for proper UI binding and updates.
    /// </summary>
    /// <summary>
    /// Products displayed inline with this message.
    /// Populated when AI performs grounding search and finds products.
    /// Using ObservableCollection for proper UI binding and updates.
    /// </summary>
    public ObservableCollection<Product> Products { get; } = new();
    
    private string ConvertMarkdownToHtml(string markdown)
    {
        try
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var html = Markdown.ToHtml(markdown, pipeline);
            
            // Wrap in styled HTML container
            return $@"<!DOCTYPE html>
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
        p {{ margin: 0 0 8px 0; }}
        ul, ol {{ margin: 8px 0; padding-left: 24px; }}
        li {{ margin: 4px 0; }}
        code {{ background-color: #f4f4f4; padding: 2px 6px; border-radius: 3px; font-family: 'Courier New', monospace; font-size: 0.9em; }}
        pre {{ background-color: #f4f4f4; padding: 12px; border-radius: 6px; overflow-x: auto; }}
        pre code {{ background-color: transparent; padding: 0; }}
        strong {{ font-weight: 600; }}
        em {{ font-style: italic; }}
        a {{ color: #007AFF; text-decoration: none; }}
        a:active {{ opacity: 0.7; }}
        h1, h2, h3, h4, h5, h6 {{ margin: 12px 0 8px 0; font-weight: 600; }}
        h1 {{ font-size: 1.5em; }}
        h2 {{ font-size: 1.3em; }}
        h3 {{ font-size: 1.1em; }}
        blockquote {{ border-left: 3px solid #ddd; margin: 8px 0; padding-left: 12px; color: #666; }}
        table {{ border-collapse: collapse; width: 100%; margin: 8px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f4f4f4; font-weight: 600; }}
    </style>
</head>
<body>
    {html}
</body>
</html>";
        }
        catch
        {
            // Return escaped plain text if conversion fails
            var escaped = markdown
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
            return $@"<!DOCTYPE html>
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
}

