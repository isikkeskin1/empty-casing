namespace Capri.Core;

/// <summary>
/// Prevents late Windows audio/recognition callbacks from a prior capture
/// session being attributed to a newly started one. Thread safe; it never
/// holds a lock inside NAudio/SAPI callbacks.
/// </summary>
public sealed class AudioCaptureSessionGate
{
    private long _generation;
    private long _active;

    /// <summary>Begin a new, unique session, invalidating all earlier IDs.</summary>
    public long Begin()
    {
        var token = Interlocked.Increment(ref _generation);
        Interlocked.Exchange(ref _active, token);
        return token;
    }

    /// <summary>Invalidate callbacks for the current session immediately.</summary>
    public void End()
    {
        Interlocked.Exchange(ref _active, 0);
        Interlocked.Increment(ref _generation);
    }

    public bool IsCurrent(long token) =>
        token > 0 && Interlocked.Read(ref _active) == token;
}
