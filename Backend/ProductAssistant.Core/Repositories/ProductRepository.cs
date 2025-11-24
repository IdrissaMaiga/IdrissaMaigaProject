using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Data;
using ProductAssistant.Core.Models;
using System.Linq.Expressions;

namespace ProductAssistant.Core.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(AppDbContext context, ILogger<ProductRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products.ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> GetAllAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return await _context.Products.ToListAsync(cancellationToken);
        }
        
        return await _context.Products
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Products.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<Product> AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        // Preserve CreatedAt if already set (e.g., from scraper), otherwise set it now
        if (entity.CreatedAt == default(DateTime))
        {
            entity.CreatedAt = DateTime.UtcNow;
        }
        
        await _context.Products.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _context.Products.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.AnyAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Enumerable.Empty<Product>();

        // Category field removed - return empty list
        return new List<Product>();
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Product>();

        var lowerSearchTerm = searchTerm.ToLowerInvariant();
        return await _context.Products
            .Where(p => p.Name.ToLower().Contains(lowerSearchTerm) ||
                       (p.StoreName != null && p.StoreName.ToLower().Contains(lowerSearchTerm)))
            .ToListAsync(cancellationToken);
    }

}

