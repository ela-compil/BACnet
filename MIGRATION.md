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

### Publishing `BACnet.Serial` for Linux / Raspberry Pi

On the modern targets (`net8.0`/`net10.0`), `BACnet.Serial` pulls the `System.IO.Ports` package, which
ships a platform-specific native library alongside its managed assembly. When you publish for a
non-Windows target — a Raspberry Pi being the common case — that native library is only deployed if the
publish knows which platform it targets. Publish with an explicit Runtime Identifier so it is included:

```sh
dotnet publish --runtime linux-arm64   # 64-bit Raspberry Pi OS
dotnet publish --runtime linux-arm     # 32-bit Raspberry Pi OS
```

Without a Runtime Identifier (most often with `--self-contained` or single-file publishes) the native
part is missing and opening a port throws at runtime.

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

## Scheduling types are spec-shaped now

4.0 adds first-class serialization for the Schedule and Calendar objects (Weekly_Schedule,
Exception_Schedule, Date_List), which replaces two old types that did not match the standard:

- **`BACnetCalendarEntry` → `BacnetCalendarEntry`.** The old struct held a `List<object> Entries`
  bag of every entry in a Date_List. The new class models the standard's CHOICE: exactly one of
  `Date`, `DateRange` or `WeekNDay` is set, and one instance represents one entry. Reading a
  Calendar's `PROP_DATE_LIST` now returns **one `BacnetValue` per entry** (it used to be a single
  value holding the whole list), each with `Tag = BACNET_APPLICATION_TAG_CALENDAR_ENTRY` and a
  `BacnetCalendarEntry` in `Value`. The same list can be passed straight back to
  `WritePropertyRequest`.

- **`BacnetweekNDay` → `BacnetWeekNDay`.** The raw `month`/`week`/`wday` bytes became the
  `BacnetMonthOptions`, `BacnetWeekOfMonthOptions` and `BacnetDayOfWeekOptions` enums, and
  `IsAFittingDate` now implements the week-of-month octet (values 1-9) the old type ignored.
  The byte-based constructor `(day, month, week)` keeps its shape.

- **`BacnetDeviceObjectPropertyReference.deviceIndentifier` → `deviceIdentifier`.** The misspelled
  public field and the constructor parameter of the same name lost the typo; behavior is unchanged.

- **`BacnetDate.toDateTime()` → `ToDateTime()`**, which now returns the `new DateTime(1, 1, 1)`
  wildcard sentinel for unrepresentable dates instead of `DateTime.Now`, and understands day 32
  ("last day of the month"). The periodic day specials 33/34 ("odd/even days") yield the sentinel
  and are evaluated by `IsAFittingDate`.

- Values decoded from `PROP_WEEKLY_SCHEDULE` and `PROP_EXCEPTION_SCHEDULE` now carry the dedicated
  application tags (`..._WEEKLY_SCHEDULE`, `..._SPECIAL_EVENT`) and typed objects
  (`BacnetDailySchedule`, `BacnetSpecialEvent`) instead of `..._CONTEXT_SPECIFIC_DECODED` with a
  nested `BacnetValue[]` tree. Code that pattern-matched the old opaque shape must switch to the
  typed objects — reading and writing schedules no longer needs any manual re-assembly.

## The unspecified ("wildcard") TIME has a dedicated marker

Earlier versions decoded a fully-wildcarded Time (`FF FF FF FF` — "any time", e.g. the timestamp
of an event transition that never happened) to `DateTime(1,1,1)`, which is also exactly what a
decoded **midnight** looks like. The two were indistinguishable, and writing such a value back
turned a real 00:00:00.00 into the wildcard.

4.0 separates them: a fully-wildcarded Time decodes to **`ASN1.BACNET_TIME_WILDCARD`**
(`DateTime.MaxValue`) and only that marker encodes back to `FF FF FF FF`; `DateTime(1,1,1)` is
plain midnight in both directions. Update any code that compared a decoded time against
`DateTime.MinValue` to detect "unspecified":

```diff
- if (timestamp.Time == DateTime.MinValue)
+ if (timestamp.Time == ASN1.BACNET_TIME_WILDCARD)
```

Combined date+time values (`BACnetDateTime`, datetime timestamps, log-record stamps) cannot carry
an unspecified time and keep degrading it to 00:00 on decode, as before.

**Partially**-wildcarded values are kept per-octet instead of being clamped: a decoded TIME whose
octets cannot be represented faithfully as a `DateTime` (e.g. `11:22:**.**`) comes back as the new
`BacnetTime` struct, and an unrepresentable DATE (patterns like "last day of every odd month", or
invalid calendar dates) comes back as the existing `BacnetDate` struct — both under their usual
`..._TAG_TIME`/`..._TAG_DATE` application tags, and both re-encode byte-identically. Code that
unconditionally cast such values to `DateTime` must handle the struct forms; fully-specified
values are unaffected.

`PROP_EFFECTIVE_PERIOD` now decodes as **one `BacnetValue` holding a `BacnetDateRange`** (tag
`..._TAG_DATERANGE`) instead of two `DateTime` values, so open boundaries survive round-trips.
Writes accept both the typed range and the legacy two-date shape.

`PROP_EVENT_TIME_STAMPS` elements decode as **`BacnetGenericTime`** (preserving which
BACnetTimeStamp choice the device used) instead of a bare `DateTime`; sequence-number stamps still
come back as an unsigned. Timestamps whose time octets are partially wildcarded expose the
original octets via the new `BacnetGenericTime.PartialTime` while `Time` keeps the familiar
best-effort clamped value, and a wildcarded Date+Time property pair merges into the new
`BacnetDateTime` struct instead of a clamped `DateTime`.
