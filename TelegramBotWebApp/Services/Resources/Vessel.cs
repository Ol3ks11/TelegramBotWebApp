using TelegramBotWebApp.Services.Resources;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

public class Vessel
{

    [JsonProperty("vesselName")]
    public string vesselName { get; set; }

    [JsonProperty("carrierVesselCode")]
    public string carrierVesselCode { get; set; }

    public Schedule schedule { get; set; }

    public void GetSchedule(string comsumerKey)
    {
        schedule = JsonConvert.DeserializeObject<Schedule>(GetScheduleJson(comsumerKey).Result);

        string fileName = "emoji_flags.txt";
        string path = Path.Combine(Environment.CurrentDirectory, @"Services\Resources\", fileName);
        string[] emojis = File.ReadAllLines(path);
        foreach (var vesselCall in schedule.vesselCalls)
        {
            foreach (var line in emojis)
            {
                if (vesselCall.facility.countryName.Contains(line.Split(':')[1].Trim()))
                {
                    vesselCall.facility.emoji = line.Split(':')[0].Trim();
                }
            }
        }
    }

    private async Task<string> GetScheduleJson(string consumerKey)
    {
        DateTime todayDate = DateTime.Today.AddDays(-1);
        string today = todayDate.ToString("yyyy-MM-dd");
        string plus90 = todayDate.AddDays(90).ToString("yyyy-MM-dd");
        HttpRequestMessage request = new();
        string getPortsURL = "https://api.maersk.com/schedules/vessel-schedules?carrierVesselCode=" 
                             +carrierVesselCode+"&fromDate="+today+"&toDate="+plus90+"&carrierCodes=MAEU%2CMCPU%2CSEAU%2CSEJJ";
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
        return null;
    }
}