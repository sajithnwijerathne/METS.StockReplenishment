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

	public async Task<RequestDto> CreateDraftAsync(
    CreateRequestDto dto,
    CancellationToken cancellationToken = default)
	{
		using var response = await _httpClient.PostAsJsonAsync(
			"api/requests",
			dto,
			cancellationToken);

		await EnsureSuccessAsync(response);

		var created = await response.Content.ReadFromJsonAsync<RequestDto>(cancellationToken: cancellationToken);

		return created ?? throw new InvalidOperationException("The API returned an empty response.");
	}

	public async Task SubmitRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
	{
		using var response = await _httpClient.PostAsync(
			$"api/requests/{requestId}/submit",
			content: null,
			cancellationToken);

		await EnsureSuccessAsync(response);
	}

	public async Task ApproveRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
	{
		using var response = await _httpClient.PostAsync(
			$"api/requests/{requestId}/approve",
			content: null,
			cancellationToken);

		await EnsureSuccessAsync(response);
	}

	public async Task RejectRequestAsync(
		Guid requestId,
		RejectRequestDto dto,
		CancellationToken cancellationToken = default)
	{
		using var response = await _httpClient.PostAsJsonAsync(
			$"api/requests/{requestId}/reject",
			dto,
			cancellationToken);

		await EnsureSuccessAsync(response);
	}

	public async Task FulfillRequestAsync(
		Guid requestId,
		FulfillRequestDto dto,
		CancellationToken cancellationToken = default)
	{
		using var response = await _httpClient.PostAsJsonAsync(
			$"api/requests/{requestId}/fulfill",
			dto,
			cancellationToken);

		await EnsureSuccessAsync(response);
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

	private static async Task EnsureSuccessAsync(HttpResponseMessage response)
	{
		if (response.IsSuccessStatusCode)
		{
			return;
		}

		var errorBody = await response.Content.ReadAsStringAsync();
		var message = string.IsNullOrWhiteSpace(errorBody)
				? $"Request failed with status code {(int)response.StatusCode}."
				: errorBody;

		throw new InvalidOperationException(message);
	}
}

public class LocationOptionDto
{
	public string Code { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;
}