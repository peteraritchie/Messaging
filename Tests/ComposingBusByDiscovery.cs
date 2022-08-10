using PRI.Messaging.Patterns;
using PRI.Messaging.Patterns.Extensions.Bus;

using Tests.Mocks;

namespace PRI.Messaging.Tests;

public class ComposingBusByDiscovery
{
	[Fact]
	public void TranslatorResolverIsInvoked()
	{
		var bus = new Bus();
		var calledCount = 0;
		bus.AddResolver(() =>
		{
			calledCount++;
			return new Pipe();
		});
		var directory = Path.GetDirectoryName(new Uri(GetType().Assembly.Location).LocalPath);
		bus.AddHandlersAndTranslators(directory!, "Tests*.dll", "Tests.Mocks");

		Assert.Equal(1, calledCount);
	}

	[Fact]
	public void HandlerResolverIsInvoked()
	{
		var bus = new Bus();
		var calledCount = 0;
		bus.AddResolver(() =>
		{
			calledCount++;
			return new Message2Consumer();
		});
		var directory = Path.GetDirectoryName(new Uri(GetType().Assembly.Location).LocalPath);
		bus.AddHandlersAndTranslators(directory!, "Tests*.dll", "Tests.Mocks");

		Assert.Equal(1, calledCount);
	}

	[Fact]
	public void CanFindConsumers()
	{
		var bus = new Bus();
		var directory = Path.GetDirectoryName(new Uri(GetType().Assembly.Location).LocalPath);
		bus.AddHandlersAndTranslators(directory!, "Tests*.dll", "Tests.Mocks");

		Assert.Equal(2, bus._consumerInvokers.Count);
	}

	private readonly string _classNamespace;

	public ComposingBusByDiscovery()
	{
		_classNamespace = GetType().Namespace!;
	}

	[Fact]
	public void CorrectConsumersFound()
	{
		var bus = new Bus();
		var directory = Path.GetDirectoryName(new Uri(GetType().Assembly.Location).LocalPath);
		bus.AddHandlersAndTranslators(directory!, "Tests*.dll", _classNamespace);

		Assert.True(bus._consumerInvokers.ContainsKey(typeof(Message2).AssemblyQualifiedName!));
		Assert.False(bus._consumerInvokers.ContainsKey(typeof(Message1).AssemblyQualifiedName!)); // was True
	}

	[Fact]
	public void DiscoveredBusConsumesMessageCorrectly()
	{
		var bus = new Bus();
		var directory = Path.GetDirectoryName(typeof(Pipe).Assembly.Location);
		Assert.NotNull(typeof(Pipe).Namespace);
		bus.AddHandlersAndTranslators(directory!, Path.GetFileName(typeof(Pipe).Assembly.Location), typeof(Pipe).Namespace!);

		var message1 = new Message1 { CorrelationId = "1234" };
		bus.Handle(message1);

		Assert.Same(message1, Pipe.LastGlobalMessageProcessed);
		Assert.NotNull(Message2Consumer.LastMessageReceived);
		Assert.Equal(message1.CorrelationId, Message2Consumer.LastMessageReceived!.CorrelationId);
	}
}
