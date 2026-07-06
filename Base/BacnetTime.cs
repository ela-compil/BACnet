namespace System.IO.BACnet;

/// <summary>
/// Time ::= [APPLICATION 11] OCTET STRING (SIZE(4)) - hour, minute, second, hundredths - where
/// any octet may individually be X'FF' = unspecified (ASHRAE 135-2016 §20.2.13). Unlike a
/// DateTime this keeps partially-wildcarded times lossless: decoded TIME values carry a
/// BacnetTime whenever the octets cannot be represented faithfully as a DateTime.
/// </summary>
public struct BacnetTime : ASN1.IEncode, ASN1.IDecode
{
    public byte hour;       /* 0..23, 255 any */
    public byte minute;     /* 0..59, 255 any */
    public byte second;     /* 0..59, 255 any */
    public byte hundredths; /* 0..99, 255 any */

    public BacnetTime(byte hour, byte minute, byte second = 0, byte hundredths = 0)
    {
        this.hour = hour;
        this.minute = minute;
        this.second = second;
        this.hundredths = hundredths;
    }

    public BacnetTime(TimeSpan timeOfDay)
    {
        hour = (byte)timeOfDay.Hours;
        minute = (byte)timeOfDay.Minutes;
        second = (byte)timeOfDay.Seconds;
        hundredths = (byte)(timeOfDay.Milliseconds / 10);
    }

    /// <summary>The fully-unspecified time, matching any time of day.</summary>
    public static readonly BacnetTime Any = new BacnetTime(255, 255, 255, 255);

    /// <summary>Like the TimeSpan constructor, but maps the <see cref="ASN1.BACNET_TIME_WILDCARD"/>
    /// marker to <see cref="Any"/>.</summary>
    public static BacnetTime FromDateTime(DateTime time)
    {
        return time == ASN1.BACNET_TIME_WILDCARD ? Any : new BacnetTime(time.TimeOfDay);
    }

    public bool IsFullySpecified => hour <= 23 && minute <= 59 && second <= 59 && hundredths <= 99;

    public bool IsFullyUnspecified => hour == 255 && minute == 255 && second == 255 && hundredths == 255;

    /// <summary>Unspecified components count as zero, mirroring the tolerant DateTime decode.</summary>
    public TimeSpan ToTimeSpan()
    {
        return new TimeSpan(0, hour <= 23 ? hour : 0, minute <= 59 ? minute : 0, second <= 59 ? second : 0,
            hundredths <= 99 ? hundredths * 10 : 0);
    }

    /// <summary>Every specified octet must match independently (the §20.2.13 wildcard semantics).</summary>
    public bool IsAFittingTime(TimeSpan timeOfDay)
    {
        if (hour != 255 && timeOfDay.Hours != hour)
            return false;
        if (minute != 255 && timeOfDay.Minutes != minute)
            return false;
        if (second != 255 && timeOfDay.Seconds != second)
            return false;
        if (hundredths != 255 && timeOfDay.Milliseconds / 10 != hundredths)
            return false;

        return true;
    }

    public void Encode(EncodeBuffer buffer)
    {
        buffer.Add(hour);
        buffer.Add(minute);
        buffer.Add(second);
        buffer.Add(hundredths);
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        if (offset + 4 > count)
            return -1;

        hour = buffer[offset];
        minute = buffer[offset + 1];
        second = buffer[offset + 2];
        hundredths = buffer[offset + 3];
        return 4;
    }

    public override string ToString()
    {
        return (hour != 255 ? hour.ToString("00") : "**") + ":" +
               (minute != 255 ? minute.ToString("00") : "**") + ":" +
               (second != 255 ? second.ToString("00") : "**") + "." +
               (hundredths != 255 ? hundredths.ToString("00") : "**");
    }
}
