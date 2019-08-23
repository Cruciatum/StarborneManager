using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IBM.Data.DB2.Core;
using Starborne_Management_Bot.Classes.HelperObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starborne_Management_Bot.Classes.Commands
{
    public class WarnControl : ModuleBase<SocketCommandContext>
    {
        [Command("warn")]
        public async Task Warn([Remainder]string arg)
        {
            var user = Context.Message.MentionedUsers.FirstOrDefault();
            if (user == null)
            {
                var m = await Context.Channel.SendMessageAsync($"You need to tell me who to warn though {Context.User.Mention}");
                GlobalVars.AddRandomTracker(m);
                return;
            }
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

                short warnCount = 0;
                ushort warnThreshold = GlobalVars.GuildOptions.Single(go => go.GuildID == Context.Guild.Id).PunishThreshold;

                if (warnThreshold == 0)
                {
                    var m = await Context.Channel.SendMessageAsync($"You need to set a max warning threshold first!\nTry using `{Context.Message.Content.Substring(0, 1)}warn max [amount]`.");
                    GlobalVars.AddRandomTracker(m);
                    conn.Close();
                    conn.Dispose();
                    return;
                }

                DB2Command cmd = new DB2Command($"SELECT WarnCount FROM SBUsers WHERE GuildID = {Context.Guild.Id} AND UserID = {user.Id};", conn);
                DB2DataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    warnCount = dr.GetInt16(0);
                }
                dr.Close();

                var sql = $"UPDATE SBUsers SET WarnCount = {++warnCount} WHERE UserID = {user.Id};";
                DBControl.UpdateDB(sql);

                conn.Close();
                conn.Dispose();

                if (warnCount >= warnThreshold)
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention}, {user.Mention} has {warnCount} out of {warnThreshold} warnings, time to punish!");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"{user.Mention}, you have been warned.\nYou are now at {warnCount} out of {warnThreshold} warnings.");
                }
            }
        }

        [Command("warn max"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetMaxWarn(ushort maxAmount)
        {
            if (maxAmount > 0)
            {
                var sql = $"UPDATE SBGuilds SET WarnThreshold = {maxAmount} WHERE GuildID = {Context.Guild.Id}";
                DBControl.UpdateDB(sql);
                var m = await Context.Channel.SendMessageAsync($"Updated maximum number of allowed warnings, now set to {maxAmount}.");
                GlobalVars.AddRandomTracker(m);
            }
            else
            {
                var m = await Context.Channel.SendMessageAsync($"Warnings are now disabled!");
                GlobalVars.AddRandomTracker(m);
            }
            GlobalVars.GuildOptions.Single(go => go.GuildID == Context.Guild.Id).PunishThreshold = maxAmount;
        }
    }
}
