using PRI.ProductivityExtensions.ActionExtensions;
using System.Runtime.CompilerServices;
using PRI.Messaging.Patterns.Exceptions;
using PRI.Messaging.Primitives;
// second element is used for debugging:
using TokenType = System.Tuple<System.Guid, System.Action<PRI.Messaging.Primitives.IMessage>>;
using System.Diagnostics.CodeAnalysis;
using InvokerDictionariesDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<System.Guid, System.Action<PRI.Messaging.Primitives.IMessage>>>;

[assembly: InternalsVisibleTo("Tests")]
[assembly: InternalsVisibleTo("Tests-Core")]

namespace PRI.Messaging.Patterns
{
	/// <summary>
	/// An implementation of a composable bus https://en.wikipedia.org/wiki/Bus_(computing) that transfers messages between zero or more producers
	/// and zero or more consumers, decoupling producers from consumers.
	/// <example>
	/// Compose a bus from any/all consumers located in current directory in the Rock.QL.Endeavor.MessageHandlers namespace with a filename that matches Rock.QL.Endeavor.*Handler*.dll
	/// <code>
	/// var bus = new Bus();
	/// var directory = Path.GetDirectoryName(GetType().Assembly.Location);
	/// bus.AddHandlersAndTranslators(directory, "Rock.QL.Endeavor.*Handler*.dll", "Rock.QL.Endeavor.MessageHandlers");
	/// </code>
	/// Manually compose a bus and send it a message
	/// <code>
	/// var bus = new Bus();
	///
	/// var message2Consumer = new MessageConsumer();
	/// bus.AddHandler(message2Consumer);
	///
	/// bus.Handle(new Message());
	/// </code>
	/// </example>
	/// </summary>
	public class Bus : IBus, IDisposable
	{
#pragma warning disable IDE1006 // Naming Styles
		internal readonly Dictionary<string, Action<IMessage>> _consumerInvokers = new();
		internal readonly InvokerDictionariesDictionary _consumerInvokersDictionaries = new();
#pragma warning restore IDE1006 // Naming Styles

		private readonly ReaderWriterLockSlim _readerWriterConsumersLock = new();
		private readonly ReaderWriterLockSlim _readerWriterAsyncConsumersLock = new();

		protected virtual void Handle(IMessage message, out bool wasProcessed)
		{
			var isEvent = message is IEvent;
			var messageType = message.GetType();

			wasProcessed = false;
			if (TryGetConsumer(messageType.AssemblyQualifiedName!, out var consumerInvoker))
			{
				consumerInvoker!(message);
				wasProcessed = true;
				if (!isEvent)
					return;
			}

			// check base type hierarchy.
			messageType = messageType.BaseType;
			while (messageType != typeof(object))
			{
				if (TryGetConsumer(messageType!.AssemblyQualifiedName!, out consumerInvoker))
				{
					consumerInvoker!(message);
					wasProcessed = true;
					if (!isEvent)
						return;
				}
				messageType = messageType.BaseType;
			}

			// check any implemented interfaces
			messageType = message.GetType();
			foreach (var interfaceType in messageType.FindInterfaces((_, _) => true, null))
			{
				if (!TryGetConsumer(interfaceType.AssemblyQualifiedName!, out consumerInvoker))
					continue;
				consumerInvoker!(message);
				wasProcessed = true;

				if (!isEvent)
					return;
			}
		}

		private bool TryGetConsumer(string invokerKey, [MaybeNullWhen(false)] out Action<IMessage>? consumerInvoker)
		{
			_readerWriterConsumersLock.EnterReadLock();
			try
			{
				return _consumerInvokers.TryGetValue(invokerKey, out consumerInvoker);
			}
			finally
			{
				_readerWriterConsumersLock.ExitReadLock();
			}
		}

		public virtual void Handle(IMessage message)
		{
#pragma warning disable IDE0059 // Unnecessary assignment of a value
			Handle(message, out bool wasProcessed);
#pragma warning restore IDE0059 // Unnecessary assignment of a value
		}

