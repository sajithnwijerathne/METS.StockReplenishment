using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly IReplenishmentRequestService _requestService;

    public RequestsController(IReplenishmentRequestService requestService)
    {
        _requestService = requestService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<RequestListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<RequestListItemDto>>> GetRequests(
        [FromQuery] RequestStatus? status,
        [FromQuery] Priority? priority,
        [FromQuery] string? locationCode,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var filter = new RequestFilterDto
        {
            Status = status,
            Priority = priority,
            LocationCode = locationCode,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _requestService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> GetRequestById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var request = await _requestService.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return NotFound(new { message = $"Request '{id}' was not found." });
        }

        return Ok(request);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequestDto>> CreateDraft(
        [FromBody] CreateRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var createdRequest = await _requestService.CreateDraftAsync(dto, cancellationToken);

            return CreatedAtAction(
                nameof(GetRequestById),
                new { id = createdRequest.Id },
                createdRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> UpdateDraft(
        Guid id,
        [FromBody] CreateRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updatedRequest = await _requestService.UpdateDraftAsync(id, dto, cancellationToken);
            return Ok(updatedRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Submit(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _requestService.SubmitAsync(id, cancellationToken);
            return Accepted(new
            {
                message = "Request submitted. Stock validation is in progress.",
                requestId = id
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Approve(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _requestService.ApproveAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Reject(
        Guid id,
        [FromBody] RejectRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _requestService.RejectAsync(id, dto, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/fulfill")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Fulfill(
        Guid id,
        [FromBody] FulfillRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _requestService.FulfillAsync(id, dto, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}