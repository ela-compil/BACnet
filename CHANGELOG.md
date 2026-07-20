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
- **Scheduling types** replaced with spec-shaped ones (see MIGRATION.md): `BACnetCalendarEntry`
  (a `List<object>` bag) → `BacnetCalendarEntry` (a real CHOICE, one entry per value),
  `BacnetweekNDay` → `BacnetWeekNDay` (month/week-of-month/day-of-week enums, week-of-month
  actually evaluated), `BacnetDate.toDateTime()` → `ToDateTime()`. Decoded `Date_List`,
  `Weekly_Schedule` and `Exception_Schedule` values now carry dedicated application tags and typed
  objects instead of the opaque `CONTEXT_SPECIFIC_DECODED` shapes.
- **`BacnetDeviceObjectPropertyReference`**: the misspelled public field `deviceIndentifier` is now
  `deviceIdentifier` (the constructor parameter included).
- **`Services.EncodeCreateProperty` → `EncodeCreateObject`**: the encoder was misnamed - it encodes
  a CreateObject-Request (BACnet has no CreateProperty service). Same signature; the `BacnetClient`
  request methods are unaffected.

### Added
- Multi-targeting: `net48;netstandard2.0;net8.0;net10.0`.
- New packages: `BACnet.Ethernet`, `BACnet.Serial`, `BACnet.Logging.CommonLogging`.
- Central Package Management, MinVer tag-driven versioning, Source Link + symbol (`.snupkg`) packages.
- `BacnetIpUdpProtocolTransport` and `BacnetIpV6UdpProtocolTransport` accept a `maxApdu` constructor
  parameter to lower the advertised and sent APDU size (e.g. `MAX_APDU1024` keeps every frame,
  including segmented responses, under a 1500-byte MTU instead of relying on IP fragmentation).
- CI: multi-OS build matrix; tag-triggered publish to both nuget.org (Trusted Publishing) and GitHub Packages.
- Runnable sample projects moved in-repo under `Examples/` and built in CI.
- ASHRAE 135 Annex F golden-vector encode/decode tests - complete: all 62 Annex F example
  encodings are asserted byte-for-byte.
- `Services.EncodeCreateObject` overload taking a `BacnetObjectTypes` (CreateObject by object type,
  the device assigns the instance number) and `Services.EncodeDeleteObject`.
