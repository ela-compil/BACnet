namespace System.IO.BACnet;

/// <summary>
/// Global entry point for wiring Microsoft.Extensions.Logging into the BACnet stack.
/// <para>
/// The stack is instantiated directly (not through a DI container), so it reads its logger factory
/// from here. Assign <see cref="Factory"/> once at start-up — e.g.
/// <c>BacnetLogging.Factory = LoggerFactory.Create(b =&gt; b.AddConsole());</c> — and every
/// <see cref="BacnetClient"/>, transport and BVLC layer created afterwards logs through it.
/// Individual instances can still override their <c>Log</c> property directly.
/// </para>
/// <para>Defaults to <see cref="NullLoggerFactory"/>, so logging is a no-op until configured.</para>
/// </summary>
public static class BacnetLogging
{
    /// <summary>The factory used to create default loggers for newly-created stack objects.</summary>
    public static ILoggerFactory Factory { get; set; } = NullLoggerFactory.Instance;

    /// <summary>Creates a logger categorised by <typeparamref name="T"/> from <see cref="Factory"/>.</summary>
    public static ILogger CreateLogger<T>() => Factory.CreateLogger(typeof(T).FullName ?? typeof(T).Name);

    /// <summary>Creates a logger categorised by <paramref name="type"/> from <see cref="Factory"/>.</summary>
    public static ILogger CreateLogger(Type type) => Factory.CreateLogger(type.FullName ?? type.Name);
}
