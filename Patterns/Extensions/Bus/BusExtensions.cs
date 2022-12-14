using System.Diagnostics.CodeAnalysis;

using System;
using System.Linq;
using PRI.Messaging.Primitives;
using PRI.ProductivityExtensions.ReflectionExtensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PRI.Messaging.Patterns.Exceptions;

namespace PRI.Messaging.Patterns.Extensions.Bus
{
	public static class BusExtensions
	{
		private static readonly Dictionary<IBus, Dictionary<string, Delegate>> busResolverDictionaries = new();
#if false
		private static readonly Dictionary<IBus, IServiceProvider> busServiceProviders = new();
#endif

		/// <summary>
		/// Add the ability to resolve an instance of <typeparam name="T"></typeparam>
		/// Without adding a resolver the bus will attempt to find and call the default
		/// constructor to create an instance when adding handlers from assemblies.
		/// Use <see cref="AddResolver{T}"/> if your handler does not have default constructor
		/// or you want to re-use an instance.
		/// </summary>
		/// <example>
		/// <code>
		/// // For type MessageHandler, call the c'tor that takes an IBus parameter
		/// bus.AddResolver(()=> new MessageHandler(bus));
		/// // find all the handlers in assemblies named *handlers.dll" in the current directory in the namespace
		/// "MyCorp.MessageHandlers
		/// bus.AddHandlersAndTranslators(Directory.GetCurrentDirectory(), "*handlers.dll", "MyCorp.MessageHandlers");
		/// </code>
		/// </example>
		/// <typeparam name="T">The type of instance to resolve.  Typically of type IConsumer{TMessage}</typeparam>
		/// <param name="bus">The <seealso cref="IBus"/> instance this will apply to</param>
		/// <param name="resolver">A delegate that returns an instance of <typeparamref name="T"/></param>
		public static void AddResolver<T>(this IBus bus, Func<T> resolver)
		{
			if (!busResolverDictionaries.ContainsKey(bus))
				busResolverDictionaries.Add(bus, new Dictionary<string, Delegate>());
			var dictionary = busResolverDictionaries[bus];
			var key = typeof(T).AssemblyQualifiedName!;
			dictionary[key] = resolver;
		}

#if false
		public static void SetServiceProvider(this IBus bus, IServiceProvider serviceProvider)
		{
			if (bus == null)
				throw new ArgumentNullException(nameof(bus));
			if (serviceProvider == null)
				throw new ArgumentNullException(nameof(serviceProvider));
			if (!busServiceProviders.ContainsKey(bus))
				busServiceProviders.Add(bus, serviceProvider);
			else
				busServiceProviders[bus] = serviceProvider;
		}
#endif
		/// <summary>
		/// A private class to handle Activator creations as a service locator type
		/// </summary>
		private sealed class ActivatorServiceProvider : IServiceProvider
		{
			public object? GetService(Type serviceType)
			{
				return Activator.CreateInstance(serviceType);
			}
		}

		/// <summary>
		/// Dynamically load message handlers by filename in a specific directory.
		/// </summary>
		/// <example>
		/// <code>
		/// // find all the handlers in assemblies named *handlers.dll" in the current directory in the namespace
		/// "MyCorp.MessageHandlers
		/// bus.AddHandlersAndTranslators(Directory.GetCurrentDirectory(), "*handlers.dll", "MyCorp.MessageHandlers");
		/// </code>
		/// </example>
		/// <param name="bus">The <seealso cref="IBus"/> instance this will apply to</param>
		/// <param name="directory">What directory to search</param>
		/// <param name="wildcard">What filenames to search</param>
		/// <param name="namespace">Include IConsumers{TMessage} within this namespace</param>
		public static void AddHandlersAndTranslators(this IBus bus, string directory, string wildcard, string @namespace)
		{
			IEnumerable<Type> consumerTypes = typeof(IConsumer<>)./*GetTypeInfo().*/ByImplementedInterfaceInDirectory(directory, wildcard, @namespace);
			AddHandlersAndTranslators(bus, consumerTypes);
		}

