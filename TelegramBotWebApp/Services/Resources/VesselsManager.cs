using Newtonsoft.Json;
using System.Text;
using TelegramBotWebApp.Services.Resources;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Examples.WebHook.Services;
using Telegram.Bot.Examples.WebHook;

namespace TelegramBotWebApp
{
    public class VesselsManager
    {
        public List<string> BuildSchedule(Schedule rootSchedule, UserSet user)
        {
            StringBuilder builder = new();
            List<string> result = new();
            builder.AppendLine($"Schedule for <b>{user.targetVessel.vesselName}</b>:");
            builder.AppendLine();
            if (user.PrintAscending == true)
            {
                for (int i = 0; i < rootSchedule.vesselCalls.Count; i++)
                {
                    string portEmoji = rootSchedule.vesselCalls[i].facility.emoji;
                    string portName = rootSchedule.vesselCalls[i].facility.portName.ToUpper();
                    string carrierTerminalCode = rootSchedule.vesselCalls[i].facility.carrierTerminalCode;

                    builder.AppendLine($"<code>Port call:</code> {portEmoji} <b>{portName}</b>");
                    builder.AppendLine($"<code>Terminal code:</code> <i>{carrierTerminalCode}</i>");

                    for (int j = 0 ; j < rootSchedule.vesselCalls[i].callSchedules.Count; j++)
                    {
                        string callEventType      = rootSchedule.vesselCalls[i].callSchedules[j].transportEventTypeCode.ToUpper();
                        string callEventDateTime  = rootSchedule.vesselCalls[i].callSchedules[j].classifierDateTime.ToString("dd-MM-yyyy HH:mm");
                        string callEventClassCode = rootSchedule.vesselCalls[i].callSchedules[j].eventClassifierCode.ToUpper();

                        builder.AppendLine($"<code>{callEventType}:</code> {callEventDateTime} , {callEventClassCode}");
                    }
                    
                    builder.AppendLine();

                    if (builder.Length > 1800)
                    {
                        result.Add(builder.ToString());
                        builder.Clear();
                    }
                }
            }
            else
            {
                for (int i = rootSchedule.vesselCalls.Count - 1; i >= 0; i--)
                {
                    string portEmoji = rootSchedule.vesselCalls[i].facility.emoji;
                    string portName = rootSchedule.vesselCalls[i].facility.portName.ToUpper();
                    string carrierTerminalCode = rootSchedule.vesselCalls[i].facility.carrierTerminalCode;

                    builder.AppendLine($"<code>Port call:</code> {portEmoji} <b>{portName}</b>");
                    builder.AppendLine($"<code>Terminal code:</code> <i>{carrierTerminalCode}</i>");

                    for (int j = 0; j < rootSchedule.vesselCalls[i].callSchedules.Count; j++)
                    {
                        string callEventType = rootSchedule.vesselCalls[i].callSchedules[j].transportEventTypeCode.ToUpper();
                        string callEventDateTime = rootSchedule.vesselCalls[i].callSchedules[j].classifierDateTime.ToString("dd-MM-yyyy HH:mm");
                        string callEventClassCode = rootSchedule.vesselCalls[i].callSchedules[j].eventClassifierCode.ToUpper();

                        builder.AppendLine($"<code>{callEventType}:</code> {callEventDateTime} , {callEventClassCode}");
                    }

                    builder.AppendLine();

                    if (builder.Length > 1800)
                    {
                        result.Add(builder.ToString());
                        builder.Clear();
                    }
                }
            }
            result.Add(builder.ToString());
            return result;
        }
        public List<Vessel> GetActiveVesselsList(string consumerKey)
        {
            //RootActiveVessels vessels = new();
            //vessels = JsonConvert.DeserializeObject<RootActiveVessels>(GetActiveVesselsJson().Result);
            List<Vessel> activeVesselsList = JsonConvert.DeserializeObject<List<Vessel>>(GetActiveVesselsJson(consumerKey).Result);
            return activeVesselsList;

        }
        public async Task<string> GetActiveVesselsJson(string consumerKey)
        {
            HttpRequestMessage request = new();
            string getPortsURL = "https://api.maersk.com/schedules/active-vessels";
            request.RequestUri = new Uri(getPortsURL);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Consumer-Key", consumerKey);
            HttpClient client = new();
            try
            {
                var maerskResponse = await client.SendAsync(request);
                string stringMaerskResponse = await maerskResponse.Content.ReadAsStringAsync();
                return stringMaerskResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                client.Dispose();
            }
            return "fail";
        }
        public List<Vessel> GetMatchingVesselsFrActive(string name, string consumerKey)
        {
            //RootActiveVessels root = GetActiveVesselsList();
            List<Vessel> activeVesselsList = GetActiveVesselsList(consumerKey);
            List<Vessel> matchingVesselsList = new();
            foreach (var vessel in activeVesselsList)
            {
                if (vessel.vesselName.Contains(name.ToUpper()))
                {
                    matchingVesselsList.Add(vessel);
                }
            }
            return matchingVesselsList;
        }
    }
}