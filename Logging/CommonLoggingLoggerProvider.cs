namespace System.IO.BACnet.Logging;

/// <summary>
/// A Microsoft.Extensions.Logging <see cref="ILoggerProvider"/> that routes output to Common.Logging,
/// so applications with existing Common.Logging sinks keep working after upgrading to the MEL-based
/// BACnet 4.0. Wire it with <see cref="CommonLoggingBuilderExtensions.AddCommonLogging"/>.
/// </summary>
public sealed class CommonLoggingLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new CommonLoggingLogger(CommonLogging.LogManager.GetLogger(categoryName));

    public void Dispose() { }
}
