using NSubstitute;

namespace METS.StockReplenishment.Tests.Application;

[TestFixture]
public class ReplenishmentRequestServiceTests
{
	private IReplenishmentRequestRepository _requestRepository = null!;
	private ILocationRepository _locationRepository = null!;
	private IValidationQueue _validationQueue = null!;
	private ReplenishmentRequestService _service = null!;

	[SetUp]
	public void SetUp()
	{
		_requestRepository = Substitute.For<IReplenishmentRequestRepository>();
		_locationRepository = Substitute.For<ILocationRepository>();
		_validationQueue = Substitute.For<IValidationQueue>();

		_service = new ReplenishmentRequestService(
			_requestRepository,
			_locationRepository,
			_validationQueue);
	}

	[Test]
	public async Task CreateDraftAsync_WhenLocationExists_CreatesDraftAndPersists()
	{
		var dto = new CreateRequestDto
		{
			LocationCode = "HITACHI-Ludvika",
			Priority = Priority.Normal,
			Items =
			[
				new CreateRequestItemDto
					{
						ArticleNumber = "ART-1001",
						Description = "Bearing",
						RequestedQuantity = 12
					}
			]
		};

		_locationRepository.ExistsAsync(dto.LocationCode, Arg.Any<CancellationToken>())
			.Returns(true);

		var result = await _service.CreateDraftAsync(dto);

		Assert.That(result.LocationCode, Is.EqualTo(dto.LocationCode));
		Assert.That(result.Priority, Is.EqualTo(dto.Priority));
		Assert.That(result.Status, Is.EqualTo(RequestStatus.Draft));
		Assert.That(result.ValidationStatus, Is.EqualTo(ValidationStatus.NotStarted));

		await _requestRepository.Received(1).AddAsync(
			Arg.Is<ReplenishmentRequest>(request =>
				request.LocationCode == dto.LocationCode &&
				request.Priority == dto.Priority &&
				request.Status == RequestStatus.Draft &&
				request.ValidationStatus == ValidationStatus.NotStarted &&
				request.Items.Count == 1 &&
				request.Items[0].ArticleNumber == "ART-1001"),
			Arg.Any<CancellationToken>());

		await _requestRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Test]
	public void CreateDraftAsync_WhenLocationDoesNotExist_ThrowsInvalidOperationException()
	{
		var dto = new CreateRequestDto
		{
			LocationCode = "UNKNOWN",
			Priority = Priority.Normal,
			Items =
			[
				new CreateRequestItemDto
					{
						ArticleNumber = "ART-1001",
						Description = "Bearing",
						RequestedQuantity = 5
					}
			]
		};

		_locationRepository.ExistsAsync(dto.LocationCode, Arg.Any<CancellationToken>())
			.Returns(false);

		var act = () => _service.CreateDraftAsync(dto);

		Assert.That(act, Throws.TypeOf<InvalidOperationException>()
			.With.Message.Contains("does not exist"));
	}

	[Test]
	public async Task SubmitAsync_WhenDraft_UpdatesStateAndQueuesValidation()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequest(requestId, RequestStatus.Draft, ValidationStatus.NotStarted);

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		await _service.SubmitAsync(requestId);

		Assert.That(request.Status, Is.EqualTo(RequestStatus.Submitted));
		Assert.That(request.ValidationStatus, Is.EqualTo(ValidationStatus.Pending));
		Assert.That(request.SubmittedAt, Is.Not.Null);
		Assert.That(request.RejectionReason, Is.Null);

