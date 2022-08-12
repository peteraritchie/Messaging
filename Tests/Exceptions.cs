using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using PRI.Messaging.Patterns.Exceptions;

using static PRI.Messaging.Tests.Messaging;

namespace PRI.Messaging.Tests;

public class Exceptions
{
	[Fact]
	public void ReceivedErrorEventExceptionShouldSerialize()
	{
		Serialize<ReceivedErrorEventException<MyEvent>>();
	}

	[Fact]
	public void ReceivedErrorEventExceptionShouldConstructWithMessage()
	{
		Construct<ReceivedErrorEventException<MyEvent>>("messageText");
	}

	[Fact]
	public void ReceivedErrorEventExceptionShouldConstructWithMessageAndBaseException()
	{
		Construct<ReceivedErrorEventException<MyEvent>>("messageText", new Exception("message"));
	}

	[Fact]
	public void UnexpectedDuplicateKeyExceptionShouldSerialize()
	{
		Serialize<UnexpectedDuplicateKeyException>();
	}

	[Fact]
	public void UnexpectedDuplicateKeyExceptionShouldConstructWithMessage()
	{
		Construct<UnexpectedDuplicateKeyException>("messageText");
	}

	[Fact]
	public void UnexpectedDuplicateKeyExceptionShouldConstructWithMessageAndBaseException()
	{
		Construct<UnexpectedDuplicateKeyException>("messageText", new Exception("message"));
	}

	[Fact]
	public void UnexpectedDuplicateKeyExceptionShouldConstructWithArgumentException()
	{
		var ex = Record.Exception(()=>
			new UnexpectedDuplicateKeyException(new ArgumentException("key"), "key", new[] {"key", "key1"})
		);
		Assert.Null(ex);
	}

	private void Construct<T>(string messageText) where T : Exception
	{
		var ex = Activator.CreateInstance(typeof(T), messageText) as T;
		Assert.NotNull(ex);
	}
	private void Construct<T>(string messageText, Exception baseException) where T : Exception
	{
		var ex = Activator.CreateInstance(typeof(T), messageText, baseException) as T;
		Assert.NotNull(ex);
	}
	private void Construct<T>(ArgumentException @object) where T : Exception
	{
		var ex = Activator.CreateInstance(typeof(T), @object) as T;
		Assert.NotNull(ex);
	}

#pragma warning disable SYSLIB0011 // BinaryFormatter serialization is obsolete
	private void Serialize<T>() where T : Exception, new()
	{
		var ex = new T();
		using var stream = new MemoryStream();
		try
		{
			var formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.File));
			formatter.Serialize(stream, ex);
			stream.Position = 0;
			var deserializedException = (T)formatter.Deserialize(stream);
			throw deserializedException;
		}
		catch (SerializationException)
		{
			Assert.Fail("Unable to serialize/deserialize the exception");
		}
		catch (T)
		{
			// expected
		}
		finally
		{
			stream.Close();
		}
	}
#pragma warning restore SYSLIB0011 // BinaryFormatter serialization is obsolete
}