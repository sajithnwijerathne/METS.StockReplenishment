namespace METS.StockReplenishment.Tests.Infrastructure;

[TestFixture]
public class InMemoryValidationQueueTests
{
	[Test]
	public async Task QueueAsync_ThenDequeueAsync_ReturnsQueuedIdsInOrder()
	{
		var queue = new InMemoryValidationQueue();
		var firstId = Guid.NewGuid();
		var secondId = Guid.NewGuid();

		await queue.QueueAsync(firstId);
		await queue.QueueAsync(secondId);

		var dequeuedFirst = await queue.DequeueAsync();
		var dequeuedSecond = await queue.DequeueAsync();

		Assert.That(dequeuedFirst, Is.EqualTo(firstId));
		Assert.That(dequeuedSecond, Is.EqualTo(secondId));
	}
}