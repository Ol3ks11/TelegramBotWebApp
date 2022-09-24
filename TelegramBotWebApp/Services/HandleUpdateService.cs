using Newtonsoft.Json;
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

    private string enterVessel = "Please enter Vessel name.";

    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger)
    {
        _botClient = botClient;
        _logger = logger;
        string activeVesselsJsonPath = Path.Combine(Environment.CurrentDirectory, @"Services\Resources\active.json");
        vesselManager = JsonConvert.DeserializeObject<VesselsManager>(System.IO.File.ReadAllText(activeVesselsJsonPath));
    }

    public async Task EchoAsync(Update update)
    {
        var handler = update.Type switch
        {
            UpdateType.Message            => BotOnMessageReceived(update.Message!),
            UpdateType.EditedMessage      => BotOnMessageReceived(update.EditedMessage!),
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

    private async Task BotOnMessageReceived(Message message)
    {
        _logger.LogInformation("Receive message type: {message.Type}", message.Type);
        _logger.LogInformation("From: {message.From.FirstName} {message.From.LastName}", message.From.FirstName, message.From.LastName);
        _logger.LogInformation("MessageText: {message.Text}", message.Text);
        if (message.Type != MessageType.Text)
            return;

        var action = message.Text!.Split(' ')[0] switch
        {
            "/setup"   => Setup(_botClient, message),
            "/refresh" => Refresh(_botClient, message),
            _          => CheckIfNameLegit(_botClient, message)
        };

        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        _logger.LogInformation("Sent message text:{SentMessageId}", sentMessage.Text);

        async Task<Message> Setup(ITelegramBotClient bot, Message message)
        {
            await bot.UnpinAllChatMessages(message.Chat.Id);
            return await bot.SendTextMessageAsync(message.Chat.Id, enterVessel);
        }

        async Task<Message> Refresh(ITelegramBotClient bot, Message message)
        {
            if (message.Chat.PinnedMessage == null)
            {
                _logger.LogInformation("Missing Pinned message.");
                return await _botClient.SendTextMessageAsync(message.Chat.Id, "⚓️ Please setup your Vessel via /Setup command. ⚓️");
            }
            else
            {
                return await SendSchedule();
            }
        }

        async Task<Message> SendSchedule()
        {
            var ship = vesselManager.ships.Find(ship => ship.ShipName == message.Chat.PinnedMessage.Text);
            int shipIndex = vesselManager.ships.IndexOf(ship);
            vesselManager.UpdateShipPorts(shipIndex);
            _logger.LogInformation("Sending schedule for {ship.ShipName}", ship.ShipName);
            return await _botClient.SendTextMessageAsync(message.Chat.Id, vesselManager.BuildSchedule(shipIndex),ParseMode.Html);
        }

        async Task<Message> CheckIfNameLegit(ITelegramBotClient bot, Message message)
        {
            List<Ship> shipList = vesselManager.ships.FindAll(ship => ship.ShipName.Contains(message.Text.ToUpper()));

            if (shipList.Count != 0)
            {
                if (shipList.Count < 2)
                {
                    _logger.LogInformation("Match found.");
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "✅ Match found! ✅");
                    var messageToPin = await _botClient.SendTextMessageAsync(message.Chat.Id, shipList[0].ShipName);
                    await _botClient.PinChatMessageAsync(message.Chat.Id, messageToPin.MessageId);
                    return await _botClient.SendTextMessageAsync(message.Chat.Id, "🔄 Please enter /refresh to recieve a schedule. 📅");
                }
                _logger.LogInformation("Found more then one match.");
                return await _botClient.SendTextMessageAsync(message.Chat.Id, "⚠ Found more then one match, be more specific. ⚠");
            }
            _logger.LogInformation("Can not find a vessel with a matching name.");
            return await _botClient.SendTextMessageAsync(message.Chat.Id, "❌ Can not find a vessel with a matching name. ❌");
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
