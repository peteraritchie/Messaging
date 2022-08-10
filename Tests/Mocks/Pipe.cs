using System;
using PRI.Messaging.Primitives;

namespace Tests.Mocks
{
	public class Pipe : IPipe<Message1, Message2>
	{
		private Action<Message2>? _consumer;
		internal static IMessage? LastGlobalMessageProcessed;
		internal IMessage? LastMessageProcessed;

		public void Handle(Message1 message)
		{
			if (_consumer == null)
			{
				throw new InvalidOperationException($"{nameof(_consumer)} was unexpectedly null in {this.GetType().FullName}");
			}

			LastMessageProcessed = message;
			LastGlobalMessageProcessed = message;
			_consumer(new Message2 {CorrelationId = message.CorrelationId});
		}

		public void AttachConsumer(IConsumer<Message2> consumer)
		{
			AttachConsumer(consumer.Handle);
		}

		public void AttachConsumer(Action<Message2> consumer)
		{
			_consumer = consumer;
		}
	}
}