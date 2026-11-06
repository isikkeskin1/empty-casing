using System.Text;
using System.Windows.Threading;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// Opt-in reader of sanitized, local-only lifecycle events produced by the
/// user's Codex/Claude hooks. Does not read their transcripts or control them.
/// </summary>
public sealed class CodeHuskBridge : IDisposable
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly Action<CodeHuskEvent> _onEvent;
    private readonly List<CodeHuskEvent> _recent = new();
    private readonly string _root;
    private long _offset;
    private string _partial = "";

    public bool Enabled { get; private set; }
    public IReadOnlyList<CodeHuskEvent> Recent => _recent.ToArray();
    public string Root => _root;
    public string EventFile => Path.Combine(_root, "events.ndjson");
    private string LeaseFile => Path.Combine(_root, "active.lease");

    public string Status => !Enabled ? "OFF · no coding events are read" :
        _recent.Count == 0 ? "ON · listening for sanitized Codex / Claude Code hooks" :
        _recent[^1].FriendlyDescription;

    public CodeHuskBridge(Action<CodeHuskEvent> onEvent, string? root = null)
    {
        _onEvent = onEvent;
        _root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CapriDesktop", "husk");
        _timer.Tick += (_, _) => Poll();
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled == Enabled) return;
        _timer.Stop();
        _partial = "";
        _recent.Clear();
        _offset = 0;
        if (!enabled)
        {
            Enabled = false;
            try { File.Delete(LeaseFile); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            // Hooks may remain installed but cannot write without a fresh lease.
            return;
        }
        Directory.CreateDirectory(_root);
        // Skip historical signals from a previous session. Nothing is persisted
        // into Capri's conversation or model memory.
        if (File.Exists(EventFile)) _offset = new FileInfo(EventFile).Length;
        Enabled = true;
        TouchLease();
        _timer.Start();
    }

    private void TouchLease()
    {
        try
        {
            File.WriteAllText(LeaseFile, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                Encoding.ASCII);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public void Poll()
    {
        if (!Enabled) return;
        TouchLease();
        try
        {
            if (!File.Exists(EventFile)) return;
            using var file = new FileStream(EventFile, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            if (file.Length < _offset)
            {
                _offset = 0;
                _partial = "";
            }
            // A malformed or unexpectedly noisy relay cannot cause unbounded IO.
            if (file.Length - _offset > 32768)
            {
                _offset = Math.Max(0, file.Length - 32768);
                _partial = "";
            }
            file.Position = _offset;
            var limit = (int)Math.Min(32768, file.Length - _offset);
            if (limit <= 0) return;
            var bytes = new byte[limit];
            var count = file.Read(bytes, 0, limit);
            if (count <= 0) return;
            _offset += count;
            var parts = (_partial + Encoding.UTF8.GetString(bytes, 0, count)).Split('\n');
            _partial = parts[^1].Length <= 1024 ? parts[^1] : "";
            foreach (var line in parts.Take(parts.Length - 1).TakeLast(50))
            {
                var evt = CodeHuskEvent.Parse(line.TrimEnd('\r'), DateTimeOffset.UtcNow);
                if (evt is null) continue;
                // Ignore exact duplicates from overlapping hook configurations.
                if (_recent.Count > 0 && _recent[^1] == evt) continue;
                _recent.Add(evt);
                if (_recent.Count > 24) _recent.RemoveAt(0);
                _onEvent(evt);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public void Dispose() => SetEnabled(false);
}
