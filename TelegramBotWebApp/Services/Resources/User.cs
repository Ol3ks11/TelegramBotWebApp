using Telegram.Bot.Types;

public class User
{
    public User()
    {
        chat = new Chat();
        languageCode = "blank";
        targetVessel = new Vessel();
        PrintAscending = true;
        requestCount = 0;
        lastActive = DateTime.Now;
    }
    public Chat chat { get; set; }
    public string languageCode { get; set; }
    public Vessel targetVessel { get; set; }
    public bool PrintAscending { get; set; }
    public int requestCount { get; set; }
    public DateTime lastActive { get; set; }

}