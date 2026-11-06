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
