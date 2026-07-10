using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly ILocationRepository _locationRepository;

    public LocationsController(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LocationOptionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LocationOptionResponse>>> GetLocations(
        CancellationToken cancellationToken = default)
    {
        var locations = await _locationRepository.GetAllAsync(cancellationToken);

        var response = locations
            .Select(location => new LocationOptionResponse
            {
                Code = location.Code,
                Name = location.Name
            })
            .ToList();

        return Ok(response);
    }

    public class LocationOptionResponse
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}