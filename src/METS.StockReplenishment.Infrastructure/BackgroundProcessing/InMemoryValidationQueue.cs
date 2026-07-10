using System.Threading.Channels;
public class InMemoryValidationQueue : IValidationQueue
{
    private readonly Channel<Guid> _queue;

    public InMemoryValidationQueue()
    {
        _queue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask QueueAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return _queue.Writer.WriteAsync(requestId, cancellationToken);
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}