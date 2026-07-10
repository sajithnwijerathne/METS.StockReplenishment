public interface IValidationQueue
{
    ValueTask QueueAsync(Guid requestId, CancellationToken cancellationToken = default);

    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken = default);
}