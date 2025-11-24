namespace ProductAssistant.Core.Repositories;

using ProductAssistant.Core.Models;

/// <summary>
/// Repository interface for ConversationMessage entity with domain-specific methods
/// </summary>
public interface IConversationMessageRepository : IRepository<ConversationMessage>
{
    Task<IEnumerable<ConversationMessage>> GetByConversationIdAsync(int conversationId, int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConversationMessage>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

