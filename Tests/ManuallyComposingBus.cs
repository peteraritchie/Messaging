using PRI.Messaging.Patterns;
using PRI.Messaging.Primitives;

using Tests.Mocks;

namespace PRI.Messaging.Tests;

public class ManuallyComposingBus
{
	public ManuallyComposingBus()
	{
		Pipe.LastMessageProcessed = null;
	}

	[Fact]
	public void ManuallyComposedTypeSynchronouslyHandlesMessageProperly()
	{
		var message1 = new Message1 { CorrelationId = "1234" };

		var bus = new Bus();

		var message2Consumer = new Message2Consumer();
		bus.AddHandler(message2Consumer);

		var pipe = new Pipe();
		bus.AddTranslator(pipe);

		bus.Handle(message1);

		Assert.Same(message1, Pipe.LastMessageProcessed);
		Assert.NotNull(Message2Consumer.LastMessageReceived);
		Assert.Equal(message1.CorrelationId, Message2Consumer.LastMessageReceived!.CorrelationId);
	}

	[Fact]
	public void ManuallyComposedWithTranslatorFirstTypeHandlesMessageProperly()
	{
		var message1 = new Message1 { CorrelationId = "1234" };

		var bus = new Bus();

		var pipe = new Pipe();
		bus.AddTranslator(pipe);

		var message2Consumer = new Message2Consumer();
		bus.AddHandler(message2Consumer);

		bus.Handle(message1);

		Assert.Same(message1, Pipe.LastMessageProcessed);
		Assert.NotNull(Message2Consumer.LastMessageReceived);
		Assert.Equal(message1.CorrelationId, Message2Consumer.LastMessageReceived!.CorrelationId);
	}

	[Fact]
	public void ManuallyComposedWithTwoTranslatorsFirstTypeHandlesMessageProperly()
	{
		var message1 = new Message1 { CorrelationId = "1234" };

		var bus = new Bus();

		var pipe1 = new Pipe();
		bus.AddTranslator(pipe1);
		var pipe2 = new Pipe();
		bus.AddTranslator(pipe2);

		var message2Consumer = new Message2Consumer();
		bus.AddHandler(message2Consumer);

		bus.Handle(message1);

		Assert.Same(message1, Pipe.LastMessageProcessed);
		Assert.NotNull(Message2Consumer.LastMessageReceived);
		Assert.Equal(message1.CorrelationId, Message2Consumer.LastMessageReceived!.CorrelationId);
	}

	public class Message4 : IMessage
	{
		public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
	}

	public class Message5 : Message4
	{
	}

	public class MyConsumer : IConsumer<Message2>
	{
		public void Handle(Message2 message)
		{
			// this space intentionally left empty.
		}
	}

	[Fact]
	public void ManuallyComposedTypeWithMultipleTranslatorsHandlesMessageProperly()
	{
		var message4 = new Message4 { CorrelationId = "1234" };

		using var bus = new Bus();
		Message5? message5 = null;
		var message2Consumer = new MyConsumer();
		bus.AddHandler(message2Consumer);
		bus.AddHandler(new ActionConsumer<Message5>(m => message5 = m));
		var pipe = new Pipe();
		bus.AddTranslator(pipe);
		bus.AddTranslator(new ActionPipe<Message4, Message5>(m => new Message5 { CorrelationId = m.CorrelationId }));

		bus.Handle(message4);

		Assert.NotNull(message5);
		Assert.Equal(message4.CorrelationId, message5!.CorrelationId);
		Assert.NotNull(Pipe.LastMessageProcessed);
		Assert.Equal(message4.CorrelationId, message5.CorrelationId);
	}

	[Fact]
	public void ManuallyComposedTypeHandlesWithDuplicateHandlerMulticasts()
	{
		var bus = new Bus();

		var message2Consumer = new Message2Consumer();
		bus.AddHandler(message2Consumer);
		bus.AddHandler(message2Consumer);

		var message1 = new Message2 { CorrelationId = "1234" };
		bus.Handle(message1);
		Assert.Equal(2, message2Consumer.MessageReceivedCount);
	}
}