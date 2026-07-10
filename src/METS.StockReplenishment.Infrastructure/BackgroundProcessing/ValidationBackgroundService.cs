using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class ValidationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IValidationQueue _validationQueue;
    private readonly ILogger<ValidationBackgroundService> _logger;

    public ValidationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IValidationQueue validationQueue,
        ILogger<ValidationBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _validationQueue = validationQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var requestId = await _validationQueue.DequeueAsync(stoppingToken);

                using var scope = _serviceScopeFactory.CreateScope();

                var requestRepository = scope.ServiceProvider.GetRequiredService<IReplenishmentRequestRepository>();
                var stockValidationService = scope.ServiceProvider.GetRequiredService<IStockValidationService>();

                var request = await requestRepository.GetByIdAsync(requestId, stoppingToken);
                if (request is null)
                {
                    _logger.LogWarning("Validation skipped because request {RequestId} was not found.", requestId);
                    continue;
                }

                if (request.Status != RequestStatus.Submitted)
                {
                    _logger.LogInformation(
                        "Validation skipped because request {RequestId} is in status {Status}.",
                        requestId,
                        request.Status);
                    continue;
                }

                var validationStatus = await stockValidationService.ValidateAsync(request, stoppingToken);

                request.ValidationStatus = validationStatus;

                await requestRepository.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "Validation completed for request {RequestId} with result {ValidationStatus}.",
                    requestId,
                    validationStatus);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing stock validation.");
            }
        }
    }
}