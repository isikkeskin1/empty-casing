using System.IO;
using System.Text;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// Only changes the user's own Codex/Claude hook JSON after explicit consent.
/// Backs up existing files and merges our marked handlers alongside other hooks.
/// The relay discards all payloads (including prompts, tool inputs, filenames).
/// </summary>
public sealed class CodeHuskHookInstaller
{
    private readonly string _root;
    private const string RelayFileName = "CapriHuskRelay.ps1";
    public string RelayPath => Path.Combine(_root, RelayFileName);

    public CodeHuskHookInstaller(string root) => _root = root;

    public string ConfigPath(string agent)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
            throw new InvalidOperationException("Windows user profile is unavailable.");
        return agent switch
        {
            "codex" => Path.Combine(home, ".codex", "hooks.json"),
            "claude" => Path.Combine(home, ".claude", "settings.json"),
            _ => throw new ArgumentException("Unknown coding assistant.", nameof(agent))
        };
    }

    public bool IsInstalled(string agent)
    {
        var path = ConfigPath(agent);
        if (!File.Exists(path)) return false;
        try { return CodeHuskHookConfig.ContainsOwnHooks(File.ReadAllText(path)); }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public string Install(string agent)
    {
        var path = ConfigPath(agent);
        var old = File.Exists(path) ? File.ReadAllText(path) : null;
        var updated = CodeHuskHookConfig.Merge(old, RelayPath, agent);
        Directory.CreateDirectory(_root);
        // Overwrite only our own small local relay script, never external scripts.
        File.WriteAllText(RelayPath, RelayScript, new UTF8Encoding(false));
        ApplyWithBackup(path, old, updated);
        return (agent == "codex" ? "Codex" : "Claude Code") +
               " read-only hooks installed. Restart the coding assistant so " +
               "it loads the new settings. Turn on 'Watch coding assistants' in Capri. " +
               "No code-run or approval permissions were granted.";
    }

    public string Uninstall(string agent)
    {
        var path = ConfigPath(agent);
        if (!File.Exists(path)) return "No Capri hooks were found for " + agent + ".";
        var old = File.ReadAllText(path);
        if (!CodeHuskHookConfig.ContainsOwnHooks(old))
            return "No Capri hooks were found for " + agent + ".";
        var updated = CodeHuskHookConfig.Remove(old);
        ApplyWithBackup(path, old, updated);
        return "Removed only Capri's " + agent + " hooks. Other hooks were preserved. " +
               "Restart your coding assistant to unload them.";
    }

    private static void ApplyWithBackup(string path, string? old, string updated)
    {
        if (old == updated) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (old is not null)
        {
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffff");
            File.Copy(path, path + ".capri-backup-" + stamp + ".json");
        }
        var temporary = path + ".capri-tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, updated, new UTF8Encoding(false));
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    /// <summary>
    /// Minimal PowerShell command hook: zero stdout, no input-body processing,
    /// no subprocesses, no network and no trust/approval decisions. A fresh lease
    /// from the owner-armed Capri app is required or it exits immediately.
    /// </summary>
    private const string RelayScript = """
param(
    [ValidateSet("codex","claude")][string]$Agent,
    [ValidateSet("prompt","working","approval","complete","error","subagent","compact")][string]$Event,
    [string]$CapriHookId
)
$ErrorActionPreference = 'SilentlyContinue'
try {
    if ($CapriHookId -ne 'capri-desktop-husk-v1') { exit 0 }
    $root = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'CapriDesktop\husk'
    $lease = Join-Path $root 'active.lease'
    if (-not [IO.File]::Exists($lease)) { exit 0 }
    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $tick = [long]0
    $raw = [IO.File]::ReadAllText($lease).Trim()
    if (-not [long]::TryParse($raw, [ref]$tick)) { exit 0 }
    if ($now - $tick -lt 0 -or $now - $tick -gt 10) { exit 0 }

    # Agent/Event are validated literals. Never read stdin or CLI tool payloads.
    $line = '{"agent":"' + $Agent + '","event":"' + $Event +
            '","timestamp":' + $now + '}' + "`n"
    $file = Join-Path $root 'events.ndjson'
    $stream = [IO.FileStream]::new(
        $file, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
    try {
        if ($stream.Length -lt 524288) {
            $bytes = [Text.Encoding]::UTF8.GetBytes($line)
            $stream.Write($bytes, 0, $bytes.Length)
        }
    }
    finally { $stream.Dispose() }
} catch { }
exit 0
""";
}
