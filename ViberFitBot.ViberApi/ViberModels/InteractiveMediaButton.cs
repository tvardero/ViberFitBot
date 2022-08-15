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

    [JsonPropertyName("Columns")]
    public int Columns { get; init; } = 6;

    [JsonPropertyName("Rows")]
    public int Rows { get; init; } = 1;

    [JsonPropertyName("TextVAlign")]
    public string TextVAlign { get; init; } = "top";

    [JsonPropertyName("Frame")]
    public object? Frame { get; init; } = null;
}