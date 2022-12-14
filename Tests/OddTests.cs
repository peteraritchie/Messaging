using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;

using PRI.ProductivityExtensions.TemporalExtensions;

using Xunit;
using System.Runtime.CompilerServices;

namespace PRI.Messaging.Tests;

public class OddTests
{
	private readonly int zero = 0;
	private int _hash;

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void M(int x)
	{
		// for testing purposes
	}

	//[Fact(Explicit = true)]
	public void MeasureDelegatePerformance()
	{

		int n = 50000000;
		Action<int> d = M;
		M(1);
		d(1);
		var stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < n; ++i)
		{
			d(i);
		}
		Console.WriteLine(stopwatch.Elapsed.ToEnglishString());
		stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < n; ++i)
		{
			M(i);
		}
		Console.WriteLine(stopwatch.Elapsed.ToEnglishString());
	}

	//[Fact(Explicit = true)]
	public void MeasureDictionary()
	{
		var an = new AssemblyName("temp");
		AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
		ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
		var dict1 = new Dictionary<string, Action>();
		var dict2 = new Dictionary<Guid, Action>();
		var dict3 = new Dictionary<Type, Action>();
		int iterations = 100000 / 4;
		var keys1 = new string[iterations];
		var keys2 = new Guid[iterations];
		var keys3 = new Type[iterations];

		for (var i = 0; i < iterations; ++i)
		{
			keys1[i] = i.ToString();
			keys2[i] = Guid.NewGuid();
			TypeBuilder tb = moduleBuilder.DefineType($"Class{i}",
					TypeAttributes.Public |
					TypeAttributes.Class |
					TypeAttributes.AutoClass |
					TypeAttributes.AnsiClass |
					TypeAttributes.BeforeFieldInit |
					TypeAttributes.AutoLayout,
					null);
			Assert.NotNull(tb);
			keys3[i] = tb.CreateType()!;
		}
		Stopwatch stopwatch;
		stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < iterations; ++i)
		{
			_hash = keys1[i].GetHashCode();
		}
		Console.WriteLine($"string hash {stopwatch.Elapsed.ToEnglishString()} {iterations} iterations");
		stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < iterations; ++i)
		{
			_hash = keys2[i].GetHashCode();
		}
		Console.WriteLine($"Guid hash {stopwatch.Elapsed.ToEnglishString()} {iterations} iterations");
		stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < iterations; ++i)
		{
			_hash = keys3[i].GetHashCode();
		}
		Console.WriteLine($"Type hash {stopwatch.Elapsed.ToEnglishString()} {iterations} iterations");

		stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < iterations; ++i)
		{
			dict1.Add(keys1[i], () => { });
		}
		Console.WriteLine($"string key {stopwatch.Elapsed.ToEnglishString()} {iterations} iterations");
		stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < iterations; ++i)
		{
			dict2.Add(keys2[i], () => { });
		}
		Console.WriteLine($"Guid key {stopwatch.Elapsed.ToEnglishString()} {iterations} iterations");
		stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < iterations; ++i)
		{
			dict3.Add(keys3[i], () => { });
		}
		Console.WriteLine($"Type key {stopwatch.Elapsed.ToEnglishString()} {iterations} iterations");
	}

	//[Fact(Explicit = true)]
	public void MeasureMoreDelegatePerformances()
	{
		int n = 50000000;
		Action<int>[] d = { M };
		Action<int> a = M;
		M(1);
		d[zero](1);
		a(1);
		Action<int> e = i =>
		{
			for (int l = 0; l < d.Length; ++l)
				d[l](i);
		};
		var stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < n; ++i)
		{
			e(i);
		}
		Console.WriteLine(stopwatch.Elapsed.ToEnglishString());
		stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < n; ++i)
		{
			a(i);
		}
		Console.WriteLine(stopwatch.Elapsed.ToEnglishString());
		stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < n; ++i)
		{
			M(i);
		}
		Console.WriteLine(stopwatch.Elapsed.ToEnglishString());
	}
}
