using System.Text.Json.Serialization;

namespace ViberFitBot.ViberApi.ViberModels;

public class InteractiveMedia
{
    [JsonPropertyName("Buttons")]
    public ICollection<InteractiveMediaButton> Buttons { get; init; } = new HashSet<InteractiveMediaButton>();

    [JsonPropertyName("Type")]
    public string Type { get; init; } = null!;

    [JsonPropertyName("ButtonsGroupColumns")]
    public int ButtonsGroupColumns { get; init; } = 6;

    [JsonPropertyName("ButtonsGroupRows")]
    public int ButtonsGroupRows { get; init; } = 7;
}
