using ProductAssistant.Core.Services;
using ShopAssistant.Views;

namespace ShopAssistant
{
	public partial class AppShell : Shell
	{
		private readonly IAuthService _authService;
		private bool _isInitialized = false;
		private bool _isNavigating = false;

		public AppShell(IAuthService authService)
		{
			_authService = authService;
			
			try
			{
				InitializeComponent();
				
				// Register routes for navigation
				Routing.RegisterRoute("DebugLog", typeof(Views.DebugLogPage));
				
				// Subscribe to navigation events for route protection
				Navigating += OnNavigating;
				
				// Update tab visibility based on auth state
				UpdateTabVisibility();
			}
			catch (Exception)
			{
				throw;
			}
		}
		
		private async void UpdateTabVisibility()
		{
			try
			{
				var isAuthenticated = await _authService.IsAuthenticatedAsync();
				// Find MainTabBar by name - it's defined in XAML with x:Name="MainTabBar"
				var tabBar = this.FindByName<TabBar>("MainTabBar");
				if (tabBar != null)
				{
					tabBar.IsVisible = isAuthenticated;
				}
			}
			catch (Exception)
			{
				var tabBar = this.FindByName<TabBar>("MainTabBar");
				if (tabBar != null)
				{
					tabBar.IsVisible = false;
				}
			}
		}

		protected override async void OnNavigated(ShellNavigatedEventArgs args)
		{
			base.OnNavigated(args);
			
			// Only check authentication once on first navigation
			if (!_isInitialized)
			{
				_isInitialized = true;
				await CheckAuthenticationAsync();
			}
		}

		private async void OnNavigating(object? sender, ShellNavigatingEventArgs e)
		{
			var targetRoute = e.Target.Location.OriginalString;
			System.Diagnostics.Debug.WriteLine($"[AppShell] OnNavigating to: {targetRoute}, _isNavigating: {_isNavigating}");
			
			// Prevent navigation loops
			if (_isNavigating)
			{
				System.Diagnostics.Debug.WriteLine("[AppShell] Navigation blocked - already navigating");
				return;
			}

			// Always allow navigation to Login page
			if (targetRoute.Contains("Login", StringComparison.OrdinalIgnoreCase))
			{
				System.Diagnostics.Debug.WriteLine("[AppShell] Allowing navigation to Login page");
				return;
			}

			// Protected routes that require authentication
			var protectedRoutes = new[] { "Chat", "Collection", "Settings" };
			
			// Check if navigating to a protected route
			bool isProtectedRoute = protectedRoutes.Any(route => 
				targetRoute.Contains(route, StringComparison.OrdinalIgnoreCase));
			
			System.Diagnostics.Debug.WriteLine($"[AppShell] Is protected route: {isProtectedRoute}");
			
			if (isProtectedRoute)
			{
				try
				{
					// First check if we have a token and userId
					var token = await _authService.GetAccessTokenAsync();
					var userId = await _authService.GetUserIdAsync();
					
					System.Diagnostics.Debug.WriteLine($"[AppShell] Token exists: {!string.IsNullOrEmpty(token)}, UserId exists: {!string.IsNullOrEmpty(userId)}");
					
					if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
					{
						System.Diagnostics.Debug.WriteLine("[AppShell] No token or userId - redirecting to Login");
						e.Cancel();
						
						_isNavigating = true;
						await Task.Delay(100);
						await GoToAsync("//Login");
						_isNavigating = false;
						return;
					}
					
					// Then validate authentication
					var isAuthenticated = await _authService.IsAuthenticatedAsync();
					System.Diagnostics.Debug.WriteLine($"[AppShell] IsAuthenticated: {isAuthenticated}");
					
					if (!isAuthenticated)
					{
						System.Diagnostics.Debug.WriteLine("[AppShell] Not authenticated - redirecting to Login");
						e.Cancel();
						
						_isNavigating = true;
						await Task.Delay(100);
						await GoToAsync("//Login");
						_isNavigating = false;
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("[AppShell] Authentication passed - allowing navigation");
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"[AppShell] Exception during auth check: {ex.Message}");
					e.Cancel();
					
					_isNavigating = true;
					await Task.Delay(100);
					await GoToAsync("//Login");
					_isNavigating = false;
				}
			}
		}

		private async Task CheckAuthenticationAsync()
		{
			if (_isNavigating)
			{
				return;
			}

			try
			{
				// First check if we have a token at all
				var token = await _authService.GetAccessTokenAsync();
				var userId = await _authService.GetUserIdAsync();
				
				if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
				{
					_isNavigating = true;
					await GoToAsync("//Login");
					_isNavigating = false;
					return;
				}
				
				// Then validate authentication
				var isAuthenticated = await _authService.IsAuthenticatedAsync();
				
				var currentRoute = CurrentState?.Location?.OriginalString ?? "";
				
				_isNavigating = true;
				
				if (!isAuthenticated)
				{
					// Redirect to login if not authenticated
					if (!currentRoute.Contains("Login", StringComparison.OrdinalIgnoreCase))
					{
						await GoToAsync("//Login");
					}
				}
				else
				{
					// User is authenticated, go to chat if on login page
					if (currentRoute.Contains("Login", StringComparison.OrdinalIgnoreCase) || 
					    string.IsNullOrEmpty(currentRoute))
					{
						await GoToAsync("//Chat");
					}
				}
			}
			catch (Exception)
			{
				// On error, go to login
				try
				{
					_isNavigating = true;
					await GoToAsync("//Login");
				}
				catch (Exception)
				{
				}
				finally
				{
					_isNavigating = false;
				}
			}
			finally
			{
				_isNavigating = false;
			}
		}

		public async Task OnUserLoggedIn()
		{
			if (_isNavigating)
			{
				return;
			}

			try
			{
				_isNavigating = true;
				
				// Show tabs and navigate to Chat
				var tabBar = this.FindByName<TabBar>("MainTabBar");
				if (tabBar != null)
				{
					tabBar.IsVisible = true;
				}
				await GoToAsync("//Chat");
			}
			catch (Exception)
			{
				await Task.Delay(200);
				var tabBar = this.FindByName<TabBar>("MainTabBar");
				if (tabBar != null)
				{
					tabBar.IsVisible = true;
				}
				await GoToAsync("//Chat");
			}
			finally
			{
				_isNavigating = false;
			}
		}

		public async Task OnUserLoggedOut()
		{
			if (_isNavigating)
			{
				return;
			}

			try
			{
				_isNavigating = true;
				
				// Hide tabs and navigate to Login
				var tabBar = this.FindByName<TabBar>("MainTabBar");
				if (tabBar != null)
				{
					tabBar.IsVisible = false;
				}
				await GoToAsync("//Login");
			}
			finally
			{
				_isNavigating = false;
			}
		}
	}
}
