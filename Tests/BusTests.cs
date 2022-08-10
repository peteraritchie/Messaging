
using System.Diagnostics;

using PRI.Messaging.Patterns;
using PRI.Messaging.Patterns.Extensions.Bus;
using PRI.Messaging.Primitives;

using Tests.Mocks;

namespace PRI.Messaging.Tests;

public class BusTests
{
	[Fact]
	public void BusConsumesMessagesCorrectly()
	{
		Message1? receivedMessage;
		Message1 message1;
		using (var bus = new Bus())
		{
			receivedMessage = null;
			bus.AddHandler(new ActionConsumer<Message1>(m => receivedMessage = m));

			message1 = new Message1 { CorrelationId = "1234" };
			bus.Handle(message1);
		}

		Assert.Same(message1, receivedMessage);
		Assert.NotNull(receivedMessage);
		Assert.Equal(message1.CorrelationId, receivedMessage!.CorrelationId);
	}

	[Fact]
	public void EnsureInterfaceHandlerIsInvoked()
	{
		var bus = new Bus();
		Message1? receivedMessage = null;
		bus.AddHandler(new ActionConsumer<Message1>(m => receivedMessage = m));
		string? text = null;
		bus.AddHandler(new ActionConsumer<IEvent>(_ => text = "ding"));

		var message1 = new Message1 { CorrelationId = "1234" };
		bus.Handle(message1);
		bus.Handle(new TheEvent());
		Assert.Same(message1, receivedMessage);
		Assert.NotNull(receivedMessage);
		Assert.Equal(message1.CorrelationId, receivedMessage!.CorrelationId);
		Assert.Equal("ding", text);
	}

	public class TheEvent : IEvent
	{
		public TheEvent()
		{
			CorrelationId = Guid.NewGuid().ToString("D");
			OccurredDateTime = DateTime.UtcNow;
		}

		public string CorrelationId { get; set; }
		public DateTime OccurredDateTime { get; set; }
	}

	[Fact]
	public void EnsureWithMultipleMessageTypesInterfaceHandlerIsInvoked()
	{
		var bus = new Bus();
		Message1? receivedMessage1 = null;
		bus.AddHandler(new ActionConsumer<Message1>(m => receivedMessage1 = m));
		Message2? receivedMessage2 = null;
		bus.AddHandler(new ActionConsumer<Message2>(m => receivedMessage2 = m));
		string? text = null;
		bus.AddHandler(new ActionConsumer<IEvent>(_ => text = "ding"));

		var message1 = new Message1 { CorrelationId = "1234" };
		bus.Handle(message1);
		bus.Handle(new TheEvent());
		Assert.Same(message1, receivedMessage1);
		Assert.NotNull(receivedMessage1);
		Assert.Equal(message1.CorrelationId, receivedMessage1!.CorrelationId);
		Assert.Equal("ding", text);
	}
	[Fact]
	public void BaseTypeHandlerIsCalledCorrectly()
	{
		var bus = new Bus();
		Message1? receivedMessage1 = null;
		bus.AddHandler(new ActionConsumer<Message1>(m => receivedMessage1 = m));
		var message1 = new Message1Specialization { CorrelationId = "1234" };
		bus.Handle(message1);
		Assert.Same(message1, receivedMessage1);
	}
	public class Message1Specialization : Message1
	{
	}

	[Fact]
	public void BaseBaseTypeHandlerIsCalledCorrectly()
	{
		var bus = new Bus();
		Message1? receivedMessage1 = null;
		bus.AddHandler(new ActionConsumer<Message1>(m => receivedMessage1 = m));
		var message1 = new Message1SpecializationSpecialization() { CorrelationId = "1234" };
		bus.Handle(message1);
		Assert.NotNull(receivedMessage1);
		Assert.Same(message1, receivedMessage1);
	}

	public class Message1SpecializationSpecialization : Message1Specialization
	{
	}

	[Fact]
	public void RemoveLastSubscribedHandlerDoesNotThrow()
	{
		var exception = Record.Exception(() =>
		{
			var bus = new Bus();
			Message1? receivedMessage1 = null;
			var token = bus.AddHandler(new ActionConsumer<Message1>(m => receivedMessage1 = m));
			bus.Handle(new Message1());
			bus.RemoveHandler(new ActionConsumer<Message1>(m => receivedMessage1 = m), token);
		});
		Assert.Null(exception);
	}

	[Fact]
	public void RemoveLastSubscribedHandlerClearsInternalDictionaries()
	{
		var bus = new Bus();
		Message1? receivedMessage1 = null;
		var token = bus.AddHandler(new ActionConsumer<Message1>(m => receivedMessage1 = m));
		bus.Handle(new Message1());
		bus.RemoveHandler(new ActionConsumer<Message1>(m => receivedMessage1 = m), token);
		Assert.Empty(bus._consumerInvokers);
		Assert.Empty(bus._consumerInvokersDictionaries);
	}

	[Fact]
	public void InterleavedRemoveHandlerRemovesCorrectHandler()
	{
		string? ordinal = null;
		var bus = new Bus();
		var actionConsumer1 = new ActionConsumer<Message1>(_ => ordinal = "1");
		var token1 = bus.AddHandler(actionConsumer1);
		var actionConsumer2 = new ActionConsumer<Message1>(_ => ordinal = "2");
#pragma warning disable S1481 // Unused local variables should be removed
		var token2 = bus.AddHandler(actionConsumer2);
#pragma warning restore S1481 // Unused local variables should be removed
		bus.Handle(new Message1());
		bus.RemoveHandler(actionConsumer1, token1);
		bus.Send(new Message1());
		Assert.Equal("2", ordinal);
	}

