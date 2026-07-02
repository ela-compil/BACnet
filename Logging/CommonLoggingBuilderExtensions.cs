// Placed in the Microsoft.Extensions.Logging namespace so AddCommonLogging() is discoverable
// wherever an ILoggingBuilder is configured, alongside AddConsole(), AddDebug(), etc.
namespace Microsoft.Extensions.Logging;

using System.IO.BACnet.Logging;

/// <summary>Extension methods for routing Microsoft.Extensions.Logging output to Common.Logging.</summary>
public static class CommonLoggingBuilderExtensions
{
    /// <summary>
    /// Routes all log output through Common.Logging (<c>LogManager</c>), preserving the application's
    /// existing Common.Logging configuration and sinks.
    /// </summary>
    public static ILoggingBuilder AddCommonLogging(this ILoggingBuilder builder)
    {
        builder.AddProvider(new CommonLoggingLoggerProvider());
        return builder;
    }
}
