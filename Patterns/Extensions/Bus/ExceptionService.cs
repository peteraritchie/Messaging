using System.Runtime.CompilerServices;

namespace PRI.Messaging.Patterns.Extensions.Bus
{
	internal static class ExceptionService
	{
		public static InvalidOperationException InvalidOperationExceptionFromMethod(string message, [CallerMemberName] string name = "")
		{
			return new InvalidOperationException($"{message} from method {name}");
		}
	}
}
