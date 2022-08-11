using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using ViberFitBot.ViberApi.Infrastructure;
using ViberFitBot.ViberApi.Models;
using ViberFitBot.ViberApi.Resources;
using ViberFitBot.ViberApi.ViberModels;

namespace ViberFitBot.ViberApi.Services;

public class ViberApiService
{
    public static Dictionary<string, DialogState> State { get; } = new();

    public ViberApiService(ViberApiHttpClient httpClient, TrackService service, ILogger<ViberApiService> logger)
    {
        _httpClient = httpClient;
        _service = service;
        _logger = logger;
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
                            case Actions.Top10Action:
                                {
                                    dialogState.State = DialogState.StateEnum.Top10_WaitingForImei;
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
                case DialogState.StateEnum.Top10_WaitingForImei:
                    {
                        if (ulong.TryParse(text, out var imei))
                        {
                            var top10 = await _service.GetTop10TracksAsync(imei.ToString());
                            var table = GenerateTableOfTop10Tracks(3, top10);
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

    private InteractiveMedia GenerateTableOfTop10Tracks(double utcOffset, IEnumerable<Track> statistics)
    {
        var tableHtml = new StringBuilder("<table><thead><th>Date</th><th>Distance in metres</th><th>Duration</th></thead><tbody>");

        foreach (var stat in statistics)
        {
            tableHtml.Append($"<td>{stat.StartTimeUtc.AddHours(utcOffset).ToString("dd.MM.yy hh:mm")}</td>");
            tableHtml.Append($"<td>{stat.DistanceMetres:N0}</td>");
            tableHtml.Append($"<td>{stat.Duration:G}</td>");
        }

        tableHtml.Append("</tbody></table>");

        _logger.LogInformation("Created table: {tableHtml}", tableHtml.ToString());

        return new()
        {
            Type = "rich_media",
            Buttons = new HashSet<InteractiveMediaButton>()
            {
                new()
                {
                    ActionType="none",
                    ActionBody="",
                    Text = tableHtml.ToString()
                }
            }
        };
    }

    private readonly ViberApiHttpClient _httpClient;
    private readonly TrackService _service;
    private readonly ILogger<ViberApiService> _logger;
}