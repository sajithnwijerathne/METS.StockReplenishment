public interface ILocationRepository
{
    Task<bool> ExistsAsync(string locationCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default);
}