using System.Text;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotWebApp.Services.Resources;

namespace Telegram.Bot.Examples.WebHook.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;
    private VesselsManager vesselManager = new();
    private SqlManager sqlManager;

    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger)
    {
        _botClient = botClient;
        _logger = logger;

        IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).Build();
        sqlManager = new SqlManager(config);
    }

    public async Task EchoAsync(Update update)
    {
        Chat chat = GetChat(update).Result;

        var handler = update.Type switch
        {
            UpdateType.Message            => BotOnMessageReceived(update, chat),
            UpdateType.EditedMessage      => BotOnMessageReceived(update, chat),
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

    private async Task<Chat> GetChat(Update update)
    {
        if (update.CallbackQuery != null)
        {
            return await _botClient.GetChatAsync(update.CallbackQuery.Message.Chat.Id);
        }
        return await _botClient.GetChatAsync(update.Message.Chat.Id);
    }

    private async Task BotOnCallBackReceived(Update update)
    {
        //SqlManager sqlManager = new SqlManager();
        TelegramBotWebApp.Services.Resources.User user = sqlManager.GetUser(update);
        Chat chat = update.CallbackQuery.Message.Chat;
        if (update.CallbackQuery != null)
        {
            if (update.CallbackQuery.Data.Split(' ')[0] == "port")
            {
                string portname = update.CallbackQuery.Data.Remove(0, 4).Trim();
                Port port = sqlManager.GetPortFromDbByName(portname);
                await _botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"🏭 {port.emoji}{port.portName}");
                sqlManager.RemovePort(user.TelegramId.ToString());
                sqlManager.AddPort(user.TelegramId.ToString(), port);
                await _botClient.SendTextMessageAsync(chat.Id, $"🏭 {port.emoji}{port.portName}");
                await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_port to recieve a schedule. 📅");
            }
            else
            {
                Ship ship = sqlManager.GetShipFromDbByName(update.CallbackQuery.Data);
                await _botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"🛳 {ship.ShipName}");
                sqlManager.RemoveShip(user.TelegramId.ToString());
                sqlManager.AddShip(user.TelegramId.ToString(), ship);
                await _botClient.SendTextMessageAsync(chat.Id, $"🛳 {ship.ShipName}");
                await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_ship to recieve a schedule. 📅");
            }
        }
    }

    private async Task BotOnMessageReceived(Update update, Chat chat)
    {
        ToLogRecievedMsg(update);
        //SqlManager sqlManager = new SqlManager();
        TelegramBotWebApp.Services.Resources.User user = sqlManager.GetUser(update);
        
        if (update.Message.Type != MessageType.Text)
            return;
        var action = update.Message.Text!.Split(' ')[0] switch
        {
            "/start"            => Start(_botClient),
            "/status"           => GetStatus(_botClient),
            "/help"             => GetHelp(_botClient),

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
            builder.AppendLine("You can get vessels schedule or ports schedule here.");
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
                builder.AppendLine($"To re-set target vessel, just enter another name.");
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine($"🛳❌Your target vessel is missing.");
                builder.AppendLine("Please enter vessel name to set it up.");
                builder.AppendLine();
            }
            if (user.PortTarget != null)
            {
                builder.AppendLine($"🏭✅ Your target port is - {user.PortTarget.portName}");
                builder.AppendLine($"🏭🔄 Enter /refresh_port to get port schedule.");
                builder.AppendLine($"To re-set target port, just enter another name.");
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine($"🏭❌ Your target port is missing.");
                builder.AppendLine("Please enter port name to set it up.");
                builder.AppendLine();
            }
            if (user.PrintAscending == true)
            {
                builder.AppendLine($"📅 Schedule will be printed in <b>ascending order</b> ⬇️ <i>(from top to bottom)</i>.");
                builder.AppendLine($"/order_descending to change order to descending (from bottom to top).");

            }
            else if (user.PrintAscending == false)
            {
                builder.AppendLine($"📅 Schedule will be printed in <b>descending order</b> ⬆️ <i>(from bottom to top)</i>.");
                builder.AppendLine($"/order_ascending to change order to ascending (from top to bottom).");
            }
            return await bot.SendTextMessageAsync(chat, builder.ToString(), ParseMode.Html);
        }

        async Task<Message> GetHelp(ITelegramBotClient bot)
        {
            StringBuilder builder = new();
            builder.AppendLine("Enter /status - to get status of your target vessel and port.");
            builder.AppendLine();
            builder.AppendLine($"🛳🔄 Enter /refresh_ship to get ship schedule.");
            builder.AppendLine($"To re-set target vessel, just enter another name.");
            builder.AppendLine();
            builder.AppendLine($"🏭🔄 Enter /refresh_port to get port schedule.");
            builder.AppendLine($"To re-set target port, enter port name with PORT keyword at start.");
            builder.AppendLine($"For example: \"PORT Manila\", or \"PORT Aarhus\"");
            builder.AppendLine();
            builder.AppendLine($"Enter /order_descending to change print order to descending.");
            builder.AppendLine($"Descending - Nearest date is last.");
            builder.AppendLine();
            builder.AppendLine($"Enter /order_ascending to change print order to descending.");
            builder.AppendLine($"Ascending - Nearest date is first.");
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
            sqlManager.RemoveShip(user.TelegramId.ToString());
            user = sqlManager.GetUser(update);
            return await bot.SendTextMessageAsync(chat, "🛳 Please enter vessel`s name. 🛳");
        }

        async Task<Message> SetupPort(ITelegramBotClient bot)
        {
            sqlManager.RemovePort(user.TelegramId.ToString());
            user = sqlManager.GetUser(update);
            return await bot.SendTextMessageAsync(chat, "🏭 Please enter port`s name. 🏭");
        }

        async Task<Message> RefreshShip(ITelegramBotClient bot)
        {
            if (user.VesselTarget == null)
            {
                return await _botClient.SendTextMessageAsync(chat, "⚓️ Your target vessel is missing, enter vessel name first. ⚓️");
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
                return await _botClient.SendTextMessageAsync(chat, "🌉 Your target port is missing, enter port name with PORT keyword at start.. 🌉");
            }
            else
            {
                return await SendPortSchedule();
            }
        }

        async Task<Message> SendShipSchedule()
        {
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
            //SqlManager sqlManager = new();
            Port port = sqlManager.GetPortFromDbByName(update.Message.Text);
            Ship ship = sqlManager.GetShipFromDbByName(update.Message.Text);

            if (port != null && ship != null)
            {
                InlineKeyboardButton portButton = new($"🏭 Port: {port.portName}{port.emoji}");
                portButton.CallbackData = "port " + port.portName;
                InlineKeyboardButton shipButton = new($"🛳 Ship: {ship.ShipName}");
                shipButton.CallbackData = ship.ShipName;
                List<InlineKeyboardButton> row1 = new();
                row1.Add(portButton);
                List<InlineKeyboardButton> row2 = new();
                row2.Add(shipButton);
                List<List<InlineKeyboardButton>> keyboard = new();
                keyboard.Add(row1);
                keyboard.Add(row2);
                InlineKeyboardMarkup inlineKeyboard = new(keyboard);
                return await _botClient.SendTextMessageAsync(chat.Id, "Found several results:", replyMarkup: inlineKeyboard);
            }

            if (port != null)
            {
                await _botClient.SendTextMessageAsync(chat.Id, "✅ Match found! ✅");
                await _botClient.SendTextMessageAsync(chat.Id, $"🏭 {port.emoji}{port.portName}{port.emoji} 🏭");
                sqlManager.RemovePort(user.TelegramId.ToString());
                sqlManager.AddPort(user.TelegramId.ToString(), port);
                return await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_port to recieve a schedule. 📅");
            }

            if (ship != null)
            {
                await _botClient.SendTextMessageAsync(chat.Id, "✅ Match found! ✅");
                await _botClient.SendTextMessageAsync(chat.Id, $"🛳 {ship.ShipName} 🛳");
                sqlManager.RemoveShip(user.TelegramId.ToString());
                sqlManager.AddShip(user.TelegramId.ToString(), ship);
                return await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_ship to recieve a schedule. 📅");
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
        _logger.LogInformation("\n SqlString: {sqlManager.sqlConnectstring}", sqlManager.sqlConnectstring);
    }
    private void ToLogSentMsg(Message sentMessage)
    {
        _logger.LogInformation("\n The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        _logger.LogInformation("\n Sent message text:{SentMessageId}", sentMessage.Text);

    }

}
