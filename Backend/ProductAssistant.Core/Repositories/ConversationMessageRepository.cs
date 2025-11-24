using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Data;
using ProductAssistant.Core.Models;
using System.Linq.Expressions;

namespace ProductAssistant.Core.Repositories;

public class ConversationMessageRepository : IConversationMessageRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ConversationMessageRepository> _logger;

    public ConversationMessageRepository(AppDbContext context, ILogger<ConversationMessageRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ConversationMessage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ConversationMessages
            .Include(m => m.Conversation)
            .Include(m => m.Product)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ConversationMessage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ConversationMessages
            .Include(m => m.Conversation)
            .Include(m => m.Product)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConversationMessage>> FindAsync(Expression<Func<ConversationMessage, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.ConversationMessages
            .Include(m => m.Conversation)
            .Include(m => m.Product)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConversationMessage> AddAsync(ConversationMessage entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        await _context.ConversationMessages.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<ConversationMessage> UpdateAsync(ConversationMessage entity, CancellationToken cancellationToken = default)
    {
        _context.ConversationMessages.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _context.ConversationMessages.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ConversationMessages.AnyAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ConversationMessage>> GetByConversationIdAsync(int conversationId, int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _context.ConversationMessages
            .Include(m => m.Product)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConversationMessage>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ConversationMessages
            .Include(m => m.Conversation)
            .Include(m => m.Product)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

