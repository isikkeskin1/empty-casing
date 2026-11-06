namespace Capri.Core;

/// <summary>Switches between a local Ollama backend and a configured fixed cloud API endpoint.</summary>
public sealed class BrainRouter : IBrainProvider, IDisposable
{
    private readonly AppSettings _settings;
    private readonly OllamaBrain _local;
    private readonly CloudBrain _cloud;

    public BrainRouter(AppSettings settings, SecretStore secrets, HttpClient? localClient = null,
        HttpClient? cloudClient = null)
    {
        _settings = settings;
        _local = new OllamaBrain(settings, localClient);
        _cloud = new CloudBrain(settings, secrets, cloudClient);
    }

    public bool IsLocal => ProviderCatalog.IsLocal(_settings.BrainProvider);

    public Task<bool> IsAvailableAsync(CancellationToken ct = default) =>
        IsLocal ? _local.IsAvailableAsync(ct) : Task.FromResult(_cloud.HasCredentials());

    public Task<string> ReplyAsync(IReadOnlyList<ChatTurn> turns, CancellationToken ct = default) =>
        IsLocal ? _local.ReplyAsync(turns, ct) : _cloud.ReplyAsync(turns, ct);

    public Task<string> StreamReplyAsync(
        IReadOnlyList<ChatTurn> turns, Func<string, Task> onDelta,
        CancellationToken ct = default) =>
        IsLocal ? _local.StreamReplyAsync(turns, onDelta, ct) :
                  _cloud.StreamReplyAsync(turns, onDelta, ct);

    public Task<string> SeeScreenAsync(byte[] jpeg, string question, CancellationToken ct = default) =>
        IsLocal ? _local.SeeScreenAsync(jpeg, question, ct) : _cloud.SeeScreenAsync(jpeg, question, ct);

    public Task<AssistantAction?> PlanActionAsync(string request, CancellationToken ct = default) =>
        IsLocal ? _local.PlanActionAsync(request, ct) : _cloud.PlanActionAsync(request, ct);

    public Task<string> PlanUiNavigationRawAsync(string context, CancellationToken ct = default) =>
        IsLocal ? _local.PlanUiNavigationRawAsync(context, ct) :
                  _cloud.PlanUiNavigationRawAsync(context, ct);

    public Task<string> TestCloudAsync(CancellationToken ct = default) =>
        _cloud.TestAsync(ct);

    public void Dispose()
    {
        _local.Dispose();
        _cloud.Dispose();
    }
}
