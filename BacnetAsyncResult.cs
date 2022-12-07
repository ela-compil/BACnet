namespace System.IO.BACnet;

public class BacnetAsyncResult : IDisposable
{
    private BacnetClient _comm;
    private readonly byte _waitInvokeId;
    private Exception _error;
    private readonly byte[] _transmitBuffer;
    private readonly int _transmitLength;
    private readonly bool _waitForTransmit;
    private readonly int _transmitTimeout;
    public BacnetAddress Address { get; }

    private TaskCompletionSource<byte[]> _tcs = new();
    public Task<byte[]> GetResult() => _tcs.Task;

    public byte[] Result => this.GetResult().GetAwaiter().GetResult();

    public Exception Error
    {
        get => _error;
        private set
        {
            _error = value;
            _tcs.SetException(value);
        }
    }

    public BacnetAsyncResult(BacnetClient comm, BacnetAddress adr, byte invokeId, byte[] transmitBuffer, int transmitLength, bool waitForTransmit, int transmitTimeout)
    {
        _transmitTimeout = transmitTimeout;
        Address = adr;
        _waitForTransmit = waitForTransmit;
        _transmitBuffer = transmitBuffer;
        _transmitLength = transmitLength;
        _comm = comm;
        _waitInvokeId = invokeId;
        _comm.OnComplexAck += OnComplexAck;
        _comm.OnError += OnError;
        _comm.OnAbort += OnAbort;
        _comm.OnReject += OnReject;
        _comm.OnSimpleAck += OnSimpleAck;
    }

    public async Task<byte[]> GetResultOrTimeout(int timeoutMs)
    {
        var task = this.GetResult();
        using var source = new CancellationTokenSource();
        if (task == await Task.WhenAny(task, Task.Delay(timeoutMs, source.Token)))
        {
            source.Cancel();
            return await task;
        }

        return null;
    }

    public bool WaitForDone(int timeoutMs)
    {
        var result = this.GetResultOrTimeout(timeoutMs).GetAwaiter().GetResult();
        return result != null;
    }

    public void Resend()
    {
        _tcs = new TaskCompletionSource<byte[]>();
        try
        {
            if (_comm.Transport.Send(_transmitBuffer, _comm.Transport.HeaderLength, _transmitLength, Address, _waitForTransmit, _transmitTimeout) < 0)
            {
                Error = new IOException("Write Timeout");
            }
        }
        catch (Exception ex)
        {
            Error = new Exception($"Write Exception: {ex.Message}");
        }
    }

    private void OnSimpleAck(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] data, int dataOffset, int dataLength)
    {
        if (invokeId != _waitInvokeId)
            return;

        // TODO: Should this be null?
        _tcs.SetResult(data);
    }

    private void OnAbort(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte invokeId, BacnetAbortReason reason, byte[] buffer, int offset, int length)
    {
        if (invokeId != _waitInvokeId)
            return;

        Error = new Exception($"Abort from device, reason: {reason}");
    }

    private void OnReject(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte invokeId, BacnetRejectReason reason, byte[] buffer, int offset, int length)
    {
        if (invokeId != _waitInvokeId)
            return;

        Error = new Exception($"Reject from device, reason: {reason}");
    }

    private void OnError(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode, byte[] buffer, int offset, int length)
    {
        if (invokeId != _waitInvokeId)
            return;

        Error = new Exception($"Error from device: {errorClass} - {errorCode}");
    }

    private void OnComplexAck(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
    {
        if (invokeId != _waitInvokeId)
            return;

        var result = new byte[length];

        if (length > 0)
            Array.Copy(buffer, offset, result, 0, length);

        _tcs.SetResult(result);
    }

    public void Dispose()
    {
        if (_comm != null)
        {
            _comm.OnComplexAck -= OnComplexAck;
            _comm.OnError -= OnError;
            _comm.OnAbort -= OnAbort;
            _comm.OnReject -= OnReject;
            _comm.OnSimpleAck -= OnSimpleAck;
            _comm = null;
        }
    }
}
