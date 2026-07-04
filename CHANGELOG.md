# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/), and the project follows tag-driven
[MinVer](https://github.com/adamralph/minver) versioning.

## 4.0.0

The 4.0 release modernises the build, packaging and dependencies of the library.
See [MIGRATION.md](MIGRATION.md) for upgrade guidance.

### Breaking
- **Logging**: replaced the unmaintained `Common.Logging` with
  `Microsoft.Extensions.Logging`. The public `Log` property is now `ILogger`; configure a factory via
  `BacnetLogging.Factory`. An optional `BACnet.Logging.CommonLogging` bridge preserves existing sinks.
- **Native transports** moved out of the core into optional packages: the pcap-based Ethernet
  transport → **`BACnet.Ethernet`**, the physical serial port → **`BACnet.Serial`**. The MS/TP and PTP
  protocols remain in the core; wire a serial port with `SerialTransport.Mstp(...)` / `.Ptp(...)`.

### Added
- Multi-targeting: `net48;netstandard2.0;net8.0;net10.0`.
- New packages: `BACnet.Ethernet`, `BACnet.Serial`, `BACnet.Logging.CommonLogging`.
- Central Package Management, MinVer tag-driven versioning, Source Link + symbol (`.snupkg`) packages.
- `BacnetIpUdpProtocolTransport` accepts a `maxApdu` constructor parameter to lower the advertised and
  sent APDU size (e.g. `MAX_APDU1024` keeps every frame, including segmented responses, under a
  1500-byte MTU instead of relying on IP fragmentation).
- CI: multi-OS build matrix; tag-triggered publish to both nuget.org (Trusted Publishing) and GitHub Packages.
- Runnable sample projects moved in-repo under `Examples/` and built in CI.
- ASHRAE 135 Annex F golden-vector encode/decode tests.

### Changed
- The core package is now pure-managed with no native dependencies (closes the pcap → log4net chain, #112).
- SharpPcap bumped 4.x → 6.x in `BACnet.Ethernet` (drops the vulnerable transitive log4net).

### Fixed
- Segmented responses no longer crash with an `IndexOutOfRangeException` when the APDU limit exceeds
  the transport payload buffer (the default `maxPayload: 1472` vs the 1476-byte B/IP APDU): the segment
  encoder clamps its window to the physical buffer and `EncodeBuffer` reports `NotEnoughBuffer` instead
  of writing out of bounds.
- Segmented responses now respect the requester's max-APDU-length-accepted (ASHRAE 135 §5.2.1.2). The
  value is captured automatically when `GetSegmentBuffer` is called inside a request event handler, or
  can be passed explicitly via the new `GetSegmentBuffer(maxSegments, requesterMaxAdpu)` overload.
- `Iam()` can now answer a Who-Is that arrived through a BACnet router: when the receiver address
  carries a `RoutedSource`, the I-Am is NPDU-addressed to the original source network and sent back via
  the router, so the requester receives it as ASHRAE 135 §16.10.4 requires (previously such replies
  were mis-addressed and never reached the requester).
- Segmented ComplexACKs no longer set the reserved `SERVER` bit in the PDU type octet
  (ASHRAE 135 §20.1.5 reserves bits 1-0 as zero; strict peers could discard such frames). This aligns
  the wire format with YABE and bacnet-stack.
- OS detection in the UDP transport now uses `RuntimeInformation.IsOSPlatform`, fixing `DontFragment` on
  macOS (#91) and making the Windows-only `SIO_UDP_CONNRESET` guard analyzer-clean.
- Response correlation matches by invoke-id per ASHRAE 135 §20.1.2.6 (#141, #149).
- Multi-object `WritePropertyMultiple` decoding (#158), DATE/TIME/DATETIME culture-invariant
  serialization (#159), float/double property serialization (#143), and several encoder fixes surfaced
  by the Annex F vectors (wildcard time, private-transfer ack, log-record status flags).
- `GetAddressDefaultInterface` now throws a helpful error instead of returning null when the local
  interface is ambiguous (#100).
