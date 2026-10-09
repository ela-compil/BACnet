# Threat model

## What this project does and where untrusted input enters
- A managed .NET BACnet (ASHRAE 135) protocol stack, used in client and device (server) roles in building-automation
  systems: HVAC, lighting, access control and metering.
- All bytes received from the network or a serial line are untrusted. BACnet has no authentication at these layers,
  so any host on the IP network or any node on the MS/TP bus can send any frame.
- Entry points:
  - BACnet/IP over UDP, IPv4 and IPv6: `Transport/BacnetIpUdpProtocolTransport.cs`,
    `Transport/BacnetIpV6UdpProtocolTransport.cs`, including BVLC and BBMD / foreign-device handling
    (`Serialize/BVLC.cs`, `Transport/BVLCV6.cs`).
  - MS/TP and PTP over serial: `Transport/BacnetMstpProtocolTransport.cs`, `Transport/BacnetPtpProtocolTransport.cs`,
    `Serialize/MSTP.cs`, `Serialize/PTP.cs`, `Serial/`.
  - BACnet/Ethernet (pcap): `Ethernet/`.
  - Named pipes: `Transport/BacnetPipeTransport.cs`.
  - Decoding above the link layer: `BACnetClient.OnRecieve` → NPDU (`Serialize/NPDU.cs`) → APDU
    (`Serialize/APDU.cs`) → service decoding (`Serialize/Services.cs`, `Serialize/ASN1.cs`), and segment reassembly
    (`BACnetClient.ProcessSegment` / `PerformDefaultSegmentHandling`).

## Components that matter most / least
- In scope, highest priority: the decoders in `Serialize/` (ASN1, APDU, NPDU, BVLC, MSTP, PTP, Services) and
  `BACnetClient.cs` (request/response dispatch, segmentation, invoke-id and transaction tracking).
- In scope: the transports in `Transport/`, `Ethernet/` and `Serial/`, including BBMD broadcast distribution and the
  foreign-device registration table.
- Out of scope: `Storage/` (the sample device object database), `Examples/` (sample applications), `Tests/`, and
  `Logging/`.

## How to exercise it
- The image is built in `/src`. Run the unit tests offline with
  `dotnet test --project Tests/BACnet.Tests.csproj --no-build -c Debug`.
- `Tests/Serialize/` has encode/decode round-trip tests checked against ASHRAE 135 Annex F. They are the easiest place
  to feed crafted byte arrays into the decoders.
- `Tests/BacnetClient*Tests.cs` drive `BacnetClient` through an in-memory transport (`Tests/Support/LoopbackTransport.cs`), including segmentation. Use them
  as a pattern to inject crafted frames end to end without a real network.

## How you rate severity
- High: an unhandled exception, crash, deadlock or infinite loop in the receive path that a single remote packet (or
  a short sequence of packets) can trigger, taking down the client or device.
- High: bypassing access checks or state rules in server-side request handling, such as making a device accept a
  write or a device-communication-control or reinitialize request it should reject.
- High: a remote party causing frames to be forwarded or amplified through BBMD / foreign-device handling beyond what
  the standard allows.
- Medium: unbounded memory or CPU growth driven by remote input, e.g. through segment reassembly, invoke-id tracking
  or the foreign-device table.
- Medium: a malformed reply that a malicious device sends back to a client and that makes the client decode wrong
  values without raising an error.
- Low: issues reachable only through local configuration or API misuse by the application that hosts the library.

## Anything to leave alone
- Exceptions that the library catches and logs, where the client or device keeps working, are not vulnerabilities.
- Do not report the lack of authentication or encryption in BACnet/IP or MS/TP itself. That is part of the protocol.
- Do not report findings in `Examples/` or `Storage/`, or the missing XML-doc warnings (CS1573/CS1570) at build time.
