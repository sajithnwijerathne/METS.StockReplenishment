public interface IReplenishmentRequestService
{
    Task<RequestDto> CreateDraftAsync(CreateRequestDto dto, CancellationToken cancellationToken = default);

    Task<RequestDto?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<RequestListItemDto>> GetPagedAsync(
        RequestFilterDto filter,
        CancellationToken cancellationToken = default);

    Task SubmitAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task ApproveAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task RejectAsync(Guid requestId, RejectRequestDto dto, CancellationToken cancellationToken = default);

    Task FulfillAsync(Guid requestId, FulfillRequestDto dto, CancellationToken cancellationToken = default);
}