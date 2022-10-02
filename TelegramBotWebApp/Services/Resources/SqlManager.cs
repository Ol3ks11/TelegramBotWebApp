using Microsoft.VisualBasic;
using System.Data;
using System.Data.SqlClient;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotWebApp.Services.Resources
{
    internal class SqlManager
    {

        string sqlConnectstring;
        SqlConnection sqlConnection = new();

        public SqlManager()
        {
            sqlConnectstring = @"Server=tcp:sqlserverforbot.database.windows.net,1433;"
                              +@"Initial Catalog=telegram-bot-sql-server;Persist Security Info=False;"
                              +@"User ID=myadmin;Password=Bobbas47;MultipleActiveResultSets=False;"
                              +@"Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            sqlConnection.ConnectionString = sqlConnectstring;
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
            }
            else
            {
                _port = null;
            }
            Disconnect();
            return _port;
        }

        public string GetEmoji(string countryName)
        {
            Connect();
            SqlCommand selectCmd = new($"SELECT Emoji FROM CountriesTable WHERE CountryName = '{countryName}'");
            selectCmd.Connection = sqlConnection;
            SqlDataAdapter adapter = new(selectCmd);
            DataTable table = new();
            adapter.Fill(table);
            Disconnect();
            if (table.Rows.Count == 0)
            {
                return "";
            }
            return table.Rows[0][0].ToString();
        }

        public User GetUser(Update update)
        {
            int userId = (int)update.Message.From.Id;
            string name = $"{update.Message.From.FirstName} {update.Message.From.LastName}";

            Connect();
            SqlCommand selectCmd = new($"SELECT COUNT (*) FROM Users WHERE TelegramID={userId}");
            selectCmd.Connection = sqlConnection;
            SqlDataAdapter adapter = new(selectCmd);
            DataTable table = new();
            adapter.Fill(table);
            Disconnect();
            if (table.Rows.Count == 0)
            {
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
                return user;
            }
            else
            {
                User user = new();
                user.TelegramId = (int)table.Rows[0][1];
                user.Name = table.Rows[0][2].ToString();
                user.VesselTarget = GetShipFromDbByName(table.Rows[0][3].ToString());
                user.PortTarget = GetPortFromDbByName(table.Rows[0][4].ToString());
                if (table.Rows[0][4].ToString() == "1")
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

        public void RemoveShip(Update update)
        {
            int userId = (int)update.Message.From.Id;
            string query = $"UPDATE Users SET VesselLock = @null WHERE TelegramID=@userID;";
            SqlCommand insertCmd = new();
            insertCmd.CommandType = CommandType.Text;
            insertCmd.CommandText = query;
            insertCmd.Parameters.Add("@userID", SqlDbType.NVarChar);
            insertCmd.Parameters.Add("@null", SqlDbType.NVarChar);
            insertCmd.Parameters["@userID"].Value = userId;
            insertCmd.Parameters["@null"].Value = null;
            insertCmd.Connection = sqlConnection;
            Connect();
            insertCmd.ExecuteNonQuery();
            Disconnect();
        }

        public void AddShip(Update update, Ship ship)
        {
            int userId = (int)update.Message.From.Id;
            string query = $"UPDATE Users SET VesselLock = @shipname WHERE TelegramID=@userID;";
            SqlCommand insertCmd = new();
            insertCmd.CommandType = CommandType.Text;
            insertCmd.CommandText = query;
            insertCmd.Parameters.Add("@userID", SqlDbType.Int);
            insertCmd.Parameters.Add("@shipname", SqlDbType.NVarChar); 
            insertCmd.Parameters["@userID"].Value = userId;
            insertCmd.Parameters["@shipname"].Value = ship.ShipName;
            insertCmd.Connection = sqlConnection;
            Connect();
            insertCmd.ExecuteNonQuery();
            Disconnect();
        }

        public void RemovePort(Update update)
        {
            int userId = (int)update.Message.From.Id;
            string query = $"UPDATE Users SET PortLock = @null WHERE TelegramID=@userID;";
            SqlCommand insertCmd = new();
            insertCmd.CommandType = CommandType.Text;
            insertCmd.CommandText = query;
            insertCmd.Parameters.Add("@userID", SqlDbType.Int);
            insertCmd.Parameters.Add("@null", SqlDbType.NVarChar);
            insertCmd.Parameters["@userID"].Value = userId;
            insertCmd.Parameters["@null"].Value = null;
            insertCmd.Connection = sqlConnection;
            Connect();
            insertCmd.ExecuteNonQuery();
            Disconnect();
        }

        public void AddPort(Update update, Port port)
        {
            int userId = (int)update.Message.From.Id;
            string query = $"UPDATE Users SET PortLock = @portName WHERE TelegramID=@userID;";
            SqlCommand insertCmd = new();
            insertCmd.CommandType = CommandType.Text;
            insertCmd.CommandText = query;
            insertCmd.Parameters.Add("@userID", SqlDbType.Int);
            insertCmd.Parameters.Add("@portName", SqlDbType.NVarChar);
            insertCmd.Parameters["@userID"].Value = userId;
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
        /*public void AddCountries(List<EmojiClass> emojiList)
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
        }*/
        #endregion
    }
}
