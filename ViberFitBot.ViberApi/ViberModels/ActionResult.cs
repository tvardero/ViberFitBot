using System.Text.Json.Serialization;

namespace ViberFitBot.ViberApi.ViberModels;

public class ViberActionResult
{
    public int Status { get; init; }

    [JsonPropertyName("status_message")]
    public string StatusMessage { get; init; } = null!;
}