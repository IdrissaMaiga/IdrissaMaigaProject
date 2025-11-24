using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Data;
using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services;

public class ConversationMemoryService : IConversationMemoryService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ConversationMemoryService> _logger;

    public ConversationMemoryService(IServiceScopeFactory serviceScopeFactory, ILogger<ConversationMemoryService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    // Conversation Management
    public async Task<List<Conversation>> GetConversationsAsync(string userId)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await context.Conversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations for user: {UserId}", userId);
            return new List<Conversation>();
        }
    }

    public async Task<Conversation> CreateConversationAsync(string userId, string title = "New Conversation")
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var conversation = new Conversation
            {
                UserId = userId,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();

            _logger.LogInformation("Conversation created with id: {Id} for user: {UserId}", conversation.Id, userId);
            return conversation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<Conversation?> GetConversationByIdAsync(int conversationId)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Don't include Messages by default - it can cause issues and messages are loaded separately
            var conversation = await context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);
            
            if (conversation != null)
            {
                _logger.LogInformation("Found conversation {ConversationId} for user {UserId}", conversationId, conversation.UserId);
            }
            else
            {
                _logger.LogWarning("Conversation {ConversationId} not found", conversationId);
            }
            
            return conversation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation: {ConversationId}", conversationId);
            return null;
        }
    }

    public async Task UpdateConversationTitleAsync(int conversationId, string title)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var conversation = await context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.Title = title;
                conversation.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversation title: {ConversationId}", conversationId);
        }
    }

    public async Task DeleteConversationAsync(int conversationId)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var conversation = await context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                context.Conversations.Remove(conversation);
                await context.SaveChangesAsync();
                _logger.LogInformation("Conversation deleted: {ConversationId}", conversationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation: {ConversationId}", conversationId);
        }
    }

    // Message Management (Updated)
    public async Task<List<ConversationMessage>> GetConversationHistoryAsync(int conversationId, int limit = 10)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // First verify conversation exists
            var conversationExists = await context.Conversations.AnyAsync(c => c.Id == conversationId);
            if (!conversationExists)
            {
                _logger.LogWarning("Conversation {ConversationId} does not exist when retrieving history", conversationId);
                return new List<ConversationMessage>();
            }
            
            // Get total message count for logging
            var totalCount = await context.ConversationMessages
                .Where(m => m.ConversationId == conversationId)
                .CountAsync();
            
            // Get messages in chronological order (oldest first) for proper conversation flow
            // Add secondary ordering by Id to ensure deterministic ordering when timestamps are identical
            // Use a reasonable maximum limit to prevent performance issues (1000 messages max)
            var effectiveLimit = limit > 0 && limit <= 1000 ? limit : (limit > 1000 ? 1000 : 100);
            var messages = await context.ConversationMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.Id)
                .Take(effectiveLimit)
                .ToListAsync();
            
            _logger.LogInformation("Loaded {Count} messages for conversation {ConversationId} (total in DB: {TotalCount}, requested limit: {Limit})", 
                messages.Count, conversationId, totalCount, limit);
            
            if (totalCount > 0 && messages.Count == 0)
            {
                _logger.LogWarning("WARNING: Conversation {ConversationId} has {TotalCount} messages in database but query returned 0 messages. This may indicate a query issue.", 
                    conversationId, totalCount);
            }
            
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history: {ConversationId}", conversationId);
            return new List<ConversationMessage>();
        }
    }

    public async Task<string> GetConversationContextAsync(int conversationId)
    {
        try
        {
            var history = await GetConversationHistoryAsync(conversationId, 10);
            
            if (!history.Any())
                return string.Empty;

            var context = new System.Text.StringBuilder();
            context.AppendLine("Previous conversation context:");
            
            foreach (var message in history)
            {
                var role = message.IsUserMessage ? "User" : "Assistant";
                var content = message.IsUserMessage ? message.Message : message.Response;
                context.AppendLine($"{role}: {content}");
            }
            
            return context.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation context: {ConversationId}", conversationId);
            return string.Empty;
        }
    }

    public async Task SaveMessageAsync(ConversationMessage message)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Verify conversation exists
            var conversationExists = await context.Conversations.AnyAsync(c => c.Id == message.ConversationId);
            if (!conversationExists)
            {
                _logger.LogError("Cannot save message: Conversation {ConversationId} does not exist", message.ConversationId);
                throw new InvalidOperationException($"Conversation {message.ConversationId} does not exist");
            }
            
            message.CreatedAt = DateTime.UtcNow;
            context.ConversationMessages.Add(message);
            var savedCount = await context.SaveChangesAsync();
            
            _logger.LogInformation("Saved message to conversation {ConversationId}, IsUserMessage: {IsUserMessage}, MessageId: {MessageId}, SaveChanges returned: {SavedCount}",
                message.ConversationId, message.IsUserMessage, message.Id, savedCount);

            // Update conversation's UpdatedAt timestamp
            var conversation = await context.Conversations.FindAsync(message.ConversationId);
            if (conversation != null)
            {
                conversation.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Conversation {ConversationId} not found when updating UpdatedAt timestamp", message.ConversationId);
            }

            // Keep only last 100 messages per conversation
            var messageCount = await context.ConversationMessages
                .Where(m => m.ConversationId == message.ConversationId)
                .CountAsync();

            if (messageCount > 100)
            {
                var messagesToDelete = await context.ConversationMessages
                    .Where(m => m.ConversationId == message.ConversationId)
                    .OrderBy(m => m.CreatedAt)
                    .Take(messageCount - 100)
                    .ToListAsync();

                context.ConversationMessages.RemoveRange(messagesToDelete);
                await context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up old messages for conversation {ConversationId}, removed {Count} messages", 
                    message.ConversationId, messagesToDelete.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message for conversation: {ConversationId}, IsUserMessage: {IsUserMessage}, Message: {Message}, Response: {Response}", 
                message.ConversationId, message.IsUserMessage, message.Message?.Substring(0, Math.Min(50, message.Message?.Length ?? 0)) ?? "", 
                message.Response?.Substring(0, Math.Min(50, message.Response?.Length ?? 0)) ?? "");
            throw; // Re-throw to let caller know save failed
        }
    }

    // Legacy methods (deprecated - kept for backward compatibility)
    [Obsolete("Use GetConversationHistoryAsync(int conversationId) instead")]
    public async Task<List<ConversationMessage>> GetConversationHistoryAsync(string userId, int limit = 10)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Get the most recent conversation for this user
            var conversation = await context.Conversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync();

            if (conversation == null)
            {
                // Create a default conversation if none exists
                conversation = await CreateConversationAsync(userId, "Main Conversation");
            }

            return await GetConversationHistoryAsync(conversation.Id, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history for user: {UserId}", userId);
            return new List<ConversationMessage>();
        }
    }

    [Obsolete("Use DeleteConversationAsync(int conversationId) instead")]
    public async Task ClearConversationAsync(string userId)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var conversations = await context.Conversations
                .Where(c => c.UserId == userId)
                .ToListAsync();

            context.Conversations.RemoveRange(conversations);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversations for user: {UserId}", userId);
        }
    }

    [Obsolete("Use GetConversationContextAsync(int conversationId) instead")]
    public async Task<string> GetConversationContextAsync(string userId)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Get the most recent conversation for this user
            var conversation = await context.Conversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync();

            if (conversation == null)
                return string.Empty;

            return await GetConversationContextAsync(conversation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation context for user: {UserId}", userId);
            return string.Empty;
        }
    }
}
