# BACnet examples

Sample applications demonstrating the BACnet library. They build as their own solution
(`BACnet.Examples.slnx`) and are compiled in CI (on Windows) so they cannot drift out of date.

## Provenance & license

These examples are adapted from **YABE (Yet Another BACnet Explorer)** and the
`ela-compil/BACnet.Examples` repository, originally written by **Morten Kvistgaard** and
**Frédéric Chaxel**. They are licensed under the **MIT License**, the same as this repository;
each source file keeps its original copyright header. The projects have been migrated to
SDK-style, retargeted to modern .NET, and updated to the 4.0 API.

## The examples

| Example | Kind | Notes |
|---------|------|-------|
| `ObjectBrowseSample` | console | Minimal Who-Is / device discovery |
| `BasicReadWrite` | console | Read and write a property |
| `BasicServer` | console | A minimal BACnet device/server |
| `BasicServerTstLowEnd` | console | A trimmed low-end server |
| `BasicAdviseCOV` | console | Subscribe to Change-Of-Value notifications |
| `BasicAlarmListener` | console | Receive & acknowledge alarms/events |
| `DemoBBMD` | console | BBMD / foreign-device registration |
| `MultipleDevices` | console | Several virtual devices in one process |
| `AnotherStorageImplementation` | console | Full server object model (BaCSharp) |
| `RaspberrySample` | console | Raspberry Pi GPIO via sysfs |
| `RaspberryNetCore` | console | Raspberry Pi GPIO on modern .NET |
| `Wheather2_to_Bacnet` | console (Windows) | Bridges a weather API to BACnet; reads config from the registry |
| `Bacnet.Room.Simulator` | WinForms (Windows) | A room simulator with a GUI |
| `BacnetToDatabase` | WinForms (Windows) | Stores read values into a SQLite database |

Most examples target `net8.0`; the two WinForms apps and `Wheather2_to_Bacnet` target
`net8.0-windows` and build on Windows only.
