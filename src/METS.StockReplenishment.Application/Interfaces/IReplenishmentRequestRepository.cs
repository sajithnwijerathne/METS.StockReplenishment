public interface IReplenishmentRequestRepository
{
    Task<ReplenishmentRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReplenishmentRequest>> GetPagedAsync(
        RequestFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(RequestFilterDto filter, CancellationToken cancellationToken = default);

    Task AddAsync(ReplenishmentRequest request, CancellationToken cancellationToken = default);

    Task RemoveItemsAsync(
        IEnumerable<RequestItem> items,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}