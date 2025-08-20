using System.Text.Json;
using System.Text.Json.Nodes;

public static class LogMaskingHelper
{
    // Fields to mask in logs (case-insensitive)
    private static readonly string[] SensitiveFields = { "password", "email", "token" };

    public static string MaskSensitiveData(string? json, int truncateTo = 1000)
    {
        if (string.IsNullOrWhiteSpace(json)) return "[empty]";

        try
        {
            var root = JsonNode.Parse(json);
            MaskFieldsRecursive(root);
            var result = root?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "[invalid json]";
            return result.Length > truncateTo ? result[..truncateTo] + "..." : result;
        }
        catch
        {
            return "[unreadable json]";
        }
    }

    private static void MaskFieldsRecursive(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var prop in obj.ToList())
            {
                if (SensitiveFields.Contains(prop.Key, StringComparer.OrdinalIgnoreCase))
                {
                    obj[prop.Key] = "***";
                }
                else
                {
                    MaskFieldsRecursive(prop.Value);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                MaskFieldsRecursive(item);
            }
        }
    }
}
