using System.Text.Json.Serialization;

namespace ViberFitBot.ViberApi.ViberModels;

public class InteractiveMediaButton
{
    [JsonPropertyName("ActionType")]
    public string ActionType { get; init; } = "reply";

    [JsonPropertyName("ActionBody")]
    public string ActionBody { get; init; } = null!;

    [JsonPropertyName("Text")]
    public string Text { get; init; } = null!;
}