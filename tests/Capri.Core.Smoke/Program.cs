using System.Net;
using Capri.Core;

var directory = Path.Combine(Path.GetTempPath(), "capri-smoke-" + Guid.NewGuid().ToString("N"));
try
{
    var store = new LocalDataStore(directory);
    var settings = new AppSettings { OllamaModel = "test-model", OverlayLeft = 101, OverlayTop = 202 };
    store.SaveSettings(settings);
    Check(store.LoadSettings().OverlayLeft == 101, "settings persist");

    var turns = new[]
    {
        new ChatTurn { Role = "user", Text = "hello" },
        new ChatTurn { Role = "assistant", Text = "hey" }
    };
    store.SaveHistory(turns);
    Check(store.LoadHistory().Count == 2, "history persists");
    Check(Directory.Exists(store.CharacterFolder), "character directory exists");

    var reminder = ReminderParser.Parse("remind me in 10 minutes to stretch");
    Check(reminder?.Kind == ReminderKind.Reminder &&
          reminder.Delay == TimeSpan.FromMinutes(10) &&
          reminder.Message == "stretch", "natural-language reminder parsed");
    var timer = ReminderParser.Parse("set a timer for 30 seconds");
    Check(timer?.Kind == ReminderKind.Timer && timer.Delay == TimeSpan.FromSeconds(30),
        "natural-language timer parsed");
    Check(ReminderParser.Parse("list reminders")?.Kind == ReminderKind.List &&
          ReminderParser.Parse("cancel reminder #8")?.CancelId == 8,
        "reminder list and cancellation parsed");
    Check(ReminderParser.Parse("remind me in 9000 days to wait") is null &&
          ReminderParser.Parse("remind me what you said") is null,
        "out-of-range or ambiguous reminder requests rejected");

    var item = new ReminderEntry
    {
        Id = 7, Message = "test notification", DueAt = DateTimeOffset.UtcNow.AddMinutes(3)
    };
    store.SaveReminders(new[] { item });
    var loadedReminder = store.LoadReminders().Single();
    Check(loadedReminder.Id == 7 && loadedReminder.Message == "test notification",
        "reminder schedule persists across app restarts");


    var timingPath = Path.Combine(directory, "timing.json");
    File.WriteAllText(timingPath, "{\"frameDurationsMs\":[42,83,8,5000]}");
    var timing = AnimationTiming.Load(timingPath);
    Check(timing.DurationAt(0) == 42 && timing.DurationAt(1) == 83,
        "animation timing loads individual frame durations");
    Check(timing.DurationAt(2) == 20 && timing.DurationAt(3) == 2000,
        "animation timing bounds pathological frame durations");
    Check(timing.DurationAt(99) == 100, "missing frame duration uses 100ms fallback");
    File.WriteAllText(timingPath, "{invalid");
    Check(AnimationTiming.Load(timingPath).DurationAt(0) == 100,
        "invalid animation manifest falls back safely");

    var near = PerchLayout.IsNearTopEdge(118, 100, 30);
    Check(near && !PerchLayout.IsNearTopEdge(200, 100, 30),
        "perch triggers only near an application top edge");
    Check(Math.Abs(PerchLayout.RelativePosition(180, 100, 500) - 0.2) < 0.001,
        "window perch remembers the horizontal attachment");
    var perch = PerchLayout.Place(100, 600, 500, 0.6, 270, 0);
    Check(perch.Left >= 100 && perch.Left <= 330 && perch.Top == 244,
        "perch aligns with the window edge and stays within its width");
    var maximized = PerchLayout.Place(0, 800, 0, 0.5, 270, 6);
    Check(maximized.Top == 6, "maximized windows keep Capri visible");
    var narrow = PerchLayout.Place(100, 220, 400, 0.5, 270, 0);
    Check(Math.Abs(narrow.Left - 25) < 0.001, "narrow windows center the overlay");

    var motion = new TossMotion.State(100, 100, 900, -600, false);
    for (var i = 0; i < 1800 && !motion.Stopped; i++)
    {
        motion = TossMotion.Step(motion, 1.0 / 60.0, 0, 0, 700, 350);
        CheckBounds(motion);
    }
    Check(motion.Stopped, "toss decelerates and settles on the screen floor");

    Check(!new AppSettings().WatchScreen && !new AppSettings().ListenToMicrophone &&
          !new AppSettings().EnableDesktopActions, "perception and actions are off by default");
    Check(ActionPolicy.IsPurchaseRequest("please buy this for me") &&
          ActionPolicy.IsPurchaseRequest("check out my cart"),
          "purchases and checkout are explicitly refused");
    Check(!ActionPolicy.TryValidate(new AssistantAction("shell", "powershell"), out _),
          "arbitrary shell commands cannot execute");
    Check(!ActionPolicy.TryValidate(new AssistantAction("open_url", "file:///C:/Windows"), out _),
          "local file URLs cannot execute");
    Check(!ActionPolicy.TryValidate(new AssistantAction("open_url", "http://example.com"), out _),
          "only HTTPS links are allowed");
    Check(ActionPolicy.TryValidate(new AssistantAction("open_app", "notepad"), out _),
          "allowlisted applications validate");
    Check(ActionPolicy.ParsePlannerResponse("{\"kind\":\"open_app\",\"argument\":\"notepad\"}")?.Kind == "open_app",
          "assistant action JSON parses");
    Check(ActionPolicy.ParsePlannerResponse("{\"kind\":\"buy\",\"argument\":\"anything\"}") is null,
          "untrusted planner actions are rejected");

    var fakeHandler = new FakeHandler();
    using var client = new HttpClient(fakeHandler);
    using var brain = new OllamaBrain(settings, client);
    Check(await brain.IsAvailableAsync(), "local Ollama health check");
    var reply = await brain.ReplyAsync(turns);
    Check(reply == "ready", "local Ollama reply decoded");
    Check(fakeHandler.SawUserMessage && fakeHandler.SawCorrectModel, "Ollama request includes conversation and selected model");
    settings.VisionModel = "vision-test";
    var observation = await brain.SeeScreenAsync(new byte[] { 1, 2, 3 }, "describe this");
    Check(observation == "screen described" && fakeHandler.SawVisionImage,
        "vision request sends base64 image to the selected local model");
    var plan = await brain.PlanActionAsync("please open notepad");
    Check(plan?.Kind == "open_app" && plan.Argument == "notepad" && fakeHandler.SawJsonPlan,
        "assistant generates an allowlisted plan without executing anything");
    Check(await brain.PlanActionAsync("please buy this") is null,
        "purchase prompt cannot reach the action planner");
    Check(ProviderCatalog.IsLocal("local") && !ProviderCatalog.IsLocal("nvidia"),
        "provider selection distinguishes local from cloud");
    Check(ProviderCatalog.Get("nvidia").ApiBase == "https://integrate.api.nvidia.com/v1/",
        "NVIDIA API endpoint is pinned");
    Check(ProviderCatalog.Get("gemini").ApiBase == "https://generativelanguage.googleapis.com/v1beta/openai/",
        "Gemini compatible API endpoint is pinned");

    if (OperatingSystem.IsWindows())
    {
        var vault = new SecretStore(directory);
        vault.Save("nvidia", "test-key-only-for-smoke");
        Check(vault.HasKey("nvidia") && vault.Read("nvidia") == "test-key-only-for-smoke",
            "DPAPI secret roundtrips for current Windows user");
        var encrypted = File.ReadAllBytes(Path.Combine(directory, "credentials", "nvidia.dpapi"));
        Check(!System.Text.Encoding.UTF8.GetString(encrypted).Contains("test-key-only-for-smoke"),
            "API key not stored in plaintext");

        var cloudSettings = new AppSettings
        {
            BrainProvider = "nvidia", CloudTextModel = "test-chat", CloudVisionModel = "test-vision"
        };
        var cloudFake = new CloudFakeHandler();
        using var cloudClient = new HttpClient(cloudFake);
        using var cloud = new CloudBrain(cloudSettings, vault, cloudClient);
        Check(await cloud.TestAsync() == "ready", "cloud API returns chat completion");
        Check(cloudFake.Authenticated && cloudFake.CorrectHost &&
              cloudFake.CorrectTextModel, "cloud API uses fixed HTTPS endpoint and bearer key");
        Check(await cloud.SeeScreenAsync(new byte[] { 1, 2, 3 }, "look") == "ready" &&
              cloudFake.SawVision, "vision request sends an image only to selected provider");
        Check((await cloud.PlanActionAsync("open notepad"))?.Kind == "open_app",
            "cloud planner returns an allowlisted proposal");
        Check(await cloud.PlanActionAsync("buy something") is null,
            "cloud action planner blocks buying before network call");
        vault.Save("fish", "fish-key-for-test-only");
        Check(vault.HasKey("fish") && vault.Read("fish") == "fish-key-for-test-only",
            "Fish API key encrypted with Windows per-user DPAPI");
        var fishVaultBytes = File.ReadAllBytes(Path.Combine(directory, "credentials", "fish.dpapi"));
        Check(!System.Text.Encoding.UTF8.GetString(fishVaultBytes).Contains("fish-key-for-test-only"),
            "Fish API key is never stored in plaintext");
        var fishFake = new FishMockHandler();
        using var fishHttpClient = new HttpClient(fishFake);
        using var fish = new FishAudioClient(vault, fishHttpClient);
        var fishAudio = await fish.SynthesizeAsync("Hello choom!", FishAudioClient.FreeModel);
        Check(fishAudio.Length == 96 && fishFake.SawFreeModel &&
              fishFake.SawValidPayload && fishFake.SawBearer && fishFake.SawFixedHttps,
            "Fish API request sends free model header, text and encrypted-store Bearer token");
        await fish.SynthesizeAsync("Voice test.", FishAudioClient.FreeModel, "licensed-voice-123");
        Check(fishFake.SawReferenceId, "optional Fish voice reference ID included in request");
        Check(!FishAudioClient.IsSupportedModel("s2.1-unknown") &&
              !FishAudioClient.IsValidReference("../../secrets"),
            "unrecognized models and unsafe voice IDs rejected");
        try
        {
            await fish.SynthesizeAsync("hello", "s2.1-unknown");
            throw new Exception("Unsupported Fish model reached the network client.");
        }
        catch (ArgumentException) { /* Deliberately blocked. */ }
        vault.Delete("fish");
        Check(!vault.HasKey("fish"), "saved Fish API key can be deleted");

        vault.Delete("nvidia");
        Check(!vault.HasKey("nvidia"), "stored provider key can be deleted");
    }

    Console.WriteLine("All Capri.Core smoke checks passed.");
}
finally
{
    if (Directory.Exists(directory)) Directory.Delete(directory, true);
}

