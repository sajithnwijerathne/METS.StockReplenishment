using Microsoft.EntityFrameworkCore;

public class ReplenishmentRequestRepository : IReplenishmentRequestRepository
{
    private readonly StockReplenishmentDbContext _dbContext;

    public ReplenishmentRequestRepository(StockReplenishmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReplenishmentRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ReplenishmentRequests
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ReplenishmentRequest>> GetPagedAsync(
        RequestFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(filter);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        RequestFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilter(filter).CountAsync(cancellationToken);
    }

    public async Task AddAsync(ReplenishmentRequest request, CancellationToken cancellationToken = default)
    {
        await _dbContext.ReplenishmentRequests.AddAsync(request, cancellationToken);
    }

    public Task RemoveItemsAsync(
        IEnumerable<RequestItem> items,
        CancellationToken cancellationToken = default)
    {
        _dbContext.RequestItems.RemoveRange(items);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ReplenishmentRequest> ApplyFilter(RequestFilterDto filter)
    {
        var query = _dbContext.ReplenishmentRequests.AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(x => x.Priority == filter.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.LocationCode))
        {
            query = query.Where(x => x.LocationCode == filter.LocationCode);
        }

        return query;
    }
}