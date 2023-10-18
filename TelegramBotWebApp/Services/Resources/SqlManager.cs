using System.Data;
using System.Data.SqlClient;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Examples.WebHook;

namespace TelegramBotWebApp.Services.Resources
{
    public class SqlManager
    {
        /*public SqlConnection sqlConnection = new();
        public void SetupConnectionString(string conString)
        {
            sqlConnection.ConnectionString = conString;
        }
        private void Connect()
        {
            sqlConnection.Open();
        }
        private void Disconnect()
        {
            sqlConnection.Close();
        }
        public Ship GetShipFromDbByName(string ShipName)
        {
            if (ShipName == "")
            {
                return null;
            }
            Connect();
            SqlCommand selectCmd = new($"SELECT * FROM ShipsTable WHERE ShipName LIKE '%{ShipName}%'");
            selectCmd.Connection = sqlConnection;
            SqlDataAdapter adapter = new(selectCmd);
            Ship _ship = new();
            DataTable table = new();
            adapter.Fill(table);
            if (table.Rows.Count != 0)
            {
                _ship.ShipName = table.Rows[0][1].ToString();
                _ship.ShipCode = table.Rows[0][2].ToString();
            }
            else
            {
                _ship = null;
            }
            Disconnect();
            return _ship;
        }
        public Port GetPortFromDbByName(string portName)
        {
            if (portName == "")
            {
                return null;
            }
            Connect();
            SqlCommand selectCmd = new($"SELECT * FROM PortsTable WHERE PortName = '{portName}'");
            selectCmd.Connection = sqlConnection;
            SqlDataAdapter sqlAdapter = new SqlDataAdapter(selectCmd);
            Port _port = new();
            DataTable table = new();
            sqlAdapter.Fill(table);
            if (table.Rows.Count != 0)
            {
                _port.portName = table.Rows[0][1].ToString();
                _port.countryName = table.Rows[0][2].ToString();
                _port.emoji = table.Rows[0][3].ToString();
                _port.GeoId = table.Rows[0][4].ToString();
            }
            else
            {
                _port = null;
            }
            Disconnect();
            return _port;
        }
        public User GetUser(Update update)
        {
            int userId;
            string name;

            //check if update is message or callbackQuery
            if (update.Message != null)
            {
                userId = (int)update.Message.From.Id;
                name = $"{update.Message.From.FirstName} {update.Message.From.LastName}";
            }
            else
            {
                userId = (int)update.CallbackQuery.From.Id;
                name = $"{update.CallbackQuery.From.FirstName} {update.CallbackQuery.From.LastName}";
            }
            //check if user exist in Users DB table
            Connect();
            SqlCommand selectCmd = new($"SELECT TelegramID, Name, VesselLock, PortLock, PrintAscending " +
                                       $"FROM Users " +
                                       $"WHERE TelegramID={userId}");
            selectCmd.Connection = sqlConnection;
            SqlDataAdapter adapter = new(selectCmd);
            DataTable table = new();
            adapter.Fill(table);
            Disconnect();
            if (table.Rows.Count == 0)
            {
                //make a new entry in users table
                string query = $"INSERT INTO Users (TelegramID, Name, Requests, PrintAscending)" 
                             + $"VALUES (@userId, @name, @requests, @printAsc);";
                SqlCommand insertCmd = new();
                insertCmd.CommandType = CommandType.Text;
                insertCmd.CommandText = query;
                insertCmd.Parameters.Add("@userId", SqlDbType.Int);
                insertCmd.Parameters.Add("@name", SqlDbType.NVarChar);
                insertCmd.Parameters.Add("@requests", SqlDbType.Int);
                insertCmd.Parameters.Add("@printAsc", SqlDbType.Bit);
                insertCmd.Parameters["@userId"].Value = userId;
                insertCmd.Parameters["@name"].Value = name;
                insertCmd.Parameters["@requests"].Value = 0;
                insertCmd.Parameters["@printAsc"].Value = 1;
                insertCmd.Connection = sqlConnection;
                Connect();
                insertCmd.ExecuteNonQuery();
                Disconnect();

                User user = new();
                user.TelegramId = userId;
                user.Name = name;
                user.PrintAscending = true;
                user.PortTarget = null;
                user.VesselTarget = null;
                return user;
            }
            else
            {
                //retrieve user from DB
                User user = new();
                user.TelegramId = (int)table.Rows[0][0];
                user.Name = table.Rows[0][1].ToString();
                user.VesselTarget = GetShipFromDbByName(table.Rows[0][2].ToString());
                user.PortTarget = GetPortFromDbByName(table.Rows[0][3].ToString());
                if (table.Rows[0][4].ToString() == "True")
                {
                    user.PrintAscending = true;
                }
                else
                {
                    user.PrintAscending = false;
                }
                return user;
            }
        }
        public void RemoveShip(string userID)
        {
            string query = $"UPDATE Users SET VesselLock = NULL WHERE TelegramID=@userID;";
            SqlCommand insertCmd = new();
            insertCmd.CommandType = CommandType.Text;
            insertCmd.CommandText = query;
            insertCmd.Parameters.Add("@userID", SqlDbType.NVarChar);
            insertCmd.Parameters["@userID"].Value = userID;
            insertCmd.Connection = sqlConnection;
            Connect();
            insertCmd.ExecuteNonQuery();
            Disconnect();
        }
        public void AddShip(string userID, Ship ship)
        {
            string query = $"UPDATE Users SET VesselLock = @shipname WHERE TelegramID=@userID;";
            SqlCommand insertCmd = new();
            insertCmd.CommandType = CommandType.Text;
            insertCmd.CommandText = query;
            insertCmd.Parameters.Add("@userID", SqlDbType.Int);
            insertCmd.Parameters.Add("@shipname", SqlDbType.NVarChar);
            insertCmd.Parameters["@userID"].Value = userID;
            insertCmd.Parameters["@shipname"].Value = ship.ShipName;
            insertCmd.Connection = sqlConnection;
            Connect();
            insertCmd.ExecuteNonQuery();
            Disconnect();
        }
        public void RemovePort(string userID)
        {
            string query = $"UPDATE Users SET PortLock = NULL WHERE TelegramID=@userID;";
            SqlCommand insertCmd = new();
            insertCmd.CommandType = CommandType.Text;
            insertCmd.CommandText = query;
            insertCmd.Parameters.Add("@userID", SqlDbType.Int);
            insertCmd.Parameters["@userID"].Value = userID;
            insertCmd.Connection = sqlConnection;
            Connect();
            insertCmd.ExecuteNonQuery();
            Disconnect();
        }
        public void AddPort(string userID, Port port)
        {
            string query = $"UPDATE Users SET PortLock = @portName WHERE TelegramID=@userID;";
            SqlCommand insertCmd = new();
            insertCmd.CommandType = CommandType.Text;
            insertCmd.CommandText = query;
            insertCmd.Parameters.Add("@userID", SqlDbType.Int);
            insertCmd.Parameters.Add("@portName", SqlDbType.NVarChar);
            insertCmd.Parameters["@userID"].Value = userID;
            insertCmd.Parameters["@portName"].Value = port.portName;
            insertCmd.Connection = sqlConnection;
            Connect();
            insertCmd.ExecuteNonQuery();
            Disconnect();
        }
        public void AddToRequestsCount(Update update)
        {
            int userId = (int)update.Message.From.Id;
            string query = $"UPDATE Users SET Requests = Requests + 1 WHERE TelegramID = @userID";
            SqlCommand insertCmd = new();
            insertCmd.CommandType = CommandType.Text;
            insertCmd.CommandText = query;
            insertCmd.Parameters.Add("@userID", SqlDbType.Int);
            insertCmd.Parameters["@userID"].Value = userId;
            insertCmd.Connection = sqlConnection;
            Connect();
            insertCmd.ExecuteNonQuery();
            Disconnect();
        }
        public void ChangePrintAscending(Update update,int option)
        {
            int userId = (int)update.Message.From.Id;
            string query = $"UPDATE Users SET PrintAscending = {option} WHERE TelegramID = @userID";
            SqlCommand insertCmd = new();
            insertCmd.CommandType = CommandType.Text;
            insertCmd.CommandText = query;
            insertCmd.Parameters.Add("@userID", SqlDbType.Int);
            insertCmd.Parameters["@userID"].Value = userId;
            insertCmd.Connection = sqlConnection;
            Connect();
            insertCmd.ExecuteNonQuery();
            Disconnect();
        }

        #region Populate Tables
        public void AddCountries(List<EmojiClass> emojiList)
        {
            Connect();
            foreach (var emoji in emojiList)
            {
                SqlCommand checkCmd = new("SELECT COUNT (*) FROM CountriesTable WHERE CountryName=@name");
                checkCmd.Parameters.Add("@name", SqlDbType.NVarChar);
                checkCmd.Parameters["@name"].Value = emoji.Name;
                checkCmd.Connection = sqlConnection;
                Object obj = checkCmd.ExecuteScalar();

                if ((int)obj != 0)
                    continue;

                string query = $"INSERT INTO CountriesTable (CountryName, Emoji) VALUES (@name, @emoji);";
                SqlCommand insertCmd = new();
                insertCmd.CommandType = CommandType.Text;
                insertCmd.CommandText = query;
                insertCmd.Parameters.Add("@name", SqlDbType.NVarChar);
                insertCmd.Parameters.Add("@emoji", SqlDbType.NVarChar);
                insertCmd.Parameters["@name"].Value = emoji.Name;
                insertCmd.Parameters["@emoji"].Value = emoji.Emoji;
                insertCmd.Connection = sqlConnection;
                insertCmd.ExecuteNonQuery();
            }
            Disconnect();
        }

        public void AddVessels(VesselsManager vesselsManager)
        {
            foreach (var vessel in vesselsManager.ships)
            {
                SqlCommand checkCmd = new("SELECT COUNT (*) FROM ShipsTable WHERE ShipName=@ShipName");
                checkCmd.Parameters.Add("@ShipName", SqlDbType.NVarChar);
                checkCmd.Parameters["@ShipName"].Value = vessel.ShipName;
                checkCmd.Connection = sqlConnection;
                Object obj = checkCmd.ExecuteScalar();

                if ((int)obj != 0)
                    continue;

                string query = $"INSERT INTO ShipsTable (ShipName, ShipCode) VALUES (@ShipName, @ShipCode);";
                SqlCommand insertCmd = new();
                insertCmd.CommandType = CommandType.Text;
                insertCmd.CommandText = query;
                insertCmd.Parameters.Add("@ShipName", SqlDbType.NVarChar);
                insertCmd.Parameters.Add("@ShipCode", SqlDbType.NVarChar);
                insertCmd.Parameters["@ShipName"].Value = vessel.ShipName;
                insertCmd.Parameters["@ShipCode"].Value = vessel.ShipCode;
                insertCmd.Connection = sqlConnection;
                insertCmd.ExecuteNonQuery();
            }
        }

        public async Task AddPortGeoID()
        {
            string stringMaerskResponse = null;
            HttpRequestMessage requestForPortsList = new();
            string getPortsURL = "https://api.maerskline.com/maeu/schedules/port/active";
            requestForPortsList.RequestUri = new Uri(getPortsURL);
            HttpClient client = new();
            try
            {
                var maerskResponse = await client.SendAsync(requestForPortsList);
                stringMaerskResponse = await maerskResponse.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            { Console.WriteLine(e.Message); }
            finally
            { client.Dispose(); }

            Ship temp =  JsonConvert.DeserializeObject<Ship>(stringMaerskResponse);

            Connect();
            foreach (var port in temp.Ports)
            {
                string query = $"UPDATE PortsTable SET GeoID = {port.GeoId} WHERE PortName = {port.locationName}";
                SqlCommand insertCmd = new();
                insertCmd.CommandType = CommandType.Text;
                insertCmd.CommandText = query;
                //insertCmd.Parameters.Add("@location", SqlDbType.NVarChar);
                //insertCmd.Parameters["@location"].Value = port.locationName;
                insertCmd.Connection = sqlConnection;
                insertCmd.ExecuteNonQuery();
            }
            Disconnect();
        }
        #endregion
        */
    }
}
