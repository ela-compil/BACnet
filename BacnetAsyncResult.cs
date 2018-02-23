using System.Threading;

namespace System.IO.BACnet
{
    public class BacnetAsyncResult : IAsyncResult, IDisposable
    {
        private BacnetClient _comm;
        private readonly byte _waitInvokeId;
        private Exception _error;
        private readonly byte[] _transmitBuffer;
        private readonly int _transmitLength;
        private readonly bool _waitForTransmit;
        private readonly TimeSpan _transmitTimeout;
        private ManualResetEvent _waitHandle;

        public bool Segmented { get; private set; }
        public byte[] Result { get; private set; }
        public object AsyncState { get; set; }
        public bool CompletedSynchronously { get; private set; }
        public WaitHandle AsyncWaitHandle => _waitHandle;
        public bool IsCompleted => _waitHandle.WaitOne(0);
        public BacnetAddress Address { get; }

        public Exception Error
        {
            get => _error;
            set
            {
                _error = value;
                CompletedSynchronously = true;
                _waitHandle.Set();
            }
        }

        public BacnetAsyncResult(BacnetClient comm, BacnetAddress adr, byte invokeId,
            byte[] transmitBuffer, int transmitLength, bool waitForTransmit, TimeSpan transmitTimeout)
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
            _comm.OnSegment += OnSegment;
            _waitHandle = new ManualResetEvent(false);
        }

        public BacnetAsyncResult Send()
        {
            try
            {
                var bytesSent = _comm.Transport.Send(_transmitBuffer, _comm.Transport.HeaderLength,
                    _transmitLength, Address, _waitForTransmit, (int)_transmitTimeout.TotalMilliseconds);

                if (_waitForTransmit && bytesSent < 0)
                    Error = new IOException("Write Timeout");
            }
            catch (Exception ex)
            {
                Error = new Exception($"Write Exception: {ex.Message}");
            }

            return this;
        }

        public void Resend()
        {
            Send();
        }

        private void OnSegment(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte sequenceNumber, byte[] buffer, int offset, int length)
        {
            if (invokeId != _waitInvokeId)
                return;

            Segmented = true;
            _waitHandle.Set();
        }

        private void OnSimpleAck(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] data, int dataOffset, int dataLength)
        {
            if (invokeId != _waitInvokeId)
                return;

            _waitHandle.Set();
        }

        private void OnAbort(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte invokeId, BacnetAbortReason reason, byte[] buffer, int offset, int length)
        {
            if (invokeId != _waitInvokeId)
                return;

            Error = new BacnetAbortException(reason);
        }

        private void OnReject(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte invokeId, BacnetRejectReason reason, byte[] buffer, int offset, int length)
        {
            if (invokeId != _waitInvokeId)
                return;

            Error = new BacnetRejectException(reason);
        }

        private void OnError(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode, byte[] buffer, int offset, int length)
        {
            if (invokeId != _waitInvokeId)
                return;

            Error = new BacnetErrorException(errorClass, errorCode);
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
        }

        /// <summary>
        /// Will continue waiting until all segments are recieved
        /// </summary>
        public bool WaitForDone(TimeSpan timeout)
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

        public void GetResult(TimeSpan? timeout = null, int retryCount = 0)
        {
            GetResult<object>(timeout, retryCount);
        }

        /// <exception cref="TimeoutException">
        /// Exception is thrown if given <paramref name="timeout"/> was exceeded before full response was received.
        /// </exception>
        public TResult GetResult<TResult>(TimeSpan? timeout = null, int retryCount = 0, Func<BacnetAsyncResult, TResult> decodeFunc = null)
        {
            if (retryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount), "Retry count should be a non-negative value");
            
            while (true)
            {
                if (WaitForDone(timeout ?? TimeSpan.FromSeconds(1)) && Error == null)
                    return decodeFunc != null
                        ? decodeFunc.Invoke(this)
                        : default(TResult);
                
                if (retryCount == 0)
                    break;
                
                switch (Error)
                {
                    case BacnetAbortException abortException:
                    case BacnetRejectException rejectException:
                    case BacnetErrorException errorException:
                        throw Error;
                }

                retryCount--;
                Resend();
            }

            throw Error ?? new BacnetApduTimeoutException(Result?.Length > 0
                ? $"Failed to receive complete response within {timeout}"
                : $"No response within {timeout}");
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
}