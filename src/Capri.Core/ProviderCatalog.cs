namespace Capri.Core;

/// <summary>Fixed endpoints: API keys and screenshots can never be sent to arbitrary user-provided servers.</summary>
public static class ProviderCatalog
{
    public sealed record Provider(string Id, string Name, string ApiBase, string SuggestedModel, string KeyPage);

    public static readonly IReadOnlyList<Provider> Cloud = new[]
    {
        new Provider("nvidia", "NVIDIA API Catalog", "https://integrate.api.nvidia.com/v1/",
            "meta/llama-3.1-70b-instruct", "https://build.nvidia.com/settings/api-keys"),
        new Provider("gemini", "Google AI Studio (Gemini)", "https://generativelanguage.googleapis.com/v1beta/openai/",
            "gemini-3.8-flash", "https://aistudio.google.com/apikey"),
        new Provider("openrouter", "OpenRouter", "https://openrouter.ai/api/v1/",
            "openrouter/auto", "https://openrouter.ai/settings/keys"),
        new Provider("openai", "OpenAI API", "https://api.openai.com/v1/",
            "gpt-4.1-mini", "https://platform.openai.com/api-keys")
    };

    public static Provider Get(string id) =>
        Cloud.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentOutOfRangeException(nameof(id), "Unknown cloud provider.");

    public static bool IsLocal(string? id) => string.IsNullOrWhiteSpace(id)
        || string.Equals(id, "local", StringComparison.OrdinalIgnoreCase);
}
