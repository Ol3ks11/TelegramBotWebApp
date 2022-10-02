using System.Text;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotWebApp.Services.Resources;

namespace Telegram.Bot.Examples.WebHook.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;
    private VesselsManager vesselManager = new();

    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task EchoAsync(Update update)
    {
        Chat chat = GetChat(update).Result;

        var handler = update.Type switch
        {
            UpdateType.Message            => BotOnMessageReceived(update, chat),
            UpdateType.EditedMessage      => BotOnMessageReceived(update, chat),
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

    private async Task<Chat> GetChat(Update update)
    {
        return await _botClient.GetChatAsync(update.Message.Chat.Id);
    }

    private async Task BotOnMessageReceived(Update update, Chat chat)
    {
        ToLogRecievedMsg(update);
        SqlManager sqlManager = new SqlManager();
        TelegramBotWebApp.Services.Resources.User user = sqlManager.GetUser(update);
        
        if (update.Message.Type != MessageType.Text)
            return;
        var action = update.Message.Text!.Split(' ')[0] switch
        {
            "/start"            => Start(_botClient),
            "/status"           => GetStatus(_botClient),

            "/setup_ship"       => SetupShip(_botClient),
            "/setup_port"       => SetupPort(_botClient),

            "/refresh_ship"     => RefreshShip(_botClient),
            "/refresh_port"     => RefreshPort(_botClient),

            "/order_descending" => ChangePrintOrder(_botClient,0),
            "/order_ascending"  => ChangePrintOrder(_botClient,1),
            _                   => CheckIfNameLegit(_botClient)
        };
        Message sentMessage = await action;
        ToLogSentMsg(sentMessage);
        sqlManager.AddToRequestsCount(update);

        async Task<Message> Start(ITelegramBotClient bot)
        {
            StringBuilder builder = new();
            builder.AppendLine("Welcome to Maersk Schedule Bot!");
            builder.AppendLine("You can get vessel`s schedule or port`s schedule here.");
            await bot.SendTextMessageAsync(chat, builder.ToString());
            return await GetStatus(_botClient);
        }

        async Task<Message> GetStatus(ITelegramBotClient bot)
        {
            StringBuilder builder = new();
            if (user.VesselTarget != null)
            {
                builder.AppendLine($"🛳✅ Your target vessel is - {user.VesselTarget.ShipName}");
                builder.AppendLine($"🛳🔄 Enter /refresh_ship to get ship schedule.");
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine($"🛳❌Your target vessel is missing, please enter /setup_ship to set it up.");
                builder.AppendLine();
            }
            if (user.PortTarget != null)
            {
                builder.AppendLine($"🏭✅ Your target port is - {user.PortTarget.portName}");
                builder.AppendLine($"🏭🔄 Enter /refresh_port to get port schedule.");
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine($"🏭❌Your target port is missing, please enter /setup_port to set it up.");
                builder.AppendLine();
            }
            if (user.PrintAscending == true)
            {
                builder.AppendLine($"Schedule will be printed in ascending order (from top to bottom).");
                builder.AppendLine($"Enter /order_descending to change to descending order (from bottom to top).");

            }
            else if (user.PrintAscending == false)
            {
                builder.AppendLine($"Schedule will be printed in descending order (from bottom to top).");
                builder.AppendLine($"Enter /order_ascending to change to ascending order (from top to bottom).");
            }
            return await bot.SendTextMessageAsync(chat, builder.ToString());
        }

        async Task<Message> ChangePrintOrder(ITelegramBotClient bot,int option)
        {
            sqlManager.ChangePrintAscending(update, option);
            string isAscending = null;
            if (option == 1)
            {
                isAscending = "ascending";
            }
            else if (option == 0)
            {
                isAscending = "descending";
            }
            return await bot.SendTextMessageAsync(chat, "Schedule print order changed to - " + isAscending + ".");

        }

        async Task<Message> SetupShip(ITelegramBotClient bot)
        {
            sqlManager.RemoveShip(update);
            return await bot.SendTextMessageAsync(chat, "🛳 Please enter vessel`s name. 🛳");
        }

        async Task<Message> SetupPort(ITelegramBotClient bot)
        {
            sqlManager.RemovePort(update);
            return await bot.SendTextMessageAsync(chat, "🏭 Please enter port`s name. 🏭");
        }

        async Task<Message> RefreshShip(ITelegramBotClient bot)
        {
            if (user.VesselTarget == null)
            {
                return await _botClient.SendTextMessageAsync(chat, "⚓️ Please setup your target vessel via /Setup_ship command. ⚓️");
            }
            else
            {
                return await SendShipSchedule();
            }
        }

        async Task<Message> RefreshPort(ITelegramBotClient bot)
        {
            if (user.PortTarget == null)
            {
                return await _botClient.SendTextMessageAsync(chat, "🌉 Please setup your target port via /Setup_port command. 🌉");
            }
            else
            {
                return await SendPortSchedule();
            }
        }

        async Task<Message> SendShipSchedule()
        {
            SqlManager sqlManager = new();
            Ship ship = user.VesselTarget;
            ship = vesselManager.UpdateShipPorts(ship);
            List<string> schedule = vesselManager.BuildSchedule(ship,user);
            for (int i=0;i<schedule.Count-1;i++)
            {
                await _botClient.SendTextMessageAsync(chat.Id, schedule[i], ParseMode.Html);
            }
            return await _botClient.SendTextMessageAsync(chat.Id, schedule[schedule.Count - 1], ParseMode.Html);
        }

        async Task<Message> SendPortSchedule()
        {
            SqlManager sqlManager = new();
            Port port = user.PortTarget;
            port = vesselManager.UpdatePortShips(port);
            List<string> schedule = vesselManager.BuildSchedule(port,user);
            for (int i = 0; i < schedule.Count - 1; i++)
            {
                await _botClient.SendTextMessageAsync(chat.Id, schedule[i], ParseMode.Html);
            }
            return await _botClient.SendTextMessageAsync(chat.Id, schedule[schedule.Count - 1], ParseMode.Html);
        }

        async Task<Message> CheckIfNameLegit(ITelegramBotClient bot)
        {
            SqlManager sqlManager = new();
            Ship ship = sqlManager.GetShipFromDbByName(update.Message.Text);
            Port port = sqlManager.GetPortFromDbByName(update.Message.Text);

            if (ship != null)
            {
                await _botClient.SendTextMessageAsync(chat.Id, "✅ Match found! ✅");
                await _botClient.SendTextMessageAsync(chat.Id, $"🛳 {ship.ShipName} 🛳");

                sqlManager.AddShip(update, ship);
                return await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_ship to recieve a schedule. 📅");
            }
            if (port != null)
            {
                await _botClient.SendTextMessageAsync(chat.Id, "✅ Match found! ✅");
                await _botClient.SendTextMessageAsync(chat.Id,$"🏭 {port.portName} 🏭");
                sqlManager.AddPort(update, port);
                return await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_port to recieve a schedule. 📅");
            }

            return await _botClient.SendTextMessageAsync(chat.Id, "❌ Can not find a matching name. ❌");
        }
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
    public Task HandleErrorAsync(Exception exception)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
        return Task.CompletedTask;
    }
    private void ToLogRecievedMsg(Update update)
    {
        _logger.LogInformation("\n Receive message type: {message.Type}", update.Message.Type);
        _logger.LogInformation("\n From: {message.From.FirstName} {message.From.LastName}", update.Message.From.FirstName, update.Message.From.LastName);
        _logger.LogInformation("\n MessageText: {message.Text}", update.Message.Text);
    }
    private void ToLogSentMsg(Message sentMessage)
    {
        _logger.LogInformation("\n The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        _logger.LogInformation("\n Sent message text:{SentMessageId}", sentMessage.Text);

    }

}
