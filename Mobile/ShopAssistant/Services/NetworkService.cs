using Microsoft.Maui.Networking;
using ProductAssistant.Core.Services;

namespace ShopAssistant.Services;

public class NetworkService : INetworkService
{
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;

    public NetworkService()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var isConnected = e.NetworkAccess == NetworkAccess.Internet;
        ConnectivityChanged?.Invoke(this, isConnected);
    }

    public Task<bool> CheckConnectivityAsync()
    {
        return Task.FromResult(IsConnected);
    }
}



