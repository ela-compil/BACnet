namespace System.IO.BACnet;

/// <summary>
/// Tracks a single outstanding confirmed request and correlates the reply to it.
/// Per ASHRAE 135 (20.1.2.6) the invoke-id is source-device-unique — unique across all of this
/// device's outstanding requests — and is the value used to "associate the response ... with the
/// original request". Replies are therefore matched by invoke-id alone; the transport source
/// address is intentionally not compared, because a conformant reply routed via a BACnet
/// router/BBMD arrives with the router's address (not the addressed device's), and a strict
/// address check would wrongly drop it (issues #141, #149).
/// </summary>
public class BacnetAsyncResult : IAsyncResult, IDisposable
{
    private BacnetClient _comm;
    private readonly byte _waitInvokeId;
    private Exception _error;
    private readonly byte[] _transmitBuffer;
    private readonly int _transmitLength;
    private readonly bool _waitForTransmit;
    private readonly int _transmitTimeout;
    private ManualResetEvent _waitHandle;
    private readonly BacnetAddress _address;

    // Non-blocking waiting path used by the *Async request methods. It runs in parallel with the
    // ManualResetEvent above (which still drives the synchronous WaitForDone), so the public API is
    // unchanged: both are signalled at the same points. Continuations run asynchronously so a waiter
    // resuming never executes on — and thus never stalls — the transport's receive thread.
    private readonly object _asyncLock = new();
    private TaskCompletionSource<bool> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _done;

    public bool Segmented { get; private set; }
    public byte[] Result { get; private set; }
    public object AsyncState { get; set; }
    public bool CompletedSynchronously { get; private set; }
    public WaitHandle AsyncWaitHandle => _waitHandle;
    public bool IsCompleted => _waitHandle.WaitOne(0);
    public BacnetAddress Address => _address;

    public Exception Error
    {
        get => _error;
        set
        {
            _error = value;
            CompletedSynchronously = true;
            _waitHandle.Set();
            MarkDone();
        }
    }

    public BacnetAsyncResult(BacnetClient comm, BacnetAddress adr, byte invokeId, byte[] transmitBuffer, int transmitLength, bool waitForTransmit, int transmitTimeout)
    {
        _transmitTimeout = transmitTimeout;
        _address = adr;
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
        _comm.OnSegment += OnSegment;
        _waitHandle = new ManualResetEvent(false);
    }

    public void Resend()
    {
        try
        {
            if (_comm.Transport.Send(_transmitBuffer, _comm.Transport.HeaderLength, _transmitLength, _address, _waitForTransmit, _transmitTimeout) < 0)
            {
                Error = new IOException("Write Timeout");
            }
        }
        catch (Exception ex)
        {
            Error = new Exception($"Write Exception: {ex.Message}");
        }
    }

    private void OnSegment(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte sequenceNumber, byte[] buffer, int offset, int length)
    {
        if (invokeId != _waitInvokeId)
            return;

        Segmented = true;
        _waitHandle.Set();
        MarkActivity();
    }

    private void OnSimpleAck(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] data, int dataOffset, int dataLength)
    {
        if (invokeId != _waitInvokeId)
            return;

        _waitHandle.Set();
        MarkDone();
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

        Segmented = false;
        Result = new byte[length];

        if (length > 0)
            Array.Copy(buffer, offset, Result, 0, length);

        //notify waiter even if segmented
        _waitHandle.Set();
        MarkDone();
    }

    /// <summary>
    /// Will continue waiting until all segments are recieved
    /// </summary>
    public bool WaitForDone(int timeout)
    {
        while (true)
        {
            if (!AsyncWaitHandle.WaitOne(timeout))
                return false;
            if (Segmented)
                _waitHandle.Reset();
            else
                return true;
        }
    }

    /// <summary>
    /// Non-blocking equivalent of <see cref="WaitForDone"/>. Completes when a terminal reply (simple
    /// ack, final complex ack, error, abort or reject) for this invoke-id arrives, mirroring the
    /// segmented semantics of <see cref="WaitForDone"/>: each incoming segment re-arms the timeout so
    /// the whole transfer is bounded per segment, not in total.
    /// </summary>
    /// <returns><c>true</c> if the request completed within the timeout; <c>false</c> on timeout.</returns>
    public async Task<bool> GetResultOrTimeout(int timeoutMs, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            Task signal;
            lock (_asyncLock)
            {
                if (_done)
                    return true;
                if (_completionSource.Task.IsCompleted)
                    _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                signal = _completionSource.Task;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var completed = await Task.WhenAny(signal, Task.Delay(timeoutMs, timeoutCts.Token)).ConfigureAwait(false);

            if (completed != signal)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }

            // A terminal reply or a segment arrived; stop the timer and loop to re-evaluate: _done
            // ends the wait, a bare segment re-arms it (the completed source is replaced above).
            timeoutCts.Cancel();
        }
    }

    private void MarkActivity()
    {
        lock (_asyncLock)
            _completionSource.TrySetResult(true);
    }

    private void MarkDone()
    {
        lock (_asyncLock)
        {
            _done = true;
            _completionSource.TrySetResult(true);
        }
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
            _comm.OnSegment -= OnSegment;
            _comm = null;
        }

        if (_waitHandle != null)
        {
            _waitHandle.Dispose();
            _waitHandle = null;
        }
    }
}
