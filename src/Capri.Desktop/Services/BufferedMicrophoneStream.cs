using System.IO;
using System.Collections.Concurrent;

namespace Capri.Desktop.Services;

/// <summary>
/// Thread-safe bounded PCM stream for Windows Speech Recognition. Capture callbacks never block;
/// if the speech engine lags, old audio is dropped rather than unbounded buffering.
/// </summary>
internal sealed class BufferedMicrophoneStream : Stream
{
    private readonly BlockingCollection<byte[]> _queue = new(40);
    private byte[]? _current;
    private int _offset;
    private volatile bool _closed;
    private long _readPosition;

    public void Push(byte[] bytes, int count)
    {
        if (_closed || count <= 0) return;
        var copy = new byte[count];
        Buffer.BlockCopy(bytes, 0, copy, 0, count);
        try
        {
            if (_queue.TryAdd(copy)) return;
            _queue.TryTake(out _);
            _queue.TryAdd(copy);
        }
        catch (InvalidOperationException) { } // Recording stopped while the callback was running.
        catch (ObjectDisposedException) { } // SAPI/capture raced with an explicit STOP.
    }

    public void Complete()
    {
        _closed = true;
        try
        {
            if (!_queue.IsAddingCompleted) _queue.CompleteAdding();
        }
        catch (ObjectDisposedException) { } // A second STOP/callback may arrive after teardown.
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0 || count < 0 || offset > buffer.Length - count)
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (count == 0) return 0;
        try
        {
            if (_current is null || _offset >= _current.Length)
            {
                if (!_queue.TryTake(out _current, Timeout.Infinite)) return 0;
                _offset = 0;
            }
            if (_current is null) return 0;
            var copied = Math.Min(count, _current.Length - _offset);
            Buffer.BlockCopy(_current, _offset, buffer, offset, copied);
            _offset += copied;
            Interlocked.Add(ref _readPosition, copied);
            return copied;
        }
        catch (InvalidOperationException) { return 0; }
        catch (ObjectDisposedException) { return 0; } // Normal when STOP closes a SAPI read.
    }

    public override bool CanRead => !_closed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    // System.Speech's SAPI COM wrapper queries Length at construction and
    // Position during Read, even when the underlying PCM stream is live.
    // An effectively unbounded length allows recording to continue until
    // Complete() signals end-of-input. We never buffer that amount of data.
    public override long Length => long.MaxValue;
    public override long Position
    {
        get => Interlocked.Read(ref _readPosition);
        set => throw new NotSupportedException("Live microphone audio cannot be repositioned.");
    }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        Complete();
        if (disposing)
        {
            // A COM recognition worker may still be blocked in Read during
            // StopListening. Complete wakes it; disposing the collection is
            // safe now because concurrent readers tolerate disposal.
            _queue.Dispose();
        }
        base.Dispose(disposing);
    }
}
