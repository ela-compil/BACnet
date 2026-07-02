# .NET library for BACnet

[![build](https://github.com/ela-compil/BACnet/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/ela-compil/BACnet/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/BACnet.svg?label=BACnet)](https://www.nuget.org/packages/BACnet)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A standalone BACnet protocol stack for .NET.

The stack was originally developed by **Morten Kvistgaard** — with significant contributions from
**F. Chaxel**, **Steve Karg**, and the [BACnet Stack (in C)](https://sourceforge.net/projects/bacnet/) —
as part of [YABE (Yet Another BACnet Explorer)](https://sourceforge.net/projects/yetanotherbacnetexplorer/).
This repository was **forked from the YABE SourceForge SVN** and is maintained here as an independent
library on NuGet; it is a separate codebase from YABE (YABE keeps its own copy of the stack).

## Packages

| Package | Description |
|---------|-------------|
| [![NuGet](https://img.shields.io/nuget/v/BACnet.svg?label=BACnet)](https://www.nuget.org/packages/BACnet) | Core stack — pure-managed BACnet/IP, MS/TP & PTP protocol, encode/decode. No native dependencies. |
| [![NuGet](https://img.shields.io/nuget/v/BACnet.Ethernet.svg?label=BACnet.Ethernet)](https://www.nuget.org/packages/BACnet.Ethernet) | pcap-based BACnet/Ethernet (ISO 8802-3) transport (SharpPcap / PacketDotNet). |
| [![NuGet](https://img.shields.io/nuget/v/BACnet.Serial.svg?label=BACnet.Serial)](https://www.nuget.org/packages/BACnet.Serial) | Physical serial-port transport (`System.IO.Ports`) for MS/TP and PTP. |
| [![NuGet](https://img.shields.io/nuget/v/BACnet.Logging.CommonLogging.svg?label=BACnet.Logging.CommonLogging)](https://www.nuget.org/packages/BACnet.Logging.CommonLogging) | Optional bridge to route the stack's logs to `Common.Logging`. |

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

A minimal Who-Is / read a property over BACnet/IP:

```csharp
using System.IO.BACnet;

var client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0));
client.OnIam += (sender, adr, deviceId, maxApdu, seg, vendorId)
    => Console.WriteLine($"Found device {deviceId} at {adr}");
client.Start();
client.WhoIs();
```

The [`Examples/`](Examples/) folder has runnable samples — basic read/write, a device/server,
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

4.0 has a few breaking changes — see [`MIGRATION.md`](MIGRATION.md) for details:

- **Logging** moved from `Common.Logging` to `Microsoft.Extensions.Logging` (the `Log` property is now `ILogger`).
- **Native transports** split into optional packages: pcap → `BACnet.Ethernet`, serial → `BACnet.Serial`.
  The MS/TP and PTP protocols stay in the core; use `SerialTransport.Mstp(...)` / `.Ptp(...)` from `BACnet.Serial`.

## GitHub Packages

Releases are also published to GitHub Packages. To restore from there, add the source
`https://nuget.pkg.github.com/ela-compil/index.json` (a GitHub PAT with `read:packages` is required).

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md). Licensed under the [MIT License](LICENSE).
