namespace System.IO.BACnet;

/// <summary>
/// Convenience factories for wiring the MS/TP and PTP protocol transports over a physical serial
/// port. These replace the <c>(portName, baudRate, ...)</c> convenience constructors that used to
/// live on <see cref="BacnetMstpProtocolTransport"/> / <see cref="BacnetPtpProtocolTransport"/>
/// before the <see cref="BacnetSerialPortTransport"/> implementation (and its System.IO.Ports
/// dependency) moved into this optional BACnet.Serial package. The core transports still accept any
/// <see cref="IBacnetSerialTransport"/> directly.
/// </summary>
public static class SerialTransport
{
    /// <summary>Creates an MS/TP transport driven by a physical serial port.</summary>
    public static BacnetMstpProtocolTransport Mstp(string portName, int baudRate, short sourceAddress = -1,
        byte maxMaster = 127, byte maxInfoFrames = 1)
        => new BacnetMstpProtocolTransport(new BacnetSerialPortTransport(portName, baudRate),
            sourceAddress, maxMaster, maxInfoFrames);

    /// <summary>Creates a PTP transport driven by a physical serial port.</summary>
    public static BacnetPtpProtocolTransport Ptp(string portName, int baudRate, bool isServer)
        => new BacnetPtpProtocolTransport(new BacnetSerialPortTransport(portName, baudRate), isServer);
}
