using System.IO;
using System.Security.Cryptography;
using System.Text;
using NAudio.Wave;
using Whisper.net;

namespace Capri.Desktop.Services;

/// <summary>
/// Optional on-device multilingual speech-to-text. Recording starts ONLY after
/// the owner presses Talk Once, lasts at most seven seconds, and is held in
/// RAM. No raw microphone audio is uploaded or saved.
/// </summary>
public static class LocalWhisperTranscriber
{
    // Official whisper.cpp ggml-base multilingual model; pinned by SHA256.
    // This is the MULTILINGUAL base, not English-only base.en.
    private static readonly Uri ModelUri = new(
        "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin");
    private const long ModelLength = 147_951_465;
    private const string ModelSha256 =
        "60ed5bc3dd14eea856493d334349b405782ddcaf0028d4b5df4088345fba2efe";

    public static string ModelPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CapriDesktop", "models", "ggml-base.bin");

    public static bool ModelInstalled =>
        File.Exists(ModelPath) && new FileInfo(ModelPath).Length == ModelLength;

    public static async Task InstallModelAsync(
        IProgress<double>? progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ModelPath)!);
        if (ModelInstalled) return;
        var pending = ModelPath + ".partial";
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(15)
            };
            using var response = await client.GetAsync(ModelUri,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long length &&
                length != ModelLength)
                throw new InvalidDataException(
                    "Unexpected model download size. Refusing unverified file.");

            await using (var remote = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var file = new FileStream(pending, FileMode.Create,
                             FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true))
            {
                var buffer = new byte[128 * 1024];
                long written = 0;
                int n;
                while ((n = await remote.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    written += n;
                    if (written > ModelLength)
                        throw new InvalidDataException("Model download exceeded expected size.");
                    await file.WriteAsync(buffer.AsMemory(0, n), cancellationToken);
                    progress?.Report(written / (double)ModelLength);
                }
            }
            if (new FileInfo(pending).Length != ModelLength)
                throw new InvalidDataException("Model download was incomplete.");

            // A downloader must never trust only HTTPS or a file name; verify
            // against the exact official model digest before making it usable.
            await using (var file = File.OpenRead(pending))
            {
                var actual = Convert.ToHexString(
                    await SHA256.HashDataAsync(file, cancellationToken));
                if (!actual.Equals(ModelSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Model SHA-256 mismatch.");
            }
            File.Move(pending, ModelPath, overwrite: true);
            progress?.Report(1);
        }
        finally
        {
            if (File.Exists(pending)) File.Delete(pending);
        }
    }

    public static async Task<string> CaptureAndTranscribeAsync(
        int microphoneIndex, TimeSpan duration, CancellationToken cancellationToken)
    {
        if (!ModelInstalled)
            throw new InvalidOperationException(
                "Install the verified multilingual Whisper model in INPUTS first.");
        if (!AudioInputDevices.MicrophoneExists(microphoneIndex))
            throw new InvalidOperationException("Selected microphone is disconnected.");
        if (duration < TimeSpan.FromSeconds(2) || duration > TimeSpan.FromSeconds(7))
            throw new ArgumentOutOfRangeException(nameof(duration));

        // WaveInEvent is 16-kHz mono, 16-bit from AudioInputDevices.CreateCapture.
        // A WAV stream in RAM is accepted directly by Whisper.net.
        using var buffer = new MemoryStream();
        var gate = new object();
        var recording = true;
        using (var writer = new WaveFileWriter(buffer, new WaveFormat(16000, 16, 1)))
        using (var capture = AudioInputDevices.CreateCapture(microphoneIndex))
        {
            capture.DataAvailable += (_, args) =>
            {
                lock (gate)
                {
                    if (recording)
                        writer.Write(args.Buffer, 0, args.BytesRecorded);
                }
            };
            capture.StartRecording();
            try { await Task.Delay(duration, cancellationToken); }
            finally
            {
                lock (gate) recording = false;
                try { capture.StopRecording(); }
                catch (InvalidOperationException) { }
            }
            writer.Flush();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var bytes = buffer.ToArray();
        if (bytes.Length < 16000)
            throw new InvalidOperationException("Not enough microphone audio captured.");

        return await Task.Run(async () =>
        {
            using var factory = WhisperFactory.FromPath(ModelPath);
            using var processor = factory.CreateBuilder()
                .WithLanguage("auto")
                .Build();
            await using var pcmWav = new MemoryStream(bytes, writable: false);
            var result = new StringBuilder();
            await foreach (var segment in processor.ProcessAsync(pcmWav)
                               .WithCancellation(cancellationToken))
            {
                if (result.Length > 550) break;
                result.Append(' ').Append(segment.Text?.Trim());
            }
            return result.ToString().Trim();
        }, cancellationToken);
    }
}