	[Fact]
	public void RemoveHandlerWithNullTokenThrows()
	{
		var bus = new Bus();
		Assert.Throws<ArgumentNullException>(() => bus.RemoveHandler(new ActionConsumer<Message1>(_ => { }), null!));
	}

	[Fact]
	public void RemoveHandlerWithTokenThatIsNotTokenTypeThrows()
	{
		var bus = new Bus();
		Assert.Throws<InvalidOperationException>(() => bus.RemoveHandler(new ActionConsumer<Message1>(_ => { }), new object()));
	}

	[Fact]
	public void RemoveHandlerTwiceSucceeds()
	{
		var exception = Record.Exception(() =>
		{
			var bus = new Bus();
			var actionConsumer1 = new ActionConsumer<Message1>(_ => { });
			var token1 = bus.AddHandler(actionConsumer1);
			bus.Send(new Message1());
			bus.RemoveHandler(actionConsumer1, token1);
			bus.RemoveHandler(actionConsumer1, token1);
		});
		Assert.Null(exception);
	}

	public abstract class Message : IMessage
	{
		protected Message(string c)
		{
			CorrelationId = c;
		}

		public string CorrelationId { get; set; }
		public abstract Task<IEvent> RequestAsync(IBus bus);
	}

	public abstract class MessageBase<TMessage, TEvent> : Message
		where TEvent : IEvent where TMessage : IMessage
	{
		protected MessageBase() : base(Guid.NewGuid().ToString("D"))
		{
		}

		public override async Task<IEvent> RequestAsync(IBus bus)
		{
			IEvent result = await bus.RequestAsync<MessageBase<TMessage, TEvent>, TEvent>(this);
			return result;
		}
	}

	public abstract class EventBase : IEvent
	{
		protected EventBase(string correlationId)
		{
			CorrelationId = correlationId;
			OccurredDateTime = DateTime.UtcNow;
		}

		public string CorrelationId { get; set; }
		public DateTime OccurredDateTime { get; set; }
	}
	public class Command1 : MessageBase<Command1, Event1> { }
	public class Command2 : MessageBase<Command2, Event2> { }
	public class Command3 : MessageBase<Command3, Event3> { }
	public class Command4 : MessageBase<Command4, Event4> { }
	public class Command5 : MessageBase<Command5, Event5> { }
	public class Command6 : MessageBase<Command6, Event6> { }
	public class Command7 : MessageBase<Command7, Event7> { }
	public class Command8 : MessageBase<Command8, Event8> { }
	public class Event1 : EventBase { public Event1(string c) : base(c) { } }
	public class Event2 : EventBase { public Event2(string c) : base(c) { } }
	public class Event3 : EventBase { public Event3(string c) : base(c) { } }
	public class Event4 : EventBase { public Event4(string c) : base(c) { } }
	public class Event5 : EventBase { public Event5(string c) : base(c) { } }
	public class Event6 : EventBase { public Event6(string c) : base(c) { } }
	public class Event7 : EventBase { public Event7(string c) : base(c) { } }
	public class Event8 : EventBase { public Event8(string c) : base(c) { } }

	public class TracingBus : Bus
	{
		public override void Handle(IMessage message)
		{
			var @event = message as IEvent;
			if (@event != null)
			{
				LogEvent(@event);
			}
			bool wasProcessed;
			var stopwatch = Stopwatch.StartNew();
			var processingStartTime = DateTime.Now;
			try
			{
				Handle(message, out wasProcessed);
				var r = new Task<int>(() => 1);
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
			if (@event == null)
			{
				var timeSpan = stopwatch.Elapsed;
				LogOperationDuration(message.GetType(), processingStartTime, timeSpan, message.CorrelationId, wasProcessed);
			}
			else if (!wasProcessed)
			{
				throw new InvalidOperationException($"{@message.CorrelationId} of type {message.GetType().Name} was not processed.");
			}
		}

		public List<string> Log { get; } = new();

		private void LogOperationDuration(Type getType, DateTime processingStartTime, TimeSpan timeSpan, string messageCorrelationId, bool wasMessageProcessed)
		{
			lock (Log)
			{
				Log.Add($"{getType} {processingStartTime} {timeSpan} {messageCorrelationId} {wasMessageProcessed}");
			}
		}

		private void LogException(Exception exception)
		{
			lock (Log)
			{
				Log.Add(exception.Message);
			}
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private void LogEvent(IEvent @event)
		{
			lock (Log)
			{
				Log.Add($"Event {@event.GetType().Name} occurred {@event.CorrelationId}");
			}
		}
	}
	public class Result<T>
	{
		private readonly Func<T> get;
		public T Value => get();

		private Result(Func<T> get)
		{
			this.get = get;
		}

		public static Result<T> Success(T value)
		{
			return new Result<T>(() => value);
		}
		public static Result<T> Failure(Exception exception)
		{
			return new Result<T>(() => throw exception);
		}

		public static Result<T> Uninitialized { get; } = Result<T>.Failure(new InvalidOperationException("Uninitialized"));
	}
}