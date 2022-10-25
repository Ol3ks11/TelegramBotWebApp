using System;
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
            UpdateType.CallbackQuery      => BotOnCallBackReceived(update, chat),
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

    private async Task BotOnCallBackReceived(Update update, Chat chat)
    {
        UserSet user = ParsePinnedMsg(update).Result;
        //Chat chat = update.CallbackQuery.Message.Chat;
        if (update.CallbackQuery != null)
        {
            if (update.CallbackQuery.Data.Split(' ')[0] == "port")
            {
                string portname = update.CallbackQuery.Data.Remove(0, 4).Trim();
                Port port = vesselManager.GetPortFromActive(portname);
                await _botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"🏭 {port.emoji}{port.portName}");
                await EditPinnedPort(port);
                await _botClient.SendTextMessageAsync(chat.Id, $"🏭 {port.emoji}{port.portName}");
                await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_port to recieve a schedule. 📅");
            }
            else
            {
                Ship ship = vesselManager.GetShipFromActive(update.CallbackQuery.Data);
                await _botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"🛳 {ship.ShipName}");
                await EditPinnedShip(ship);
                await _botClient.SendTextMessageAsync(chat.Id, $"🛳 {ship.ShipName}");
                await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_ship to recieve a schedule. 📅");
            }
        }

        async Task<Message> EditPinnedShip(Ship ship)
        {
            Message pindMsg = chat.PinnedMessage;
            string[] pinnedMsg = chat.PinnedMessage.Text.Split(';');
            StringBuilder builder = new();
            builder.Append($"🛳✅: {ship.ShipName},{ship.ShipCode};");
            builder.Append($"{pinnedMsg[1]};");
            builder.Append($"{pinnedMsg[2]}");

            return await _botClient.EditMessageTextAsync(chat.Id, pindMsg.MessageId, builder.ToString());
        }

        async Task<Message> EditPinnedPort(Port port)
        {
            Message pindMsg = chat.PinnedMessage;
            string[] pinnedMsg = chat.PinnedMessage.Text.Split(';');
            StringBuilder builder = new();
            builder.Append($"{pinnedMsg[0]};");
            builder.Append($"🏭✅: {port.portName},{port.GeoId};");
            builder.Append($"{pinnedMsg[2]}");

            return await _botClient.EditMessageTextAsync(chat.Id, pindMsg.MessageId, builder.ToString());
        }

    }
    private async Task BotOnMessageReceived(Update update, Chat chat)
    {
        UserSet user = ParsePinnedMsg(update).Result;
        ToLogRecievedMsg(update,user);
        

        if (update.Message.Type != MessageType.Text)
            return;
        var action = update.Message.Text!.Split(' ')[0] switch
        {
            "/start"            => Start(_botClient),
            "/status"           => GetStatus(_botClient),
            "/help"             => GetHelp(_botClient),

            "/refresh_ship"     => RefreshShip(_botClient),
            "/refresh_port"     => RefreshPort(_botClient),

            "/order_descending" => ChangePrintOrder(_botClient,0),
            "/order_ascending"  => ChangePrintOrder(_botClient,1),
            _                   => CheckIfNameLegit(_botClient)
        };
        Message sentMessage = await action;
        ToLogSentMsg(sentMessage);

        async Task<Message> Start(ITelegramBotClient bot)
        {
            //await SetPinnedMsg(chat);
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
            builder.AppendLine($"To re-set target port, just enter another name.");
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
            string isAscending = null;
            EditPrintOrder(option);
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
            Port port = vesselManager.GetPortFromActive(update.Message.Text);
            Ship ship = vesselManager.GetShipFromActive(update.Message.Text);

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
                EditPinnedPort(port);
                return await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_port to recieve a schedule. 📅");
            }

            if (ship != null)
            {
                await _botClient.SendTextMessageAsync(chat.Id, "✅ Match found! ✅");
                await _botClient.SendTextMessageAsync(chat.Id, $"🛳 {ship.ShipName} 🛳");
                EditPinnedShip(ship);
                return await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_ship to recieve a schedule. 📅");
            }
            return await _botClient.SendTextMessageAsync(chat.Id, "❌ Can not find a matching name. ❌");
        }

        async Task<Message> EditPinnedShip(Ship ship)
        {
            Message pindMsg = chat.PinnedMessage;
            string[] pinnedMsg = chat.PinnedMessage.Text.Split(';');
            StringBuilder builder = new();
            builder.Append($"🛳✅: {ship.ShipName},{ship.ShipCode};");
            builder.Append($"{pinnedMsg[1]};");
            builder.Append($"{pinnedMsg[2]}");
            
            return await _botClient.EditMessageTextAsync(chat.Id,pindMsg.MessageId, builder.ToString());
        }

        async Task<Message> EditPinnedPort(Port port)
        {
            Message pindMsg = chat.PinnedMessage;
            string[] pinnedMsg = chat.PinnedMessage.Text.Split(';');
            StringBuilder builder = new();
            builder.Append($"{pinnedMsg[0]};");
            builder.Append($"🏭✅: {port.portName},{port.GeoId};");
            builder.Append($"{pinnedMsg[2]}");

            return await _botClient.EditMessageTextAsync(chat.Id, pindMsg.MessageId, builder.ToString());
        }

        async Task<Message> EditPrintOrder(int option)
        {
            Message pindMsg = chat.PinnedMessage;
            string[] pinnedMsg = chat.PinnedMessage.Text.Split(';');
            StringBuilder builder = new();
            builder.Append($"{pinnedMsg[0]};");
            builder.Append($"{pinnedMsg[1]};");
            if (option == 1)
            {
                builder.Append($"📅:⬇️");
            }
            else if (option == 0)
            {
                builder.Append($"📅:⬆️");
            }
            return await _botClient.EditMessageTextAsync(chat.Id, pindMsg.MessageId, builder.ToString());
        }

    }
    private async Task<UserSet> ParsePinnedMsg(Update update)
    {
        //pinned message format: "🛳✅: Vessel Name, Code; 🏭✅: Port Name, GeoId; 📅: ⬇️/⬆️"
        UserSet user = new();
        Chat chat = GetChat(update).Result;
        if (IsPinMsgLegit(chat) == false)
        {
            _logger.LogInformation("Pin Message is NOT legit");
            await _botClient.UnpinAllChatMessages(chat.Id);
            user.PortTarget = null;
            user.VesselTarget = null;
            await SetPinnedMsg(chat);
            return user;
        }
        _logger.LogInformation("Pin Message IS legit");
        string[] settings = chat.PinnedMessage.Text.Split(';');

        Ship userShip = new();
        if (settings[0].Split(',')[0].Split(':')[0].Trim()[1] == '✅')
        {
            userShip.ShipName = settings[0].Split(',')[0].Split(':')[1].Trim();
            userShip.ShipCode = settings[0].Split(',')[1].Trim();
        }

        Port userPort = new();
        if (settings[1].Split(':')[0].Trim()[1] == '✅')
        {
            userPort.portName = settings[1].Split(',')[0].Split(':')[1].Trim();
            userPort.GeoId = settings[1].Split(',')[1].Trim();
        }

        user.VesselTarget = userShip;
        user.PortTarget = userPort;
        if (settings[2].Split(':')[1].Trim() == "⬇️")
        {
            user.PrintAscending = true;
        }
        else if (settings[2].Split(':')[1].Trim() == "⬆️")
        {
            user.PrintAscending = false;
        }
        return user;
    }

    private async Task SetPinnedMsg(Chat chat)
    {
        var message = await _botClient.SendTextMessageAsync(chat.Id, "🛳🚫: Name, Code; 🏭🚫: Name, GeoId; 📅:⬇️");
        await _botClient.PinChatMessageAsync(chat.Id, message.MessageId);
    }

    private bool IsPinMsgLegit(Chat chat)
    {
        if (chat.PinnedMessage == null)
        {
            return false;
        }
        string pinnedMsg = chat.PinnedMessage.Text;
        if (pinnedMsg.Contains("🛳") && pinnedMsg.Contains("🏭") && pinnedMsg.Contains("📅"))
        {
            return true;
        }
        return false;
    }

    private async Task<Chat> GetChat(Update update)
    {
        if (update.CallbackQuery != null)
        {
            return await _botClient.GetChatAsync(update.CallbackQuery.Message.Chat.Id);
        }
        return await _botClient.GetChatAsync(update.Message.Chat.Id);
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
    private void ToLogRecievedMsg(Update update, UserSet user)
    {
        _logger.LogInformation("\n Receive message type: {message.Type}", update.Message.Type);
        _logger.LogInformation("\n From: {message.From.FirstName} {message.From.LastName}", update.Message.From.FirstName, update.Message.From.LastName);
        _logger.LogInformation("\n MessageText: {message.Text}", update.Message.Text);
        _logger.LogInformation("\n UserSettings: {shipName} {shipCode} {portName}", user.VesselTarget.ShipName,user.VesselTarget.ShipCode,user.PortTarget.portName);
    }
    private void ToLogSentMsg(Message sentMessage)
    {
        _logger.LogInformation("\n The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        _logger.LogInformation("\n Sent message text:{SentMessageId}", sentMessage.Text);

    }

}
