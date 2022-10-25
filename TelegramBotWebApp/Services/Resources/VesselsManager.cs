using Newtonsoft.Json;
using System.Text;
using System.Runtime.Caching;

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
            Root root = GetRoot();
            foreach (var port in ship.Ports)
            {
                foreach (var rootport in root.Ports)
                {
                    if (port.portName == rootport.portName)
                    {
                        port.emoji = rootport.emoji;
                    }
                }
            }
            return ship;
        }
        public Port UpdatePortShips(Port port)
        {
            Port temp = new();
            //populate temp.Vessels list from API
            temp = JsonConvert.DeserializeObject<Port>(GetShipsJson(port).Result);
            //finalize all fields in temp
            temp.portName = port.portName;
            temp.GeoId = port.GeoId;
            port = temp;
            return port;
        }
        private async Task<string> GetPortsJson(Ship ship)
        {
            string fromDateStr = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            string toDateStr = DateOnly.FromDateTime(DateTime.Now.AddDays(89)).ToString("yyyy-MM-dd");

            HttpRequestMessage requestForPortsList = new();
            string getPortsURL = "https://api.maerskline.com/maeu/schedules/vessel?vesselCode="
                               + ship.ShipCode.Trim()
                               + "&fromDate="
                               + fromDateStr
                               + "&toDate="
                               + toDateStr;
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
        private async Task<string> GetShipsJson(Port port)
        {
            string fromDateStr = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            string toDateStr = DateOnly.FromDateTime(DateTime.Now.AddDays(14)).ToString("yyyy-MM-dd");

            HttpRequestMessage requestForPortsList = new();
            string getPortsURL = "https://api.maerskline.com/maeu/schedules/port?portGeoId="
                               + port.GeoId.Trim()
                               + "&fromDate="
                               + fromDateStr
                               + "&toDate="
                               + toDateStr;
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
        public List<string> BuildSchedule(Ship ship, UserSet user)
        {
            StringBuilder builder = new();
            List<string> result = new();
            builder.AppendLine($"Schedule for <b>{ship.ShipName}</b>:");
            builder.AppendLine();
            if (user.PrintAscending == true)
            {
                for (int i = 0; i < ship.Ports.Count - 1; i++)
                {
                    string portEmoji = ship.Ports[i].emoji;
                    string portName = ship.Ports[i].portName.ToUpper();
                    string termName = ship.Ports[i].terminal;
                    string arrival = ship.Ports[i].arrival.ToString("dd-MM-yyyy HH:mm");
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
            }
            else
            {
                for (int i = ship.Ports.Count - 1; i >= 0; i--)
                {
                    string portEmoji = ship.Ports[i].emoji;
                    string portName = ship.Ports[i].portName.ToUpper();
                    string termName = ship.Ports[i].terminal;
                    string arrival = ship.Ports[i].arrival.ToString("dd-MM-yyyy HH:mm");
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
            }
            result.Add(builder.ToString());
            return result;
        }
        public List<string> BuildSchedule(Port port, UserSet user)
        {
            StringBuilder builder = new();
            List<string> result = new();
            builder.AppendLine($"Schedule for {port.emoji}<b>{port.portName}</b>:");
            builder.AppendLine();
            if (user.PrintAscending == true)
            {
                for (int i = 0; i < port.Vessels.Count - 1; i++)
                {
                    string vesselName = port.Vessels[i].ShipName.ToUpper();
                    string arrival = port.Vessels[i].Arrival.ToString("dd-MM-yyyy HH:mm");
                    string departure = port.Vessels[i].Departure.ToString("dd-MM-yyyy HH:mm");

                    builder.AppendLine($"<code>Vessel:</code> <b>{vesselName}</b>");
                    builder.AppendLine($"<code>ARR:</code> {arrival}");
                    builder.AppendLine($"<code>DEP:</code> {departure}");
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
                for (int i = port.Vessels.Count - 1; i >= 0; i--)
                {
                    string vesselName = port.Vessels[i].ShipName.ToUpper();
                    string arrival = port.Vessels[i].Arrival.ToString("dd-MM-yyyy HH:mm");
                    string departure = port.Vessels[i].Departure.ToString("dd-MM-yyyy HH:mm");

                    builder.AppendLine($"<code>Vessel:</code>: <b>{vesselName}</b>");
                    builder.AppendLine($"<code>ARR:</code> {arrival}");
                    builder.AppendLine($"<code>DEP:</code> {departure}");
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
        public Root GetRoot()
        {
            Root vessels = ParseActiveVesselsFromJson();
            Root ports = ParseActivePortsFromJson();
            string fileName = "emoji_flags.txt";
            string path = Path.Combine(Environment.CurrentDirectory, @"Services\Resources\", fileName);
            string[] emojis = File.ReadAllLines(path);
            foreach (var port in ports.Ports)
            {
                foreach (var line in emojis)
                {
                    if (line.Contains(port.countryName))
                    {
                        port.emoji = line.Split(':')[0].Trim();
                    }
                }
            }
            Root root = new();
            root.Vessels = vessels.Vessels;
            root.Ports = ports.Ports;
            MemoryCache.Default["root"] = root;
            return root;

            /*Root root = new();
            root = MemoryCache.Default["root"] as Root;
            if (root == null)
            {
                Root vessels = ParseActiveVesselsFromJson();
                Root ports = ParseActivePortsFromJson();
                string fileName = "emoji_flags.txt";
                string path = Path.Combine(Environment.CurrentDirectory, @"Services\Resources\", fileName);
                string[] emojis = File.ReadAllLines(path);
                foreach (var port in ports.Ports)
                {
                    foreach (var line in emojis)
                    {
                        if (line.Contains(port.countryName))
                        {
                            port.emoji = line.Split(':')[0].Trim();
                        }
                    }
                }

                root.Vessels = vessels.Vessels;
                root.Ports = ports.Ports;
                MemoryCache.Default["root"] = root;
            }
            return root;*/
        }
        public Root ParseActiveVesselsFromJson()
        {
            Root root = new();
            root = JsonConvert.DeserializeObject<Root>(GetActiveVessels().Result);
            return root;
        }
        private async Task<string> GetActiveVessels()
        {
            HttpRequestMessage requestForPortsList = new();
            string getPortsURL = "https://api.maerskline.com/maeu/schedules/vessel/active";
            requestForPortsList.RequestUri = new Uri(getPortsURL);
            HttpClient client = new();
            try
            {
                var maerskResponse = await client.SendAsync(requestForPortsList);
                string stringMaerskResponse = await maerskResponse.Content.ReadAsStringAsync();
                stringMaerskResponse = stringMaerskResponse.Replace("code", "vesselCode");
                stringMaerskResponse = stringMaerskResponse.Replace("name", "vessel");
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
        public Root ParseActivePortsFromJson()
        {
            Root root = new();
            root = JsonConvert.DeserializeObject<Root>(GetActivePorts().Result);
            return root;
        }
        private async Task<string> GetActivePorts()
        {
            HttpRequestMessage requestForPortsList = new();
            string getPortsURL = "https://api.maerskline.com/maeu/schedules/port/active";
            requestForPortsList.RequestUri = new Uri(getPortsURL);
            HttpClient client = new();
            try
            {
                var maerskResponse = await client.SendAsync(requestForPortsList);
                string stringMaerskResponse = await maerskResponse.Content.ReadAsStringAsync();
                stringMaerskResponse = stringMaerskResponse.Replace("locationName", "port");
                stringMaerskResponse = stringMaerskResponse.Replace("geoId", "portGeoId");
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

        public Ship GetShipFromActive(string name)
        {
            Root root = GetRoot();
            foreach (var ship in root.Vessels)
            {
                if (ship.ShipName.Contains(name.ToUpper()))
                {
                    return ship;
                }
            }
            return null;
        }
        public Port GetPortFromActive(string name)
        {
            Root root = GetRoot();
            foreach (var port in root.Ports)
            {
                if (port.portName.Contains(name))
                {
                    return port;
                }
            }
            return null;
        }
    }
}