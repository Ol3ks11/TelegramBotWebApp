using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace TelegramBotWebApp
{
    public class UserManager
    {
        public List<User> users { get; set; }
        public DateTime LastUpdated { get; set; }
        private string filePath;
        public UserManager()
        {
            string homeDir = Environment.GetEnvironmentVariable("HOME") ?? AppDomain.CurrentDomain.BaseDirectory;
            filePath = Path.Combine(homeDir, "data", "users.json");
            users = GetUsersFrJSON();
            RemoveOldUsers();
        }

        private List<User> GetUsersFrJSON()
        {
            users = JsonConvert.DeserializeObject<List<User>>(System.IO.File.ReadAllText(filePath)) ?? new List<User>();
            return users;
        }

        private void UpdateJSON()
        {
            string json = JsonConvert.SerializeObject(users, Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json);
        }

        private void RemoveOldUsers()
        {
            if ((DateTime.Now - LastUpdated).TotalHours >= 24)
            {
                DateTime threshold = DateTime.Now.AddDays(-90);
                users.RemoveAll(u => u.lastActive < threshold);
                UpdateJSON();
                LastUpdated = DateTime.Now;
            }
        }

        private void UpdateUser(User user)
        {
            if (user != null)
            {
                if (users.Any(u => u.chat.Id == user.chat.Id))
                {
                    int index = users.FindIndex(u => u.chat.Id == user.chat.Id);
                    users.RemoveAt(index);
                    users.Add(user);
                    Console.WriteLine(user.chat.Username + " user updated.");
                    UpdateJSON();
                    GetUsersFrJSON();
                }
                return;
            }
            return;
        }

        public void AddCounter(User user)
        {
            user.lastActive = DateTime.Now;
            user.requestCount++;
            Console.WriteLine(user.chat.Username + " counter is " + user.requestCount.ToString());
            UpdateUser(user);
        }

        public User GetUser(Update update)
        {
            // to change for getChatid later
            long chatId;
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
            {
                chatId = update.CallbackQuery.Message.Chat.Id;
            }
            else //(update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                chatId = update.Message.Chat.Id;
            }
            // to change for getChatid later

            if (!users.Any(u => u.chat.Id == chatId))
            {
                Console.WriteLine("New user found.");
                return NewUser(update);
            }
            else
            {
                return users.Find(u => u.chat.Id == chatId);
            }
        }

        public User NewUser(Update update)
        {
            User user = new User();
            user.chat = update.Message.Chat;
            if (update.Message.From != null)
            {
                user.languageCode = update.Message.From.LanguageCode ?? "N/A";
                user.lastActive = DateTime.Now;
                user.requestCount += 1;
            }
            users.Add(user);
            string json = JsonConvert.SerializeObject(users, Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json);
            return user;
        }

        public List<User> GetTop10Users()
        {
            return users.OrderByDescending(u => u.requestCount).Take(10).ToList();
        }
    }
}
