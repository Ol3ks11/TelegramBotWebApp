using System;
using System.Configuration;
using System.Text;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotWebApp;

namespace Telegram.Bot.Examples.WebHook.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;
    private readonly BotConfiguration _botConfig;
    private UserSet user = new();
    private Chat chat = new();
    private VesselsManager vesselManager;
    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger, IConfiguration configuration)
    {
        _botClient = botClient;
        _logger = logger;
        _botConfig = configuration.GetSection("BotConfiguration").Get<BotConfiguration>();
    }
    public async Task EchoAsync(Update update)
    {
        //chat = GetChat(update).Result;
        //user = ParsePinnedMsg(update).Result;

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
        if (update.CallbackQuery != null)
        {
            chat = GetChat(update).Result;
            user = ParsePinnedMsg(update).Result;

            if (update.CallbackQuery != null)
            {
                List<Vessel> ships = vesselManager.GetMatchingVesselsFrActive(update.CallbackQuery.Data, _botConfig.ConsumerKey);
                Vessel ship = ships.Where(x => x.vesselName.Equals(update.CallbackQuery.Data)).First();
                await _botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"🛳 {ship.vesselName}");
                await EditPinnedShip(ship);
                await _botClient.SendTextMessageAsync(chat.Id, $"🛳 {ship.vesselName}");
                await _botClient.SendTextMessageAsync(chat.Id, "🔄 Please enter /refresh_ship to recieve a schedule. 📅");
            }

            async Task<Message> EditPinnedShip(Vessel ship)
            {
                //Message pindMsg = GetChat(update).Result.PinnedMessage;
                Message pindMsg = chat.PinnedMessage;
                string[] pinnedMsg = pindMsg.Text.Split(';');
                StringBuilder builder = new();
                builder.Append($"🛳✅: {ship.vesselName},{ship.carrierVesselCode};");
                builder.Append($"{pinnedMsg[1]}");
                return await _botClient.EditMessageTextAsync(chat.Id, pindMsg.MessageId, builder.ToString());
            }
        }
    }
    private async Task BotOnMessageReceived(Update update)
    {
        chat = GetChat(update).Result;
        user = ParsePinnedMsg(update).Result;
        ToLogRecievedMsg(update,user);

        if (update.Message.Type != MessageType.Text)
            return;
        var action = update.Message.Text!.Split(' ')[0] switch
        {
            "/start"            => Start(_botClient),
            "/status"           => GetStatus(_botClient),
            "/help"             => GetHelp(_botClient),

            "/refresh_ship"     => RefreshShip(_botClient),
          //"/refresh_port"     => RefreshPort(_botClient),

            "/order_descending" => ChangePrintOrder(_botClient,0),
            "/order_ascending"  => ChangePrintOrder(_botClient,1),
            _                   => CheckIfNameLegit(_botClient)
        };
        Message sentMessage = await action;
        ToLogSentMsg(sentMessage);

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
            if (user.targetVessel != null)
            {
                builder.AppendLine($"🛳✅ Your target vessel is - {user.targetVessel.vesselName}");
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
            return await _botClient.SendTextMessageAsync(chat, builder.ToString(), ParseMode.Html);
        }

        async Task<Message> GetHelp(ITelegramBotClient bot)
        {
            StringBuilder builder = new();
            builder.AppendLine("Enter /status - to get status of your target vessel and port.");
            builder.AppendLine();
            builder.AppendLine($"🛳🔄 Enter /refresh_ship to get ship schedule.");
            builder.AppendLine($"To re-set target vessel, just enter another name.");
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
            if (user.targetVessel == null)
            {
                return await _botClient.SendTextMessageAsync(chat, "⚓️ Your target vessel is missing, enter vessel name first. ⚓️");
            }
            else
            {
                return await SendShipSchedule();
            }
        }

        async Task<Message> SendShipSchedule()
        {
            Vessel vessel = user.targetVessel;
            vessel.GetSchedule(_botConfig.ConsumerKey);
            List<string> schedule = vesselManager.BuildSchedule(vessel.schedule, user);
            for (int i = 0; i < schedule.Count - 1; i++)
            {
                await _botClient.SendTextMessageAsync(chat.Id, schedule[i], ParseMode.Html);
            }
            return await _botClient.SendTextMessageAsync(chat.Id, schedule[schedule.Count - 1], ParseMode.Html);
        }

        async Task<Message> CheckIfNameLegit(ITelegramBotClient bot)
        {
            List<Vessel> shipList = vesselManager.GetMatchingVesselsFrActive(update.Message.Text, _botConfig.ConsumerKey);
            List<List<InlineKeyboardButton>> keyboard = new();

            foreach (var ship in shipList)
            {
                InlineKeyboardButton shipButton = new($"🛳 Ship: {ship.vesselName}");
                shipButton.CallbackData = ship.vesselName;
                List<InlineKeyboardButton> row = new();
                row.Add(shipButton);
                keyboard.Add(row);
            }
            if (keyboard.Count > 1)
            {
                InlineKeyboardMarkup inlineKeyboard = new(keyboard);
                return await _botClient.SendTextMessageAsync(chat.Id, "Found several results:", replyMarkup: inlineKeyboard);
            }
            else if (keyboard.Count == 1)
            {
                InlineKeyboardMarkup inlineKeyboard = new(keyboard);
                return await _botClient.SendTextMessageAsync(chat.Id, "Match found:", replyMarkup: inlineKeyboard);
            }
            else
            {
                return await _botClient.SendTextMessageAsync(chat.Id, "❌ Can not find a matching name. ❌");
            }
        }

        async Task<Message> EditPrintOrder(int option)
        {
            Message pindMsg = chat.PinnedMessage;
            string[] pinnedMsg = chat.PinnedMessage.Text.Split(';');
            StringBuilder builder = new();
            builder.Append($"{pinnedMsg[0]};");
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
        //pinned message format: "🛳✅: Vessel Name, Code; 📅: ⬇️/⬆️"
        UserSet user = new();
        if (IsPinMsgLegit() == false)
        {
            //_logger.LogInformation("Pin Message is NOT legit");
            await _botClient.UnpinAllChatMessages(chat.Id);
            user.targetVessel = null;
            await SetPinnedMsg();
            return user;
        }
        //_logger.LogInformation("Pin Message IS legit");
        string[] settings = chat.PinnedMessage.Text.Split(';');

        if (settings[0].Split(',')[0].Split(':')[0].Contains("✅"))
        {
            Vessel userShip = new();
            userShip.vesselName = settings[0].Split(',')[0].Split(':')[1].Trim();
            userShip.carrierVesselCode = settings[0].Split(',')[1].Trim();
            user.targetVessel = userShip;
        }

        if (settings[1].Split(':')[1].Trim() == "⬇️")
        {
            user.PrintAscending = true;
        }
        else if (settings[1].Split(':')[1].Trim() == "⬆️")
        {
            user.PrintAscending = false;
        }
        return user;
    }
    private async Task SetPinnedMsg()
    {
        var message = await _botClient.SendTextMessageAsync(chat.Id, "🛳🚫: Name, Code; 📅:⬇️");
        await _botClient.PinChatMessageAsync(chat.Id, message.MessageId);
    }
    private bool IsPinMsgLegit()
    {
        if (chat.PinnedMessage == null)
        {
            return false;
        }
        string pinnedMsg = chat.PinnedMessage.Text;
        if (pinnedMsg.Contains("🛳") && pinnedMsg.Contains("📅"))
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
        _logger.LogInformation("\n MessageText: {_consumerKey}", _botConfig.ConsumerKey);
    }
    private void ToLogSentMsg(Message sentMessage)
    {
        _logger.LogInformation("\n The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        _logger.LogInformation("\n Sent message text:{SentMessageId}", sentMessage.Text);

    }

}
