public class SimulatedStockValidationService : IStockValidationService
{
    public async Task<ValidationStatus> ValidateAsync(
        ReplenishmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var delayMilliseconds = Random.Shared.Next(2000, 5001);
        await Task.Delay(delayMilliseconds, cancellationToken);

        var totalRequestedQuantity = request.Items.Sum(item => item.RequestedQuantity);

        if (request.Priority == Priority.Urgent && totalRequestedQuantity > 100)
        {
            return ValidationStatus.Invalid;
        }

        var isValid = Random.Shared.NextDouble() >= 0.2;
        return isValid ? ValidationStatus.Valid : ValidationStatus.Invalid;
    }
}