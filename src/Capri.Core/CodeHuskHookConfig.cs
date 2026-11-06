using System.Text.Json;
using System.Text.Json.Nodes;

namespace Capri.Core;

/// <summary>
/// Add or remove only Capri's own read-only hooks. Never replace a user's
/// other hooks or attempt to authorize actions in either coding assistant.
/// </summary>
public static class CodeHuskHookConfig
{
    public const string HookMarker = "capri-desktop-husk-v1";
    private static readonly (string Event, string Kind)[] Supported =
    {
        ("UserPromptSubmit", "prompt"),
        ("PermissionRequest", "approval"),
        ("Stop", "complete"),
        ("PostToolUseFailure", "error"),
        ("SubagentStart", "subagent"),
        ("PostCompact", "compact")
    };

    public static string Merge(string? json, string scriptPath, string agent)
    {
        if (agent is not ("codex" or "claude"))
            throw new ArgumentException("Unsupported coding assistant.", nameof(agent));
        if (string.IsNullOrWhiteSpace(scriptPath) || !Path.IsPathFullyQualified(scriptPath) ||
            scriptPath.Contains('"') || scriptPath.Any(char.IsControl) ||
            !scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid relay script path.", nameof(scriptPath));
        var root = ReadObject(json);
        var hooks = GetHooks(root);
        RemoveOwnHooks(hooks);
        foreach (var (eventName, eventKind) in Supported)
        {
            var array = GetOrAddArray(hooks, eventName);
            var command = "powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" +
                          scriptPath + "\" -Agent " + agent + " -Event " + eventKind +
                          " -CapriHookId " + HookMarker;
            array.Add(new JsonObject
            {
                ["hooks"] = new JsonArray(new JsonObject
                {
                    ["type"] = "command",
                    ["command"] = command
                })
            });
        }
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    public static string Remove(string? json)
    {
        var root = ReadObject(json);
        if (root["hooks"] is null)
            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        var hooks = GetHooks(root);
        RemoveOwnHooks(hooks);
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    public static bool ContainsOwnHooks(string? json)
    {
        try
        {
            var root = ReadObject(json);
            if (root["hooks"] is not JsonObject hooks) return false;
            return hooks.Any(item => item.Value is JsonArray list &&
                list.Any(group => group is JsonObject obj &&
                    obj["hooks"] is JsonArray handlers &&
                    handlers.Any(IsOwnHandler)));
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        { return false; }
    }

    private static bool IsOwnHandler(JsonNode? handler) =>
        handler is JsonObject obj &&
        (string?)obj["type"] == "command" &&
        (string?)obj["command"] is string command &&
        command.Contains("-CapriHookId " + HookMarker, StringComparison.Ordinal);

    private static JsonObject ReadObject(string? json)
    {
        if (json?.Length > 1024 * 1024)
            throw new InvalidDataException("Refusing to modify a config over 1 MB.");
        if (string.IsNullOrWhiteSpace(json)) return new JsonObject();
        var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = false },
            new JsonDocumentOptions { MaxDepth = 32 });
        if (node is not JsonObject root)
            throw new InvalidDataException("Coding assistant settings must be a JSON object.");
        return root;
    }

    private static JsonObject GetHooks(JsonObject root)
    {
        if (root["hooks"] is null)
        {
            var newHooks = new JsonObject();
            root["hooks"] = newHooks;
            return newHooks;
        }
        if (root["hooks"] is not JsonObject hooks)
            throw new InvalidDataException("Existing hooks must be a JSON object.");
        return hooks;
    }

    private static JsonArray GetOrAddArray(JsonObject hooks, string eventName)
    {
        if (hooks[eventName] is null)
        {
            var list = new JsonArray();
            hooks[eventName] = list;
            return list;
        }
        if (hooks[eventName] is not JsonArray array)
            throw new InvalidDataException("Existing hook event must be a JSON array: " + eventName);
        return array;
    }

    private static void RemoveOwnHooks(JsonObject hooks)
    {
        foreach (var key in hooks.Select(x => x.Key).ToArray())
        {
            if (hooks[key] is not JsonArray entries)
                throw new InvalidDataException("Unsupported hooks structure: " + key);
            foreach (var node in entries.ToArray())
            {
                if (node is not JsonObject group ||
                    group["hooks"] is not JsonArray handlers)
                    continue;
                foreach (var handler in handlers.ToArray())
                    if (IsOwnHandler(handler)) handlers.Remove(handler);
                if (handlers.Count == 0) entries.Remove(node);
            }
            if (entries.Count == 0) hooks.Remove(key);
        }
    }
}
