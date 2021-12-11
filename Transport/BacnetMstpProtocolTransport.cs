namespace System.IO.BACnet;

/// <summary>
/// This is the standard BACNet MSTP transport
/// </summary>
public class BacnetMstpProtocolTransport : BacnetTransportBase
{
    public delegate void FrameRecievedHandler(BacnetMstpProtocolTransport sender, BacnetMstpFrameTypes frameType,
        byte destinationAddress, byte sourceAddress, int msgLength);

    public enum GetMessageStatus
    {
        Good,
        Timeout,
        SubTimeout,
        ConnectionClose,
        ConnectionError,
        DecodeError
    }

    /// <summary>
    /// The minimum time (ms) without a DataAvailable or ReceiveError event within a frame before a receiving node may discard the frame.
    /// </summary>
    public const int T_FRAME_ABORT = 80;

    /// <summary>
    /// The time (ms) without a DataAvailable or ReceiveError event before declaration of loss of token
    /// </summary>
    public const int T_NO_TOKEN = 500;

    /// <summary>
    /// The minimum time (ms) without a DataAvailable or ReceiveError event that a node must wait for a station to begin replying to a confirmed request
    /// </summary>
    public const int T_REPLY_TIMEOUT = 295;

    /// <summary>
    /// The minimum time (ms) without a DataAvailable or ReceiveError event that a node must wait for a remote node to begin using a token or replying to a Poll For Master frame:
    /// </summary>
    public const int T_USAGE_TIMEOUT = 95;

    /// <summary>
    /// The maximum time (ms) a node may wait after reception of a frame that expects a reply before sending the first octet of a reply or Reply Postponed frame
    /// </summary>
    public const int T_REPLY_DELAY = 250;

    public const int ETIMEDOUT = 110;

    private IBacnetSerialTransport _port;
    private byte _frameCount;
    private readonly byte[] _localBuffer;
    private int _localOffset;

    /// <summary>
    /// The number of tokens received or used before a Poll For Master cycle is executed
    /// </summary>
    private const byte MaxPoll = 50;

    /// <summary>
    /// "Next Station," the MAC address of the node to which This Station passes the token. If the Next Station is unknown, NS shall be equal to TS
    /// </summary>
    private byte _ns;

    /// <summary>
    /// "Poll Station," the MAC address of the node to which This Station last sent a Poll For Master. This is used during token maintenance
    /// </summary>
    private byte _ps;

    private MessageFrame _reply;
    private readonly ManualResetEvent _replyMutex = new(false);
    private byte _replySource;
    private const byte RetryToken = 1;
    private readonly LinkedList<MessageFrame> _sendQueue = new();
    private bool _soleMaster;
    private byte _tokenCount;
    private Thread _transmitThread;

    public short SourceAddress { get; set; }
    public byte MaxMaster { get; set; }
    public bool IsRunning { get; private set; } = true;

    public BacnetMstpProtocolTransport(IBacnetSerialTransport transport, short sourceAddress = -1,
        byte maxMaster = 127, byte maxInfoFrames = 1)
    {
        SourceAddress = sourceAddress;
        MaxMaster = maxMaster;
        Type = BacnetAddressTypes.MSTP;
        HeaderLength = MSTP.MSTP_HEADER_LENGTH;
        MaxBufferLength = 502;
        MaxAdpuLength = MSTP.MSTP_MAX_APDU;
        MaxInfoFrames = maxInfoFrames;

        _localBuffer = new byte[MaxBufferLength];
        _port = transport;
    }

    public BacnetMstpProtocolTransport(string portName, int baudRate, short sourceAddress = -1,
        byte maxMaster = 127, byte maxInfoFrames = 1)
        : this(new BacnetSerialPortTransport(portName, baudRate), sourceAddress, maxMaster, maxInfoFrames)
    {
    }

    public override void Start()
    {
        if (_port == null) return;
        _port.Open();

        _transmitThread = new Thread(MstpThread)
        {
            IsBackground = true,
            Name = "MSTP Thread",
            Priority = ThreadPriority.Highest
        };
        _transmitThread.Start();
    }

