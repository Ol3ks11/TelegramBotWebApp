using Newtonsoft.Json;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Xml.Linq;
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
    private VesselsManager vesselManager; 

    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger)
    {
        _botClient = botClient;
        _logger = logger;
        string activeVesselsJsonPath = Path.Combine(Environment.CurrentDirectory, @"Services\Resources\active.json");
        vesselManager = JsonConvert.DeserializeObject<VesselsManager>(System.IO.File.ReadAllText(activeVesselsJsonPath));
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
        _logger.LogInformation("Receive message type: {message.Type}", update.Message.Type);
        _logger.LogInformation("From: {message.From.FirstName} {message.From.LastName}", update.Message.From.FirstName, update.Message.From.LastName);
        _logger.LogInformation("MessageText: {message.Text}", update.Message.Text);
        if (update.Message.Type != MessageType.Text)
            return;

        var action = update.Message.Text!.Split(' ')[0] switch
        {
            "/start"   => Setup(_botClient),
            "/setup"   => Setup(_botClient),
            "/refresh" => Refresh(_botClient),
            _          => CheckIfNameLegit(_botClient)
        };

        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        _logger.LogInformation("Sent message text:{SentMessageId}", sentMessage.Text);

        async Task<Message> Setup(ITelegramBotClient bot)
        {
            await bot.UnpinAllChatMessages(chat);
            return await bot.SendTextMessageAsync(chat, "Please enter Vessel name.");
        }

        async Task<Message> Refresh(ITelegramBotClient bot)
        {
            if (chat.PinnedMessage == null)
            {
                _logger.LogInformation("Missing Pinned message.");
                return await _botClient.SendTextMessageAsync(chat, "⚓️ Please setup your Vessel via /Setup command. ⚓️");
            }
            else
            {
                return await SendSchedule();
            }
        }

        async Task<Message> SendSchedule()
        {
            var ship = vesselManager.ships.Find(ship => ship.ShipName == chat.PinnedMessage.Text);
            int shipIndex = vesselManager.ships.IndexOf(ship);
            vesselManager.UpdateShipPorts(shipIndex);
            _logger.LogInformation("Sending schedule for {ship.ShipName}", ship.ShipName);
            return await _botClient.SendTextMessageAsync(chat, vesselManager.BuildSchedule(shipIndex),ParseMode.Html);
        }

        async Task<Message> CheckIfNameLegit(ITelegramBotClient bot)
        {
            List<Ship> shipList = vesselManager.ships.FindAll(ship => ship.ShipName.Contains(update.Message.Text.ToUpper()));

            if (shipList.Count != 0)
            {
                if (shipList.Count < 2)
                {
                    _logger.LogInformation("Match found.");
                    await _botClient.SendTextMessageAsync(chat, "✅ Match found! ✅");
                    var messageToPin = await _botClient.SendTextMessageAsync(chat, shipList[0].ShipName);
                    await _botClient.PinChatMessageAsync(chat, messageToPin.MessageId);
                    return await _botClient.SendTextMessageAsync(chat, "🔄 Please enter /refresh to recieve a schedule. 📅");
                }
                _logger.LogInformation("Found more then one match.");
                return await _botClient.SendTextMessageAsync(chat, "⚠ Found more then one match, be more specific. ⚠");
            }
            _logger.LogInformation("Can not find a vessel with a matching name.");
            return await _botClient.SendTextMessageAsync(chat, "❌ Can not find a vessel with a matching name. ❌");
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
}
