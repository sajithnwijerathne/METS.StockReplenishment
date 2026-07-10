using System.Net.Http.Json;

public class ReplenishmentApiClient
{
    private readonly HttpClient _httpClient;

    public ReplenishmentApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResultDto<RequestListItemDto>> GetRequestsAsync(
        RequestFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = BuildRequestQuery(filter);

        var result = await _httpClient.GetFromJsonAsync<PagedResultDto<RequestListItemDto>>(
            $"api/requests{query}",
            cancellationToken);

        return result ?? new PagedResultDto<RequestListItemDto>
        {
            Items = [],
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = 0
        };
    }

    public Task<RequestDto?> GetRequestByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.GetFromJsonAsync<RequestDto>(
            $"api/requests/{requestId}",
            cancellationToken);
    }

    public async Task<IReadOnlyList<LocationOptionDto>> GetLocationsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<LocationOptionDto>>(
            "api/locations",
            cancellationToken);

        return result ?? [];
    }

    private static string BuildRequestQuery(RequestFilterDto filter)
    {
        var parameters = new List<string>();

        if (filter.Status.HasValue)
        {
            parameters.Add($"status={Uri.EscapeDataString(filter.Status.Value.ToString())}");
        }

        if (filter.Priority.HasValue)
        {
            parameters.Add($"priority={Uri.EscapeDataString(filter.Priority.Value.ToString())}");
        }

        if (!string.IsNullOrWhiteSpace(filter.LocationCode))
        {
            parameters.Add($"locationCode={Uri.EscapeDataString(filter.LocationCode)}");
        }

        parameters.Add($"pageNumber={filter.PageNumber}");
        parameters.Add($"pageSize={filter.PageSize}");

        return parameters.Count == 0
            ? string.Empty
            : "?" + string.Join("&", parameters);
    }
}

public class LocationOptionDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}