using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Data;
using ProductAssistant.Core.Models;
using System.Linq.Expressions;

namespace ProductAssistant.Core.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ConversationRepository> _logger;

    public ConversationRepository(AppDbContext context, ILogger<ConversationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Conversation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> FindAsync(Expression<Func<Conversation, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation> AddAsync(Conversation entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        await _context.Conversations.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Conversation> UpdateAsync(Conversation entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Conversations.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _context.Conversations.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations.AnyAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation?> GetWithMessagesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(id, cancellationToken);
    }
}

