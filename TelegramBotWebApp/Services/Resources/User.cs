namespace TelegramBotWebApp.Services.Resources
{
    public class User
    {
        public int TelegramId { get; set; }
        public string Name { get; set; }
        public Ship VesselTarget { get; set; }
        public Port PortTarget { get; set; }
        public bool PrintAscending { get; set; }
    }
}
