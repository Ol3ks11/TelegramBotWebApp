using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Examples.WebHook;
using Telegram.Bot.Examples.WebHook.Services;
using TelegramBotWebApp;
using TelegramBotWebApp.Services.Resources;


var builder = WebApplication.CreateBuilder(args);
var botConfig = builder.Configuration.GetSection("BotConfiguration").Get<BotConfiguration>();

builder.Logging.ClearProviders();
builder.Logging.AddAzureWebAppDiagnostics();
builder.Services.AddHostedService<ConfigureWebhook>();
builder.Services.AddHttpClient("tgwebhook")
    .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botConfig.BotToken, httpClient));
builder.Services.AddScoped<HandleUpdateService>();
builder.Services.AddControllers().AddNewtonsoftJson();

VesselsManager vesselsManager = new(botConfig.ConsumerKey);
//Root root = vesselsManager.GetRoot();
builder.Services.AddSingleton<VesselsManager>(vesselsManager);

var app = builder.Build();
app.UseRouting();
app.UseCors();
app.UseEndpoints(endpoints =>
{
    var token = botConfig.BotToken;
    endpoints.MapControllerRoute(name: "tgwebhook",
                                 pattern: $"bot/{token}",
                                 new { controller = "Webhook", action = "Post" });

    endpoints.MapControllerRoute(name: "pingpoint",
                                 pattern: $"bot/ping",
                                 new { controller = "Webhook", action = "Send200" });

    endpoints.MapControllers();
});
app.Run();