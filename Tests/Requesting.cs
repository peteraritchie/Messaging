using PRI.Messaging.Patterns;
using PRI.Messaging.Patterns.Exceptions;
using PRI.Messaging.Patterns.Extensions.Bus;
using PRI.Messaging.Primitives;
using Tests.Mocks;
using Moq;
using System;

namespace PRI.Messaging.Tests;

public class Requesting
{
	[Fact]
	public async Task RequestSucceeds()
	{
		IBus bus = new Bus();
		_ = bus.AddHandler(new ActionConsumer<Message1>(m => bus.Publish(new TheEvent { CorrelationId = m.CorrelationId })));
		var response = await bus.RequestAsync<Message1, TheEvent>(new Message1 { CorrelationId = "12344321" });
		Assert.Equal("12344321", response.CorrelationId);
	}

	[Fact]
	public async Task RequestWithExceptionDuringEventThrows()
	{
		IBus bus = new Bus();
		var mockEvent = new Mock<IEvent>();
		_ = mockEvent.SetupGet(m => m.CorrelationId).Throws<InvalidOperationException>();
		_ = bus.AddHandler(new ActionConsumer<Message1>(_ => bus.Publish(mockEvent.Object)));
		_ = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
		{
			_ = await bus.RequestAsync<Message1, IEvent>(new Message1 { CorrelationId = "12344321" });
		});
	}

	[Fact]
	public async Task RequestWithErrorWithExceptionDuringSuccessEventThrows()
	{
		IBus bus = new Bus();
		var mockEvent = new Mock<IEvent>();
		_ = mockEvent.SetupGet(m => m.CorrelationId).Throws<InvalidOperationException>();
		_ = bus.AddHandler(new ActionConsumer<Message1>(_ => bus.Publish(mockEvent.Object)));
		_ = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
		{
			_ = await bus.RequestAsync<Message1, IEvent, TheErrorEvent>(new Message1 { CorrelationId = "12344321" });
		});
	}

	[Fact]
	public async Task RequestWithErrorWithExceptionDuringErrorEventThrows()
	{
		IBus bus = new Bus();
		var mockEvent = new Mock<IEvent>();
		_ = mockEvent.SetupGet(m => m.CorrelationId).Throws<InvalidOperationException>();
		_ = bus.AddHandler(new ActionConsumer<Message1>(_ => bus.Publish(mockEvent.Object)));
		_ = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
		{
			_ = await bus.RequestAsync<Message1, TheEvent, IEvent>(new Message1 { CorrelationId = "12344321" });
		});
	}

	[Fact]
	public async Task RequestWithDifferentCorrelationIdDoesNotSucceed()
	{
		IBus bus = new Bus();
		_ = bus.AddHandler(new ActionConsumer<Message1>(m =>
		{
			if (m is null)
			{
				throw new ArgumentNullException(nameof(m));
			}

			bus.Publish(new TheEvent { CorrelationId = "0" });
		}));
		var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		_ = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			_ = await bus.RequestAsync<Message1, TheEvent>(
				new Message1 { CorrelationId = "12344321" },
				cancellationTokenSource.Token);
		});
	}

	[Fact]
	public async Task RequestWithErrorEventWithErrorEventDifferentCorrelationIdDoesNotSucceed()
	{
		IBus bus = new Bus();
		_ = bus.AddHandler(new ActionConsumer<Message1>(_ => bus.Publish(new TheErrorEvent { CorrelationId = "0" })));
		var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		_ = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			_ = await bus.RequestAsync<Message1, TheEvent, TheErrorEvent>(
				new Message1 { CorrelationId = "12344321" },
				cancellationTokenSource.Token);
		});
	}

	[Fact]
	public async Task RequestWithErrorEventWithSuccessEventWithDifferentCorrelationIdDoesNotSucceed()
	{
		IBus bus = new Bus();
		_ = bus.AddHandler(new ActionConsumer<Message1>(_ => bus.Publish(new TheEvent { CorrelationId = "0" })));
		var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		_ = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			_ = await bus.RequestAsync<Message1, TheEvent, TheErrorEvent>(
				new Message1 { CorrelationId = "12344321" },
				cancellationTokenSource.Token);
		});
	}

	[Fact]
	public async Task RequestWithErrorEventThrowsAsync()
	{
		IBus bus = new Bus();
		_ = bus.AddHandler(new ActionConsumer<Message1>(m =>
		{
			bus.Publish(new TheErrorEvent { CorrelationId = m.CorrelationId });
		}));

		var exception = await Assert.ThrowsAsync<ReceivedErrorEventException<TheErrorEvent>>(async () =>
		{
			_ = await bus.RequestAsync<Message1, TheEvent, TheErrorEvent>(
				new Message1 { CorrelationId = "12344321" });
		}
			);
		Assert.NotNull(exception.ErrorEvent);
		Assert.Equal("12344321", exception.ErrorEvent.CorrelationId);
	}

	[Fact]
	public async Task RequestWithErrorEventWithSuccessEventSucceedsWithCorrectEvent()
	{
		IBus bus = new Bus();
		_ = bus.AddHandler(new ActionConsumer<Message1>(m =>
		{
			bus.Publish(new TheEvent { CorrelationId = m.CorrelationId });
		}));

		var @event = await bus.RequestAsync<Message1, TheEvent, TheErrorEvent>(
			new Message1 { CorrelationId = "12344321" });

		_ = Assert.IsType<TheEvent>(@event);
	}

	[Fact]
	public async Task RequestWithErrorEventThrowsWithExceptionWithEventAsync()
	{
		IBus bus = new Bus();
		_ = bus.AddHandler(new ActionConsumer<Message1>(m =>
		{
			bus.Publish(new TheErrorEvent { CorrelationId = m.CorrelationId });
		}));

		var exception = await Assert.ThrowsAsync<ReceivedErrorEventException<TheErrorEvent>>(async () =>
		{
			_ = await bus.RequestAsync<Message1, TheEvent, TheErrorEvent>(
				new Message1 { CorrelationId = "12344321" });
		}
			);
		Assert.NotNull(exception.ErrorEvent);
	}

	[Fact]
	public async Task RequestWithErrorEventThrowsWithExceptionWithCorrectEventAsync()
	{
		const string correlationId = "12344321";
		IBus bus = new Bus();
		_ = bus.AddHandler(new ActionConsumer<Message1>(m =>
		{
			bus.Publish(new TheErrorEvent { CorrelationId = m.CorrelationId });
		}));

		var exception = await Assert.ThrowsAsync<ReceivedErrorEventException<TheErrorEvent>>(async () =>
		{
			_ = await bus.RequestAsync<Message1, TheEvent, TheErrorEvent>(
				new Message1 { CorrelationId = correlationId });
		}
			);
		Assert.Equal(correlationId, exception.ErrorEvent!.CorrelationId);
	}

	[Fact]
	public async Task RequestWithErrorEventTimesOutAsync()
	{
		IBus bus = new Bus();
		_ = bus.AddHandler(new ActionConsumer<Message1>(_ =>
		{
			/* do nothing */
		}));

		var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
		_ = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			_ = await bus.RequestAsync<Message1, TheEvent, TheErrorEvent>(
				new Message1 { CorrelationId = "12344321" },
				cancellationTokenSource.Token);
		}
			);
	}
	[Fact]
	public void MessageAfterRequestCancellationDoesNotThrow()
	{
		var exception = Record.Exception(() =>
		{
			IBus bus = new Bus();
			_ = bus.AddHandler(new ActionConsumer<Message1>(_ =>
			{
				/* do nothing */
			}));

			var cancellationTokenSource = new CancellationTokenSource();
			_ = bus.RequestAsync<Message1, TheEvent>(
				new Message1 { CorrelationId = "12344321" },
				cancellationTokenSource.Token);
			cancellationTokenSource.Cancel();
			bus.Handle(new TheEvent { CorrelationId = "12344321" });
		});

		Assert.Null(exception);
	}

	[Fact]
	public void MessageAfterRequestWithErrorEventCancellationDoesNotThrow()
	{
		var exception = Record.Exception(() =>
		{
			IBus bus = new Bus();
			_ = bus.AddHandler(new ActionConsumer<Message1>(_ =>
			{
				/* do nothing */
			}));

			var cancellationTokenSource = new CancellationTokenSource();
			_ = bus.RequestAsync<Message1, TheEvent, TheErrorEvent>(
				new Message1 { CorrelationId = "12344321" },
				cancellationTokenSource.Token);
			cancellationTokenSource.Cancel();
			bus.Handle(new TheEvent { CorrelationId = "12344321" });
		});

		Assert.Null(exception);
	}

	[Fact]
	public async Task RequestTimesOutAsync()
	{
		IBus bus = new Bus();
		_ = bus.AddHandler(new ActionConsumer<Message1>(_ =>
		{
			/* do nothing */
		}));

		var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
		_ = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			_ = await bus.RequestAsync<Message1, TheEvent>(
				new Message1 { CorrelationId = "12344321" },
				cancellationTokenSource.Token);
		}
			);
	}

	[Fact]
	public async Task RequestWithNullBusThrowsAsync()
	{
		IBus? bus = null;
		var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
		{
			// ReSharper disable once ExpressionIsAlwaysNull
			_ = await bus!.RequestAsync<Message1, TheEvent>(
				new Message1 { CorrelationId = "12344321" });
		}
			);
		Assert.Equal("bus", exception.ParamName);
	}

	[Fact]
	public async Task ErrorEventRequestWithNullBusThrowsAsync()
	{
		IBus? bus = null;
		var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
		{
			// ReSharper disable once ExpressionIsAlwaysNull
			_ = await bus!.RequestAsync<Message1, TheEvent, TheErrorEvent>(
				new Message1 { CorrelationId = "12344321" });
		}
			);
		Assert.Equal("bus", exception.ParamName);
	}

	[Fact]
	public async Task ErrorEventRequestWithNullMessageThrowsAsync()
	{
		IBus bus = new Bus();
		Message1? message1 = null;
		var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			_ = await bus.RequestAsync<Message1, TheEvent, TheErrorEvent>(message1!)
			);
		Assert.Equal("message", exception.ParamName);
	}

	[Fact]
	public async Task RequestWithNullMessageThrowsAsync()
	{
		IBus bus = new Bus();
		Message1? message1 = null;
		var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
		{
			_ = await bus.RequestAsync<Message1, TheEvent>(message1!);
		}
			);

		Assert.Equal("message", exception.ParamName);
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

	public class TheErrorEvent : IEvent
	{
		public TheErrorEvent()
		{
			CorrelationId = Guid.NewGuid().ToString("D");
			OccurredDateTime = DateTime.UtcNow;
		}

		public string CorrelationId { get; set; }
		public DateTime OccurredDateTime { get; set; }
	}
}
