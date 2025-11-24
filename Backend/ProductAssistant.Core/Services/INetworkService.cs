namespace ProductAssistant.Core.Services;

public interface INetworkService
{
    bool IsConnected { get; }
    event EventHandler<bool>? ConnectivityChanged;
    Task<bool> CheckConnectivityAsync();
}





