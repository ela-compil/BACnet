# .NET library for BACnet

[![build](https://github.com/ela-compil/BACnet/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/ela-compil/BACnet/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/vpre/BACnet.svg?label=BACnet)](https://www.nuget.org/packages/BACnet)
[![Downloads](https://img.shields.io/nuget/dt/BACnet.svg?label=downloads)](https://www.nuget.org/packages/BACnet)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/ela-compil/BACnet/blob/master/LICENSE)

![BACnet logo](https://raw.githubusercontent.com/ela-compil/BACnet/master/logo.png)

[BACnet](http://www.bacnet.org/) (ASHRAE 135) is the standard communication protocol for
building-automation systems — HVAC, lighting, access control, and metering. This library is a
standalone BACnet stack for .NET: add a NuGet package and talk to BACnet devices from your own code,
or expose your application as a BACnet device.

## Features

- **Transports** — BACnet/IP (UDP, IPv4 & IPv6) with BBMD and foreign-device registration; BACnet/Ethernet (pcap); MS/TP and PTP over a serial port
- **Client and device (server) roles** — send requests and/or answer them from your own object model
- **Discovery** — Who-Is / I-Am and Who-Has / I-Have, including across routers
- **Data access** — ReadProperty, WriteProperty, ReadPropertyMultiple, WritePropertyMultiple, ReadRange
- **Change of Value** — SubscribeCOV / SubscribeProperty and COV notifications
- **Scheduling** — typed read/write of Schedule and Calendar properties (`Weekly_Schedule`, `Exception_Schedule`, `Date_List`, `Effective_Period`), plus example Schedule/Calendar objects implementing the standard behavior
- **Alarms & events** — event/alarm notifications, alarm summary, and acknowledgement
- **More services** — object create/delete, atomic file read/write, device-communication-control, reinitialize, time synchronization
- **Segmentation** of large requests and responses
- Complete **ASN.1 encode/decode** of the BACnet APDU set, covered by a test suite verified against ASHRAE 135 Annex F

## Packages

- **[BACnet](https://www.nuget.org/packages/BACnet)** [![NuGet](https://img.shields.io/nuget/vpre/BACnet.svg?label=nuget)](https://www.nuget.org/packages/BACnet)  
  Core stack — pure-managed BACnet/IP, MS/TP & PTP protocol, encode/decode. No native dependencies.
- **[BACnet.Ethernet](https://www.nuget.org/packages/BACnet.Ethernet)** [![NuGet](https://img.shields.io/nuget/vpre/BACnet.Ethernet.svg?label=nuget)](https://www.nuget.org/packages/BACnet.Ethernet)  
  pcap-based BACnet/Ethernet (ISO 8802-3) transport (SharpPcap / PacketDotNet).
- **[BACnet.Serial](https://www.nuget.org/packages/BACnet.Serial)** [![NuGet](https://img.shields.io/nuget/vpre/BACnet.Serial.svg?label=nuget)](https://www.nuget.org/packages/BACnet.Serial)  
  Physical serial-port transport (`System.IO.Ports`) for MS/TP and PTP.
- **[BACnet.Logging.CommonLogging](https://www.nuget.org/packages/BACnet.Logging.CommonLogging)** [![NuGet](https://img.shields.io/nuget/vpre/BACnet.Logging.CommonLogging.svg?label=nuget)](https://www.nuget.org/packages/BACnet.Logging.CommonLogging)  
  Optional bridge to route the stack's logs to `Common.Logging`.

## Supported target frameworks

| Package | net48 | netstandard2.0 | net8.0 | net10.0 |
|---------|:-----:|:--------------:|:------:|:-------:|
| BACnet (core) | ✅ | ✅ | ✅ | ✅ |
| BACnet.Ethernet | ✅ | — | ✅ | ✅ |
| BACnet.Serial | ✅ | — | ✅ | ✅ |
| BACnet.Logging.CommonLogging | ✅ | ✅ | ✅ | ✅ |

`netstandard2.0` covers .NET 6/7 and other runtimes.

## Install

```sh
dotnet add package BACnet
# optional native transports:
dotnet add package BACnet.Ethernet
dotnet add package BACnet.Serial
```

## Getting started

A minimal Who-Is device discovery over BACnet/IP:

```csharp
using System.IO.BACnet;

var client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0));

client.OnIam += (sender, adr, deviceId, maxApdu, seg, vendorId)
    => Console.WriteLine($"Found device {deviceId} at {adr}");

client.Start();

client.WhoIs();

Console.ReadLine();
```

> On a host with several network interfaces (e.g. Hyper-V/WSL/Docker virtual adapters), tell the
> transport which one to bind by passing its IP — otherwise it throws an error listing the candidates:
> `new BacnetIpUdpProtocolTransport(0xBAC0, localEndpointIp: "192.168.1.50")`.

The [`Examples/`](https://github.com/ela-compil/BACnet/tree/master/Examples) folder has runnable samples — basic read/write, a device/server,
COV subscription, alarm/event handling, BBMD, a serial device, and more.

## Logging

The stack uses **`Microsoft.Extensions.Logging`**. By default logging is a no-op; wire a factory once
and everything logs through it:

```csharp
using Microsoft.Extensions.Logging;

BacnetLogging.Factory = LoggerFactory.Create(b => b.AddConsole());
```

Console, Serilog, NLog, and log4net all work via their MEL providers. If you already use
`Common.Logging`, add the `BACnet.Logging.CommonLogging` package and call `b.AddCommonLogging()`.

## Upgrading from 3.x to 4.0

4.0 has a few breaking changes — see [`MIGRATION.md`](https://github.com/ela-compil/BACnet/blob/master/MIGRATION.md) for details:

- **Logging** moved from `Common.Logging` to `Microsoft.Extensions.Logging` (the `Log` property is now `ILogger`).
- **Native transports** split into optional packages: pcap → `BACnet.Ethernet`, serial → `BACnet.Serial`.
  The MS/TP and PTP protocols stay in the core; use `SerialTransport.Mstp(...)` / `.Ptp(...)` from `BACnet.Serial`.
- **Interface selection is explicit when the host has multiple network interfaces.** In that case
  `BacnetIpUdpProtocolTransport` no longer picks one for you — `Start()` throws and lists the
  candidates. Pass the interface IP: `new BacnetIpUdpProtocolTransport(0xBAC0, localEndpointIp: "192.168.1.50")`.
- **Renamed** the misspelled enum member `BacnetRejectReason.RECOGNIZED_SERVICE` to `UNRECOGNIZED_SERVICE` (value unchanged).
- **Scheduling types are spec-shaped now**: the old `BACnetCalendarEntry` and `BacnetweekNDay` are replaced by
  `BacnetCalendarEntry` / `BacnetWeekNDay`, and schedule properties decode into typed values
  (`BacnetDailySchedule`, `BacnetSpecialEvent`) instead of opaque nested constructs.

## GitHub Packages

Releases are also published to GitHub Packages. To restore from there, add the source
`https://nuget.pkg.github.com/ela-compil/index.json` (a GitHub PAT with `read:packages` is required).

## Credits & history

The stack was originally developed by **Morten Kvistgaard** — with significant contributions from
**F. Chaxel**, **Steve Karg**, and the [BACnet Stack (in C)](https://sourceforge.net/projects/bacnet/) —
as part of [YABE (Yet Another BACnet Explorer)](https://sourceforge.net/projects/yetanotherbacnetexplorer/).
This repository was **forked from the YABE SourceForge SVN** and is maintained here by
[**Jakub Bartkowiak**](https://github.com/gralin) and the [**Ela-compil**](https://ela.pl) team as an
independent library on NuGet; it is a separate codebase from YABE (YABE keeps its own copy of the stack).

## Trademarks

"BACnet" and the BACnet logo are registered trademarks of [ASHRAE](https://www.ashrae.org/) (the
American Society of Heating, Refrigerating and Air-Conditioning Engineers, Inc.). This project is an
independent, community-maintained implementation of the BACnet protocol; it is not affiliated with,
endorsed by, or sponsored by ASHRAE or BACnet International. The name and logo are used only to
identify the protocol this library implements.
