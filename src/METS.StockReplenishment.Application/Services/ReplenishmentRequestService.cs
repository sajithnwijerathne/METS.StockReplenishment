public class ReplenishmentRequestService : IReplenishmentRequestService
{
    private readonly IReplenishmentRequestRepository _requestRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IValidationQueue _validationQueue;
    public ReplenishmentRequestService(
        IReplenishmentRequestRepository requestRepository,
        ILocationRepository locationRepository,
        IValidationQueue validationQueue)
    {
        _requestRepository = requestRepository;
        _locationRepository = locationRepository;
        _validationQueue = validationQueue;
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
            ValidationStatus = ValidationStatus.NotStarted,
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
        await _requestRepository.SaveChangesAsync(cancellationToken);

        return new RequestDto
        {
            Id = request.Id,
            LocationCode = request.LocationCode,
            Priority = request.Priority,
            Status = request.Status,
            ValidationStatus = request.ValidationStatus
        };
    }

    public async Task<RequestDto?> GetByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            return null;
        }

        return MapToRequestDto(request);
    }

    public async Task<PagedResultDto<RequestListItemDto>> GetPagedAsync(
        RequestFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

        var normalizedFilter = new RequestFilterDto
        {
            Status = filter.Status,
            Priority = filter.Priority,
            LocationCode = filter.LocationCode?.Trim(),
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        
        var requests = await _requestRepository.GetPagedAsync(normalizedFilter, cancellationToken);
        var totalCount = await _requestRepository.CountAsync(normalizedFilter, cancellationToken);

        return new PagedResultDto<RequestListItemDto>
        {
            Items = requests.Select(MapToRequestListItemDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task SubmitAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            throw new InvalidOperationException($"Replenishment request with ID '{requestId}' does not exist.");
        }

        if (request.Status != RequestStatus.Draft)
        {
            throw new InvalidOperationException("Only draft requests can be submitted.");
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("A request must contain at least one item before it can be submitted.");
        }

        request.Status = RequestStatus.Submitted;
        request.ValidationStatus = ValidationStatus.Pending;
        request.SubmittedAt = DateTime.UtcNow;
        request.RejectionReason = null;

        await _requestRepository.SaveChangesAsync(cancellationToken);
        await _validationQueue.QueueAsync(requestId, cancellationToken);
    }

    public async Task ApproveAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await GetExistingRequestAsync(requestId, cancellationToken);

        if (request.Status != RequestStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted requests can be approved.");
        }

        if (request.ValidationStatus != ValidationStatus.Valid)
        {
            throw new InvalidOperationException("Only requests with valid stock validation can be approved.");
        }

        request.Status = RequestStatus.Approved;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectionReason = null;

        await _requestRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(
        Guid requestId,
        RejectRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(dto.Reason));
        }

        var request = await GetExistingRequestAsync(requestId, cancellationToken);

        if (request.Status != RequestStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted requests can be rejected.");
        }

        request.Status = RequestStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectionReason = dto.Reason;

        await _requestRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task FulfillAsync(
        Guid requestId,
        FulfillRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ArgumentException("At least one item is required for fulfillment.", nameof(dto.Items));
        }

        var request = await GetExistingRequestAsync(requestId, cancellationToken);

        if (request.Status != RequestStatus.Approved)
        {
            throw new InvalidOperationException("Only approved requests can be fulfilled.");
        }

        var fulfillmentsByItemId = dto.Items.ToDictionary(item => item.RequestItemId);

        foreach (var item in request.Items)
        {
            if (!fulfillmentsByItemId.TryGetValue(item.Id, out var fulfillItem))
            {
                continue;
            }
            if (fulfillItem.FulfilledQuantity > item.RequestedQuantity)
            {
                throw new InvalidOperationException($"Fulfilled quantity for item '{item.ArticleNumber}' cannot exceed requested quantity.");
            }
            item.FulfilledQuantity = fulfillItem.FulfilledQuantity;
        }

        request.Status = RequestStatus.Fulfilled;
        request.FulfilledAt = DateTime.UtcNow;

        await _requestRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<RequestDto> UpdateDraftAsync(
        Guid requestId,
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

        var request = await GetExistingRequestAsync(requestId, cancellationToken);

        if (request.Status != RequestStatus.Draft)
        {
            throw new InvalidOperationException("Only draft requests can be edited.");
        }

        var locationExists = await _locationRepository.ExistsAsync(dto.LocationCode, cancellationToken);
        if (!locationExists)
        {
            throw new InvalidOperationException($"Location with code '{dto.LocationCode}' does not exist.");
        }

        request.LocationCode = dto.LocationCode;
        request.Priority = dto.Priority;
        request.ValidationStatus = ValidationStatus.NotStarted;
        request.RejectionReason = null;
        request.SubmittedAt = null;
        request.ReviewedAt = null;
        request.FulfilledAt = null;

        var existingItems = request.Items.ToList();

        for (int i = 0; i < dto.Items.Count; i++)
        {
            var incomingItem = dto.Items[i];
            if (i < existingItems.Count)
            {
                var existingItem = existingItems[i];
                existingItem.ArticleNumber = incomingItem.ArticleNumber;
                existingItem.Description = incomingItem.Description;
                existingItem.RequestedQuantity = incomingItem.RequestedQuantity;
            }
            else
            {
                request.Items.Add(new RequestItem
                {
                    ArticleNumber = incomingItem.ArticleNumber,
                    Description = incomingItem.Description,
                    RequestedQuantity = incomingItem.RequestedQuantity,
                    FulfilledQuantity = 0
                });
            }
        }

        if (existingItems.Count > dto.Items.Count)
        {
            var itemsToRemove = existingItems.Skip(dto.Items.Count).ToList();
            await _requestRepository.RemoveItemsAsync(itemsToRemove, cancellationToken);
            foreach (var item in itemsToRemove)
            {
                request.Items.Remove(item);
            }
        }

        await _requestRepository.SaveChangesAsync(cancellationToken);

        return MapToRequestDto(request);
    }

    private async Task<ReplenishmentRequest> GetExistingRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            throw new KeyNotFoundException($"Replenishment request with ID '{requestId}' does not exist.");
        }

        return request;
    }

    private static RequestDto MapToRequestDto(ReplenishmentRequest request)
    {
        return new RequestDto
        {
            Id = request.Id,
            LocationCode = request.LocationCode,
            Priority = request.Priority,
            Status = request.Status,
            ValidationStatus = request.ValidationStatus,
            RejectionReason = request.RejectionReason,
            CreatedAt = request.CreatedAt,
            SubmittedAt = request.SubmittedAt,
            ReviewedAt = request.ReviewedAt,
            FulfilledAt = request.FulfilledAt,
            Items = request.Items.Select(MapToRequestItemDto).ToList()
        };
    }

    private static RequestItemDto MapToRequestItemDto(RequestItem item)
    {
        return new RequestItemDto
        {
            Id = item.Id,
            ArticleNumber = item.ArticleNumber,
            Description = item.Description,
            RequestedQuantity = item.RequestedQuantity,
            FulfilledQuantity = item.FulfilledQuantity
        };
    }

    private static RequestListItemDto MapToRequestListItemDto(ReplenishmentRequest request)
    {
        return new RequestListItemDto
        {
            Id = request.Id,
            LocationCode = request.LocationCode,
            Priority = request.Priority,
            Status = request.Status,
            ValidationStatus = request.ValidationStatus,
            CreatedAt = request.CreatedAt,
            ItemCount = request.Items.Count
        };
    }
}