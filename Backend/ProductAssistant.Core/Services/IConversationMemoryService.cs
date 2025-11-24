using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services;

public interface IConversationMemoryService
{
    // Conversation management
    Task<List<Conversation>> GetConversationsAsync(string userId);
    Task<Conversation> CreateConversationAsync(string userId, string title = "New Conversation");
    Task<Conversation?> GetConversationByIdAsync(int conversationId);
    Task UpdateConversationTitleAsync(int conversationId, string title);
    Task DeleteConversationAsync(int conversationId);
    
    // Message management
    Task<List<ConversationMessage>> GetConversationHistoryAsync(int conversationId, int limit = 10);
    Task SaveMessageAsync(ConversationMessage message);
    Task<string> GetConversationContextAsync(int conversationId);
    
    // Legacy methods (deprecated - kept for backward compatibility)
    [Obsolete("Use GetConversationHistoryAsync(int conversationId) instead")]
    Task<List<ConversationMessage>> GetConversationHistoryAsync(string userId, int limit = 10);
    [Obsolete("Use DeleteConversationAsync(int conversationId) instead")]
    Task ClearConversationAsync(string userId);
    [Obsolete("Use GetConversationContextAsync(int conversationId) instead")]
    Task<string> GetConversationContextAsync(string userId);
}




