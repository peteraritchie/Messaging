using System.Diagnostics;
using PRI.Messaging.Primitives;
using PRI.ProductivityExtensions.ReflectionExtensions;

namespace PRI.Messaging.Tests;

public class TypeFinderTests
{
#if false
	private static string plainType = @"namespace ClassLibrary
{
	public class PlainClass
	{
		public int GetNumber()
		{
			return 42;
		}
	}
}";
	private static string typeWithAttribute = @"namespace ClassLibrary
{
	[System.Obsolete]
	public class ClassWithAttribute
	{
		public int GetNumber()
		{
			return 42;
		}
	}
}";

	private static string typeImplementingInterface = @"namespace ClassLibrary
{
	public class InterfaceImplementor : System.ICloneable
	{
		public object Clone()
		{
			return null;
		}
	}
}";
	private static string typeImplementingGenericInterface = @"namespace ClassLibrary
{
	public class GenericInterfaceImplementor : System.IEquatable<int>
		{
			public bool Equals(int other)
			{
				return false;
			}
		}
}";
	private static string translatorType = @"namespace ClassLibrary
{
	public class Message1 : PRI.Messaging.Primitives.IMessage
	{
		public string CorrelationId { get; set; }
	}

	public class Message2 : PRI.Messaging.Primitives.IMessage
	{
		public string CorrelationId { get; set; }
	}

	public class GenericInterfaceImplementor : PRI.Messaging.Primitives.IPipe<Message1, Message2>
	{
		private PRI.Messaging.Primitives.IConsumer<Message2> _consumer;

		public bool Equals(int other)
		{
			return false;
		}

		public void Handle(Message1 message)
		{
			var c = _consumer;
			if (c != null) c.Handle(new Message2());
		}

		public void AttachConsumer(PRI.Messaging.Primitives.IConsumer<Message2> consumer)
		{
			_consumer = consumer;
		}
	}
}";

	private static void GenerateAssembly(string sourceCode, string assemblyName)
	{
		var parameters = new CompilerParameters(new[] { typeof(IMessage).Assembly.Location })
		{
			GenerateExecutable = false,
			OutputAssembly = assemblyName,
		};

		using (var provider = CodeDomProvider.CreateProvider("CSharp"))
		{
			var results = provider.CompileAssemblyFromSource(parameters, sourceCode);
			if (results.Errors.HasErrors)
			{
				foreach (var error in results.Errors)
				{
					Trace.WriteLine(error);
				}
			}
		}
	}

	[Fact]
	public void FindNoTypeByAttribute()
	{
		var assemblyName = "test1.dll";
		GenerateAssembly(plainType, assemblyName);
		FileAssert.Exists(assemblyName);
		Assert.False(TypeFinder.ByAttributeInDirectory<ObsoleteAttribute>(".", assemblyName).Any());
	}

	[Fact]
	public void FindTypeByAttribute()
	{
		var assemblyName = "test2.dll";
		GenerateAssembly(typeWithAttribute, assemblyName);
		FileAssert.Exists(assemblyName);
		var types = TypeFinder.ByAttributeInDirectory<ObsoleteAttribute>(".", assemblyName);
		Assert.True(types.Any());
	}

	// IComparable<T>
	// IEquatable<T>
	[Fact]
	public void FindNoByImplementedInterace()
	{
		var assemblyName = "test3.dll";
		GenerateAssembly(plainType, assemblyName);
		FileAssert.Exists(assemblyName);
		Assert.False(TypeFinder.ByImplementedInterfaceInDirectory<ICloneable>(".", assemblyName).Any());
	}

	[Fact]
	public void FindTypeByImplementedInterface()
	{
		var assemblyName = "test4.dll";
		GenerateAssembly(typeImplementingInterface, assemblyName);
		FileAssert.Exists(assemblyName);
		var types = TypeFinder.ByImplementedInterfaceInDirectory<ICloneable>(".", assemblyName);
		Assert.True(types.Any());
	}

	[Fact]
	public void FindTypeByImplementedInterface2()
	{
		var assemblyName = "test5.dll";
		GenerateAssembly(typeImplementingInterface, assemblyName);
		FileAssert.Exists(assemblyName);
		var types = typeof(ICloneable).ByImplementedInterfaceInDirectory(".", assemblyName);
		Assert.True(types.Any());
	}

	// todo: check types that implement two interfaces
	[Fact]
	public void FindTypeByImplementedGenericInterface()
	{
		var assemblyName = "test6.dll";
		GenerateAssembly(typeImplementingGenericInterface, assemblyName);
		FileAssert.Exists(assemblyName);
		var types = typeof(IEquatable<>).ByImplementedInterfaceInDirectory(".", assemblyName);
		Assert.True(types.Any());
	}

	[Fact]
	public void FindAllTranslators()
	{
		var assemblyName = "FindAllTranslators.dll";
		GenerateAssembly(translatorType, assemblyName);
		var types = typeof(IPipe<,>).ByImplementedInterfaceInDirectory(".", assemblyName);
		Assert.True(types.Any());
		foreach (var type in types)
		{
			//var ins = type.
		}
	}
#endif
}