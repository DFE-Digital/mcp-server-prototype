using System.Text.Json;

namespace Dfe.Mcp.Server.Application.Helpers;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    // Serialize any object to a JSON string
    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    // Deserialize a JSON string back into an object of type T
    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, _options);
    }
}
