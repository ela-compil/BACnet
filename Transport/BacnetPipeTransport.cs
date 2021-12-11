namespace System.IO.BACnet;

public class BacnetPipeTransport : IBacnetSerialTransport
{
    private PipeStream _conn;
    private IAsyncResult _currentRead;
    private IAsyncResult _currentConnect;
    private readonly bool _isServer;

    public string Name { get; }
    public int BytesToRead => PeekPipe();

    public static string[] AvailablePorts
    {
        get
        {
            try
            {
                var listOfPipes = Directory.GetFiles(@"\\.\pipe\");
                for (var i = 0; i < listOfPipes.Length; i++)
                    listOfPipes[i] = listOfPipes[i].Replace(@"\\.\pipe\", "");
                return listOfPipes;
            }
            catch (Exception ex)
            {
                var log = LogManager.GetLogger<BacnetPipeTransport>();
                log.Warn("Exception in AvailablePorts", ex);
                return InteropAvailablePorts;
            }
        }
    }

    public BacnetPipeTransport(string name, bool isServer = false)
    {
        Name = name;
        _isServer = isServer;
    }

    /// <summary>
    /// Get the available byte count. (The .NET pipe interface has a few lackings. See also the "InteropAvailablePorts" function)
    /// </summary>
    [DllImport("kernel32.dll", EntryPoint = "PeekNamedPipe", SetLastError = true)]
    private static extern bool PeekNamedPipe(IntPtr handle, IntPtr buffer, uint nBufferSize, IntPtr bytesRead, ref uint bytesAvail, IntPtr bytesLeftThisMessage);

    public int PeekPipe()
    {
        uint bytesAvail = 0;
        return PeekNamedPipe(_conn.SafePipeHandle.DangerousGetHandle(), IntPtr.Zero, 0, IntPtr.Zero, ref bytesAvail, IntPtr.Zero)
            ? (int)bytesAvail
            : 0;
    }

    public override string ToString()
    {
        return Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is not BacnetPipeTransport) return false;
        var a = (BacnetPipeTransport)obj;
        return Name.Equals(a.Name);
    }

    public void Open()
    {
        if (_conn != null) Close();

        if (!_isServer)
        {
            _conn = new NamedPipeClientStream(".", Name, PipeDirection.InOut, PipeOptions.Asynchronous);
            ((NamedPipeClientStream)_conn).Connect(3000);
        }
        else
        {
            _conn = new NamedPipeServerStream(Name, PipeDirection.InOut, 20, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }
    }

    public void Write(byte[] buffer, int offset, int length)
    {
        if (!_conn.IsConnected) return;
        try
        {
            //doing syncronous writes (to an Asynchronous pipe) seems to be a bad thing
            _conn.BeginWrite(buffer, offset, length, (r) => { _conn.EndWrite(r); }, null);
        }
        catch (IOException)
        {
            Disconnect();
        }
    }

    private void Disconnect()
    {
        if (_conn is NamedPipeServerStream stream)
        {
            try
            {
                stream.Disconnect();
            }
            catch
            {
            }
            _currentConnect = null;
        }
        _currentRead = null;
    }

    private bool WaitForConnection(int timeoutMs)
    {
        if (_conn.IsConnected) return true;
        if (_conn is not NamedPipeServerStream) return true;

        var server = (NamedPipeServerStream)_conn;

        if (_currentConnect == null)
        {
            try
            {
                _currentConnect = server.BeginWaitForConnection(null, null);
            }
            catch (IOException)
            {
                Disconnect();
                _currentConnect = server.BeginWaitForConnection(null, null);
            }
        }

        if (_currentConnect.IsCompleted || _currentConnect.AsyncWaitHandle.WaitOne(timeoutMs))
        {
            try
            {
                server.EndWaitForConnection(_currentConnect);
            }
            catch (IOException)
            {
                Disconnect();
            }
            _currentConnect = null;
        }

        return _conn.IsConnected;
    }

    public int Read(byte[] buffer, int offset, int length, int timeoutMs)
    {
        if (!WaitForConnection(timeoutMs)) return -BacnetMstpProtocolTransport.ETIMEDOUT;

        if (_currentRead == null)
        {
            try
            {
                _currentRead = _conn.BeginRead(buffer, offset, length, null, null);
            }
            catch (Exception)
            {
                Disconnect();
                return -1;
            }
        }

        if (!_currentRead.IsCompleted && !_currentRead.AsyncWaitHandle.WaitOne(timeoutMs))
            return -BacnetMstpProtocolTransport.ETIMEDOUT;

        try
        {
            var rx = _conn.EndRead(_currentRead);
            _currentRead = null;
            return rx;
        }
        catch (Exception)
        {
            Disconnect();
            return -1;
        }
    }

    public void Close()
    {
        if (_conn == null) return;
        _conn.Close();
        _conn = null;
    }

    public void Dispose()
    {
        Close();
    }

    #region " Interop Get Pipe Names "
    // ReSharper disable All

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WIN32_FIND_DATA
    {
        public uint dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);
    [DllImport("kernel32.dll")]
    private static extern bool FindClose(IntPtr hFindFile);

    /// <summary>
    /// The built-in functions for pipe enumeration isn't perfect, I'm afraid. Hence this messy interop.
    /// </summary>
    private static string[] InteropAvailablePorts
    {
        get
        {
            var ret = new List<string>();
            var handle = FindFirstFile(@"\\.\pipe\*", out var data);
            if (handle == new IntPtr(-1))
                return ret.ToArray();

            do
            {
                ret.Add(data.cFileName);
            }
            while (FindNextFile(handle, out data) != 0);

            FindClose(handle);
            return ret.ToArray();
        }
    }

    // ReSharper restore
    #endregion
}
