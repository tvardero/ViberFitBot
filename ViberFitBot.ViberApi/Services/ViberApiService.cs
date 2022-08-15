using ViberFitBot.ViberApi.Infrastructure;
using ViberFitBot.ViberApi.Models;
using ViberFitBot.ViberApi.Resources;
using ViberFitBot.ViberApi.ViberModels;

namespace ViberFitBot.ViberApi.Services;

public class ViberApiService
{
    public static Dictionary<string, DialogState> State { get; } = new();

    public ViberApiService(ViberApiHttpClient httpClient, ITrackService service, ILogger<ViberApiService> logger)
    {
        _httpClient = httpClient;
        _service = service;
    }

    // Handles both "conversation_started" and "subscribed" events
    public async Task HandleConversationStartedCallback(ConversationStartedCallback callback)
    {
        State.Remove(callback.User.Id);
        await _httpClient.SendWelcomeMessage(Responses.WelcomeMessage, Keyboards.MainMenuKeyboard);
    }

    // Handles "message" event
    public async Task HandleMessageCallback(MessageCallback callback)
    {
        var userId = callback.Sender.Id;

        var dialogState = State.GetValueOrDefault(userId) ?? new() { LatestStateChange = DateTime.UtcNow };

        async Task ResetToMainMenuAsync()
        {
            dialogState.State = DialogState.StateEnum.MainMenu;
            await _httpClient.SendTextMessageAsync(userId, Responses.WelcomeMessage, Keyboards.MainMenuKeyboard);
        }

        // If state is too old - reset it
        if (dialogState.LatestStateChange.AddMinutes(10) < DateTime.UtcNow) dialogState = new();
        dialogState.LatestStateChange = DateTime.UtcNow;

        if (callback.Message.Type != "text")
        {
            await _httpClient.SendTextMessageAsync(
                userId,
                Responses.UnsupportedMessageType,
                dialogState.State == DialogState.StateEnum.MainMenu ? Keyboards.MainMenuKeyboard : Keyboards.CancelKeyboard);
        }
        else
        {
            var text = callback.Message.Text;

            switch (dialogState.State)
            {
                case DialogState.StateEnum.MainMenu:
                    {
                        switch (text)
                        {
                            case Actions.Top5Action:
                                {
                                    dialogState.State = DialogState.StateEnum.Top5_WaitingForImei;
                                    await _httpClient.SendTextMessageAsync(userId, Responses.EnterImei, Keyboards.CancelKeyboard);
                                    break;
                                }
                            case Actions.TotalStatsAction:
                                {
                                    dialogState.State = DialogState.StateEnum.TotalStatistics_WaitingForImei;
                                    await _httpClient.SendTextMessageAsync(userId, Responses.EnterImei, Keyboards.CancelKeyboard);
                                    break;
                                }
                            case Actions.StatsOfDayAction:
                                {
                                    dialogState.State = DialogState.StateEnum.StatisticsOfDay_WaitingForImei;
                                    await _httpClient.SendTextMessageAsync(userId, Responses.EnterImei, Keyboards.CancelKeyboard);
                                    break;
                                }
                            default:
                                {
                                    await _httpClient.SendTextMessageAsync(userId, Responses.InvalidCommand, Keyboards.MainMenuKeyboard);
                                    break;
                                }
                        }
                        break;
                    }
                case DialogState.StateEnum.Top5_WaitingForImei:
                    {
                        if (ulong.TryParse(text, out var imei))
                        {
                            var top5 = await _service.GetTop5TracksAsync(imei.ToString());
                            var table = GenerateTableOfTop10Tracks(3, top5);
                            await _httpClient.SendRichMediaMessageAsync(userId, table);
                        }
                        else if (text.ToLower() != "cancel")
                        {
                            await _httpClient.SendTextMessageAsync(userId, Responses.InvalidImei, Keyboards.CancelKeyboard);
                            break;
                        }

                        await ResetToMainMenuAsync();
                        break;
                    }
                case DialogState.StateEnum.TotalStatistics_WaitingForImei:
                    {
                        if (ulong.TryParse(text, out var imei))
                        {
                            var totalStats = await _service.GetTotalStatisticsAsync(imei.ToString());
                            var message = $"Total tracks count: {totalStats.Count}\nTotal distance in metres: {totalStats.Distance:N0}\nTotal duration: {totalStats.Duration:d' days 'hh' hrs 'mm' mins'}";
                            await _httpClient.SendTextMessageAsync(userId, message);
                        }
                        else if (text.ToLower() != "cancel")
                        {
                            await _httpClient.SendTextMessageAsync(userId, Responses.InvalidImei, Keyboards.CancelKeyboard);
                            break;
                        }

                        await ResetToMainMenuAsync();
                        break;
                    }
                case DialogState.StateEnum.StatisticsOfDay_WaitingForImei:
                    {
                        if (ulong.TryParse(text, out var imei))
                        {
                            dialogState.Imei = imei.ToString();
                            dialogState.State = DialogState.StateEnum.StatisticsOfDay_WaitingForDate;
                            await _httpClient.SendTextMessageAsync(userId, Responses.EnterStartDateTime);
                        }
                        else if (text.ToLower() != "cancel")
                        {
                            await _httpClient.SendTextMessageAsync(userId, Responses.InvalidImei, Keyboards.CancelKeyboard);
                            break;
                        }
                        else
                        {
                            await ResetToMainMenuAsync();
                        }

                        break;
                    }
                case DialogState.StateEnum.StatisticsOfDay_WaitingForDate:
                    {
                        if (DateTime.TryParse(text, out var date))
                        {
                            var imei = dialogState.Imei!;

                            var statsOfDay = await _service.GetStatisticsOfPeriod(imei, date, date.AddDays(1));
                            var message = $"Tracks count: {statsOfDay.Count}\nDistance in metres: {statsOfDay.Distance:N0}\nDuration: {statsOfDay.Duration:d' days 'hh' hrs 'mm' mins'}";
                            await _httpClient.SendTextMessageAsync(userId, message);
                        }
                        else if (text.ToLower() != "cancel")
                        {
                            await _httpClient.SendTextMessageAsync(userId, Responses.InvalidDate, Keyboards.CancelKeyboard);
                            break;
                        }

                        await ResetToMainMenuAsync();
                        break;
                    }
            }
        }

        State[userId] = dialogState;
    }