- PrivateTransfer send/receive support in `BacnetClient` (#154): `PrivateTransferRequest`,
  `SendUnconfirmedPrivateTransfer`, the `OnPrivateTransfer` event, and the
  `PrivateTransferResponse` / `PrivateTransferErrorResponse` replies (ASHRAE 135 clauses 16.2/16.3).
- `WritePropertyRequest` / `BeginWritePropertyRequest` accept an optional `arrayIndex` to write a
  single array element (matching `ReadPropertyRequest`), e.g. one slot of a `Priority_Array` or
  one day of a `Weekly_Schedule`.
- Full Schedule/Calendar (scheduling) serialization (#26, #131): `BacnetTimeValue`,
  `BacnetDailySchedule`, `BacnetSpecialEvent` and the reworked `BacnetCalendarEntry` /
  `BacnetWeekNDay` give `Weekly_Schedule`, `Exception_Schedule` and `Date_List` symmetric
  encode/decode - values read from a device can be modified and written back as-is
  (ASHRAE 135-2016 clauses 12.9 / 12.24 / 21).
- Asynchronous, non-blocking client API (#46): every confirmed service has an awaitable `…Async`
  counterpart (`ReadPropertyAsync`, `WritePropertyAsync`, `SubscribeCOVAsync`, …) that no longer parks
  a thread per outstanding request, so many requests can be in flight on one client at once, each reply
  matched to its request by invoke-id. Failures throw (a `TimeoutException` once retries are exhausted,
  otherwise the device error); most methods accept an optional `CancellationToken`, and the
  file/range/private-transfer variants return typed results (`BacnetReadFileResult`,
  `BacnetReadRangeResult`, `BacnetPrivateTransferResult`).

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
- Event notifications stamped with a `time` or `sequenceNumber` BACnetTimeStamp are now decoded, so
  `OnEventNotify` fires for them (#32, #67): the decoders assumed the `dateTime` choice only, making
  notifications from devices without clocks (which commonly stamp with sequence numbers) invisible.
  The same fix applies to `DecodeAlarmAcknowledge` (an ack echoes the event's timestamp choice) and
  the GetEventInformation timestamps; new `ASN1.bacapp_decode_timestamp` handles the full CHOICE and
  `BacnetGenericTime.Tag`/`Sequence` are now populated on decode.
- Partially-wildcarded Date and Time values no longer throw during decode (#103): any octet may
  individually be X'FF' (unspecified) and Date carries special values (odd/even month, last day —
  ASHRAE 135 §20.2.12/§20.2.13); previously the exception was swallowed upstream, e.g. silently
  dropping event notifications stamped with a wildcarded time. Such values are also **lossless**
  now: a decoded `BacnetValue` carries the new per-octet `BacnetTime` (or the existing
  `BacnetDate`) whenever the octets cannot be represented faithfully as a `DateTime`, a Date+Time
  property pair with wildcards merges into the new `BacnetDateTime` struct, and timestamps keep
  their original time octets in `BacnetGenericTime.PartialTime` next to the best-effort clamped
  `Time` — so echoing an event's partially-wildcarded timestamp back (e.g. in an AcknowledgeAlarm)
  reproduces the wire bytes. Fully-specified values keep coming back as `DateTime`.
  `Event_Time_Stamps` elements now decode as `BacnetGenericTime` (preserving the BACnetTimeStamp
  choice) instead of a bare `DateTime` that crashed on re-encode. Log-record stamps and
  service-internal times (TimeSynchronization, ReadRange by-time), where the standard requires
  specific values, still clamp.
- `PROP_EFFECTIVE_PERIOD` decodes as one typed `BacnetDateRange` value instead of two bare
  `DateTime`s, so the open (wildcarded) boundaries of a Schedule's Effective_Period survive a
  read/write round-trip. Writing either shape — the typed range or two application-tagged
  dates — stays supported.
- ReadRange by-time and by-sequence responses decode correctly: the ack's trailing
  first-sequence-number was included in the returned item-data range (the extraction assumed the
  by-position layout), so decoding the last "record" of a time-based trend-log read failed.
- A TIME value of exactly midnight no longer encodes as the "any time" wildcard: `DateTime(1,1,1)`
  — which every decoded midnight is — used to double as the wildcard sentinel, so reading a 00:00
  time (or timestamp) and writing it back corrupted it to `FF FF FF FF`. The unspecified time now
  has a dedicated marker, `ASN1.BACNET_TIME_WILDCARD`: decoders return it for a fully-wildcarded
  time and encoders turn it back into `FF FF FF FF`, so midnight and the wildcard each round-trip
  losslessly (see MIGRATION.md — a fully-wildcarded time used to decode to `DateTime.MinValue`).
  Combined date+time decodes (BACnetDateTime, timestamps, log records) keep degrading an
  unspecified time component to 00:00, as those values cannot carry it.
- All unconfirmed sends now address peers behind a BACnet router correctly (NPDU destination =
  the peer's network/MAC, frame to the fronting router — the same addressing the `Iam()` fix
  established): directed `WhoIs`/`WhoHas`, `SendUnconfirmedEventNotification`,
  `SendUnconfirmedPrivateTransfer` and `SynchronizeTime`. `IHave()` gained an optional `receiver`
  parameter (appended after `source` to stay source-compatible) so it can answer a Who-Has that
  arrived through a router (135 §16.9).
- `ErrorResponse` sends the Error PDU with the plain §20.1.7 encoding again: since 3.x the error
  class/code pair was wrapped in spurious context tags, so foreign stacks mis-decoded every error this
  library returned (the tolerant decoder hid it from same-stack round-trips) (#199). Context wrapping
  is now explicit at the call sites whose ASN.1 requires it (private-transfer error-type `[0]`,
  trend-log failure log-datum `[8]`).
- Multi-object `WritePropertyMultiple` decoding (#158), DATE/TIME/DATETIME culture-invariant
  serialization (#159), float/double property serialization (#143), and several encoder fixes surfaced
  by the Annex F vectors (wildcard time, private-transfer ack, log-record status flags).
- `GetAddressDefaultInterface` now throws a helpful error instead of returning null when the local
  interface is ambiguous (#100).
