using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace System.IO.BACnet.Tests.Support;

/// <summary>
/// Captures every entry written through it together with the scopes that were active at the time.
/// The test transports deliver frames synchronously, so a plain scope stack is enough.
/// </summary>
internal sealed class RecordingLogger : ILogger
{
    private readonly List<object> _scopes = [];

    public List<(string Message, object[] Scopes)> Entries { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
    {
        _scopes.Add(state);
        return new Scope(this, state);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        Entries.Add((formatter(state, exception), _scopes.ToArray()));
    }

    /// <summary>The single entry whose message starts with <paramref name="prefix"/>.</summary>
    public (string Message, object[] Scopes) Entry(string prefix)
    {
        return Entries.Single(e => e.Message.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>The RemoteAddress property of a scope, the way a structured sink would read it.</summary>
    public static BacnetAddress RemoteAddress(object scope)
    {
        return (scope as IEnumerable<KeyValuePair<string, object>>)
            ?.FirstOrDefault(property => property.Key == "RemoteAddress").Value as BacnetAddress;
    }

    private sealed class Scope(RecordingLogger logger, object state) : IDisposable
    {
        public void Dispose() => logger._scopes.Remove(state);
    }
}
