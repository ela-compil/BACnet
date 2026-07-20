namespace System.IO.BACnet;

/// <summary>
/// The result of an AtomicReadFile request: the returned file data together with the position it was
/// read from and whether the end of the file was reached (ASHRAE 135 §14.1.1).
/// </summary>
public readonly struct BacnetReadFileResult
{
    public BacnetReadFileResult(int position, uint count, bool endOfFile, byte[] fileBuffer, int fileBufferOffset)
    {
        Position = position;
        Count = count;
        EndOfFile = endOfFile;
        FileBuffer = fileBuffer;
        FileBufferOffset = fileBufferOffset;
    }

    /// <summary>The file position the data was read from.</summary>
    public int Position { get; }

    /// <summary>The number of octets returned.</summary>
    public uint Count { get; }

    /// <summary>Whether the read reached the end of the file.</summary>
    public bool EndOfFile { get; }

    /// <summary>
    /// Buffer holding the returned data; read <see cref="Count"/> octets starting at
    /// <see cref="FileBufferOffset"/>.
    /// </summary>
    public byte[] FileBuffer { get; }

    /// <summary>The offset into <see cref="FileBuffer"/> at which the returned data starts.</summary>
    public int FileBufferOffset { get; }
}
