namespace Capri.Core;

/// <summary>
/// Linearizable microphone/call session generation tokens. A prior session
/// cannot deliver delayed audio or errors into a newly armed session, even if
/// Start and Stop race on different native audio threads.
/// </summary>
public sealed class AudioCaptureSessionGate
{
    private readonly object _transitions = new();
    private long _generation;
    private long _active;

    public long Begin()
    {
        lock (_transitions)
        {
            var token = ++_generation;
            Interlocked.Exchange(ref _active, token);
            return token;
        }
    }

    public void End()
    {
        lock (_transitions)
        {
            Interlocked.Exchange(ref _active, 0);
            ++_generation;
        }
    }

    // No locks needed on the native SAPI/NAudio worker callback hot path.
    public bool IsCurrent(long token) =>
        token > 0 && Interlocked.Read(ref _active) == token;
}
