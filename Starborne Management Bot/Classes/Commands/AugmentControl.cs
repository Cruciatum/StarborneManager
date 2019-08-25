using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using IBM.Data.DB2.Core;
using Starborne_Management_Bot.Classes.HelperObjects;
using Discord.WebSocket;

namespace Starborne_Management_Bot.Classes.Commands
{
    public class AugmentControl : ModuleBase<SocketCommandContext>
    {
        Random r = new Random();

        [Command("aug request")]
        public async Task RequestAug(string coord1, string coord2)
        {

            List<string> idList = new List<string>();
            string datestamp = DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year;

            DB2ConnectionStringBuilder sBuilder = new DB2ConnectionStringBuilder();
            sBuilder.Database = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.Server = GlobalVars.dbSettings.host + ":" + GlobalVars.dbSettings.port;

            DB2Connection conn = new DB2Connection
            {
                ConnectionString = sBuilder.ConnectionString
            };

            using (conn)
            {
                conn.Open();

                DB2Command cmd = new DB2Command($"SELECT AugID, coord1, coord2 FROM AugRequests WHERE GuildID = {Context.Guild.Id} AND Completed = 0;", conn);
                DB2DataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    if (dr.GetValue(1).ToString() == coord1 && dr.GetValue(2).ToString() == coord2)
                    {
                        var m = await Context.Channel.SendMessageAsync($"{Context.User.Mention}; this location already has a pending augmentation request.");
                        GlobalVars.AddRandomTracker(m);
                        return;
                    }
                    idList.Add(dr.GetValue(0).ToString());
                }
                dr.Close();

                conn.Close();
                conn.Dispose();
            }
            string AugID = GenerateID(idList);

            string sql = $"INSERT INTO AugRequests VALUES ('{AugID}', {Context.Guild.Id}, {Context.User.Id}, '{coord1}', '{coord2}', '{datestamp}', 0);";
            DBControl.UpdateDB(sql);

            await Context.Channel.SendMessageAsync($"Augmentation request ID {AugID} added.");
        }

        [Command("aug search")]
        public async Task CheckAugRequests(int amount = 10)
        {
            if (amount <= 0) amount = 10;

            EmbedBuilder eb = new EmbedBuilder().WithTitle($"Top {amount} oldest augment requests").WithColor(Color.Teal);
            string sql = $"SELECT AugID, UserID, coord1, coord2, DateStamp FROM AugRequests WHERE GuildID = {Context.Guild.Id} AND Completed = 0 ORDER BY DateStamp ASC LIMIT {amount};";

            await PerformSearch(eb, sql);
        }

        [Command("aug search")]
        public async Task CheckAugRequests(SocketGuildUser user, int amount = 10)
        {
            if (amount <= 0) amount = 10;

            EmbedBuilder eb = new EmbedBuilder().WithTitle($"Top {amount} oldest augment requests").WithColor(Color.Teal);
            string sql = $"SELECT TOP 10 AugID, UserID, coord1, coord2, DateStamp FROM AugRequests WHERE GuildID = {Context.Guild.Id} AND UserID = {user.Id} AND Completed = 0 ORDER BY DateStamp ASC LIMIT {amount};";

            await PerformSearch(eb, sql);
        }

        [Command("aug complete")]
        public async Task CompleteAug(string ID, bool InternalCall = false)
        {
            string sql = $"SELECT * FROM AugRequests WHERE GuildID = {Context.Guild.Id} AND AugID = '{ID}' AND Completed = 0;";

            if (!InternalCall)
            {
                DB2ConnectionStringBuilder sBuilder = new DB2ConnectionStringBuilder();
                sBuilder.Database = GlobalVars.dbSettings.db;
                sBuilder.UserID = GlobalVars.dbSettings.username;
                sBuilder.Password = GlobalVars.dbSettings.password;
                sBuilder.Server = GlobalVars.dbSettings.host + ":" + GlobalVars.dbSettings.port;

                DB2Connection conn = new DB2Connection
                {
                    ConnectionString = sBuilder.ConnectionString
                };

                using (conn)
                {
                    conn.Open();

                    DB2Command cmd = new DB2Command(sql, conn);
                    DB2DataReader dr = cmd.ExecuteReader();
                    if (dr.HasRows)
                    {
                        await CompleteAug(ID, true);
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
                sql = $"UPDATE AugRequests SET Completed = 1 WHERE AugID = '{ID}';";
                await UpdateRequest(ID, sql, Context.User, Context.Guild);
            }
        }

        [Command("aug complete")]
        public async Task CompleteAug(SocketGuildUser user, string coord1 = "", string coord2 = "")
        {
            string sql = $"SELECT AugID FROM AugRequests WHERE GuildID = {Context.Guild.Id} AND UserID = {user.Id} AND coord1 = '{coord1}' AND coord2 = '{coord2}' AND Completed = 0;";

            string id = "";
            id = SearchID(sql);
            if (id == "")
            {
                var m = await Context.Channel.SendMessageAsync("There is no active augmentation request found with your parameters.");
                GlobalVars.AddRandomTracker(m);
                return;
            }
            else
            {
                await CompleteAug(id, true);
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
            DB2ConnectionStringBuilder sBuilder = new DB2ConnectionStringBuilder();
            sBuilder.Database = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.Server = GlobalVars.dbSettings.host + ":" + GlobalVars.dbSettings.port;

            DB2Connection conn = new DB2Connection
            {
                ConnectionString = sBuilder.ConnectionString
            };

            using (conn)
            {
                conn.Open();

                DB2Command cmd = new DB2Command(sql, conn);
                DB2DataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    eb.AddField($"{dr.GetValue(0)} by {Context.Guild.GetUser(Convert.ToUInt64(dr.GetValue(1)))}", $"Request date: {dr.GetValue(4)}\nCoordinates: [{dr.GetValue(2)},{dr.GetValue(3)}]");
                }
                dr.Close();

                conn.Close();
                conn.Dispose();
            }

            if (eb.Fields.Count <= 0)
            {
                await Context.Channel.SendMessageAsync($"No active augmentation requests with your parameters.");
            }
            else
            {
                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
        }

        private string SearchID(string sql)
        {
            string id = "";

            DB2ConnectionStringBuilder sBuilder = new DB2ConnectionStringBuilder();
            sBuilder.Database = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.Server = GlobalVars.dbSettings.host + ":" + GlobalVars.dbSettings.port;

            DB2Connection conn = new DB2Connection
            {
                ConnectionString = sBuilder.ConnectionString
            };

            using (conn)
            {
                conn.Open();

                DB2Command cmd = new DB2Command(sql, conn);
                DB2DataReader dr = cmd.ExecuteReader();

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
                sql = $"UPDATE SBUsers SET AugmentsComplete = (SELECT AugmentsComplete FROM SBUsers WHERE UserID = {usr.Id}) + 1 WHERE UserID = {usr.Id} AND GuildID = {g.Id};";
                DBControl.UpdateDB(sql);
                await Context.Channel.SendMessageAsync($"Augmentation request {id} has been completed by {usr.Mention}");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"No such request found, try searching with `aug search <user> [amount]`");
            }
        }
    }
}