using System.Buffers;

namespace GreatJsonEscape.Utilities;

// Minimal ArrayBufferWriter used for buffering Utf8JsonWriter output without external packages
internal sealed class ArrayBufferWriter<T>(int initialCapacity = 256) : IBufferWriter<T>
{
    private T[] _buffer = new T[initialCapacity];
    private int _index;

    public ReadOnlyMemory<T> WrittenMemory => new(_buffer, 0, _index);
    public ReadOnlySpan<T> WrittenSpan => new(_buffer, 0, _index);

    public void Advance(int count)
    {
        _index += count;
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return new Memory<T>(_buffer, _index, _buffer.Length - _index);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return new Span<T>(_buffer, _index, _buffer.Length - _index);
    }

    private void EnsureCapacity(int sizeHint)
    {
        if (sizeHint == 0) sizeHint = 256;
        if (_buffer.Length - _index >= sizeHint) return;

        var newSize = Math.Max(_buffer.Length * 2, _index + sizeHint);
        Array.Resize(ref _buffer, newSize);
    }
}