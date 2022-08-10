using PRI.Messaging.Patterns;
using PRI.Messaging.Primitives;

namespace PRI.Messaging.Tests;

public class CreatingActionConsumers
{
	[Fact]
	public void NullDelegateCausesException()
	{
		Assert.Throws<ArgumentNullException>(() =>
		{
			new ActionConsumer<IMessage>(null!);
		});
	}
}
