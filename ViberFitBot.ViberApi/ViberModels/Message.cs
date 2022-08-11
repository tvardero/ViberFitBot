namespace ViberFitBot.ViberApi.ViberModels;

public class Message
{
    public string Type { get; init; } = "text";
    public string Text { get; init; } = null!;
    public Location Location { get; init; } = null!;
}
