using ShopAssistant.ViewModels;
using Microsoft.Maui.Controls;

namespace ShopAssistant.Views;

public partial class ChatPage : ContentPage
{
    private double _lastWidth = 0;
    private const double SmallScreenBreakpoint = 600;
    private Grid? _mainGrid;
    private CollectionView? _messagesCollectionView;

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        SizeChanged += OnSizeChanged;
        
        // Subscribe to sidebar visibility changes and messages collection changes
        if (viewModel != null)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Find the main grid and messages collection view
        _mainGrid = this.FindByName<Grid>("MainGrid");
        _messagesCollectionView = this.FindByName<CollectionView>("MessagesCollectionView");
        
        // Ensure CollectionView is properly bound and initialize sidebar state
        if (BindingContext is ChatViewModel vm)
        {
            if (_messagesCollectionView != null)
            {
                if (_messagesCollectionView.ItemsSource != vm.Messages)
                {
                    _messagesCollectionView.ItemsSource = vm.Messages;
                }
            }
            
            // Initialize sidebar state based on screen size
            var width = Width;
            if (width > 0 && width < SmallScreenBreakpoint)
            {
                vm.IsSidebarVisible = false;
            }
            else if (width > 0)
            {
                // Only set to true if not already set and screen is large enough
                // Don't override if user has manually toggled
            }
        }
        
        if (_mainGrid != null)
        {
            UpdateSidebarColumn();
        }
        
        // Scroll to bottom on appearing
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () =>
        {
            ScrollToBottom();
        });
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatViewModel.IsSidebarVisible))
        {
            UpdateSidebarColumn();
        }
    }

    private void OnMessagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Verbose logging removed - only log errors
        
        // Force CollectionView to refresh when messages change
        Dispatcher.Dispatch(() =>
        {
            if (_messagesCollectionView != null && BindingContext is ChatViewModel vm)
            {
                // Trigger CollectionView refresh without losing bindings
                var currentItemsSource = _messagesCollectionView.ItemsSource;
                if (currentItemsSource != vm.Messages)
                {
                    _messagesCollectionView.ItemsSource = vm.Messages;
                }
            }
        });
        
        // Auto-scroll to bottom when new messages are added
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            // Use Dispatcher to ensure UI is updated before scrolling
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () =>
            {
                ScrollToBottom();
            });
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
            // When collection is reset (cleared), wait a bit longer for reload
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
            {
                ScrollToBottom();
            });
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
        {
            // Collection was replaced - scroll after a delay
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(400), () =>
            {
                ScrollToBottom();
            });
        }
    }

    private void ScrollToBottom()
    {
        if (_messagesCollectionView != null && BindingContext is ChatViewModel vm && vm.Messages.Count > 0)
        {
            try
            {
                var lastMessage = vm.Messages.Last();
                _messagesCollectionView.ScrollTo(lastMessage, position: ScrollToPosition.End, animate: true);
            }
            catch
            {
                // Ignore scroll errors
            }
        }
    }

    private void UpdateSidebarColumn()
    {
        if (_mainGrid == null || BindingContext is not ChatViewModel vm) return;

        // Get the second column (sidebar column)
        if (_mainGrid.ColumnDefinitions.Count > 1)
        {
            var sidebarColumn = _mainGrid.ColumnDefinitions[1];
            if (vm.IsSidebarVisible)
            {
                // Show sidebar - set width to 280
                sidebarColumn.Width = new GridLength(280, GridUnitType.Absolute);
            }
            else
            {
                // Hide sidebar completely - set width to 0
                sidebarColumn.Width = new GridLength(0, GridUnitType.Absolute);
            }
        }
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        var width = Width;
        if (Math.Abs(width - _lastWidth) < 10) return; // Only update if significant change
        _lastWidth = width;

        // Auto-hide sidebar on small screens
        if (width < SmallScreenBreakpoint && BindingContext is ChatViewModel vm)
        {
            if (vm.IsSidebarVisible)
            {
                vm.IsSidebarVisible = false;
            }
        }
        
        UpdateSidebarColumn();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        SizeChanged -= OnSizeChanged;
        if (BindingContext is ChatViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.Messages.CollectionChanged -= OnMessagesCollectionChanged;
        }
    }

    private void OnMarkdownWebViewBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is WebView webView && webView.BindingContext is ChatMessage message && !message.IsUserMessage)
        {
            // Set HTML content when binding context changes
            SetWebViewHtml(webView, message);
        }
    }

    private void OnWebViewLoaded(object? sender, EventArgs e)
    {
        if (sender is WebView webView && webView.BindingContext is ChatMessage message && !message.IsUserMessage)
        {
            // Ensure HTML is set when WebView loads
            SetWebViewHtml(webView, message);
        }
    }

    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        // Prevent navigation to external URLs, only allow local HTML content
        if (!string.IsNullOrEmpty(e.Url) && !e.Url.StartsWith("about:blank") && !e.Url.StartsWith("data:"))
        {
            e.Cancel = true;
        }
    }
    
    private void OnRemainingItemsThresholdReached(object? sender, EventArgs e)
    {
        // Optional: Load more messages if needed
        // Verbose logging removed for cleaner debug output
    }
    
    private void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        // Track scrolling for potential optimizations
        // Verbose logging removed for cleaner debug output
    }

    private void SetWebViewHtml(WebView webView, ChatMessage message)
    {
        if (message == null || message.IsUserMessage || webView == null)
            return;

        try
        {
            var htmlContent = message.HtmlContent;
            if (!string.IsNullOrEmpty(htmlContent))
            {
                // Use Dispatcher to ensure UI thread
                Dispatcher.Dispatch(() =>
                {
                    try
                    {
                        // Always set the source to ensure content is loaded
                        // This is important when switching conversations or when items are recycled
                        var htmlSource = new HtmlWebViewSource { Html = htmlContent };
                        webView.Source = htmlSource;
                        // Verbose logging removed
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error setting WebView source: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Empty HTML content for message");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SetWebViewHtml: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}



