using Microsoft.EntityFrameworkCore;

public class LocationRepository : ILocationRepository
{
    private readonly StockReplenishmentDbContext _dbContext;

    public LocationRepository(StockReplenishmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsAsync(string locationCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .AnyAsync(x => x.Code == locationCode, cancellationToken);
    }

    public async Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }
}