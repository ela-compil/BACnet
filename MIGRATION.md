# Upgrading to BACnet 4.0

This guide covers the breaking changes in 4.0 and how to adapt your code.

## Logging: `Common.Logging` → `Microsoft.Extensions.Logging`

The library no longer depends on the unmaintained `Common.Logging`. It now uses the standard
**`Microsoft.Extensions.Logging`** abstractions, so BACnet logs flow through the same pipeline as
the rest of a modern .NET application.

The public `Log` property on `BacnetClient`, `BVLC`, `BVLCV6` and every transport changed type:

```diff
- public Common.Logging.ILog Log { get; set; }
+ public Microsoft.Extensions.Logging.ILogger Log { get; set; }
```

By default logging is a **no-op** (`NullLogger`), so if you never configured logging there is nothing
to do. To route BACnet logs somewhere, pick one of the following.

### Option 1 — configure once (recommended)

Assign an `ILoggerFactory` to `BacnetLogging.Factory` at start-up. Every `BacnetClient`, transport and
BVLC layer created afterwards logs through it:

```csharp
using Microsoft.Extensions.Logging;
using System.IO.BACnet;

BacnetLogging.Factory = LoggerFactory.Create(builder => builder.AddConsole());

var client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0));
client.Start(); // logs now go to the console
```

### Option 2 — per instance

Assign any `ILogger` directly. Only the *type* you pass changes versus 3.x:

```csharp
var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var client = new BacnetClient(transport) { Log = loggerFactory.CreateLogger<BacnetClient>() };
```

### Provider examples

`Microsoft.Extensions.Logging` has providers for every common sink. Wire them into the factory:

```csharp
// Serilog        (package: Serilog.Extensions.Logging + a Serilog sink)
BacnetLogging.Factory = LoggerFactory.Create(b => b.AddSerilog(Log.Logger));

// NLog           (package: NLog.Extensions.Logging)
BacnetLogging.Factory = LoggerFactory.Create(b => b.AddNLog());

// log4net        (package: Microsoft.Extensions.Logging.Log4Net.AspNetCore — works in any app)
BacnetLogging.Factory = LoggerFactory.Create(b => b.AddLog4Net("log4net.config"));
```

### Keeping your existing Common.Logging setup

If your application already uses `Common.Logging`, add the optional **`BACnet.Logging.CommonLogging`**
package. It routes BACnet's `Microsoft.Extensions.Logging` output back through `Common.Logging`, so your
existing configuration and sinks keep working with one line:

```csharp
using Microsoft.Extensions.Logging;

BacnetLogging.Factory = LoggerFactory.Create(b => b.AddCommonLogging());
```

## Native transports are now separate packages

The pcap and serial transports moved out of the core package so the core is pure-managed with no
native dependencies. Add the package you need:

| Transport | Package | Notes |
|-----------|---------|-------|
| Ethernet (pcap) | **`BACnet.Ethernet`** | `BacnetEthernetProtocolTransport` (now `public`); needs libpcap/Npcap. |
| Serial / MS-TP / PTP | **`BACnet.Serial`** | `BacnetSerialPortTransport` (the `System.IO.Ports` implementation). |

The `(portName, baudRate, …)` convenience constructors on `BacnetMstpProtocolTransport` and
`BacnetPtpProtocolTransport` moved to factory helpers in `BACnet.Serial`:

```diff
- var mstp = new BacnetMstpProtocolTransport("COM1", 38400);
+ var mstp = SerialTransport.Mstp("COM1", 38400);          // from the BACnet.Serial package
  // or pass your own IBacnetSerialTransport to the (unchanged) core constructor:
  var mstp2 = new BacnetMstpProtocolTransport(new BacnetSerialPortTransport("COM1", 38400));
```

The core still contains the MS/TP and PTP protocol logic and the `IBacnetSerialTransport` abstraction;
only the physical `SerialPort` implementation moved.

## Network interface must be explicit when a host has multiple interfaces

In 3.x, `BacnetIpUdpProtocolTransport` silently picked a network interface for you when the machine
had more than one. That guess was frequently wrong — binding the BACnet/IP broadcast to a virtual
adapter (Hyper-V, WSL, Docker, a VPN) instead of the real LAN — and the failure was easy to miss.

4.0 stops guessing. When several candidate IPv4 interfaces are present and you have not said which to
use, `Start()` now throws an `InvalidOperationException` listing the candidates rather than binding to
the wrong one. Hosts with a single interface are unaffected.

Select the interface by passing its local IP to the constructor:

```diff
- var transport = new BacnetIpUdpProtocolTransport(0xBAC0);
+ var transport = new BacnetIpUdpProtocolTransport(0xBAC0, localEndpointIp: "192.168.1.50");
```
