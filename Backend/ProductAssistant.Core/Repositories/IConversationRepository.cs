namespace ProductAssistant.Core.Repositories;

using ProductAssistant.Core.Models;

/// <summary>
/// Repository interface for Conversation entity with domain-specific methods
/// </summary>
public interface IConversationRepository : IRepository<Conversation>
{
    Task<IEnumerable<Conversation>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetWithMessagesAsync(int id, CancellationToken cancellationToken = default);
}
