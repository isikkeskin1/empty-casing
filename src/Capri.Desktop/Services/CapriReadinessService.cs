using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Text;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// Explicit, privacy-conscious preflight. A green result means the subsystem
/// is CONFIGURED; physical device, Discord, provider approval and on-screen QA
/// must still be tested on the consumer's actual Windows PC.
/// </summary>
public static class CapriReadinessService
{
    public static async Task<string> BuildAsync(App app, CancellationToken ct)
    {
        var settings = app.Settings;
        var report = new StringBuilder();
        void Row(string status, string title, string next) =>
            report.AppendLine(status.PadRight(7) + " " + title + "\n       " + next + "\n");

        report.AppendLine("CAPRI // RELEASE READINESS");
        report.AppendLine("This is a local configuration check, NOT a hardware certification.");
        report.AppendLine("No transcripts, credentials, mic audio or screenshots are read or uploaded.");
        report.AppendLine();

        Row(RuntimeInformation.OSArchitecture == Architecture.X64 ? "READY" : "CHECK",
            "Windows x64 architecture",
            RuntimeInformation.OSArchitecture == Architecture.X64
                ? "Supported installer architecture." : "Use an x64 Windows machine.");

        Row(app.CharacterArtLoaded ? "READY" : "SET UP",
            "Floating companion art",
            app.CharacterArtLoaded
                ? "Seven state folders detected. Review actual animation playback."
                : "Open About / Updates → Import Animation Pack; inspect install assets.");

        if (app.Store.SettingsRecoveredFromBackup)
            Row("NOTICE", "Settings restored from previous backup",
                "The last settings file was unreadable. Check your choices in SETUP and INPUTS.");

        if (ProviderCatalog.IsLocal(settings.BrainProvider))
        {
            var available = false;
            try
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
                deadline.CancelAfter(TimeSpan.FromSeconds(5));
                available = await app.Brain.IsAvailableAsync(deadline.Token);
            }
            catch (Exception ex) when (ex is HttpRequestException or
                OperationCanceledException or InvalidOperationException) { }
            Row(available && settings.SetupCompleted ? "READY" : "SET UP",
                "AI brain — local Ollama",
                available
                    ? "Ollama endpoint reachable. Run TEST BRAIN to verify your chosen model."
                    : "Install/start Ollama, pull your chat model, then use SET UP BRAIN.");
        }
        else
        {
            var configured = settings.SetupCompleted &&
                app.Secrets.HasKey(settings.BrainProvider);
            Row(configured ? "READY" : "SET UP",
                "AI brain — cloud provider",
                configured
                    ? "An encrypted provider key is saved. Use TEST BRAIN to test the actual API."
                    : "Choose a supported provider and save a key in SET UP BRAIN.");
        }

        if (settings.VoiceProvider == "fish")
        {
            Row(app.Secrets.HasKey(FishAudioClient.KeyId) ? "READY" : "SET UP",
                "Fish Audio speech",
                app.Secrets.HasKey(FishAudioClient.KeyId)
                    ? "Key is saved locally. Preview speech to test routing and balance."
                    : "Cloud speech is selected, but no Fish key is saved. Windows voice is an alternative.");
        }
        else
        {
            Row("READY", "Windows text-to-speech",
                "No Fish subscription required. Preview speech to check the actual voice.");
        }

        var modelReady = await Task.Run(() =>
            LocalWhisperTranscriber.ModelInstalled, ct);
        Row(modelReady ? "READY" : "OPTION",
            "Multilingual Whisper Talk Once",
            modelReady
                ? "Model SHA-256 matches official weights. Test real microphone/language accuracy."
                : "Optional: install the verified multilingual model in INPUTS.");

        bool windowsRecognizer = false;
        try
        {
            windowsRecognizer = SpeechRecognitionEngine.InstalledRecognizers().Count > 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or
            PlatformNotSupportedException) { }
        Row(windowsRecognizer ? "READY" : "SET UP",
            "Continuous Windows microphone / call recognition",
            windowsRecognizer
                ? "A Windows speech recognizer is installed. Test the selected language."
                : "Install a Windows Speech Recognition language pack for MIC ON and voice calls.");

        Row(PackagedOAuthClients.Get(LinkedService.Spotify).Length > 0
                ? "READY" : "PUBLISH",
            "Spotify browser authorization",
            PackagedOAuthClients.Get(LinkedService.Spotify).Length > 0
                ? "Public publisher app ID embedded. Provider consent/approval still required."
                : "Publisher must register one Spotify OAuth app and set CAPRI_SPOTIFY_CLIENT_ID.");
        Row(PackagedOAuthClients.Get(LinkedService.Roblox).Length > 0
                ? "READY" : "PUBLISH",
            "Roblox browser authorization",
            PackagedOAuthClients.Get(LinkedService.Roblox).Length > 0
                ? "Public publisher app ID embedded. Provider consent/approval still required."
                : "Publisher must register one Roblox OAuth app and set CAPRI_ROBLOX_CLIENT_ID.");
        Row(DiscordPresenceService.HasPublisherAppId ? "READY" : "PUBLISH",
            "Discord Rich Presence",
            DiscordPresenceService.HasPublisherAppId
                ? "Public app ID embedded. Requires Discord running and explicit opt-in."
                : "Publisher must register a Discord app and set CAPRI_DISCORD_CLIENT_ID.");

        Row(app.CanAutomaticallyInstallUpdates ? "READY" : "OPTION",
            "Verified automatic updates",
            app.CanAutomaticallyInstallUpdates
                ? "Installed-path update staging is supported; verify a real restart/upgrade."
                : "Portable build: use About / Updates → Restart to update.");

        Row("VERIFY", "Windows publisher trust & distribution rights",
            "Installer is not Authenticode-signed by this build. Character art is NONCOMMERCIAL " +
            "with required credits; obtain rights and signing before commercial release.");
        Row("VERIFY", "Real PC acceptance",
            "Test first-run, Discord audio/virtual mic, Spotify/Roblox browser auth, " +
            "Whisper mic, click-through windows, multi-monitor DPI, and updating.");

        report.AppendLine("Release status: PUBLIC PREVIEW until hardware QA, " +
            "publisher registrations, code signing and art rights are cleared.");
        return report.ToString();
    }
}
