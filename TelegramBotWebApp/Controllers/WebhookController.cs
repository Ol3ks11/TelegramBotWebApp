using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Examples.WebHook.Services;
using Telegram.Bot.Types;
using TelegramBotWebApp.Services.Resources;

namespace Telegram.Bot.Examples.WebHook.Controllers;

public class WebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public WebhookController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromServices] HandleUpdateService handleUpdateService, [FromBody] Update update)
    {
        await handleUpdateService.EchoAsync(update);
        return Ok();
    }
}
