using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace TelegramBotWebApp.Services.Resources
{
    public class VesselsManager
    {

        public Ship UpdateShipPorts(Ship ship)
        {
            Ship temp = new();
            temp = JsonConvert.DeserializeObject<Ship>(GetPortsJson(ship).Result);
            temp.ShipName = ship.ShipName;
            temp.ShipCode = ship.ShipCode;
            ship = temp;

            SqlManager sqlManager = new();
            foreach(var port in ship.Ports)
            {
                Port checkedPort = sqlManager.GetPortFromDbByName(port.portName);
                if (port.portName == checkedPort.locationName)
                {
                    port.countryName = checkedPort.countryName;
                }
            }
            return ship;
        }

        private async Task<string> GetPortsJson(Ship ship)
        {
            string fromDateStr = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            string toDateStr = DateOnly.FromDateTime(DateTime.Now.AddDays(89)).ToString("yyyy-MM-dd");

            HttpRequestMessage requestForPortsList = new();
            string getPortsURL = "https://api.maerskline.com/maeu/schedules/vessel?vesselCode="
                +ship.ShipCode.Trim()+"&fromDate="+ fromDateStr + "&toDate="+ toDateStr;
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

        public List<string> BuildSchedule(Ship ship)
        {
            SqlManager sqlManager = new();
            StringBuilder builder = new();
            List<string> result = new();
            builder.AppendLine($"Schedule for <b>{ship.ShipName}</b>:");
            builder.AppendLine();

            for(int i= ship.Ports.Count-1;i>=0;i--)
            {
                string portEmoji = ship.Ports[i].emoji;
                string portName  = ship.Ports[i].portName.ToUpper();
                string termName  = ship.Ports[i].terminal;
                string arrival   = ship.Ports[i].arrival.ToString("dd-MM-yyyy HH:mm");
                string departure = ship.Ports[i].departure.ToString("dd-MM-yyyy HH:mm");

                builder.AppendLine($"<code>Port call</code>: {portEmoji} <b>{portName}</b>");
                builder.AppendLine($"<code>Terminal:</code> <i>{termName}</i>");
                builder.AppendLine($"<code>ARR:</code> {arrival}");
                builder.AppendLine($"<code>DEP:</code> {departure}");
                builder.AppendLine();

                if (builder.Length > 1800)
                {
                    result.Add(builder.ToString());
                    builder.Clear();
                }
            }
            result.Add(builder.ToString());
            return result;
        }
    }
}