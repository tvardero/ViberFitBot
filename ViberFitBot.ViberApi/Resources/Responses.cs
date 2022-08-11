namespace ViberFitBot.ViberApi.Resources;

public static class Responses
{
    public const string WelcomeMessage =
@"Welcome to my bot! Use the keyboard below.";

    public const string EnterImei =
@"Enter your IMEI code (or type 'cancel') :";

    public const string EnterStartDateTime =
@"Enter date (in format YYYY-MM-DD) :";

    public const string UnsupportedMessageType =
@"Sorry, I understand only text messages.";

    public const string InvalidCommand =
@"Sorry, but I don't know yet how to answer this request.";

    public const string InvalidImei =
@"Sorry, but your's IMEI is invalid. You can type 'cancel' to return to main menu.";

    public const string InvalidDate =
@"Sorry, but I can't understand inputed date. You can type 'cancel' to return to main menu.";

    public const string SomethingWentWrong =
@"Sorry, but something is not ok with me right now.";

}