static void Check(bool condition, string description)
{
    if (!condition) throw new Exception("FAILED: " + description);
    Console.WriteLine("PASS: " + description);
}

static void CheckBounds(TossMotion.State motion)
{
    if (motion.Left < 0 || motion.Left > 700 || motion.Top < 0 || motion.Top > 350)
        throw new Exception("Toss escaped the desktop bounds");
}

sealed class FakeHandler : HttpMessageHandler
{
    public bool SawUserMessage { get; private set; }
    public bool SawCorrectModel { get; private set; }
    public bool SawVisionImage { get; private set; }
    public bool SawJsonPlan { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.Host != "127.0.0.1" || request.RequestUri.Port != 11434)
            throw new Exception("Unexpected outbound URL");
        if (request.RequestUri.AbsolutePath == "/api/tags")
            return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("{}") };

        var json = await request.Content!.ReadAsStringAsync(cancellationToken);
        SawUserMessage |= json.Contains("\"content\":\"hello\"", StringComparison.Ordinal);
        SawCorrectModel |= json.Contains("\"model\":\"test-model\"", StringComparison.Ordinal);

        if (json.Contains("\"images\":", StringComparison.Ordinal))
        {
            SawVisionImage = json.Contains("\"model\":\"vision-test\"", StringComparison.Ordinal) &&
                json.Contains("AQID", StringComparison.Ordinal);
            return Reply("screen described");
        }

        if (json.Contains("\"format\":\"json\"", StringComparison.Ordinal))
        {
            SawJsonPlan = true;
            return Reply("{\"kind\":\"open_app\",\"argument\":\"notepad\"}");
        }

        return Reply("ready");
    }

    private static HttpResponseMessage Reply(string message)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            message = new { role = "assistant", content = message }
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(json) };
    }
}


