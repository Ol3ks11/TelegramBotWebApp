using Newtonsoft.Json;

namespace TelegramBotWebApp
{
    public class VesselsManager
    {
        public List<Vessel> vesselsList { get; set; }

        public VesselsManager(string consumerKey)
        {
            vesselsList = GetActiveVessels(consumerKey);
        }

        public List<Vessel> GetActiveVessels(string consumerKey)
        {
            string activeVesselsJsonStr = GetActiveVesselsJson(consumerKey).Result;
            var vesselsList = JsonConvert.DeserializeObject<List<Vessel>>(activeVesselsJsonStr);
            if (vesselsList != null)
            {
                Console.WriteLine("Active vessels count: " + vesselsList.Count);
            }
            else
            {
                Console.WriteLine("ActiveVesselsJSON Deserialize FAILURE");
            }
            return vesselsList ?? new List<Vessel>();
        }

        internal async Task<string> GetActiveVesselsJson(string consumerKey)
        {
            HttpRequestMessage request = new();
            string getPortsURL = "https://api.maersk.com/reference-data/vessels";
            request.RequestUri = new Uri(getPortsURL);
            request.Headers.Add("API-Version", "1");
            request.Headers.Add("Consumer-Key", consumerKey);
            HttpClient client = new();
            try
            {
                var maerskResponse = await client.SendAsync(request);
                string stringMaerskResponse = await maerskResponse.Content.ReadAsStringAsync();
                Console.WriteLine("ActiveVesselsJSON fetch is DONE");
                return stringMaerskResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine("ActiveVesselsJSON fetch FAILURE");
                return e.Message;
            }
        }

        public List<Vessel> GetMatchingVesselsFrActive(string name)
        {
            List<Vessel> matchingVesselsList = new();
            foreach (var vessel in vesselsList)
            {
                {
                    string vesselNameUpper = vessel.name.ToUpper();
                    if (vesselNameUpper.Contains(name.ToUpper()))
                    {
                        matchingVesselsList.Add(vessel);
                    }
                }
            }
            return matchingVesselsList;
        }
    }
}