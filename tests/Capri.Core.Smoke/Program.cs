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

    var fakeHandler = new FakeHandler();
    using var client = new HttpClient(fakeHandler);
    using var brain = new OllamaBrain(settings, client);
    Check(await brain.IsAvailableAsync(), "local Ollama health check");
    var reply = await brain.ReplyAsync(turns);
    Check(reply == "ready", "local Ollama reply decoded");
    Check(fakeHandler.SawUserMessage && fakeHandler.SawCorrectModel, "Ollama request includes conversation and selected model");
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

sealed class FakeHandler : HttpMessageHandler
{
    public bool SawUserMessage { get; private set; }
    public bool SawCorrectModel { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.Host != "127.0.0.1" || request.RequestUri.Port != 11434)
            throw new Exception("Unexpected outbound URL");
        if (request.RequestUri.AbsolutePath == "/api/tags")
            return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("{}") };

        var json = await request.Content!.ReadAsStringAsync(cancellationToken);
        SawUserMessage = json.Contains("\"content\":\"hello\"", StringComparison.Ordinal);
        SawCorrectModel = json.Contains("\"model\":\"test-model\"", StringComparison.Ordinal);
        return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{\"message\":{\"role\":\"assistant\",\"content\":\"ready\"}}") };
    }
}
