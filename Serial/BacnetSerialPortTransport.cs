namespace System.IO.BACnet;

public class BacnetSerialPortTransport : IBacnetSerialTransport
{
    private readonly string _portName;
    private readonly SerialPort _port;

    public int BytesToRead => _port?.BytesToRead ?? 0;

    public BacnetSerialPortTransport(string portName, int baudRate)
    {
        _portName = portName;
        _port = new SerialPort(_portName, baudRate, Parity.None, 8, StopBits.One);
    }

    public override bool Equals(object obj)
    {
        if (obj is not BacnetSerialPortTransport) return false;
        var a = (BacnetSerialPortTransport)obj;
        return _portName.Equals(a._portName);
    }

    public override int GetHashCode()
    {
        return _portName.GetHashCode();
    }

    public override string ToString()
    {
        return _portName;
    }

    public void Open()
    {
        _port.Open();
    }

    public void Write(byte[] buffer, int offset, int length)
    {
        _port?.Write(buffer, offset, length);
    }

    public int Read(byte[] buffer, int offset, int length, int timeoutMs)
    {
        if (_port == null) return 0;
        _port.ReadTimeout = timeoutMs;

        try
        {
            var rx = _port.Read(buffer, offset, length);
            return rx;
        }
        catch (TimeoutException)
        {
            return -BacnetMstpProtocolTransport.ETIMEDOUT;
        }
        catch (Exception)
        {
            return -1;
        }
    }

    public void Close()
    {
        _port?.Close();
    }

    public void Dispose()
    {
        Close();
    }
}
