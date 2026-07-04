namespace System.IO.BACnet;

public class BacnetSerialPortTransport : IBacnetSerialTransport
{
    private readonly string _portName;
    private readonly int _baudRate;
    private SerialPort _port;

    public int BytesToRead => _port?.BytesToRead ?? 0;

    public BacnetSerialPortTransport(string portName, int baudRate)
    {
        // Deliberately does not touch SerialPort here: on runtimes where System.IO.Ports is a
        // package (net8/net10) merely referencing the type triggers the assembly/native load, and
        // we want that to fail inside Open() where it can be reported with actionable guidance.
        _portName = portName;
        _baudRate = baudRate;
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
        // OpenCore is a separate method on purpose: if the System.IO.Ports managed assembly is
        // missing, the JIT-time load failure is thrown at the call site (here), so a try/catch
        // inside OpenCore itself would never see it. Wrapping the call does.
        try
        {
            OpenCore();
        }
        catch (Exception ex) when (IsMissingSerialSupport(ex))
        {
            throw new PlatformNotSupportedException(
                $"Failed to load native serial-port support while opening '{_portName}'. The " +
                "System.IO.Ports native library was not deployed with the application. Publish with " +
                "an explicit Runtime Identifier so the native serial library is included, for example " +
                "'dotnet publish --runtime linux-arm64' on a 64-bit Raspberry Pi (or " +
                "'--runtime linux-arm' for a 32-bit OS).",
                ex);
        }
    }

    private void OpenCore()
    {
        _port ??= new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One);
        _port.Open();
    }

    // True when the exception chain indicates System.IO.Ports could not be loaded — either the
    // managed assembly (FileNotFound/FileLoad/TypeInitialization referencing it) or the native
    // library (DllNotFound). Distinguishes a missing-deployment problem from an in-use/access error.
    private static bool IsMissingSerialSupport(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is DllNotFoundException)
                return true;

            if ((e is FileNotFoundException || e is FileLoadException || e is TypeInitializationException)
                && e.Message?.IndexOf("System.IO.Ports", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
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
