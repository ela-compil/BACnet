namespace System.IO.BACnet.Logging;

/// <summary>
/// An <see cref="ILogger"/> that forwards every entry to a Common.Logging <c>ILog</c>, mapping
/// Microsoft.Extensions.Logging levels onto Common.Logging's.
/// </summary>
internal sealed class CommonLoggingLogger : ILogger
{
    private readonly CommonLogging.ILog _log;

    public CommonLoggingLogger(CommonLogging.ILog log)
    {
        _log = log;
    }

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => _log.IsTraceEnabled,
        LogLevel.Debug => _log.IsDebugEnabled,
        LogLevel.Information => _log.IsInfoEnabled,
        LogLevel.Warning => _log.IsWarnEnabled,
        LogLevel.Error => _log.IsErrorEnabled,
        LogLevel.Critical => _log.IsFatalEnabled,
        _ => false // LogLevel.None
    };

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (formatter == null || !IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
            return;

        switch (logLevel)
        {
            case LogLevel.Trace: _log.Trace(message, exception); break;
            case LogLevel.Debug: _log.Debug(message, exception); break;
            case LogLevel.Information: _log.Info(message, exception); break;
            case LogLevel.Warning: _log.Warn(message, exception); break;
            case LogLevel.Error: _log.Error(message, exception); break;
            case LogLevel.Critical: _log.Fatal(message, exception); break;
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
