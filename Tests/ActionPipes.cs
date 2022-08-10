using PRI.Messaging.Patterns;
using PRI.Messaging.Primitives;

using Tests.Mocks;

namespace PRI.Messaging.Tests;

public class ActionPipes
{
	[Fact]
	public void NullDelegateCausesException()
	{
		Assert.Throws<ArgumentNullException>(() => new ActionPipe<IMessage, IMessage>(null!));
	}

	[Fact]
	public void NullConsumerCausesException()
	{
		var ac = new ActionPipe<IMessage, IMessage>(m => m);
		Assert.Throws<InvalidOperationException>(() => ac.Handle(new Message1()));
	}

	[Fact]
	public void ConsumerIsCalledWhenHandlingMessage()
	{
		using var toggle = new ManualResetEventSlim(false);
		var ac = new ActionPipe<IMessage, IMessage>(m => m);
		ac.AttachConsumer(new ActionConsumer<IMessage>(_ => toggle.Set()));
		ac.Handle(new Message1());
		Assert.True(toggle.IsSet);
	}
}