		/// <summary>
		/// Dynamically load message handlers from a collection of types.
		/// </summary>
		/// <param name="bus">The <seealso cref="IBus"/> instance this will apply to</param>
		/// <param name="consumerTypes">Types to test for being a handler and load if so.</param>
		public static void AddHandlersAndTranslators(this IBus bus, IEnumerable<Type> consumerTypes)
		{
			if (!busResolverDictionaries.ContainsKey(bus))
				busResolverDictionaries.Add(bus, new Dictionary<string, Delegate>());
#if false
			IServiceProvider serviceProvider = busServiceProviders.ContainsKey(bus)
				? busServiceProviders[bus]
				: new ActivatorServiceProvider();
#else
			IServiceProvider serviceProvider = new ActivatorServiceProvider();
#endif
			foreach (var consumerType in consumerTypes)
			{
				var consumerTypeInterfaces = consumerType.GetInterfaces();
				var pipeImplementationInterface =
					Array.Find(consumerTypeInterfaces,
						t => t.IsGenericType && !t.IsGenericTypeDefinition &&
						t.GetGenericTypeDefinition() == typeof(IPipe<,>));

				if (pipeImplementationInterface != null)
				{
					var translatorType = consumerType;
					var messageTypes = pipeImplementationInterface.GetGenericTypeArguments();
					var key = translatorType.AssemblyQualifiedName!;
					// get instance of pipe
					var translatorInstance = busResolverDictionaries[bus].ContainsKey(key)
						? InvokeFunc(busResolverDictionaries[bus][key])
						: serviceProvider.GetService(translatorType);
					// get instance of the helper that will help add specific handler
					// code to the bus.
					var helperType1 = typeof(PipeAttachConsumerHelper<,>).MakeGenericType(messageTypes);
					var attachConsumerMethodInfo = helperType1.GetMethod(nameof(PipeAttachConsumerHelper<IMessage, IMessage>.AttachConsumer))!;
					attachConsumerMethodInfo.Invoke(null, new[] { translatorInstance, bus });

					var inType = messageTypes[0];
					var helperType = typeof(BusAddHandlerHelper<>).MakeGenericType(inType);
					var addHandlerMethodInfo = helperType.GetMethod(nameof(BusAddHandlerHelper<IMessage>.AddHandler))!;

					addHandlerMethodInfo.Invoke(null, new[] { bus, translatorInstance });
				}
				else
				{
					var consumerImplementationInterface =
						Array.Find(consumerTypeInterfaces,
							t => t.IsGenericType && !t.IsGenericTypeDefinition &&
							t.GetGenericTypeDefinition() == typeof(IConsumer<>));
					Debug.Assert(consumerImplementationInterface != null,
						"Unexpected state enumerating implementations of IConsumer<T>");

					var messageTypes = consumerImplementationInterface.GetGenericTypeArguments();

					var helperType = typeof(BusAddHandlerHelper<>).MakeGenericType(messageTypes);
					var addHandlerMethodInfo = helperType.GetMethod(nameof(BusAddHandlerHelper<IMessage>.AddHandler))!;
					var key = consumerType.AssemblyQualifiedName!;

					var handlerInstance = busResolverDictionaries[bus].ContainsKey(key)
						? InvokeFunc(busResolverDictionaries[bus][key])
						: serviceProvider.GetService(consumerType);
					addHandlerMethodInfo.Invoke(null, new[] { bus, handlerInstance });
				}
			}
		}

		/// <summary>
		/// Alias to be explicit about the intent of handling an <see cref="IMessage"/> instance.
		/// </summary>
		/// <typeparam name="TMessage">IMessage-based type to send</typeparam>
		/// <param name="bus">bus to send within</param>
		/// <param name="message">Message to send</param>
		public static void Send<TMessage>(this IBus bus, TMessage message) where TMessage : IMessage
		{
			bus.Handle(message);
		}

		/// <summary>
		/// Alias to be explicit about the intent of handling an <see cref="IEvent"/> instance.
		/// </summary>
		/// <typeparam name="TEvent">IEvent-based type to publish</typeparam>
		/// <param name="bus">bus to send within</param>
		/// <param name="event">Event to publish</param>
		public static void Publish<TEvent>(this IBus bus, TEvent @event) where TEvent : IEvent
		{
			bus.Handle(@event);
		}

