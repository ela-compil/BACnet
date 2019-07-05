namespace System.IO.BACnet.Serialize
{
    public static class FileAccessServices
    {
        public static void EncodeAtomicReadFile(EncodeBuffer buffer, bool isStream, BacnetObjectId objectId, int position, uint count)
        {
            ASN1.encode_application_object_id(buffer, objectId.Type, objectId.Instance);
            var tagNumber = (byte)(isStream ? 0 : 1);
            ASN1.encode_opening_tag(buffer, tagNumber);
            ASN1.encode_application_signed(buffer, position);
            ASN1.encode_application_unsigned(buffer, count);
            ASN1.encode_closing_tag(buffer, tagNumber);
        }

        public static int DecodeAtomicReadFile(byte[] buffer, int offset, int apduLen, out bool isStream, out BacnetObjectId objectId, out int position, out uint count)
        {
            objectId = default(BacnetObjectId);

            var len = 0;
            int tagLen;

            isStream = true;
            position = -1;
            count = 0;

            len = ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)
                return -1;

            len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
            objectId = new BacnetObjectId(type, instance);

            if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
            {
                /* a tag number is not extended so only one octet */
                len++;
                /* fileStartPosition */
                tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += tagLen;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
                /* requestedOctetCount */
                tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += tagLen;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                    return -1;
                len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out count);
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                    return -1;
                /* a tag number is not extended so only one octet */
                len++;
            }
            else if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
            {
                isStream = false;
                /* a tag number is not extended so only one octet */
                len++;
                /* fileStartRecord */
                tagLen =
                    ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber,
                        out lenValueType);
                len += tagLen;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
                /* RecordCount */
                tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += tagLen;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                    return -1;
                len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out count);
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                    return -1;
                /* a tag number is not extended so only one octet */
                len++;
            }
            else
                return -1;

            return len;
        }

        public static void EncodeAtomicWriteFile(EncodeBuffer buffer, bool isStream, BacnetObjectId objectId, int position, uint blockCount, byte[][] blocks, int[] counts)
        {
            ASN1.encode_application_object_id(buffer, objectId.Type, objectId.Instance);
            var tagNumber = (byte)(isStream ? 0 : 1);

            ASN1.encode_opening_tag(buffer, tagNumber);
            ASN1.encode_application_signed(buffer, position);

            if (isStream)
            {
                ASN1.encode_application_octet_string(buffer, blocks[0], 0, counts[0]);
            }
            else
            {
                ASN1.encode_application_unsigned(buffer, blockCount);
                for (var i = 0; i < blockCount; i++)
                    ASN1.encode_application_octet_string(buffer, blocks[i], 0, counts[i]);
            }

            ASN1.encode_closing_tag(buffer, tagNumber);
        }

        public static int DecodeAtomicWriteFile(byte[] buffer, int offset, int apduLen, out bool isStream, out BacnetObjectId objectId, out int position, out uint blockCount, out byte[][] blocks, out int[] counts)
        {
            var len = 0;
            int tagLen;

            objectId = default;
            isStream = true;
            position = -1;
            blockCount = 0;
            blocks = null;
            counts = null;

            len = ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)
                return -1;

            len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
            objectId = new BacnetObjectId(type, instance);

            if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
            {
                /* a tag number of 2 is not extended so only one octet */
                len++;
                /* fileStartPosition */
                tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += tagLen;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
                /* fileData */
                tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += tagLen;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                    return -1;
                blockCount = 1;
                blocks = new byte[1][];
                blocks[0] = new byte[lenValueType];
                counts = new[] { (int)lenValueType };
                len += ASN1.decode_octet_string(buffer, offset + len, apduLen, blocks[0], 0, lenValueType);
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                    return -1;
                /* a tag number is not extended so only one octet */
                len++;
            }
            else if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
            {
                isStream = false;
                /* a tag number is not extended so only one octet */
                len++;
                /* fileStartRecord */
                tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += tagLen;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
                /* returnedRecordCount */
                tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += tagLen;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                    return -1;
                len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out blockCount);
                /* fileData */
                blocks = new byte[blockCount][];
                counts = new int[blockCount];
                for (var i = 0; i < blockCount; i++)
                {
                    tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                    len += tagLen;
                    blocks[i] = new byte[lenValueType];
                    counts[i] = (int)lenValueType;
                    if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                        return -1;
                    len += ASN1.decode_octet_string(buffer, offset + len, apduLen, blocks[i], 0, lenValueType);
                }
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                    return -1;
                /* a tag number is not extended so only one octet */
                len++;
            }
            else
                return -1;

            return len;
        }

        // TODO: use overloads to get rid of params that don't make sense (stream vs. record)
        public static void EncodeAtomicReadFileAcknowledge(EncodeBuffer buffer, bool isStream, bool endOfFile, int position, uint blockCount, byte[][] blocks, int[] counts)
        {
            ASN1.encode_application_boolean(buffer, endOfFile);
            var tagNumber = (byte)(isStream ? 0 : 1);
            ASN1.encode_opening_tag(buffer, tagNumber);
            ASN1.encode_application_signed(buffer, position);

            if (isStream)
            {
                ASN1.encode_application_octet_string(buffer, blocks[0], 0, counts[0]);
            }
            else
            {
                ASN1.encode_application_unsigned(buffer, blockCount);
                for (var i = 0; i < blockCount; i++)
                    ASN1.encode_application_octet_string(buffer, blocks[i], 0, counts[i]);
            }

            ASN1.encode_closing_tag(buffer, tagNumber);
        }

        public static int DecodeAtomicReadFileAcknowledge(byte[] buffer, int offset, int apduLen, out bool endOfFile, out bool isStream, out int position, out uint count, out byte[] targetBuffer, out int targetOffset)
        {
            var len = 0;

            endOfFile = false;
            isStream = false;
            position = -1;
            count = 0;
            targetBuffer = null;
            targetOffset = -1;

            len = ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)
                return -1;

            endOfFile = lenValueType > 0;
            if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
            {
                isStream = true;
                /* a tag number is not extended so only one octet */
                len++;
                /* fileStartPosition */
                var tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += tagLen;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
                /* fileData */
                tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += tagLen;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                    return -1;
                //len += ASN1.decode_octet_string(buffer, offset + len, buffer.Length, target_buffer, target_offset, len_value_type);
                targetBuffer = buffer;
                targetOffset = offset + len;
                count = lenValueType;
                len += (int)count;
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                    return -1;
                /* a tag number is not extended so only one octet */
                len++;
            }
            else if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
            {
                throw new NotImplementedException("Non stream File transfers are not supported");
                //* a tag number is not extended so only one octet */
                //len++;
                //* fileStartRecord */
                //tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                //len += tag_len;
                //if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                //    return -1;
                //len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
                //* returnedRecordCount */
                //tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                //len += tag_len;
                //if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                //    return -1;
                //len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out count);
                //for (i = 0; i < count; i++)
                //{
                //    /* fileData */
                //    tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                //    len += tag_len;
                //    if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                //        return -1;
                //    len += ASN1.decode_octet_string(buffer, offset + len, buffer.Length, target_buffer, target_offset, len_value_type);
                //}
                //if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                //    return -1;
                //* a tag number is not extended so only one octet */
                //len++;
            }
            else
                return -1;

            return len;
        }

        public static void EncodeAtomicWriteFileAcknowledge(EncodeBuffer buffer, bool isStream, int position)
        {
            ASN1.encode_context_signed(buffer, (byte)(isStream ? 0 : 1), position);
        }

        public static int DecodeAtomicWriteFileAcknowledge(byte[] buffer, int offset, int apduLen, out bool isStream, out int position)
        {
            var len = 0;

            isStream = false;
            position = 0;

            len = ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            switch (tagNumber)
            {
                case 0:
                    isStream = true;
                    len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
                    break;

                case 1:
                    len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
                    break;

                default:
                    return -1;
            }

            return len;
        }
    }
}
