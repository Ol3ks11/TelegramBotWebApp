using Telegram.Bot;
using Telegram.Bot.Examples.WebHook;
using Telegram.Bot.Examples.WebHook.Services;
using System.Runtime.Caching;
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

VesselsManager vesselsManager = new();
Root root = vesselsManager.GetRoot();
builder.Services.AddSingleton<Root>(root);

var app = builder.Build();
app.UseRouting();
app.UseCors();
app.UseEndpoints(endpoints =>
{
    var token = botConfig.BotToken;
    endpoints.MapControllerRoute(name: "tgwebhook",
                                 pattern: $"bot/{token}",
                                 new { controller = "Webhook", action = "Post" });
    endpoints.MapControllers();
});
app.Run();