		/// <summary>
		/// Perform a request/response
		/// </summary>
		/// <typeparam name="TMessage">The type of the message being sent</typeparam>
		/// <typeparam name="TEvent">The type of the event for the response</typeparam>
		/// <param name="bus">The bus to send/receive from</param>
		/// <param name="message">The message to send</param>
		/// <returns>The event response</returns>
		public static Task<TEvent> RequestAsync<TMessage, TEvent>(this IBus bus, TMessage message) where TMessage : IMessage
			where TEvent : IEvent
		{
			return RequestAsync<TMessage, TEvent>(bus, message, CancellationToken.None);
		}

		/// <summary>
		/// Perform a request/response
		/// </summary>
		/// <typeparam name="TMessage">The type of the message being sent</typeparam>
		/// <typeparam name="TEvent">The type of the event for the response</typeparam>
		/// <param name="bus">The bus to send/receive from</param>
		/// <param name="message">The message to send</param>
		/// <param name="cancellationToken">CancellationToken to use to cancel or timeout.</param>
		/// <returns>The event response</returns>
		public static Task<TEvent> RequestAsync<TMessage, TEvent>(this IBus bus, TMessage message, CancellationToken cancellationToken) where TMessage : IMessage
			where TEvent : IEvent
		{
			if (bus == null)
				throw new ArgumentNullException(nameof(bus));
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var tcs = new TaskCompletionSource<TEvent>();
			ActionConsumer<TEvent>? actionConsumer = null;
			object? token = null;
			cancellationToken.Register(() =>
			{
				if (actionConsumer != null)
					bus.RemoveHandler(actionConsumer, token!);
				tcs.TrySetCanceled();
			}, useSynchronizationContext: false);
			actionConsumer = new ActionConsumer<TEvent>(e =>
			{
				try
				{
					if (e.CorrelationId != message.CorrelationId)
						return;
					if (actionConsumer != null)
						bus.RemoveHandler(actionConsumer, token!);
					tcs.SetResult(e);
				}
				catch (Exception ex)
				{
					tcs.SetException(ex);
				}
			});
			token = bus.AddHandler(actionConsumer);
			bus.Send(message);
			return tcs.Task;
		}

		/// <summary>
		/// Perform a request/response
		/// </summary>
		/// <typeparam name="TMessage">The type of the message being sent</typeparam>
		/// <typeparam name="TSuccessEvent">The type of the event for the response</typeparam>
		/// <typeparam name="TErrorEvent">The type of the event in case of error.</typeparam>
		/// <param name="bus">The bus to send/receive from</param>
		/// <param name="message">The message to send</param>
		/// <returns>The event response</returns>
		public static Task<TSuccessEvent> RequestAsync<TMessage, TSuccessEvent, TErrorEvent>(this IBus bus, TMessage message)
			where TMessage : IMessage
			where TSuccessEvent : IEvent
			where TErrorEvent : IEvent
		{
			return RequestAsync<TMessage, TSuccessEvent, TErrorEvent>(bus, message, CancellationToken.None);
		}

		/// <summary>
		/// Perform a request/response
		/// </summary>
		/// <typeparam name="TMessage">The type of the message being sent</typeparam>
		/// <typeparam name="TSuccessEvent">The type of the event for the response</typeparam>
		/// <param name="bus">The bus to send/receive from</param>
		/// <param name="message">The message to send</param>
		/// <param name="cancellationToken">CancellationToken to use to cancel or timeout.</param>
		/// <returns>The event response</returns>
		public static Task<TSuccessEvent> RequestAsync<TMessage, TSuccessEvent, TErrorEvent>(this IBus bus, TMessage message, CancellationToken cancellationToken) where TMessage : IMessage
			where TSuccessEvent : IEvent
			where TErrorEvent : IEvent
		{
			if (bus == null)
				throw new ArgumentNullException(nameof(bus));
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var tcs = new TaskCompletionSource<TSuccessEvent>();
			ActionConsumer<TSuccessEvent>? successActionConsumer = null;
			ActionConsumer<TErrorEvent>? errorActionConsumer = null;
			object? successToken = null;
			object? errorToken = null;
			cancellationToken.Register(() =>
			{
				if (successActionConsumer != null)
					bus.RemoveHandler(successActionConsumer, successToken!);
				if (errorActionConsumer != null)
					bus.RemoveHandler(errorActionConsumer, errorToken!);
				tcs.TrySetCanceled();
			}, useSynchronizationContext: false);

			successActionConsumer = new ActionConsumer<TSuccessEvent>(e =>
			{
				try
				{
					if (e.CorrelationId != message.CorrelationId)
						return;
					if (successActionConsumer != null)
						bus.RemoveHandler(successActionConsumer, successToken!);
					if (errorActionConsumer != null)
					{
						bus.RemoveHandler(errorActionConsumer, errorToken!);
					}
					tcs.SetResult(e);
				}
				catch (Exception ex)
				{
					tcs.SetException(ex);
				}
			});
			successToken = bus.AddHandler(successActionConsumer);
			errorActionConsumer = new ActionConsumer<TErrorEvent>(e =>
			{
				try
				{
					if (e.CorrelationId != message.CorrelationId)
						return;
					if (errorActionConsumer != null)
						bus.RemoveHandler(errorActionConsumer, errorToken!);
					if (successActionConsumer != null)
					{
						bus.RemoveHandler(successActionConsumer, successToken);
					}
					tcs.SetException(new ReceivedErrorEventException<TErrorEvent>(e));
				}
				catch (Exception ex)
				{
					tcs.SetException(ex);
				}
			});
			errorToken = bus.AddHandler(errorActionConsumer);
			bus.Send(message);
			return tcs.Task;
		}

