﻿using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using IBM.Data.DB2.Core;
using Starborne_Management_Bot.Classes.HelperObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace Starborne_Management_Bot.Classes.Commands
{
    public class Reservations : ModuleBase<SocketCommandContext>
    {
        public static List<ResponseWaiter> ResponseWaiters = new List<ResponseWaiter>();

        [Command("reserve")]
        public async Task ReserveStation(string coord1, string coord2)
        {
            var r = ResponseWaiters.SingleOrDefault(rw => rw.User == Context.User && rw.X == coord1 && rw.Y == coord2);
            if (r != null)
            {
                await r.Remove();
            }
            else
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

                    #region Check if user already has reservation
                    DB2Command cmd = new DB2Command($"SELECT Coord1, Coord2 FROM Reservations WHERE UserID = {Context.User.Id}", conn);
                    DB2DataReader dr = cmd.ExecuteReader();

                    if (dr.HasRows)
                    {
                        List<object> oList = new List<object>();
                        while (dr.Read())
                        {
                            //User already has a reserved station
                            var val1 = dr.GetValue(0);
                            var val2 = dr.GetValue(1);
                            oList.Add(new { val1, val2 });

                        }
                        if (oList.Count >= GlobalVars.GuildOptions.Single(go => go.GuildID == Context.Guild.Id).MaxReserves)
                        {
                            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, you already have the maximum amount of reserved locations. Check them with `{Context.Message.Content.Substring(0, 1)}reserve list`");
                            return;
                        }
                    }
                    dr.Close();
                    #endregion

                    #region Check for active reservation
                    cmd = new DB2Command($"SELECT UserID FROM Reservations WHERE GuildID = {Context.Guild.Id} AND Coord1 = '{coord1}' AND Coord2 = '{coord2}'", conn);
                    dr = cmd.ExecuteReader();

                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            //Already reserved by a user in this guild
                            SocketUser su = Context.Guild.Users.Single(u => u.Id == Convert.ToUInt64(dr.GetValue(0)));

                            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, this location has already been reserved by {su.Mention}");
                            return;
                        }
                    }
                    dr.Close();
                    #endregion

                    #region Get all reservations and warn user if one is within 5 hex distance
                    cmd.CommandText = $"SELECT UserID, Coord1, Coord2 FROM Reservations WHERE GuildID = {Context.Guild.Id};";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        int dist = CalcDistance(Convert.ToInt32(coord1), Convert.ToInt32(coord2), Convert.ToInt32(dr.GetValue(1)), Convert.ToInt32(dr.GetValue(2)));
                        if (dist <= 4)
                        {
                            var m = await Context.Channel.SendMessageAsync($"A nearby reservation has already been made by {Context.Guild.Users.Single(u => u.Id == Convert.ToUInt64(dr.GetValue(0))).Mention} at [{dr.GetValue(1).ToString()}, {dr.GetValue(2).ToString()}].\nTo confirm this reservation, use the same command within 15 seconds.");
                            ResponseWaiters.Add(new ResponseWaiter(m, coord1, coord2, Context.User));
                            dr.Close();

                            conn.Close();
                            conn.Dispose();
                        }
                    }
                    #endregion
                    dr.Close();

                    conn.Close();
                    conn.Dispose();
                }
            }
            var sql = $"INSERT INTO Reservations (UserID, GuildID, Coord1, Coord2, DateStamp) VALUES ({Context.User.Id},{Context.Guild.Id}, '{coord1}', '{coord2}', '{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year}');";
            DBControl.UpdateDB(sql);
            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, the location [{coord1}, {coord2}] has been reserved.");
        }

        [Command("reserve list")]
        public async Task ListReserves(IGuildUser user = null)
        {
            user = (IGuildUser)Context.Message.MentionedUsers.FirstOrDefault();
            var target = (user != null) ? (SocketUser)user : Context.User;

            DB2ConnectionStringBuilder sBuilder = new DB2ConnectionStringBuilder();
            sBuilder.Database = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.Server = GlobalVars.dbSettings.host + ":" + GlobalVars.dbSettings.port;

            DB2Connection conn = new DB2Connection
            {
                ConnectionString = sBuilder.ConnectionString
            };

            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = $"List of reservations for user {target}]";

            using (conn)
            {
                conn.Open();

                #region Get Reservation
                DB2Command cmd = new DB2Command($"SELECT UserID, Coord1, Coord2, DateStamp FROM Reservations WHERE GuildID = {Context.Guild.Id} AND UserID = {target.Id};", conn);
                DB2DataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    ulong userID = Convert.ToUInt64(dr.GetValue(0));
                    eb.AddField($"Reserved by {Context.Guild.Users.Single(u => u.Id == userID)} on {dr.GetValue(3)}", $"Location: [{dr.GetValue(1)}, {dr.GetValue(2)}]");
                }
                #endregion

                conn.Close();
                conn.Dispose();
            }
            eb.WithFooter($"Found {eb.Fields.Count} locations.");
            if (eb.Fields.Count > 0 && eb.Fields.Count <= 25)
            {
                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
            else if (eb.Fields.Count > 25)
            {
                var t = eb.Fields.Count - 24;
                List<EmbedFieldBuilder> tmp = new List<EmbedFieldBuilder>();
                for (int i = 0; i < 24; i++) { tmp.Add(eb.Fields[i]); }
                eb.Fields = tmp;
                eb.AddField($"*and {t} more locations.*", null);

                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
            else
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, there were no matching locations reserved in this alliance.");
            }
        }

        [Command("reserve check")]
        public async Task CheckReserves(string coord1 = "*", string coord2 = "*")
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

            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = $"List of reservations by coords [{coord1}, {coord2}]";

            using (conn)
            {
                conn.Open();

                #region Get Reservation
                DB2Command cmd = new DB2Command($"SELECT UserID, Coord1, Coord2, DateStamp FROM Reservations WHERE GuildID = {Context.Guild.Id} AND Coord1 LIKE '{coord1.Replace("*", "%")}' AND Coord2 LIKE '{coord2.Replace("*", "%")}';", conn);
                DB2DataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    ulong userID = Convert.ToUInt64(dr.GetValue(0));
                    eb.AddField($"Reserved by {Context.Guild.Users.Single(u => u.Id == userID)} on {dr.GetValue(3)}", $"Location: [{dr.GetValue(1)}, {dr.GetValue(2)}]");
                }
                #endregion

                conn.Close();
                conn.Dispose();
            }
            eb.WithFooter($"Found {eb.Fields.Count} locations.");
            if (eb.Fields.Count > 0 && eb.Fields.Count <= 25)
            {
                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
            else if (eb.Fields.Count > 25)
            {
                var t = eb.Fields.Count - 24;
                List<EmbedFieldBuilder> tmp = new List<EmbedFieldBuilder>();
                for (int i = 0; i < 24; i++) { tmp.Add(eb.Fields[i]); }
                eb.Fields = tmp;
                eb.AddField($"*and {t} more locations.*", null);

                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
            else
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, there were no matching locations reserved in this alliance.");
            }
        }

        [Command("reserve remove")]
        public async Task RemoveReservation(string coord1, string coord2)
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

                #region Get Reservation
                DB2Command cmd = new DB2Command($"SELECT * FROM Reservations WHERE GuildID = {Context.Guild.Id} AND UserID = {Context.User.Id} AND Coord1 = '{coord1}' AND Coord2 = '{coord2}';", conn);
                DB2DataReader dr = cmd.ExecuteReader();

                if (dr.HasRows)
                {
                    DBControl.UpdateDB($"DELETE FROM Reservations WHERE GuildID = {Context.Guild.Id} AND UserID = {Context.User.Id} AND Coord1 = '{coord1}' AND Coord2 = '{coord2}';");
                }
                #endregion

                conn.Close();
                conn.Dispose();
            }

            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, your station reservation at [{coord1} {coord2}] has been removed.");
        }

        [Command("reserve max"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetMaxReserves(ushort amount)
        {
            GlobalVars.GuildOptions.Single(go => go.GuildID == Context.Guild.Id).MaxReserves = amount;
            var sql = $"UPDATE SBGuilds SET MaxReserves = {amount} WHERE GuildID = {Context.Guild.Id};";
            DBControl.UpdateDB(sql);

            var m = await Context.Channel.SendMessageAsync($"Updated max location reservations to {amount}.");
            GlobalVars.AddRandomTracker(m);
        }

        private int CalcDistance(int X1, int Y1, int X2, int Y2)
        {
            int dist = 0;

            if ((X1 < X2 && Y1 < Y2) || (X1 > X2 && Y1 > Y2)) dist = Math.Abs(X1 - X2) + Math.Abs(Y1 - Y2);
            else dist = Math.Max(Math.Abs(X1 - X2), Math.Abs(Y1 - Y2));

            return dist;
        }
    }

    public class ResponseWaiter
    {
        public RestUserMessage Message;
        public string X;
        public string Y;
        public SocketUser User;
        public Timer t = new Timer();

        public ResponseWaiter(RestUserMessage msg, string coord1, string coord2, SocketUser usr)
        {
            Message = msg;
            X = coord1;
            Y = coord2;
            User = usr;

            async void handler(object sender, ElapsedEventArgs e)
            {
                t.Stop();
                await Remove();
            };

            t.StartTimer(handler, 15000);
        }

        public async Task Remove()
        {
            if (t.Enabled) t.Stop();
            await Message.DeleteAsync();
            Reservations.ResponseWaiters.Remove(this);
        }
    }
}
