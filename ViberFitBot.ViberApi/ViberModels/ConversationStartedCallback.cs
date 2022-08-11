namespace ViberFitBot.ViberApi.ViberModels;

public class ConversationStartedCallback : Callback
{
    public User User { get; set; } = null!;
}
