using System.Reflection;

namespace ShopAssistant;

public partial class App : Application
{
	public App()
	{
		try
		{
			InitializeComponent();
		}
	catch
		{
			throw;
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		try
		{
			// Get AppShell from DI container (it has IAuthService injected)
			var shell = Handler!.MauiContext!.Services.GetRequiredService<AppShell>();
			
			var window = new Window(shell)
			{
				Title = "Shop Assistant",
				Width = 1200,
				Height = 800
			};
			return window;
		}
		catch (TargetInvocationException)
		{
			throw;
		}
		catch (Exception)
		{
			throw;
		}
	}
}