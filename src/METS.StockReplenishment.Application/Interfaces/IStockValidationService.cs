public interface IStockValidationService
{
    Task<ValidationStatus> ValidateAsync(
        ReplenishmentRequest request,
        CancellationToken cancellationToken = default);
}