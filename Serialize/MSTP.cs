namespace System.IO.BACnet.Serialize;

public class MSTP
{
    public const byte MSTP_PREAMBLE1 = 0x55;
    public const byte MSTP_PREAMBLE2 = 0xFF;
    public const BacnetMaxAdpu MSTP_MAX_APDU = BacnetMaxAdpu.MAX_APDU480;
    public const byte MSTP_HEADER_LENGTH = 8;

    public static byte CRC_Calc_Header(byte dataValue, byte crcValue)
    {
        var crc = (ushort)(crcValue ^ dataValue);

        /* Exclusive OR the terms in the table (top down) */
        crc = (ushort)(crc ^ (crc << 1) ^ (crc << 2) ^ (crc << 3) ^ (crc << 4) ^ (crc << 5) ^ (crc << 6) ^ (crc << 7));

        /* Combine bits shifted out left hand end */
        return (byte)((crc & 0xfe) ^ ((crc >> 8) & 1));
    }

    public static byte CRC_Calc_Header(byte[] buffer, int offset, int length)
    {
        byte crc = 0xff;
        for (var i = offset; i < offset + length; i++)
            crc = CRC_Calc_Header(buffer[i], crc);
        return (byte)~crc;
    }

    public static ushort CRC_Calc_Data(byte dataValue, ushort crcValue)
    {
        var crcLow = (ushort)((crcValue & 0xff) ^ dataValue);

        /* Exclusive OR the terms in the table (top down) */
        return (ushort)((crcValue >> 8) ^ (crcLow << 8) ^ (crcLow << 3)
            ^ (crcLow << 12) ^ (crcLow >> 4)
            ^ (crcLow & 0x0f) ^ ((crcLow & 0x0f) << 7));
    }

    public static ushort CRC_Calc_Data(byte[] buffer, int offset, int length)
    {
        ushort crc = 0xffff;
        for (var i = offset; i < offset + length; i++)
            crc = CRC_Calc_Data(buffer[i], crc);
        return (ushort)~crc;
    }

    public static int Encode(byte[] buffer, int offset, BacnetMstpFrameTypes frameType, byte destinationAddress, byte sourceAddress, int msgLength)
    {
        buffer[offset + 0] = MSTP_PREAMBLE1;
        buffer[offset + 1] = MSTP_PREAMBLE2;
        buffer[offset + 2] = (byte)frameType;
        buffer[offset + 3] = destinationAddress;
        buffer[offset + 4] = sourceAddress;
        buffer[offset + 5] = (byte)((msgLength & 0xFF00) >> 8);
        buffer[offset + 6] = (byte)((msgLength & 0x00FF) >> 0);
        buffer[offset + 7] = CRC_Calc_Header(buffer, offset + 2, 5);
        if (msgLength > 0)
        {
            //calculate data crc
            var dataCrc = CRC_Calc_Data(buffer, offset + 8, msgLength);
            buffer[offset + 8 + msgLength + 0] = (byte)(dataCrc & 0xFF);  //LSB first!
            buffer[offset + 8 + msgLength + 1] = (byte)(dataCrc >> 8);
        }
        //optional pad (0xFF)
        return MSTP_HEADER_LENGTH + msgLength + (msgLength > 0 ? 2 : 0);
    }

    public static int Decode(byte[] buffer, int offset, int maxLength, out BacnetMstpFrameTypes frameType, out byte destinationAddress, out byte sourceAddress, out int msgLength)
    {
        if (maxLength < MSTP_HEADER_LENGTH)  //not enough data
        {
            frameType = BacnetMstpFrameTypes.FRAME_TYPE_BACNET_DATA_EXPECTING_REPLY; // don't care
            destinationAddress = sourceAddress = 0;   // don't care
            msgLength = 0;
            return -1;
        }

        frameType = (BacnetMstpFrameTypes)buffer[offset + 2];
        destinationAddress = buffer[offset + 3];
        sourceAddress = buffer[offset + 4];
        msgLength = (buffer[offset + 5] << 8) | (buffer[offset + 6] << 0);
        var crcHeader = buffer[offset + 7];
        ushort crcData = 0;

        if (msgLength > 0)
        {
            if (offset + 8 + msgLength + 1 >= buffer.Length)
                return -1;

            crcData = (ushort)((buffer[offset + 8 + msgLength + 1] << 8) | (buffer[offset + 8 + msgLength + 0] << 0));
        }

        if (buffer[offset + 0] != MSTP_PREAMBLE1)
            return -1;

        if (buffer[offset + 1] != MSTP_PREAMBLE2)
            return -1;

        if (CRC_Calc_Header(buffer, offset + 2, 5) != crcHeader)
            return -1;

        if (msgLength > 0 && maxLength >= MSTP_HEADER_LENGTH + msgLength + 2 && CRC_Calc_Data(buffer, offset + 8, msgLength) != crcData)
            return -1;

        return 8 + msgLength + (msgLength > 0 ? 2 : 0);
    }
}
