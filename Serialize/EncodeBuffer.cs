namespace System.IO.BACnet.Serialize;

public class EncodeBuffer
{
    public byte[] buffer;           //buffer to serialize into
    public int offset;              //offset in buffer ... will go beyond max_offset (so that you may count what's needed)
    public int max_offset;          //don't write beyond this offset 
    public int serialize_counter;   //used with 'min_limit'
    public int min_limit;           //don't write before this limit (used for segmentation)
    public EncodeResult result;
    public bool expandable;

    public EncodeBuffer()
    {
        expandable = true;
        buffer = new byte[128];
        max_offset = buffer.Length - 1;
    }

    public EncodeBuffer(byte[] buffer, int offset)
    {
        if (buffer == null) buffer = new byte[0];
        expandable = false;
        this.buffer = buffer;
        this.offset = offset;
        max_offset = buffer.Length;
    }

    public void Increment()
    {
        if (offset < max_offset)
        {
            if (serialize_counter >= min_limit)
                offset++;
            serialize_counter++;
        }
        else
        {
            if (serialize_counter >= min_limit)
                offset++;
        }
    }

    public void Add(byte b)
    {
        if (offset < max_offset)
        {
            if (serialize_counter >= min_limit)
                buffer[offset] = b;
        }
        else
        {
            if (expandable)
            {
                Array.Resize(ref buffer, buffer.Length * 2);
                max_offset = buffer.Length - 1;
                if (serialize_counter >= min_limit)
                    buffer[offset] = b;
            }
            else
                result |= EncodeResult.NotEnoughBuffer;
        }

        Increment();
    }

    public void Add(byte[] buffer, int count)
    {
        for (var i = 0; i < count; i++)
            Add(buffer[i]);
    }

    public int GetDiff(EncodeBuffer buffer)
    {
        var diff = Math.Abs(buffer.offset - offset);
        diff = Math.Max(Math.Abs(buffer.serialize_counter - serialize_counter), diff);
        return diff;
    }

    public EncodeBuffer Copy()
    {
        return new EncodeBuffer
        {
            buffer = buffer,
            max_offset = max_offset,
            min_limit = min_limit,
            offset = offset,
            result = result,
            serialize_counter = serialize_counter,
            expandable = expandable
        };
    }

    public byte[] ToArray()
    {
        var ret = new byte[offset];
        Array.Copy(buffer, 0, ret, 0, ret.Length);
        return ret;
    }

    public void Reset(int newOffset)
    {
        offset = newOffset;
        serialize_counter = 0;
        result = EncodeResult.Good;
    }

    public override string ToString()
    {
        return offset + " - " + serialize_counter;
    }

    public int GetLength()
    {
        return Math.Min(offset, max_offset);
    }
}