		public void AddTranslator<TIn, TOut>(IPipe<TIn, TOut> pipe) where TIn : IMessage where TOut : IMessage
		{
			pipe.AttachConsumer(new ActionConsumer<TOut>(m => this.Handle(m)));

			var typeGuid = typeof(TIn).AssemblyQualifiedName!;
			// never gets removed, inline guid
			var delegateGuid = Guid.NewGuid();
			_readerWriterConsumersLock.EnterWriteLock();
			try
			{
				if (_consumerInvokers.ContainsKey(typeGuid))
				{
					_consumerInvokersDictionaries[typeGuid][delegateGuid] = InlineHandler;
					_consumerInvokers[typeGuid] =
						_consumerInvokersDictionaries[typeGuid].Values.Sum();
				}
				else
				{
					_consumerInvokersDictionaries.Add(typeGuid,
						new Dictionary<Guid, Action<IMessage>> { { delegateGuid, InlineHandler } });
					_consumerInvokers.Add(typeGuid, InlineHandler);
				}
			}
			finally
			{
				_readerWriterConsumersLock.ExitWriteLock();
			}

			void InlineHandler(IMessage o)
			{
				pipe.Handle((TIn)o);
			}
		}

		private static Action<IMessage> CreateConsumerDelegate<TIn>(IConsumer<TIn> consumer) where TIn : IMessage
		{
			return o => consumer.Handle((TIn)o);
		}

		public object AddHandler<TIn>(IConsumer<TIn> consumer) where TIn : IMessage
		{
			Action<IMessage> handler = CreateConsumerDelegate(consumer);
			var typeGuid = typeof(TIn).AssemblyQualifiedName!;
			var delegateGuid = Guid.NewGuid();
			_readerWriterConsumersLock.EnterWriteLock();

			try
			{
				if (_consumerInvokers.ContainsKey(typeGuid))
				{
					_consumerInvokersDictionaries[typeGuid][delegateGuid] = handler;
					_consumerInvokers[typeGuid] = _consumerInvokersDictionaries[typeGuid].Values.Sum();
				}
				else
				{
					_consumerInvokersDictionaries.Add(typeGuid,
						new Dictionary<Guid, Action<IMessage>> { { delegateGuid, handler } });
					_consumerInvokers.Add(typeGuid, handler);
				}
			}
			finally
			{
				_readerWriterConsumersLock.ExitWriteLock();
			}
			return new TokenType(delegateGuid, handler);
		}

#pragma warning disable S927 // Parameter names should match base declaration and other partial definitions
		public void RemoveHandler<TIn>(IConsumer<TIn> consumer, object tag) where TIn : IMessage
#pragma warning restore S927 // Parameter names should match base declaration and other partial definitions
		{
			if (tag == null)
				throw new ArgumentNullException(nameof(tag));
			if (tag is not TokenType tokenType)
				throw new InvalidOperationException();
			var typeGuid = typeof(TIn).AssemblyQualifiedName!;
			_readerWriterConsumersLock.EnterUpgradeableReadLock();
			try
			{
				var hasConsumerType = _consumerInvokers.ContainsKey(typeGuid);
				if (!hasConsumerType)
					return;
				_readerWriterConsumersLock.EnterWriteLock();
				try
				{
					_consumerInvokersDictionaries[typeGuid].Remove(tokenType.Item1);
					if (_consumerInvokersDictionaries[typeGuid].Count > 0) // any more left for that type?
					{
						_consumerInvokers[typeGuid] = _consumerInvokersDictionaries[typeGuid].Values.Sum();
					}
					else
					{
						_consumerInvokers.Remove(typeGuid); // if no more, get rid of invoker
						_consumerInvokersDictionaries.Remove(typeGuid);
					}
				}
				finally
				{
					_readerWriterConsumersLock.ExitWriteLock();
				}
			}
			finally
			{
				_readerWriterConsumersLock.ExitUpgradeableReadLock();
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			_readerWriterConsumersLock.Dispose();
			_readerWriterAsyncConsumersLock.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}