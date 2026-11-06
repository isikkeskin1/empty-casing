namespace Capri.Core;

public enum VoiceIntent
{
    Ignore,
    Prompt,
    Wake,
    StopSpeech,
    StopInputs,
    MuteMicrophone
}

public readonly record struct VoiceDecision(VoiceIntent Intent, string Text = "", bool EndOneShot = false);

/// <summary>
/// Routes locally recognized Windows speech. Only an explicit wake prefix, a short opted-in
/// follow-up window, or a time-limited Talk Once arm can produce a prompt.
/// </summary>
public sealed class VoiceTurnRouter
{
    private DateTimeOffset _oneShotUntil;
    private DateTimeOffset _followUpUntil;
    private DateTimeOffset _lastAcceptedAt;
    private string _lastAccepted = "";

    public bool OneShotArmed { get; private set; }
    public bool IsOneShotExpired(DateTimeOffset now) => OneShotArmed && now >= _oneShotUntil;

    public void ArmOneShot(DateTimeOffset now, TimeSpan timeout)
    {
        if (timeout < TimeSpan.FromSeconds(3) || timeout > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        OneShotArmed = true;
        _oneShotUntil = now + timeout;
        _followUpUntil = default;
    }

    public void DisarmOneShot() => OneShotArmed = false;

    public void Reset()
    {
        OneShotArmed = false;
        _followUpUntil = default;
        _lastAcceptedAt = default;
        _lastAccepted = "";
    }

    /// <summary>
    /// Synthetic speech and desktop audio cannot issue arbitrary commands while output is playing.
    /// Only the exact "Capri stop [talking]" phrase may interrupt when separately opted in.
    /// </summary>
    public static bool IsInterruptPhrase(string? utterance)
    {
        var phrase = Normalize(utterance);
        var (wake, request) = SplitWake(phrase);
        return wake && (IsStopSpeech(request) || IsStopInputs(request));
    }

    public VoiceDecision Evaluate(
        string? utterance, DateTimeOffset now, bool allowFollowUps,
        bool outputPlaying, bool allowVoiceInterrupt)
    {
        var phrase = Normalize(utterance);
        if (phrase.Length == 0 || phrase.Length > 400) return default;

        if (IsOneShotExpired(now)) OneShotArmed = false;

        var (wake, request) = SplitWake(phrase);
        if (outputPlaying)
        {
            // Do not treat the companion's own voice as another user request.
            if (!allowVoiceInterrupt || !wake ||
                !(IsStopSpeech(request) || IsStopInputs(request)))
                return default;
        }
        else if (!wake && !OneShotArmed &&
                 !(allowFollowUps && now < _followUpUntil))
        {
            return default;
        }

        var wasOneShot = OneShotArmed;
        if (request.Length == 0 && !wake) request = phrase;
        // In Talk Once, an explicit prefix is optional.
        if (request.Length == 0 && !wake && !wasOneShot) return default;

        var normalizedRequest = request.Trim();
        if (normalizedRequest == _lastAccepted &&
            now - _lastAcceptedAt < TimeSpan.FromSeconds(2))
            return default;

        var kind = request.Length == 0 ? VoiceIntent.Wake :
            IsStopSpeech(request) ? VoiceIntent.StopSpeech :
            IsStopInputs(request) ? VoiceIntent.StopInputs :
            IsMuteMicrophone(request) ? VoiceIntent.MuteMicrophone : VoiceIntent.Prompt;

        _lastAccepted = normalizedRequest;
        _lastAcceptedAt = now;
        if (wasOneShot) OneShotArmed = false;

        // Conversation mode is a sliding, opt-in window: every accepted follow-up
        // refreshes the 20-second period. Voice output still blocks arbitrary
        // recognition, and mute/stop commands end the conversation lease.
        if (kind is VoiceIntent.StopSpeech or VoiceIntent.StopInputs or VoiceIntent.MuteMicrophone)
            _followUpUntil = default;
        else if (allowFollowUps && (wake || now < _followUpUntil) &&
                 kind is VoiceIntent.Wake or VoiceIntent.Prompt)
            _followUpUntil = now.AddSeconds(20);
        else if (!allowFollowUps) _followUpUntil = default;

        return new VoiceDecision(kind, request, wasOneShot);
    }

    private static string Normalize(string? utterance)
    {
        if (string.IsNullOrWhiteSpace(utterance)) return "";
        var parts = utterance.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts).Trim(' ', ',', '.', '!', '?', ':', ';');
    }

    private static (bool Wake, string Request) SplitWake(string phrase)
    {
        if (phrase.StartsWith("hey capri", StringComparison.OrdinalIgnoreCase) &&
            (phrase.Length == 9 || !char.IsLetterOrDigit(phrase[9])))
            return (true, phrase[9..].TrimStart(' ', ',', '.', '!', '?', ':', ';'));
        if (phrase.StartsWith("capri", StringComparison.OrdinalIgnoreCase) &&
            (phrase.Length == 5 || !char.IsLetterOrDigit(phrase[5])))
            return (true, phrase[5..].TrimStart(' ', ',', '.', '!', '?', ':', ';'));
        return (false, phrase);
    }

    private static bool IsStopSpeech(string text) =>
        text.Equals("stop", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("stop talking", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("be quiet", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("quiet", StringComparison.OrdinalIgnoreCase);

    private static bool IsStopInputs(string text) =>
        text.Equals("stop inputs", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("stop all", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("stop everything", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("stop listening and watching", StringComparison.OrdinalIgnoreCase);

    private static bool IsMuteMicrophone(string text) =>
        text.Equals("mute mic", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("mute microphone", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("mute your ears", StringComparison.OrdinalIgnoreCase);
}
