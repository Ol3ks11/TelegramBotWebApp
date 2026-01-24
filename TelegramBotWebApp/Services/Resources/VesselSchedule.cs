using MaerskScheduleBot.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
public class VesselSchedule
{

    [JsonProperty("transportCalls")]
    public List<TransportCall> scheduleList { get; set; }
    public List<string> scheduleString;

    public void InitializeSchedule(User user)
    {
        this.UpDateSchedule(user);
        this.scheduleString = this.BuildScheduleString(user);
    }

    private string GetAPIKey()
    {
        string path = Path.Combine(Environment.CurrentDirectory, @"appsettings.json");
        string jsonData = System.IO.File.ReadAllText(path);

        var root = JObject.Parse(jsonData);
        string consumerKey = (string)root["BotConfiguration"]?["ConsumerKey"];
        Console.WriteLine(consumerKey[0]);
        return consumerKey;
    }

    private void UpDateSchedule(User user)
    {
        string json = GetScheduleJson(user.targetVessel.imoNumber).Result;
        
        string result = json.Length <= 10 ? json : json[..10];
        Console.WriteLine(result);

        var rootList = JsonConvert.DeserializeObject<List<Root>>(json);
        scheduleList = rootList[0].vesselSchedules[0].scheduleList;

        for (int i = 0; i < scheduleList.Count; i++)
        {
            if (scheduleList[i].timestamps.Any(t => t.eventDateTime < DateTime.Today.AddDays(-2)))
            {
                scheduleList.RemoveAt(i);
                i--;
            }
        }

        string fileName = "PortCodes+emoji.txt";
        string path = Path.Combine(Environment.CurrentDirectory, @"Resources\", fileName);

        var countryData = System.IO.File.ReadLines(path)
        .Select(line => line.Split(':'))
        .Where(parts => parts.Length == 2)
        .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

        foreach (var transportCall in scheduleList)
        {
            if (countryData.TryGetValue(transportCall.location.UNLocationCode, out string flag))
            {
                transportCall.location.locationFlag = flag;
            }
        }
    }

    private async Task<string> GetScheduleJson(string vesselIMONumber)
    {
        DateTime startDate = DateTime.Today.AddDays(-1);
        DateTime endDate = DateTime.Today.AddDays(180);

        string startDateStr = startDate.ToString("yyyy-MM-dd");
        string endDatestr = endDate.ToString("yyyy-MM-dd");

        HttpRequestMessage request = new();
        string getPortsURL = 
            "https://api.maersk.com/ocean/commercial-schedules/dcsa/v1/vessel-schedules?" +
            "vesselIMONumber=" + vesselIMONumber + "&" +
            "startDate" + startDateStr + "&" +
            "endDate" + endDatestr;
        request.RequestUri = new Uri(getPortsURL);
        //request.Headers.Add("API-Version", "1");
        request.Headers.Add("Consumer-Key", this.GetAPIKey());
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
        return "Failed to fetch the schedule";
    }

    private List<string> BuildScheduleString(User user)
    {
        StringBuilder builder = new();
        List<string> result = new();
        builder.AppendLine($"Schedule for <b>{user.targetVessel.name}</b>:");
        builder.AppendLine();

        if (user.PrintAscending)
        {
            foreach (var call in scheduleList)
            {
                builder.AppendLine($"<code>LOCA</code>: {call.location.locationFlag}<b>{call.location.locationName}</b>");
                builder.AppendLine($"<code>CODE</code>: {call.location.UNLocationCode} - {call.location.facilitySMDGCode}");

                if (call.timestamps == null || call.timestamps.Count == 0)
                {
                    builder.AppendLine("No schedule data available.");
                    continue;
                }

                Timestamp? arrival = call.timestamps.FirstOrDefault(t => t.eventClassifierCode == "ACT" && t.eventTypeCode == "ARRI")
                                 ?? call.timestamps.FirstOrDefault(t => t.eventClassifierCode == "EST" && t.eventTypeCode == "ARRI");

                Timestamp? departure = call.timestamps.FirstOrDefault(t => t.eventClassifierCode == "ACT" && t.eventTypeCode == "DEPA")
                                 ?? call.timestamps.FirstOrDefault(t => t.eventClassifierCode == "EST" && t.eventTypeCode == "DEPA");

                string arrivalTime = arrival != null ? arrival.eventDateTime.ToString("yyyy-MM-dd HH:mm") : "N/A";
                string arrivalType = arrival != null ? arrival.eventClassifierCode : "N/A";

                string departureTime = departure != null ? departure.eventDateTime.ToString("yyyy-MM-dd HH:mm") : "N/A";
                string departureType = departure != null ? departure.eventClassifierCode : "N/A";

                string stayDuration = "N/A";
                if (arrival != null && departure != null)
                {
                    TimeSpan stayInterval = departure.eventDateTime - arrival.eventDateTime;
                    int sI = (int)Math.Floor(stayInterval.TotalHours);
                    stayDuration = sI.ToString() + " hours";
                }

                builder.AppendLine($"<code>ARRI</code>: {arrivalTime} , {arrivalType}");
                builder.AppendLine($"<code>DEPA</code>: {departureTime} , {departureType}");
                builder.AppendLine($"<code>STAY</code>: {stayDuration}");
                builder.AppendLine();

                if (builder.Length > 1800)
                {
                    result.Add(builder.ToString());
                    builder.Clear();
                }

            }

            if (builder.Length > 0)
            {
                result.Add(builder.ToString());
            }
        }
        else
        {
            for (int i = scheduleList.Count - 1; i >= 0; i--)
            {
                var call = scheduleList[i];
                builder.AppendLine($"Port Call:{call.location.locationFlag}<b>{call.location.locationName}</b>");

                if (call.timestamps == null || call.timestamps.Count == 0)
                {
                    builder.AppendLine("No schedule data available.");
                    continue;
                }

                Timestamp? arrival = call.timestamps.FirstOrDefault(t => t.eventClassifierCode == "ACT" && t.eventTypeCode == "ARRI")
                                 ?? call.timestamps.FirstOrDefault(t => t.eventClassifierCode == "EST" && t.eventTypeCode == "ARRI");

                Timestamp? departure = call.timestamps.FirstOrDefault(t => t.eventClassifierCode == "ACT" && t.eventTypeCode == "DEPA")
                                 ?? call.timestamps.FirstOrDefault(t => t.eventClassifierCode == "EST" && t.eventTypeCode == "DEPA");

                string arrivalTime = arrival != null ? arrival.eventDateTime.ToString("yyyy-MM-dd") : "N/A";
                string arrivalType = arrival != null ? arrival.eventTypeCode : "N/A";

                string departureTime = departure != null ? departure.eventDateTime.ToString("yyyy-MM-dd") : "N/A";
                string departureType = departure != null ? departure.eventTypeCode : "N/A";

                string stayDuration = "N/A";
                if (arrival != null && departure != null)
                {
                    TimeSpan stayInterval = departure.eventDateTime - arrival.eventDateTime;
                    stayDuration = stayInterval.ToString("hh") + " hours";
                }

                builder.AppendLine($"<code>ARRI:</code>: {arrivalTime} , {arrivalType}");
                builder.AppendLine($"<code>DEPA:</code>: {departureTime} , {departureType}");
                builder.AppendLine($"Stay Duration: {stayDuration}");
                builder.AppendLine();

                if (builder.Length > 1800)
                {
                    result.Add(builder.ToString());
                    builder.Clear();
                }
            }
            
            if (builder.Length > 0)
            {
                result.Add(builder.ToString());
            }
        }
        return result;
    }
}
