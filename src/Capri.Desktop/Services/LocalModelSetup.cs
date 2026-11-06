using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Capri.Desktop.Services;

/// <summary>Downloads named models through the Ollama CLI; never runs commands from the AI model.</summary>
public static partial class LocalModelSetup
{
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9_.:/-]{0,99}$", RegexOptions.CultureInvariant)]
    private static partial Regex ModelIdPattern();

    public static string OllamaExecutable()
    {
        var local = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe");
        return File.Exists(local) ? local : "ollama.exe";
    }

    public static async Task PullAsync(string model, Action<string> report,
        CancellationToken cancellationToken = default)
    {
        if (!ModelIdPattern().IsMatch(model))
            throw new ArgumentException("Invalid model ID. Use a model name from Ollama.", nameof(model));

        var start = new ProcessStartInfo
        {
            FileName = OllamaExecutable(), RedirectStandardOutput = true,
            RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        start.ArgumentList.Add("pull");
        start.ArgumentList.Add(model);

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Ollama could not start. Install Ollama first.");
        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        });

        var stdout = Task.Run(async () =>
        {
            while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
                if (!string.IsNullOrWhiteSpace(line)) report(line);
        }, cancellationToken);
        var stderr = Task.Run(async () =>
        {
            while (await process.StandardError.ReadLineAsync(cancellationToken) is { } line)
                if (!string.IsNullOrWhiteSpace(line)) report(line);
        }, cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(stdout, stderr);
        if (process.ExitCode != 0)
            throw new InvalidOperationException("Ollama model download failed (exit " + process.ExitCode + ").");
    }
}
