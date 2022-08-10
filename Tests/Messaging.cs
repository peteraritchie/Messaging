using PRI.Messaging.Patterns;
using PRI.Messaging.Patterns.Extensions.Consumer;
using PRI.Messaging.Primitives;
using PRI.Messaging.Patterns.Extensions.Bus;

namespace PRI.Messaging.Tests;

public class Messaging
{
	private readonly IBus bus;

	private readonly List<IMessage> messages;

	public class TestBus : Bus
	{
		public new IMessage? Handle(IMessage message)
		{
			bool wasProcessed;
			base.Handle(message, out wasProcessed);
			return wasProcessed
				? message
				: null;
		}
	}

	public Messaging()
	{
		bus = new TestBus();
		messages = new List<IMessage>();
		bus.AddHandler(new ActionConsumer<IMessage>(message =>
		{
			messages.Add(message);
		}));
	}

	public class MyEvent : IEvent
	{
		public MyEvent()
		{
			CorrelationId = Guid.NewGuid().ToString("D");
			OccurredDateTime = DateTime.UtcNow;
		}

		public string CorrelationId { get; set; }
		public DateTime OccurredDateTime { get; set; }
	}

	public class MyMessage : IMessage
	{
		public MyMessage()
		{
			CorrelationId = Guid.NewGuid().ToString("D");
		}

		public string CorrelationId { get; set; }
	}

	public class MessageHandler : IConsumer<MyMessage>
	{
		public IMessage? LastMessageReceived;
		public void Handle(MyMessage message)
		{
			LastMessageReceived = message;
		}
	}

	public class EventHandler : IConsumer<MyEvent>
	{
		public IMessage? LastMessageReceived;
		public void Handle(MyEvent message)
		{
			LastMessageReceived = message;
		}
	}

	[Fact]
	public void ConsumerPublishSucceeds()
	{
		var consumer = new EventHandler();

		var myEvent = new MyEvent();
		consumer.Publish(myEvent);
		Assert.Equal(myEvent.CorrelationId, consumer.LastMessageReceived!.CorrelationId);
	}

	[Fact]
	public void ConsumerSendSucceeds()
	{
		var consumer = new MessageHandler();

		var myEvent = new MyMessage();
		consumer.Send(myEvent);
		Assert.Equal(myEvent.CorrelationId, consumer.LastMessageReceived!.CorrelationId);
	}

	[Fact]
	public void SendingMessageSends()
	{
		var myMessage = new MyMessage();
		bus.Send(myMessage);
		Assert.Contains(messages, e => myMessage.CorrelationId.Equals(e.CorrelationId));
	}

	[Fact]
	public void SendingMessageResultsInMessageProcessedEvent()
	{
		var myMessage = new MyMessage();

		var messageProcessed = ((TestBus)bus).Handle(myMessage);
		Assert.NotNull(messageProcessed);
	}

	[Fact]
	public void PublishingEventResultsInMessageProcessedEvent()
	{
		var myEvent = new MyEvent();

		var messageProcessed = ((TestBus)bus).Handle(myEvent);
		Assert.NotNull(messageProcessed);
	}

	[Fact]
	public void SendingMessageResultsInCorrectMessageProcessedEvent()
	{
		var myMessage = new MyMessage();

		var messageProcessed = ((TestBus)bus).Handle(myMessage);
		Assert.Same(myMessage, messageProcessed);
	}

	[Fact]
	public void PublishingEventResultsInCorrectMessageProcessedEvent()
	{
		var myEvent = new MyEvent();

		var messageProcessed = ((TestBus)bus).Handle(myEvent);
		Assert.Same(myEvent, messageProcessed);
	}

	[Fact]
	public void PublishingEventPublishes()
	{
		var myEvent = new MyEvent();
		bus.Publish(myEvent);
		Assert.Contains(messages, e => myEvent.CorrelationId.Equals(e.CorrelationId));
	}
}
