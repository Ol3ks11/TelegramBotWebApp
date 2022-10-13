using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Examples.WebHook.Services;
using Telegram.Bot.Types;

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
        var conString = _configuration.GetValue<string>("ConnectionStrings:MyConString");
        await handleUpdateService.EchoAsync(update, conString);
        return Ok();
    }
}
