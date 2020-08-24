using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Data.SqlClient;
using Starborne_Management_Bot.Classes.HelperObjects;
using Discord.WebSocket;

namespace Starborne_Management_Bot.Classes.Commands
{
    public class ShipControl : ModuleBase<SocketCommandContext>
    {
        Random r = new Random();

        [Command("ship request")]
        public async Task RequestShip(string shipName, int amount, string coord1, string coord2)
        {
            //int amount = -1;
            //int.TryParse(amt, out amount);
            //if (amount == -1)
            //{
            //    await Context.Channel.SendMessageAsync($"Can not parse the value you entered for `Amount`. Please try again.\nYour input: {amt}");
            //    return;
            //}
            List<string> idList = new List<string>();
            string datestamp = DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year;

            SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
            sBuilder.InitialCatalog = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.DataSource = GlobalVars.dbSettings.host + @"\" + GlobalVars.dbSettings.instance + "," + GlobalVars.dbSettings.port;

            SqlConnection conn = new SqlConnection
            {
                ConnectionString = sBuilder.ConnectionString
            };

            using (conn)
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand($"SELECT ReqID, ShipName, Amount, coord1, coord2 FROM ShipRequests WHERE GuildID = {Context.Guild.Id} AND Completed = 0;", conn);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    if (dr.GetValue(3).ToString() == coord1 && dr.GetValue(4).ToString() == coord2)
                    {
                        var m = await Context.Channel.SendMessageAsync($"{Context.User.Mention}; this location already has a pending ship request.");
                        GlobalVars.AddRandomTracker(m);
                        return;
                    }
                    idList.Add(dr.GetValue(0).ToString());
                }
                dr.Close();

                conn.Close();
                conn.Dispose();
            }
            string ReqID = GenerateID(idList);

            string sql = $"INSERT INTO ShipRequests VALUES ('{ReqID}', {Context.Guild.Id}, {Context.User.Id}, '{shipName}', '{coord1}', '{coord2}', {amount}, '{datestamp}', 0);";
            DBControl.UpdateDB(sql);

            await Context.Channel.SendMessageAsync($"Ship request ID {ReqID} added.");
        }

        [Command("ship search")]
        public async Task CheckShipRequests(int amount = 10)
        {
            if (amount <= 0) amount = 10;

            EmbedBuilder eb = new EmbedBuilder().WithTitle($"Top {amount} oldest ship requests").WithColor(Color.Teal);
            string sql = $"SELECT TOP 10 ReqID, UserID, coord1, coord2, DateStamp, ShipName, Amount FROM ShipRequests WHERE GuildID = {Context.Guild.Id} AND Completed = 0 ORDER BY DateStamp ASC;";

            await PerformSearch(eb, sql);
        }

        [Command("ship search")]
        public async Task CheckShipRequests(SocketGuildUser user, int amount = 10)
        {
            if (amount <= 0) amount = 10;

            EmbedBuilder eb = new EmbedBuilder().WithTitle($"Top {amount} oldest ship requests").WithColor(Color.Teal);
            string sql = $"SELECT TOP {amount} ReqID, UserID, coord1, coord2, DateStamp, ShipName, Amount FROM ShipRequests WHERE GuildID = {Context.Guild.Id} AND UserID = {user.Id} AND Completed = 0 ORDER BY DateStamp ASC;";

            await PerformSearch(eb, sql);
        }

        [Command("ship complete")]
        public async Task CompleteShipReq(string ID, bool InternalCall = false)
        {
            string sql = $"SELECT * FROM ShipRequests WHERE GuildID = {Context.Guild.Id} AND ReqID = '{ID}' AND Completed = 0;";

            if (!InternalCall)
            {
                SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
                sBuilder.InitialCatalog = GlobalVars.dbSettings.db;
                sBuilder.UserID = GlobalVars.dbSettings.username;
                sBuilder.Password = GlobalVars.dbSettings.password;
                sBuilder.DataSource = GlobalVars.dbSettings.host + @"\" + GlobalVars.dbSettings.instance + "," + GlobalVars.dbSettings.port;

                SqlConnection conn = new SqlConnection
                {
                    ConnectionString = sBuilder.ConnectionString
                };

                using (conn)
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.HasRows)
                    {
                        await CompleteShipReq(ID, true);
                    }
                    else
                    {
                        await UpdateRequest("", sql, Context.User, Context.Guild);
                    }
                    dr.Close();

                    conn.Close();
                    conn.Dispose();
                }
            }
            else
            {
                sql = $"UPDATE ShipRequests SET Completed = 1 WHERE ReqID = '{ID}';";
                await UpdateRequest(ID, sql, Context.User, Context.Guild);
            }
        }

        [Command("ship complete")]
        public async Task CompleteShipReq(SocketGuildUser user, string coord1 = "", string coord2 = "")
        {
            string sql = $"SELECT ReqID FROM ShipRequests WHERE GuildID = {Context.Guild.Id} AND UserID = {user.Id} AND coord1 = '{coord1}' AND coord2 = '{coord2}' AND Completed = 0;";

            string id = "";
            id = SearchID(sql);
            if (id == "")
            {
                var m = await Context.Channel.SendMessageAsync("There is no active ship request found with your parameters.");
                GlobalVars.AddRandomTracker(m);
                return;
            }
            else
            {
                await CompleteShipReq(id, true);
            }
        }

        private string GenerateID(List<string> idList)
        {

            string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string id = "";
            id = MakeID(alphabet);
            while (idList.Contains(id))
            {
                id = MakeID(alphabet);
            }
            return id;
        }

        private string MakeID(string alphabet)
        {
            string s = "";
            for (int i = 0; i < 10; i++)
            {
                s += alphabet[r.Next(0, 62)];
            }
            return s;
        }

        private async Task PerformSearch(EmbedBuilder eb, string sql)
        {
            SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
            sBuilder.InitialCatalog = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.DataSource = GlobalVars.dbSettings.host + @"\" + GlobalVars.dbSettings.instance + "," + GlobalVars.dbSettings.port;

            SqlConnection conn = new SqlConnection
            {
                ConnectionString = sBuilder.ConnectionString
            };

            using (conn)
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    eb.AddField($"{dr.GetValue(0)} by {Context.Guild.GetUser(Convert.ToUInt64(dr.GetValue(1)))}", $"Request date: {dr.GetValue(4)}\nShips requested: {dr.GetValue(5)} - Amount: {dr.GetValue(6)}\nCoordinates: /goto {dr.GetValue(2)} {dr.GetValue(3)}");
                }
                dr.Close();

                conn.Close();
                conn.Dispose();
            }

            if (eb.Fields.Count <= 0)
            {
                await Context.Channel.SendMessageAsync($"No active ship requests with your parameters.");
            }
            else
            {
                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
        }

        private string SearchID(string sql)
        {
            string id = "";

            SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
            sBuilder.InitialCatalog = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.DataSource = GlobalVars.dbSettings.host + @"\" + GlobalVars.dbSettings.instance + "," + GlobalVars.dbSettings.port;

            SqlConnection conn = new SqlConnection
            {
                ConnectionString = sBuilder.ConnectionString
            };

            using (conn)
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    id = dr.GetValue(0).ToString();
                }
                dr.Close();

                conn.Close();
                conn.Dispose();
            }

            return id;
        }

        private async Task UpdateRequest(string id, string sql, SocketUser usr, SocketGuild g)
        {
            if (id != "")
            {
                DBControl.UpdateDB(sql);
                await Context.Channel.SendMessageAsync($"Ship request {id} has been completed by {usr.Mention}");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"No such request found, try searching with `ship search <user> [amount]`");
            }
        }
    }
}
