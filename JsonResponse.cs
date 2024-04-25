using System.Text.Json.Serialization;

namespace HyperSploit;

/// <summary>
/// Xiaomi JSON response
/// </summary>
public class JsonResponse {
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("descEN")]
    public string Description { get; set; }
}

/// <summary>
/// Source generation context
/// </summary>
[JsonSerializable(typeof(JsonResponse))]
internal partial class SourceGenerationContext : JsonSerializerContext;