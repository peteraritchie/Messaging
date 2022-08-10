using PRI.Messaging.Primitives;

namespace PRI.Messaging.Patterns.Exceptions;

[Serializable]
public sealed class ReceivedErrorEventException<TEvent> : Exception
	where TEvent : IEvent
{
	public TEvent? ErrorEvent { get; }

	public ReceivedErrorEventException(TEvent errorEvent)
	{
		ErrorEvent = errorEvent;
	}

	private ReceivedErrorEventException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
		:base(serializationInfo, streamingContext)
	{
	}

	public ReceivedErrorEventException() : base()
	{
	}

	public ReceivedErrorEventException(string? message) : base(message)
	{
	}

	public ReceivedErrorEventException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}