    public override int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address,
        bool waitForTransmission, int timeout)
    {
        if (SourceAddress == -1) throw new Exception("Source address must be set up before sending messages");

        //add to queue
        var function = NPDU.DecodeFunction(buffer, offset);
        var frameType = (function & BacnetNpduControls.ExpectingReply) == BacnetNpduControls.ExpectingReply
            ? BacnetMstpFrameTypes.FRAME_TYPE_BACNET_DATA_EXPECTING_REPLY
            : BacnetMstpFrameTypes.FRAME_TYPE_BACNET_DATA_NOT_EXPECTING_REPLY;
        var copy = new byte[dataLength + MSTP.MSTP_HEADER_LENGTH + 2];
        Array.Copy(buffer, offset, copy, MSTP.MSTP_HEADER_LENGTH, dataLength);
        var f = new MessageFrame(frameType, address.adr[0], copy, dataLength);
        lock (_sendQueue)
            _sendQueue.AddLast(f);
        if (_reply == null)
        {
            _reply = f;
            _replyMutex.Set();
        }

        if (!waitForTransmission)
            return dataLength;

        //wait for message to be sent
        if (!f.SendMutex.WaitOne(timeout))
            return -ETIMEDOUT;

        return dataLength;
    }

    public override bool WaitForAllTransmits(int timeout)
    {
        while (_sendQueue.Count > 0)
        {
            ManualResetEvent ev;
            lock (_sendQueue)
                ev = _sendQueue.First.Value.SendMutex;

            if (ev.WaitOne(timeout))
                return false;
        }
        return true;
    }

    public override BacnetAddress GetBroadcastAddress()
    {
        return new BacnetAddress(BacnetAddressTypes.MSTP, 0xFFFF, new byte[] { 0xFF });
    }

    public override void Dispose()
    {
        _port?.Close();
        _port = null;
    }

    public event FrameRecievedHandler FrameRecieved;

    public override bool Equals(object obj)
    {
        if (obj is not BacnetMstpProtocolTransport) return false;
        var a = (BacnetMstpProtocolTransport)obj;
        return _port.Equals(a._port);
    }

    public override int GetHashCode()
    {
        return _port.GetHashCode();
    }

    public override string ToString()
    {
        return _port.ToString();
    }

    private void QueueFrame(BacnetMstpFrameTypes frameType, byte destinationAddress)
    {
        lock (_sendQueue)
            _sendQueue.AddLast(new MessageFrame(frameType, destinationAddress, null, 0));
    }

    private void SendFrame(BacnetMstpFrameTypes frameType, byte destinationAddress)
    {
        SendFrame(new MessageFrame(frameType, destinationAddress, null, 0));
    }

    private void SendFrame(MessageFrame frame)
    {
        if (SourceAddress == -1 || _port == null) return;
        int tx;
        if (frame.Data == null || frame.Data.Length == 0)
        {
            var tmpTransmitBuffer = new byte[MSTP.MSTP_HEADER_LENGTH];
            tx = MSTP.Encode(tmpTransmitBuffer, 0, frame.FrameType, frame.DestinationAddress,
                (byte)SourceAddress, 0);
            _port.Write(tmpTransmitBuffer, 0, tx);
        }
        else
        {
            tx = MSTP.Encode(frame.Data, 0, frame.FrameType, frame.DestinationAddress, (byte)SourceAddress,
                frame.DataLength);
            _port.Write(frame.Data, 0, tx);
        }
        frame.SendMutex.Set();
        Log.Debug($"{frame.FrameType} {frame.DestinationAddress:X2}");
    }

    private void RemoveCurrentMessage(int msgLength)
    {
        var fullMsgLength = MSTP.MSTP_HEADER_LENGTH + msgLength + (msgLength > 0 ? 2 : 0);
        if (_localOffset > fullMsgLength)
            Array.Copy(_localBuffer, fullMsgLength, _localBuffer, 0, _localOffset - fullMsgLength);
        _localOffset -= fullMsgLength;
    }

    private StateChanges PollForMaster()
    {
        while (true)
        {
            //send
            SendFrame(BacnetMstpFrameTypes.FRAME_TYPE_POLL_FOR_MASTER, _ps);

            //wait
            var status = GetNextMessage(T_USAGE_TIMEOUT, out var frameType, out var destinationAddress,
                out var sourceAddress, out var msgLength);

            if (status == GetMessageStatus.Good)
            {
                try
                {
                    if (frameType == BacnetMstpFrameTypes.FRAME_TYPE_REPLY_TO_POLL_FOR_MASTER &&
                        destinationAddress == SourceAddress)
                    {
                        _soleMaster = false;
                        _ns = sourceAddress;
                        _ps = (byte)SourceAddress;
                        _tokenCount = 0;
                        return StateChanges.ReceivedReplyToPFM;
                    }
                    else
                        return StateChanges.ReceivedUnexpectedFrame;
                }
                finally
                {
                    RemoveCurrentMessage(msgLength);
                }
            }
            if (_soleMaster)
            {
                /* SoleMaster */
                _frameCount = 0;
                return StateChanges.SoleMaster;
            }
            if (_ns != SourceAddress)
            {
                /* DoneWithPFM */
                return StateChanges.DoneWithPFM;
            }
            if ((_ps + 1) % (MaxMaster + 1) != SourceAddress)
            {
                /* SendNextPFM */
                _ps = (byte)((_ps + 1) % (MaxMaster + 1));
            }
            else
            {
                /* DeclareSoleMaster */
                _soleMaster = true;
                _frameCount = 0;
                return StateChanges.DeclareSoleMaster;
            }
        }
    }

    private StateChanges DoneWithToken()
    {
        if (_frameCount < MaxInfoFrames)
        {
            /* SendAnotherFrame */
            return StateChanges.SendAnotherFrame;
        }
        if (!_soleMaster && _ns == SourceAddress)
        {
            /* NextStationUnknown */
            _ps = (byte)((SourceAddress + 1) % (MaxMaster + 1));
            return StateChanges.NextStationUnknown;
        }
        if (_tokenCount < MaxPoll - 1)
        {
            _tokenCount++;
            if (_soleMaster && _ns != (SourceAddress + 1) % (MaxMaster + 1))
            {
                /* SoleMaster */
                _frameCount = 0;
                return StateChanges.SoleMaster;
            }
            /* SendToken */
            return StateChanges.SendToken;
        }
        if ((_ps + 1) % (MaxMaster + 1) == _ns)
        {
            if (!_soleMaster)
            {
                /* ResetMaintenancePFM */
                _ps = (byte)SourceAddress;
                _tokenCount = 1;
                return StateChanges.ResetMaintenancePFM;
            }
            /* SoleMasterRestartMaintenancePFM */
            _ps = (byte)((_ns + 1) % (MaxMaster + 1));
            _ns = (byte)SourceAddress;
            _tokenCount = 1;
            return StateChanges.SoleMasterRestartMaintenancePFM;
        }
        /* SendMaintenancePFM */
        _ps = (byte)((_ps + 1) % (MaxMaster + 1));
        return StateChanges.SendMaintenancePFM;
    }

    private StateChanges WaitForReply()
    {
        //fetch message
        var status = GetNextMessage(T_REPLY_TIMEOUT, out var frameType, out var destinationAddress,
            out var sourceAddress, out var msgLength);

        if (status == GetMessageStatus.Good)
        {
            try
            {
                if (destinationAddress == (byte)SourceAddress &&
                    (frameType == BacnetMstpFrameTypes.FRAME_TYPE_TEST_RESPONSE ||
                     frameType == BacnetMstpFrameTypes.FRAME_TYPE_BACNET_DATA_NOT_EXPECTING_REPLY))
                {
                    //signal upper layer
                    if (frameType != BacnetMstpFrameTypes.FRAME_TYPE_TEST_RESPONSE)
                    {
                        var remoteAddress = new BacnetAddress(BacnetAddressTypes.MSTP, 0, new[] { sourceAddress });
                        try
                        {
                            InvokeMessageRecieved(_localBuffer, MSTP.MSTP_HEADER_LENGTH, msgLength, remoteAddress);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Exception in MessageRecieved event", ex);
                        }
                    }

                    /* ReceivedReply */
                    return StateChanges.ReceivedReply;
                }
                else if (frameType == BacnetMstpFrameTypes.FRAME_TYPE_REPLY_POSTPONED)
                {
                    /* ReceivedPostpone */
                    return StateChanges.ReceivedPostpone;
                }
                else
                {
                    /* ReceivedUnexpectedFrame */
                    return StateChanges.ReceivedUnexpectedFrame;
                }
            }
            finally
            {
                RemoveCurrentMessage(msgLength);
            }
        }

        if (status != GetMessageStatus.Timeout)
            return StateChanges.InvalidFrame;

        /* ReplyTimeout */
        _frameCount = MaxInfoFrames;
        return StateChanges.ReplyTimeOut;
        /* InvalidFrame */
    }

    private StateChanges UseToken()
    {
        if (_sendQueue.Count == 0)
        {
            /* NothingToSend */
            _frameCount = MaxInfoFrames;
            return StateChanges.NothingToSend;
        }
        /* SendNoWait / SendAndWait */
        MessageFrame messageFrame;
        lock (_sendQueue)
        {
            messageFrame = _sendQueue.First.Value;
            _sendQueue.RemoveFirst();
        }
        SendFrame(messageFrame);
        _frameCount++;
        if (messageFrame.FrameType == BacnetMstpFrameTypes.FRAME_TYPE_BACNET_DATA_EXPECTING_REPLY ||
            messageFrame.FrameType == BacnetMstpFrameTypes.FRAME_TYPE_TEST_REQUEST)
            return StateChanges.SendAndWait;
        return StateChanges.SendNoWait;
    }

    private StateChanges PassToken()
    {
        for (var i = 0; i <= RetryToken; i++)
        {
            //send 
            SendFrame(BacnetMstpFrameTypes.FRAME_TYPE_TOKEN, _ns);

            //wait for it to be used
            var status = GetNextMessage(T_USAGE_TIMEOUT, out _, out _, out _, out _);
            if (status == GetMessageStatus.Good || status == GetMessageStatus.DecodeError)
                return StateChanges.SawTokenUser; //don't remove current message
        }

        //give up
        _ps = (byte)((_ns + 1) % (MaxMaster + 1));
        _ns = (byte)SourceAddress;
        _tokenCount = 0;
        return StateChanges.FindNewSuccessor;
    }

    private StateChanges Idle()
    {
        var noTokenTimeout = T_NO_TOKEN + 10 * SourceAddress;

        while (_port != null)
        {
            //get message
            var status = GetNextMessage(noTokenTimeout, out var frameType, out var destinationAddress,
                out var sourceAddress, out var msgLength);

            if (status == GetMessageStatus.Good)
            {
                try
                {
                    if (destinationAddress == SourceAddress || destinationAddress == 0xFF)
                    {
                        switch (frameType)
                        {
                            case BacnetMstpFrameTypes.FRAME_TYPE_POLL_FOR_MASTER:
                                if (destinationAddress == 0xFF)
                                    QueueFrame(BacnetMstpFrameTypes.FRAME_TYPE_REPLY_TO_POLL_FOR_MASTER,
                                        sourceAddress);
                                else
                                {
                                    //respond to PFM
                                    SendFrame(BacnetMstpFrameTypes.FRAME_TYPE_REPLY_TO_POLL_FOR_MASTER,
                                        sourceAddress);
                                }
                                break;
                            case BacnetMstpFrameTypes.FRAME_TYPE_TOKEN:
                                if (destinationAddress != 0xFF)
                                {
                                    _frameCount = 0;
                                    _soleMaster = false;
                                    return StateChanges.ReceivedToken;
                                }
                                break;
                            case BacnetMstpFrameTypes.FRAME_TYPE_TEST_REQUEST:
                                if (destinationAddress == 0xFF)
                                    QueueFrame(BacnetMstpFrameTypes.FRAME_TYPE_TEST_RESPONSE, sourceAddress);
                                else
                                {
                                    //respond to test
                                    SendFrame(BacnetMstpFrameTypes.FRAME_TYPE_TEST_RESPONSE, sourceAddress);
                                }
                                break;
                            case BacnetMstpFrameTypes.FRAME_TYPE_BACNET_DATA_NOT_EXPECTING_REPLY:
                            case BacnetMstpFrameTypes.FRAME_TYPE_BACNET_DATA_EXPECTING_REPLY:
                                try
                                {
                                    //signal upper layer
                                    var remoteAddress = new BacnetAddress(BacnetAddressTypes.MSTP, 0, new[] { sourceAddress });
                                    InvokeMessageRecieved(_localBuffer, MSTP.MSTP_HEADER_LENGTH, msgLength, remoteAddress);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error("Exception in MessageRecieved event", ex);
                                }
                                if (frameType == BacnetMstpFrameTypes.FRAME_TYPE_BACNET_DATA_EXPECTING_REPLY)
                                {
                                    _replySource = sourceAddress;
                                    _reply = null;
                                    _replyMutex.Reset();
                                    return StateChanges.ReceivedDataNeedingReply;
                                }
                                break;
                        }
                    }
                }
                finally
                {
                    RemoveCurrentMessage(msgLength);
                }
            }
            else if (status == GetMessageStatus.Timeout)
            {
                /* GenerateToken */
                _ps = (byte)((SourceAddress + 1) % (MaxMaster + 1));
                _ns = (byte)SourceAddress;
                _tokenCount = 0;
                return StateChanges.GenerateToken;
            }
            else if (status == GetMessageStatus.ConnectionClose)
            {
                Log.Debug("No connection");
            }
            else if (status == GetMessageStatus.ConnectionError)
            {
                Log.Debug("Connection Error");
            }
            else
            {
                Log.Debug("Garbage");
            }
        }

        return StateChanges.Reset;
    }

    private StateChanges AnswerDataRequest()
    {
        if (_replyMutex.WaitOne(T_REPLY_DELAY))
        {
            SendFrame(_reply);
            lock (_sendQueue)
                _sendQueue.Remove(_reply);
            return StateChanges.Reply;
        }
        SendFrame(BacnetMstpFrameTypes.FRAME_TYPE_REPLY_POSTPONED, _replySource);
        return StateChanges.DeferredReply;
    }

    private StateChanges Initialize()
    {
        _tokenCount = MaxPoll; /* cause a Poll For Master to be sent when this node first receives the token */
        _frameCount = 0;
        _soleMaster = false;
        _ns = (byte)SourceAddress;
        _ps = (byte)SourceAddress;
        return StateChanges.DoneInitializing;
    }

    private void MstpThread()
    {
        try
        {
            var stateChange = StateChanges.Reset;

            while (_port != null)
            {
                Log.Debug(stateChange);

                switch (stateChange)
                {
                    case StateChanges.Reset:
                        stateChange = Initialize();
                        break;
                    case StateChanges.DoneInitializing:
                    case StateChanges.ReceivedUnexpectedFrame:
                    case StateChanges.Reply:
                    case StateChanges.DeferredReply:
                    case StateChanges.SawTokenUser:
                        stateChange = Idle();
                        break;
                    case StateChanges.GenerateToken:
                    case StateChanges.FindNewSuccessor:
                    case StateChanges.SendMaintenancePFM:
                    case StateChanges.SoleMasterRestartMaintenancePFM:
                    case StateChanges.NextStationUnknown:
                        stateChange = PollForMaster();
                        break;
                    case StateChanges.DoneWithPFM:
                    case StateChanges.ResetMaintenancePFM:
                    case StateChanges.ReceivedReplyToPFM:
                    case StateChanges.SendToken:
                        stateChange = PassToken();
                        break;
                    case StateChanges.ReceivedDataNeedingReply:
                        stateChange = AnswerDataRequest();
                        break;
                    case StateChanges.ReceivedToken:
                    case StateChanges.SoleMaster:
                    case StateChanges.DeclareSoleMaster:
                    case StateChanges.SendAnotherFrame:
                        stateChange = UseToken();
                        break;
                    case StateChanges.NothingToSend:
                    case StateChanges.SendNoWait:
                    case StateChanges.ReplyTimeOut:
                    case StateChanges.InvalidFrame:
                    case StateChanges.ReceivedReply:
                    case StateChanges.ReceivedPostpone:
                        stateChange = DoneWithToken();
                        break;
                    case StateChanges.SendAndWait:
                        stateChange = WaitForReply();
                        break;
                }
            }
            Log.Debug("MSTP thread is closing down");
        }
        catch (Exception ex)
        {
            Log.Error("Exception in MSTP thread", ex);
        }

        IsRunning = false;
    }

    private void RemoveGarbage()
    {
        //scan for preambles
        for (var i = 0; i < _localOffset - 1; i++)
        {
            if (_localBuffer[i] != MSTP.MSTP_PREAMBLE1 || _localBuffer[i + 1] != MSTP.MSTP_PREAMBLE2)
                continue;

            if (i <= 0)
                return;

            //move back
            Array.Copy(_localBuffer, i, _localBuffer, 0, _localOffset - i);
            _localOffset -= i;
            Log.Debug("Garbage");
            return;
        }

        //one preamble?
        if (_localOffset > 0 && _localBuffer[_localOffset - 1] == MSTP.MSTP_PREAMBLE1)
        {
            if (_localOffset != 1)
            {
                _localBuffer[0] = MSTP.MSTP_PREAMBLE1;
                _localOffset = 1;
                Log.Debug("Garbage");
            }
            return;
        }

        //no preamble?
        if (_localOffset > 0)
        {
            _localOffset = 0;
            Log.Debug("Garbage");
        }
    }

    private GetMessageStatus GetNextMessage(int timeoutMs, out BacnetMstpFrameTypes frameType,
        out byte destinationAddress, out byte sourceAddress, out int msgLength)
    {
        int timeout;

        frameType = BacnetMstpFrameTypes.FRAME_TYPE_TOKEN;
        destinationAddress = 0;
        sourceAddress = 0;
        msgLength = 0;

        //fetch header
        while (_localOffset < MSTP.MSTP_HEADER_LENGTH)
        {
            if (_port == null) return GetMessageStatus.ConnectionClose;

            timeout = _localOffset > 0
                ? T_FRAME_ABORT // set sub timeout
                : timeoutMs; // set big silence timeout

            //read 
            var rx = _port.Read(_localBuffer, _localOffset, MSTP.MSTP_HEADER_LENGTH - _localOffset, timeout);
            if (rx == -ETIMEDOUT)
            {
                //drop message
                var status = _localOffset == 0 ? GetMessageStatus.Timeout : GetMessageStatus.SubTimeout;
                _localBuffer[0] = 0xFF;
                RemoveGarbage();
                return status;
            }
            if (rx < 0)
            {
                //drop message
                _localBuffer[0] = 0xFF;
                RemoveGarbage();
                return GetMessageStatus.ConnectionError;
            }
            if (rx == 0)
            {
                //drop message
                _localBuffer[0] = 0xFF;
                RemoveGarbage();
                return GetMessageStatus.ConnectionClose;
            }
            _localOffset += rx;

            //remove paddings & garbage
            RemoveGarbage();
        }

        //decode
        if (MSTP.Decode(_localBuffer, 0, _localOffset, out frameType, out destinationAddress,
                out sourceAddress, out msgLength) < 0)
        {
            //drop message
            _localBuffer[0] = 0xFF;
            RemoveGarbage();
            return GetMessageStatus.DecodeError;
        }

        //valid length?
        var fullMsgLength = msgLength + MSTP.MSTP_HEADER_LENGTH + (msgLength > 0 ? 2 : 0);
        if (msgLength > MaxBufferLength)
        {
            //drop message
            _localBuffer[0] = 0xFF;
            RemoveGarbage();
            return GetMessageStatus.DecodeError;
        }

        //fetch data
        if (msgLength > 0)
        {
            timeout = T_FRAME_ABORT; //set sub timeout
            while (_localOffset < fullMsgLength)
            {
                //read 
                var rx = _port.Read(_localBuffer, _localOffset, fullMsgLength - _localOffset, timeout);
                if (rx == -ETIMEDOUT)
                {
                    //drop message
                    var status = _localOffset == 0 ? GetMessageStatus.Timeout : GetMessageStatus.SubTimeout;
                    _localBuffer[0] = 0xFF;
                    RemoveGarbage();
                    return status;
                }
                if (rx < 0)
                {
                    //drop message
                    _localBuffer[0] = 0xFF;
                    RemoveGarbage();
                    return GetMessageStatus.ConnectionError;
                }
                if (rx == 0)
                {
                    //drop message
                    _localBuffer[0] = 0xFF;
                    RemoveGarbage();
                    return GetMessageStatus.ConnectionClose;
                }
                _localOffset += rx;
            }

            //verify data crc
            if (
                MSTP.Decode(_localBuffer, 0, _localOffset, out frameType, out destinationAddress,
                    out sourceAddress, out msgLength) < 0)
            {
                //drop message
                _localBuffer[0] = 0xFF;
                RemoveGarbage();
                return GetMessageStatus.DecodeError;
            }
        }

        //signal frame event
        if (FrameRecieved != null)
        {
            var frameTypeCopy = frameType;
            var destinationAddressCopy = destinationAddress;
            var sourceAddressCopy = sourceAddress;
            var msgLengthCopy = msgLength;

            ThreadPool.QueueUserWorkItem(
                o => { FrameRecieved(this, frameTypeCopy, destinationAddressCopy, sourceAddressCopy, msgLengthCopy); }, null);
        }

        Log.Debug($"{frameType} {destinationAddress:X2}");

        //done
        return GetMessageStatus.Good;
    }

    private class MessageFrame
    {
        public readonly byte[] Data;
        public readonly int DataLength;
        public readonly byte DestinationAddress;
        public readonly BacnetMstpFrameTypes FrameType;
        public readonly ManualResetEvent SendMutex;

        public MessageFrame(BacnetMstpFrameTypes frameType, byte destinationAddress, byte[] data, int dataLength)
        {
            FrameType = frameType;
            DestinationAddress = destinationAddress;
            Data = data;
            DataLength = dataLength;
            SendMutex = new ManualResetEvent(false);
        }
    }

    private enum StateChanges
    {
        /* Initializing */
        Reset,
        DoneInitializing,

        /* Idle, NoToken */
        GenerateToken,
        ReceivedDataNeedingReply,
        ReceivedToken,

        /* PollForMaster */
        ReceivedUnexpectedFrame, //also from WaitForReply
        DoneWithPFM,
        ReceivedReplyToPFM,
        SoleMaster, //also from DoneWithToken
        DeclareSoleMaster,

        /* UseToken */
        SendAndWait,
        NothingToSend,
        SendNoWait,

        /* DoneWithToken */
        SendToken,
        ResetMaintenancePFM,
        SendMaintenancePFM,
        SoleMasterRestartMaintenancePFM,
        SendAnotherFrame,
        NextStationUnknown,

        /* WaitForReply */
        ReplyTimeOut,
        InvalidFrame,
        ReceivedReply,
        ReceivedPostpone,

        /* PassToken */
        FindNewSuccessor,
        SawTokenUser,

        /* AnswerDataRequest */
        Reply,
        DeferredReply
    }

    #region " Sniffer mode "

    // Used in Sniffer only mode
    public delegate void RawMessageReceivedHandler(byte[] buffer, int offset, int lenght);

    public event RawMessageReceivedHandler RawMessageRecieved;

    public void StartSpyMode()
    {
        if (_port == null) return;
        _port.Open();

        var th = new Thread(MstpThreadSniffer) { IsBackground = true };
        th.Start();
    }

    // Just Sniffer mode, no Bacnet activity generated here
    // Modif FC
    private void MstpThreadSniffer()
    {
        while (true)
        {
            try
            {
                var status = GetNextMessage(T_NO_TOKEN, out _, out _, out _, out var msgLength);

                switch (status)
                {
                    case GetMessageStatus.ConnectionClose:
                        _port = null;
                        return;

                    case GetMessageStatus.Good:
                        // frame event client ?
                        if (RawMessageRecieved != null)
                        {
                            var length = msgLength + MSTP.MSTP_HEADER_LENGTH + (msgLength > 0 ? 2 : 0);

                            // Array copy
                            // after that it could be put asynchronously another time in the Main message loop
                            // without any problem
                            var packet = new byte[length];
                            Array.Copy(_localBuffer, 0, packet, 0, length);

                            // No need to use the thread pool, if the pipe is too slow
                            // frames task list will grow infinitly
                            RawMessageRecieved(packet, 0, length);
                        }
                        RemoveCurrentMessage(msgLength);
                        break;
                }
            }
            catch
            {
                _port = null;
            }
        }
    }

    #endregion
}
