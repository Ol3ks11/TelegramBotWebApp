using Newtonsoft.Json;
using System.Text;

namespace TelegramBotWebApp.Services.Resources
{
    public class VesselsManager
    {
        [JsonProperty("vessels")]
        public List<Ship> ships { get; set; }

        public void UpdateShipPorts(int shipsId)
        {
            Ship temp = new();
            temp = JsonConvert.DeserializeObject<Ship>(GetPortsJson(ships[shipsId]).Result);
            temp.ShipName = ships[shipsId].ShipName;
            temp.ShipCode = ships[shipsId].ShipCode;
            ships[shipsId] = temp;
        }

        private async Task<string> GetPortsJson(Ship ship)
        {
            string fromDateStr = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            string toDateStr = DateOnly.FromDateTime(DateTime.Now.AddDays(89)).ToString("yyyy-MM-dd");

            HttpRequestMessage requestForPortsList = new();
            string getPortsURL = "https://api.maerskline.com/maeu/schedules/vessel?vesselCode="
                +ship.ShipCode+"&fromDate="+ fromDateStr + "&toDate="+ toDateStr;
            requestForPortsList.RequestUri = new Uri(getPortsURL);
            HttpClient client = new();
            try
            {
                var maerskResponse = await client.SendAsync(requestForPortsList);
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

        public string BuildSchedule(int shipIndex)
        {
            UpdateShipPorts(shipIndex);
            StringBuilder builder = new();
            builder.AppendLine("Schedule for <b>" + ships[shipIndex].ShipName + "</b>:");
            builder.AppendLine();
            for(int i= ships[shipIndex].Ports.Count-1;i>0;i--)
            {
                builder.AppendLine("Port call:  <b>" + ships[shipIndex].Ports[i].port.ToUpper() + "</b>");
                builder.AppendLine("Terminal: " + ships[shipIndex].Ports[i].terminal);
                builder.AppendLine("ARR: " + ships[shipIndex].Ports[i].arrival.ToString("dd-MM-yyyy HH:mm"));
                builder.AppendLine("DEP: " + ships[shipIndex].Ports[i].departure.ToString("dd-MM-yyyy HH:mm"));
                builder.AppendLine();
                if (builder.Length > 3500)
                {
                    return builder.ToString();
                }
            }

            /*foreach (var port in ships[shipIndex].Ports)
            {
                builder.AppendLine("Port call: <b>" + port.port + "</b>");
                builder.AppendLine("Terminal: " + port.terminal);
                builder.AppendLine("Arrival: " + port.arrival.ToString("dd.MM.yyyy HH:mm"));
                builder.AppendLine("Departure: " + port.departure.ToString("dd.MM.yyyy HH:mm"));
                builder.AppendLine();
                if (builder.Length > 3500)
                {
                    return builder.ToString();
                }
            }*/
            return builder.ToString();
        }
    }
}