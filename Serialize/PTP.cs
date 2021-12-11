namespace System.IO.BACnet.Serialize;

public class PTP
{
    public const byte PTP_PREAMBLE1 = 0x55;
    public const byte PTP_PREAMBLE2 = 0xFF;
    public const byte PTP_GREETING_PREAMBLE1 = 0x42;
    public const byte PTP_GREETING_PREAMBLE2 = 0x41;
    public const BacnetMaxAdpu PTP_MAX_APDU = BacnetMaxAdpu.MAX_APDU480;
    public const byte PTP_HEADER_LENGTH = 6;

    public static int Encode(byte[] buffer, int offset, BacnetPtpFrameTypes frameType, int msgLength)
    {
        buffer[offset + 0] = PTP_PREAMBLE1;
        buffer[offset + 1] = PTP_PREAMBLE2;
        buffer[offset + 2] = (byte)frameType;
        buffer[offset + 3] = (byte)((msgLength & 0xFF00) >> 8);
        buffer[offset + 4] = (byte)((msgLength & 0x00FF) >> 0);
        buffer[offset + 5] = MSTP.CRC_Calc_Header(buffer, offset + 2, 3);
        if (msgLength > 0)
        {
            //calculate data crc
            var dataCrc = MSTP.CRC_Calc_Data(buffer, offset + 6, msgLength);
            buffer[offset + 6 + msgLength + 0] = (byte)(dataCrc & 0xFF);  //LSB first!
            buffer[offset + 6 + msgLength + 1] = (byte)(dataCrc >> 8);
        }
        return PTP_HEADER_LENGTH + msgLength + (msgLength > 0 ? 2 : 0);
    }

    public static int Decode(byte[] buffer, int offset, int maxLength, out BacnetPtpFrameTypes frameType, out int msgLength)
    {
        if (maxLength < PTP_HEADER_LENGTH) // not enough data
        {
            frameType = BacnetPtpFrameTypes.FRAME_TYPE_CONNECT_REQUEST; // don't care
            msgLength = 0;
            return -1;     //not enough data
        }

        frameType = (BacnetPtpFrameTypes)buffer[offset + 2];
        msgLength = (buffer[offset + 3] << 8) | (buffer[offset + 4] << 0);
        var crcHeader = buffer[offset + 5];
        ushort crcData = 0;

        if (msgLength > 0)
        {
            if (offset + 6 + msgLength + 1 >= buffer.Length)
                return -1;

            crcData = (ushort)((buffer[offset + 6 + msgLength + 1] << 8) | (buffer[offset + 6 + msgLength + 0] << 0));
        }

        if (buffer[offset + 0] != PTP_PREAMBLE1)
            return -1;

        if (buffer[offset + 1] != PTP_PREAMBLE2)
            return -1;

        if (MSTP.CRC_Calc_Header(buffer, offset + 2, 3) != crcHeader)
            return -1;

        if (msgLength > 0 && maxLength >= PTP_HEADER_LENGTH + msgLength + 2 && MSTP.CRC_Calc_Data(buffer, offset + 6, msgLength) != crcData)
            return -1;

        return 8 + msgLength + (msgLength > 0 ? 2 : 0);
    }

}