		/// <summary>
		/// Given only a delegate, invoke it via reflection
		/// </summary>
		/// <param name="func">The delegate to invoke</param>
		/// <returns>Whatever <paramref name="func"/> returns</returns>
		private static object? InvokeFunc(Delegate func)
		{
			return func.Method.Invoke(func.Target, null);
		}

		private static class BusAddHandlerHelper<TMessage> where TMessage : IMessage
		{
			[UsedImplicitly]
			public static void AddHandler(IBus bus, IConsumer<TMessage> consumer)
			{
				bus.AddHandler(consumer);
			}
		}

		private static class PipeAttachConsumerHelper<TIn, TOut>
			where TIn : IMessage
			where TOut : IMessage
		{
			[UsedImplicitly]
			public static void AttachConsumer(IPipe<TIn, TOut> pipe, IConsumer<TOut> bus)
			{
				pipe.AttachConsumer(new ActionConsumer<TOut>(bus.Handle));
			}
		}
	}
}

namespace JetBrains.Annotations
{
	/// <summary>
	/// Indicates that the marked symbol is used implicitly (e.g. via reflection, in external library),
	/// so this symbol will not be marked as unused (as well as by other usage inspections).
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	[ExcludeFromCodeCoverage]
	internal sealed class UsedImplicitlyAttribute : Attribute
	{
		public UsedImplicitlyAttribute()
			: this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default)
		{
		}

		public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags)
			: this(useKindFlags, ImplicitUseTargetFlags.Default)
		{
		}

		public UsedImplicitlyAttribute(ImplicitUseTargetFlags targetFlags)
			: this(ImplicitUseKindFlags.Default, targetFlags)
		{
		}

		public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
		{
			UseKindFlags = useKindFlags;
			TargetFlags = targetFlags;
		}

		public ImplicitUseKindFlags UseKindFlags { get; }

		public ImplicitUseTargetFlags TargetFlags { get; }
	}

	/// <summary>
	/// Specify what is considered used implicitly when marked
	/// with <see cref="JetBrains.Annotations.MeansImplicitUseAttribute"/> or <see cref="UsedImplicitlyAttribute"/>.
	/// </summary>
	[Flags]
	internal enum ImplicitUseTargetFlags
	{
		Default = Itself,
		Itself = 1,
		/// <summary>Members of entity marked with attribute are considered used.</summary>
		Members = 2,
		/// <summary>Entity marked with attribute and all its members considered used.</summary>
		WithMembers = Itself | Members
	}

	[Flags]
	internal enum ImplicitUseKindFlags
	{
		/// <summary>Only entity marked with attribute considered used.</summary>
		Access = 1,
		/// <summary>Indicates implicit assignment to a member.</summary>
		Assign = 2,
		/// <summary>
		/// Indicates implicit instantiation of a type with fixed constructor signature.
		/// That means any unused constructor parameters won't be reported as such.
		/// </summary>
		InstantiatedWithFixedConstructorSignature = 4,
		Default = Access | Assign | InstantiatedWithFixedConstructorSignature,
		/// <summary>Indicates implicit instantiation of a type.</summary>
		InstantiatedNoFixedConstructorSignature = 8,
	}
}
