using System.Text;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotWebApp;
using Strings = TelegramBotWebApp.Services.Resourses.Strings;

namespace Telegram.Bot.Examples.WebHook.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient client;
    private readonly ILogger<HandleUpdateService> logger;
    private readonly BotConfiguration botConfig;
    private User user = new();
    private Chat chat = new();
    private Update update = new Update();
    private VesselsManager vesselManager;
    private UserManager userManager;
    public HandleUpdateService(ITelegramBotClient _botClient, ILogger<HandleUpdateService> _logger, IConfiguration _configuration)
    {
        client = _botClient;
        logger = _logger;
        botConfig = _configuration.GetSection("BotConfiguration").Get<BotConfiguration>();
    }
    public async Task EchoAsync(Update _update, VesselsManager _vesselsManager)
    {
        update = _update;
        vesselManager = _vesselsManager;
        userManager = new UserManager();
        user = userManager.GetUser(update);
        chat = user.chat;

        var handler = update.Type switch
        {
            UpdateType.Message            => BotOnMessageReceived(update),
            UpdateType.EditedMessage      => BotOnMessageReceived(update),
            UpdateType.CallbackQuery      => BotOnCallBackReceived(update),
            _                             => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        #pragma warning disable CA1031
        catch (Exception exception)
        #pragma warning restore CA1031
        {
            await HandleErrorAsync(exception);
        }
    }
    private async Task BotOnCallBackReceived(Update update)
    {
        if (update.CallbackQuery != null && update.CallbackQuery.Data != null)
        {
            Vessel vessel = vesselManager.vesselsList.Where(x => x.name.Equals(update.CallbackQuery.Data)).First();
            await client.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"🛳 {vessel.name}");

            user.targetVessel = vessel;
            userManager.AddCounter(user);
            await SendShipSchedule();
        }
    }

    private async Task BotOnMessageReceived(Update update)
    {
        if (update.Message == null || update.Message.Type != MessageType.Text)
            return;

        var action = update.Message.Text!.Split(' ')[0] switch
        {
            "/start"            => Start(),
            "/status"           => GetStatus(),
            "/help"             => GetHelp(),
            "/refresh_ship"     => RefreshShip(),
            "/order_descending" => ChangePrintOrder(false),
            "/order_ascending"  => ChangePrintOrder(true),
            "/top10"            => GetTop10users(),

            _                   => CheckIfNameLegit()
        };

        Message sentMessage = await action;
    }

    private async Task<Message> Start()
    {
        await client.SendTextMessageAsync(chat, Strings.welcome);
        return await GetStatus();
    }

    private async Task<Message> GetStatus()
    {
        StringBuilder builder = new();
        if (user.targetVessel != null)
        {
            builder.AppendLine($"{Strings.target_vessel_set} - {user.targetVessel.name}");
            builder.AppendLine(Strings.command_refresh_ship);
            builder.AppendLine();
        }
        else
        {
            builder.AppendLine(Strings.target_vessel_null);
            builder.AppendLine();
        }
        if (user.PrintAscending == true)
        {
            builder.AppendLine(Strings.print_order_ascending_true);
        }
        else if (user.PrintAscending == false)
        {
            builder.AppendLine(Strings.print_order_ascending_false);
        }
        userManager.AddCounter(user);
        return await client.SendTextMessageAsync(chat, builder.ToString(), ParseMode.Html);
    }

    private async Task<Message> GetHelp()
    {
        userManager.AddCounter(user);
        return await client.SendTextMessageAsync(chat, Strings.command_help);
    }

    private async Task<Message> ChangePrintOrder(bool option)
    {
        string isAscending;
        if (option)
        {
            isAscending = "ascending";
            user.PrintAscending = true;
        }
        else
        {
            isAscending = "descending";
            user.PrintAscending = false;
        }
        userManager.AddCounter(user);
        return await client.SendTextMessageAsync(chat, "Schedule print order changed to - " + isAscending + ".");
    }

    private async Task<Message> RefreshShip()
    {
        if (user.targetVessel.name == "blank")
        {
            userManager.AddCounter(user);
            return await client.SendTextMessageAsync(chat, Strings.target_vessel_null);
        }
        else
        {
            userManager.AddCounter(user);
            return await SendShipSchedule();
        }
    }

    private async Task<Message> SendShipSchedule()
    {
        VesselSchedule vesselSchedule = new(logger);
        vesselSchedule.InitializeSchedule(user);
        var stringList = vesselSchedule.scheduleString;
        for (int i = 0; i < stringList.Count - 1; i++)
        {
            await client.SendTextMessageAsync(chat.Id, stringList[i], ParseMode.Html);
        }
        return await client.SendTextMessageAsync(chat.Id, stringList[stringList.Count - 1], ParseMode.Html);
    }

    private async Task<Message> CheckIfNameLegit()
    {
        List<Vessel> shipList = vesselManager.GetMatchingVesselsFrActive(update.Message.Text);
        List<List<InlineKeyboardButton>> keyboard = new();

        foreach (var ship in shipList)
        {
            InlineKeyboardButton shipButton = new($"🛳 Ship: {ship.name}");
            shipButton.CallbackData = ship.name;
            List<InlineKeyboardButton> row = new();
            row.Add(shipButton);
            keyboard.Add(row);
        }
        if (keyboard.Count > 1)
        {
            InlineKeyboardMarkup inlineKeyboard = new(keyboard);
            return await client.SendTextMessageAsync(chat.Id, Strings.search_results_several, replyMarkup: inlineKeyboard);
        }
        else if (keyboard.Count == 1)
        {
            InlineKeyboardMarkup inlineKeyboard = new(keyboard);
            return await client.SendTextMessageAsync(chat.Id, Strings.search_results_single, replyMarkup: inlineKeyboard);
        }
        else
        {
            return await client.SendTextMessageAsync(chat.Id, Strings.search_results_none);
        }
    }

    private async Task<Message> GetTop10users()
    {
        List<User> topUsers = userManager.GetTop10Users();
        StringBuilder builder = new();
        builder.AppendLine("🏆 Top 10 Active Users 🏆");
        builder.AppendLine();
        int rank = 1;
        foreach (var topUser in topUsers)
        {
            builder.AppendLine($"{rank}. @{topUser.chat.Username} - {topUser.requestCount} requests");
            rank++;
        }
        userManager.AddCounter(user);
        return await client.SendTextMessageAsync(chat, builder.ToString());
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
    public Task HandleErrorAsync(Exception exception)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
        return Task.CompletedTask;
    }
    private void ToLogRecievedMsg(Update update, User user)
    {
        logger.LogCritical("\n Receive message type: {message.Type}", update.Message.Type);
        logger.LogCritical("\n From: {message.From.FirstName} {message.From.LastName}", update.Message.From.FirstName, update.Message.From.LastName);
        logger.LogCritical("\n MessageText: {message.Text}", update.Message.Text);
    }
    private void ToLogSentMsg(Message sentMessage)
    {
        logger.LogInformation("\n The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        logger.LogInformation("\n Sent message text:{SentMessageId}", sentMessage.Text);

    }

}
