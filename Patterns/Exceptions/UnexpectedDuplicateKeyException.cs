namespace PRI.Messaging.Patterns.Exceptions;

[Serializable]
public class UnexpectedDuplicateKeyException : Exception
{
	public UnexpectedDuplicateKeyException(ArgumentException argumentException, string key, IEnumerable<string> keys, string context = "<unknown>")
		: base($"{key} already found in {string.Concat("", keys)} ({context}) ", argumentException)
	{
	}

	[Obsolete("The UnexpectedDuplicateKeyException ctor UnexpectedDuplicateKeyException() is not supported.")]
	public UnexpectedDuplicateKeyException() : base()
	{
	}

	[Obsolete("The UnexpectedDuplicateKeyException ctor UnexpectedDuplicateKeyException(string? message) is not supported.")]
	public UnexpectedDuplicateKeyException(string? message) : base(message)
	{
	}

	[Obsolete("The UnexpectedDuplicateKeyException ctor UnexpectedDuplicateKeyException(string? message, Exception? innerExcption) is not supported.")]
	public UnexpectedDuplicateKeyException(string? message, Exception? innerException) : base(message, innerException)
	{
	}

	protected UnexpectedDuplicateKeyException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
		:base(serializationInfo, streamingContext)
	{
	}
}
