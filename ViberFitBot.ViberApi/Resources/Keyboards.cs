using ViberFitBot.ViberApi.ViberModels;

namespace ViberFitBot.ViberApi.Resources;

public static class Keyboards
{
    public static readonly InteractiveMedia MainMenuKeyboard = new()
    {
        Type = "keyboard",
        Buttons = new HashSet<InteractiveMediaButton>()
        {
            new()
            {
                Text = "5️⃣ Top 5 trips",
                ActionType = "reply",
                ActionBody = Actions.Top5Action
            },
            new()
            {
                Text="🕒 Total statistics",
                ActionType = "reply",
                ActionBody = Actions.TotalStatsAction
            },
            new()
            {
                Text="🔎 Statistics of a day",
                ActionType = "reply",
                ActionBody = Actions.StatsOfDayAction
            }
        }
    };

    public static readonly InteractiveMedia CancelKeyboard = new()
    {
        Type = "keyboard",
        Buttons = new HashSet<InteractiveMediaButton>()
        {
            new ()
            {
                Text = "❌ Cancel",
                ActionType = "reply",
                ActionBody = Actions.CancelAction
            }
        }
    };
}