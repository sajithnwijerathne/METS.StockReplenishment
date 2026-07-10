using System.Net.NetworkInformation;

public class ReplenishmentRequestService : IReplenishmentRequestService
{
    private readonly IReplenishmentRequestRepository _requestRepository;
    private readonly ILocationRepository _locationRepository;

    public ReplenishmentRequestService(IReplenishmentRequestRepository requestRepository, ILocationRepository locationRepository)
    {
        _requestRepository = requestRepository;
        _locationRepository = locationRepository;
    }

    public async Task<RequestDto> CreateDraftAsync(
        CreateRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (string.IsNullOrWhiteSpace(dto.LocationCode))
        {
            throw new ArgumentException("Location code is required.", nameof(dto.LocationCode));
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ArgumentException("At least one item is required.", nameof(dto.Items));
        }

        var locationExists = await _locationRepository.ExistsAsync(dto.LocationCode, cancellationToken);
        if (!locationExists)
        {
            throw new InvalidOperationException($"Location with code '{dto.LocationCode}' does not exist.");
        }

        var request = new ReplenishmentRequest
        {
            Id = Guid.NewGuid(),
            LocationCode = dto.LocationCode,
            Priority = dto.Priority,
            Status = RequestStatus.Draft,
            ValidationStatus = ValidationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = dto.Items.Select(item => new RequestItem
            {
                Id = Guid.NewGuid(),
                ArticleNumber = item.ArticleNumber,
                Description = item.Description,
                RequestedQuantity = item.RequestedQuantity,
                FulfilledQuantity = 0
            }).ToList()
        };

        await _requestRepository.AddAsync(request, cancellationToken);

        return new RequestDto
        {
            Id = request.Id,
            LocationCode = request.LocationCode,
            Priority = request.Priority,
            Status = request.Status,
            ValidationStatus = request.ValidationStatus
        };
    }
}