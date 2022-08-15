using System.ComponentModel.DataAnnotations.Schema;

namespace ViberFitBot.ViberApi.Models;

[NotMapped]
public class DialogState
{
    public enum StateEnum
    {
        NotSpecified = 0,
        MainMenu = NotSpecified,
        Top5_WaitingForImei,
        TotalStatistics_WaitingForImei,
        StatisticsOfDay_WaitingForImei,
        StatisticsOfDay_WaitingForDate
    }

    public StateEnum State { get; set; }
    public string? Imei { get; set; }
    public DateTime? SearchPeriodBeginning { get; set; }
    public DateTime LatestStateChange { get; set; }
}