sealed class CloudFakeHandler : HttpMessageHandler
{
    public bool Authenticated { get; private set; }
    public bool CorrectHost { get; private set; }
    public bool CorrectTextModel { get; private set; }
    public bool SawVision { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CorrectHost = request.RequestUri?.Scheme == "https" &&
            request.RequestUri.Host == "integrate.api.nvidia.com" &&
            request.RequestUri.AbsolutePath == "/v1/chat/completions";
        Authenticated = request.Headers.Authorization?.Scheme == "Bearer" &&
            request.Headers.Authorization.Parameter == "test-key-only-for-smoke";
        if (!Authenticated || !CorrectHost) throw new Exception("Cloud destination or authentication invalid.");

        var json = await request.Content!.ReadAsStringAsync(cancellationToken);
        CorrectTextModel |= json.Contains("\"model\":\"test-chat\"", StringComparison.Ordinal);
        SawVision |= json.Contains("\"model\":\"test-vision\"", StringComparison.Ordinal) &&
            json.Contains("data:image/jpeg;base64,AQID", StringComparison.Ordinal);
        var content = json.Contains("\"kind\"", StringComparison.Ordinal) ||
                      json.Contains("open notepad", StringComparison.Ordinal)
            ? "{\"kind\":\"open_app\",\"argument\":\"notepad\"}" : "ready";
        var response = System.Text.Json.JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { role = "assistant", content } } }
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(response)
        };
    }
}


sealed class FishMockHandler : HttpMessageHandler
{
    public bool SawFixedHttps { get; private set; }
    public bool SawFreeModel { get; private set; }
    public bool SawBearer { get; private set; }
    public bool SawValidPayload { get; private set; }
    public bool SawReferenceId { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SawFixedHttps = request.RequestUri?.ToString() == "https://api.fish.audio/v1/tts";
        SawBearer = request.Headers.Authorization?.Scheme == "Bearer" &&
            request.Headers.Authorization.Parameter == "fish-key-for-test-only";
        SawFreeModel = request.Headers.TryGetValues("model", out var values) &&
            values.Single() == FishAudioClient.FreeModel;
        var payload = await request.Content!.ReadAsStringAsync(cancellationToken);
        SawValidPayload |= payload.Contains("\"text\":\"Hello choom!\"", StringComparison.Ordinal) &&
            payload.Contains("\"format\":\"mp3\"", StringComparison.Ordinal);
        SawReferenceId |= payload.Contains("\"reference_id\":\"licensed-voice-123\"", StringComparison.Ordinal);
        var audio = new byte[96];
        audio[0] = (byte)'I'; audio[1] = (byte)'D'; audio[2] = (byte)'3';
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(audio)
        };
    }
}
