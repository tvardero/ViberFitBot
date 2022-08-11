namespace ViberFitBot.ViberApi.ViberModels;

public class MessageCallback : Callback
{
    public User Sender { get; init; } = null!;
    public Message Message { get; init; } = null!;
}