		await _requestRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
		await _validationQueue.Received(1).QueueAsync(requestId, Arg.Any<CancellationToken>());
	}

	[Test]
	public void SubmitAsync_WhenRequestIsNotDraft_ThrowsInvalidOperationException()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequest(requestId, RequestStatus.Submitted, ValidationStatus.Pending);

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		var act = () => _service.SubmitAsync(requestId);

		Assert.That(act, Throws.TypeOf<InvalidOperationException>()
			.With.Message.EqualTo("Only draft requests can be submitted."));
	}

	[Test]
	public void ApproveAsync_WhenValidationIsNotValid_ThrowsInvalidOperationException()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequest(requestId, RequestStatus.Submitted, ValidationStatus.Invalid);

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		var act = () => _service.ApproveAsync(requestId);

		Assert.That(act, Throws.TypeOf<InvalidOperationException>()
			.With.Message.Contains("valid stock validation"));
	}

	[Test]
	public async Task ApproveAsync_WhenSubmittedAndValid_ApprovesRequest()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequest(requestId, RequestStatus.Submitted, ValidationStatus.Valid);

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		await _service.ApproveAsync(requestId);

		Assert.That(request.Status, Is.EqualTo(RequestStatus.Approved));
		Assert.That(request.ReviewedAt, Is.Not.Null);
		Assert.That(request.RejectionReason, Is.Null);

		await _requestRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task RejectAsync_WhenSubmitted_SetsRejectedStateAndReason()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequest(requestId, RequestStatus.Submitted, ValidationStatus.Valid);
		var dto = new RejectRequestDto
		{
			Reason = "Stock request is not required."
		};

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		await _service.RejectAsync(requestId, dto);

		Assert.That(request.Status, Is.EqualTo(RequestStatus.Rejected));
		Assert.That(request.ReviewedAt, Is.Not.Null);
		Assert.That(request.RejectionReason, Is.EqualTo(dto.Reason));

		await _requestRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Test]
	public void FulfillAsync_WhenAnyQuantityExceedsRequested_ThrowsInvalidOperationException()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequest(requestId, RequestStatus.Approved, ValidationStatus.Valid);
		var requestItemId = request.Items[0].Id;

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		var dto = new FulfillRequestDto
		{
			Items =
			[
				new FulfillRequestItemDto
					{
						RequestItemId = requestItemId,
						FulfilledQuantity = 999
					}
			]
		};

		var act = () => _service.FulfillAsync(requestId, dto);

		Assert.That(act, Throws.TypeOf<InvalidOperationException>()
			.With.Message.Contains("cannot exceed requested quantity"));
	}

	[Test]
	public async Task FulfillAsync_WhenApproved_UpdatesQuantitiesAndMarksFulfilled()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequest(requestId, RequestStatus.Approved, ValidationStatus.Valid);
		var firstItem = request.Items[0];

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		var dto = new FulfillRequestDto
		{
			Items =
			[
				new FulfillRequestItemDto
					{
						RequestItemId = firstItem.Id,
						FulfilledQuantity = firstItem.RequestedQuantity
					}
			]
		};

		await _service.FulfillAsync(requestId, dto);

		Assert.That(firstItem.FulfilledQuantity, Is.EqualTo(firstItem.RequestedQuantity));
		Assert.That(request.Status, Is.EqualTo(RequestStatus.Fulfilled));
		Assert.That(request.FulfilledAt, Is.Not.Null);

		await _requestRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task GetPagedAsync_NormalizesPaginationAndMapsResult()
	{
		var filter = new RequestFilterDto
		{
			Status = RequestStatus.Draft,
			PageNumber = 0,
			PageSize = 0
		};

		var requests = new List<ReplenishmentRequest>
		{
			BuildRequest(Guid.NewGuid(), RequestStatus.Draft, ValidationStatus.NotStarted)
		};

		_requestRepository.GetPagedAsync(
				Arg.Is<RequestFilterDto>(f => f.PageNumber == 1 && f.PageSize == 10),
				Arg.Any<CancellationToken>())
			.Returns(requests);

		_requestRepository.CountAsync(
				Arg.Is<RequestFilterDto>(f => f.PageNumber == 1 && f.PageSize == 10),
				Arg.Any<CancellationToken>())
			.Returns(1);

		var result = await _service.GetPagedAsync(filter);

		Assert.That(result.PageNumber, Is.EqualTo(1));
		Assert.That(result.PageSize, Is.EqualTo(10));
		Assert.That(result.TotalCount, Is.EqualTo(1));
		Assert.That(result.Items, Has.Count.EqualTo(1));
		Assert.That(result.Items[0].Status, Is.EqualTo(RequestStatus.Draft));
	}

	private static ReplenishmentRequest BuildRequest(
		Guid requestId,
		RequestStatus status,
		ValidationStatus validationStatus)
	{
		return new ReplenishmentRequest
		{
			Id = requestId,
			LocationCode = "HITACHI-Ludvika",
			Priority = Priority.Normal,
			Status = status,
			ValidationStatus = validationStatus,
			CreatedAt = DateTime.UtcNow,
			Items =
			[
				new RequestItem
					{
						Id = Guid.NewGuid(),
						ReplenishmentRequestId = requestId,
						ArticleNumber = "ART-1001",
						Description = "Bearing",
						RequestedQuantity = 12,
						FulfilledQuantity = 0
					}
			]
		};
	}

	private static ReplenishmentRequest BuildRequestWithItems(
		Guid requestId,
		RequestStatus status,
		ValidationStatus validationStatus,
		params RequestItem[] items)
	{
		return new ReplenishmentRequest
		{
			Id = requestId,
			LocationCode = "HITACHI-Ludvika",
			Priority = Priority.Normal,
			Status = status,
			ValidationStatus = validationStatus,
			CreatedAt = DateTime.UtcNow,
			Items = items.ToList()
		};
	}

	[Test]
	public async Task UpdateDraftAsync_WhenRequestIsDraft_UpdatesRequestAndPersists()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequest(requestId, RequestStatus.Draft, ValidationStatus.NotStarted);

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		_locationRepository.ExistsAsync("HITACHI-Västerås", Arg.Any<CancellationToken>())
			.Returns(true);

		var dto = new CreateRequestDto
		{
			LocationCode = "HITACHI-Västerås",
			Priority = Priority.Urgent,
			Items =
			[
				new CreateRequestItemDto
				{
					ArticleNumber = "ART-2001",
					Description = "Updated Item",
					RequestedQuantity = 8
				}
			]
		};

		var result = await _service.UpdateDraftAsync(requestId, dto);

		Assert.That(result.LocationCode, Is.EqualTo("HITACHI-Västerås"));
		Assert.That(result.Priority, Is.EqualTo(Priority.Urgent));
		Assert.That(result.Status, Is.EqualTo(RequestStatus.Draft));
		Assert.That(result.ValidationStatus, Is.EqualTo(ValidationStatus.NotStarted));

		Assert.That(request.LocationCode, Is.EqualTo("HITACHI-Västerås"));
		Assert.That(request.Priority, Is.EqualTo(Priority.Urgent));
		Assert.That(request.Items, Has.Count.EqualTo(1));
		Assert.That(request.Items[0].ArticleNumber, Is.EqualTo("ART-2001"));

		await _requestRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task UpdateDraftAsync_WhenReplacingWithFewerItems_RemovesExistingItemsAndPersists()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequestWithItems(
			requestId,
			RequestStatus.Draft,
			ValidationStatus.NotStarted,
			new RequestItem
			{
				Id = Guid.NewGuid(),
				ReplenishmentRequestId = requestId,
				ArticleNumber = "ART-1001",
				Description = "Bearing",
				RequestedQuantity = 12,
				FulfilledQuantity = 0
			},
			new RequestItem
			{
				Id = Guid.NewGuid(),
				ReplenishmentRequestId = requestId,
				ArticleNumber = "ART-1002",
				Description = "Seal",
				RequestedQuantity = 4,
				FulfilledQuantity = 0
			});

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		_requestRepository.RemoveItemsAsync(Arg.Any<IEnumerable<RequestItem>>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		_locationRepository.ExistsAsync("HITACHI-Västerås", Arg.Any<CancellationToken>())
			.Returns(true);

		var dto = new CreateRequestDto
		{
			LocationCode = "HITACHI-Västerås",
			Priority = Priority.Urgent,
			Items =
			[
				new CreateRequestItemDto
				{
					ArticleNumber = "ART-2001",
					Description = "Updated Item",
					RequestedQuantity = 8
				}
			]
		};

		var result = await _service.UpdateDraftAsync(requestId, dto);

		Assert.That(result.LocationCode, Is.EqualTo("HITACHI-Västerås"));
		Assert.That(result.Priority, Is.EqualTo(Priority.Urgent));
		Assert.That(result.Items, Has.Count.EqualTo(1));
		Assert.That(result.Items[0].ArticleNumber, Is.EqualTo("ART-2001"));

		Assert.That(request.Items, Has.Count.EqualTo(1));
		Assert.That(request.Items[0].ArticleNumber, Is.EqualTo("ART-2001"));
		Assert.That(request.Items[0].Description, Is.EqualTo("Updated Item"));
		Assert.That(request.Items[0].RequestedQuantity, Is.EqualTo(8));

		await _requestRepository.Received(1).RemoveItemsAsync(
			Arg.Is<IEnumerable<RequestItem>>(items => items.Count() == 1),
			Arg.Any<CancellationToken>());

		await _requestRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task UpdateDraftAsync_WhenReplacingWithMoreItems_RebuildsItemCollection()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequest(requestId, RequestStatus.Draft, ValidationStatus.NotStarted);

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		_requestRepository.RemoveItemsAsync(Arg.Any<IEnumerable<RequestItem>>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		_locationRepository.ExistsAsync("HITACHI-Ludvika", Arg.Any<CancellationToken>())
			.Returns(true);

		var dto = new CreateRequestDto
		{
			LocationCode = "HITACHI-Ludvika",
			Priority = Priority.Normal,
			Items =
			[
				new CreateRequestItemDto
				{
					ArticleNumber = "ART-2001",
					Description = "Bearing",
					RequestedQuantity = 5
				},
				new CreateRequestItemDto
				{
					ArticleNumber = "ART-2002",
					Description = "Seal",
					RequestedQuantity = 9
				}
			]
		};

		var result = await _service.UpdateDraftAsync(requestId, dto);

		Assert.That(result.Items, Has.Count.EqualTo(2));
		Assert.That(result.Items[0].ArticleNumber, Is.EqualTo("ART-2001"));
		Assert.That(result.Items[1].ArticleNumber, Is.EqualTo("ART-2002"));

		Assert.That(request.Items, Has.Count.EqualTo(2));
		Assert.That(request.Items[0].ArticleNumber, Is.EqualTo("ART-2001"));
		Assert.That(request.Items[1].ArticleNumber, Is.EqualTo("ART-2002"));

		await _requestRepository.DidNotReceive().RemoveItemsAsync(
			Arg.Any<IEnumerable<RequestItem>>(),
			Arg.Any<CancellationToken>());

		await _requestRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Test]
	public void UpdateDraftAsync_WhenRequestIsNotDraft_ThrowsInvalidOperationException()
	{
		var requestId = Guid.NewGuid();
		var request = BuildRequest(requestId, RequestStatus.Submitted, ValidationStatus.Pending);

		_requestRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
			.Returns(request);

		var dto = new CreateRequestDto
		{
			LocationCode = "HITACHI-Ludvika",
			Priority = Priority.Normal,
			Items =
			[
				new CreateRequestItemDto
				{
					ArticleNumber = "ART-1001",
					Description = "Bearing",
					RequestedQuantity = 5
				}
			]
		};

		var act = () => _service.UpdateDraftAsync(requestId, dto);

		Assert.That(act, Throws.TypeOf<InvalidOperationException>()
			.With.Message.EqualTo("Only draft requests can be edited."));
	}
}