    private static InteractiveMedia GenerateTableOfTop10Tracks(double utcOffset, IEnumerable<Track> statistics)
    {
        if (!statistics.Any()) return new()
        {
            Type = "rich_media",
            Buttons = new HashSet<InteractiveMediaButton>() { new() { ActionType = "none", ActionBody = "", Text = "Nothing to show" } }
        };

        var result = new InteractiveMedia()
        {
            Type = "rich_media",
            ButtonsGroupRows = 6
        };

        // First block in carousel
        // Headers
        result.Buttons.Add(new()
        {
            Columns = 2,
            ActionType = "none",
            ActionBody = "",
            Text = ""
        });
        result.Buttons.Add(new()
        {
            Columns = 4,
            ActionType = "none",
            ActionBody = "",
            Text = "Date"
        });

        // Content
        byte count = 0;
        foreach (var stat in statistics)
        {
            result.Buttons.Add(new()
            {
                Columns = 2,
                ActionType = "none",
                ActionBody = "",
                Text = (++count).ToString()
            });
            result.Buttons.Add(new()
            {
                Columns = 4,
                ActionType = "none",
                ActionBody = "",
                Text = stat.StartTimeUtc.AddHours(utcOffset).ToString("dd.MM.yy hh:mm")
            });
        }

        // Second block in carousel
        // Headers
        result.Buttons.Add(new()
        {
            Columns = 3,
            ActionType = "none",
            ActionBody = "",
            Text = "Distance (metres)"
        });
        result.Buttons.Add(new()
        {
            Columns = 3,
            ActionType = "none",
            ActionBody = "",
            Text = "Duration"
        });

        // Content
        foreach (var stat in statistics)
        {

            result.Buttons.Add(new()
            {
                Columns = 3,
                ActionType = "none",
                ActionBody = "",
                Text = $"{stat.DistanceMetres:N0}"
            });
            result.Buttons.Add(new()
            {
                Columns = 3,
                ActionType = "none",
                ActionBody = "",
                Text = $"{stat.Duration.TotalHours:N0} hrs {stat.Duration.Minutes} min"
            });
        }

        return result;
    }

    private readonly ViberApiHttpClient _httpClient;
    private readonly ITrackService _service;
}