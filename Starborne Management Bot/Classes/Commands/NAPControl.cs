using Discord;
using Discord.Commands;
using System.Data.SqlClient;
using Starborne_Management_Bot.Classes.HelperObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Starborne_Management_Bot.Classes.Commands
{
    public class NAPControl : ModuleBase<SocketCommandContext>
    {
        [Command("nap list")]
        public async Task ListNAP()
        {
            SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
            sBuilder.InitialCatalog = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.DataSource =GlobalVars.dbSettings.host + @"\" +GlobalVars.dbSettings.instance + "," +GlobalVars.dbSettings.port;

            SqlConnection conn = new SqlConnection
            {
                ConnectionString = sBuilder.ConnectionString
            };

            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = $"Active Non-Aggression Pacts";

            using (conn)
            {
                conn.Open();

                #region Get all NAPs
                SqlCommand cmd = new SqlCommand($"SELECT NAPGuildName, NAPGuildTag, UserID, DateStamp FROM NAPs WHERE GuildID = {Context.Guild.Id};", conn);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    var madeBy = Context.Guild.GetUser(Convert.ToUInt64(dr.GetValue(2)));
                    eb.AddField($"{Convert.ToString(dr.GetValue(0))} {(Convert.ToString(dr.GetValue(1)) != "" ? $"({Convert.ToString(dr.GetValue(1))})" : "")}", $"Created by {(string.IsNullOrEmpty(madeBy.Nickname) ? madeBy.ToString() : $"{madeBy.Nickname} ({madeBy.ToString()})")}\n*Created on {Convert.ToString(dr.GetValue(3))}*");
                }
                dr.Close();
                #endregion

                eb.WithFooter($"Found {eb.Fields.Count} Non-Aggression Pacts.");
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
                    eb.AddField($"*and {t} more NAPs.*", null);

                    await Context.Channel.SendMessageAsync(null, false, eb.Build());
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention}, there are no active Non-Aggression Pacts.");
                }

                conn.Close();
                conn.Dispose();
            }
        }

        [Command("nap add")]
        public async Task AddNAP(string allianceName, string allianceTag = "")
        {
            
            SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
            sBuilder.InitialCatalog = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.DataSource =GlobalVars.dbSettings.host + @"\" +GlobalVars.dbSettings.instance + "," +GlobalVars.dbSettings.port;

            SqlConnection conn = new SqlConnection
            {
                ConnectionString = sBuilder.ConnectionString
            };

            using (conn)
            {
                conn.Open();

                #region Check if user already has reservation
                SqlCommand cmd = new SqlCommand($"SELECT * FROM NAPs WHERE GuildID = {Context.Guild.Id} AND ( NAPGuildName = '{allianceName}' OR NAPGuildTag = '{allianceTag}');", conn);
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.HasRows)
                {
                    await Context.Channel.SendMessageAsync($"This alliance name/tag has already been added.\nView active Non-Aggression Pacts by using `{Context.Message.Content.Substring(0,1)}nap list`");
                    return;
                }
                dr.Close();
                #endregion

                conn.Close();
                conn.Dispose();
            }
            var sql = $"INSERT INTO NAPs (GuildID, NAPGuildName, NAPGuildTag, UserID, DateStamp) VALUES ({Context.Guild.Id}, '{allianceName}', '{allianceTag}', {Context.User.Id} , '{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year}');";
            DBControl.UpdateDB(sql);

            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, {allianceName}{(allianceTag == "" ? "" : $"({allianceTag})")} has been added to the active NAP list.");
        }

        [Command("nap remove")]
        public async Task RemoveNAP(string allianceName)
        {
            SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
            sBuilder.InitialCatalog = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.DataSource =GlobalVars.dbSettings.host + @"\" +GlobalVars.dbSettings.instance + "," +GlobalVars.dbSettings.port;

            SqlConnection conn = new SqlConnection
            {
                ConnectionString = sBuilder.ConnectionString
            };

            using (conn)
            {
                conn.Open();

                #region Check if user already has reservation
                SqlCommand cmd = new SqlCommand($"SELECT NapID FROM NAPs WHERE GuildID = {Context.Guild.Id} AND NAPGuildName = '{allianceName}';", conn);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read()) {
                    if (dr.HasRows)
                    {
                        DBControl.UpdateDB($"DELETE FROM NAPs WHERE NapID = {dr.GetValue(0)};");
                        await Context.Channel.SendMessageAsync($"You no longer have a Non-Aggression Pact with {allianceName}");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("No NAP found");
                    }
                }
                dr.Close();
                #endregion

                conn.Close();
                conn.Dispose();
            }
        }
    }
}
