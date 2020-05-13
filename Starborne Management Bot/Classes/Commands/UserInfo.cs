using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starborne_Management_Bot.Classes.Commands
{
    public class UserInfo : ModuleBase<SocketCommandContext>
    {
        [Command("userinfo"), Alias("ui", "info")]
        public async Task GetUserInfo(SocketGuildUser u)
        {
            EmbedBuilder eb = new EmbedBuilder().WithAuthor($"{(u.Nickname == "" ? u.ToString() : $"{u.Nickname} ({u.ToString()})")}", u.GetAvatarUrl()).WithColor(Color.Purple);

            short Warncount = 0;
            int AugsCompleted = 0;


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

                SqlCommand cmd = new SqlCommand($"SELECT WarnCount, AugmentsComplete FROM SBUsers WHERE UserID = {u.Id} AND GuildID = {Context.Guild.Id}", conn);
                SqlDataReader dr = cmd.ExecuteReader();

                if (!dr.HasRows)
                {
                    var m = await Context.Channel.SendMessageAsync("User not found");
                    GlobalVars.AddRandomTracker(m);
                    return;
                }
                while (dr.Read())
                {
                    Warncount = Convert.ToInt16(dr.GetValue(0));
                    AugsCompleted = Convert.ToInt32(dr.GetValue(1));
                }
                dr.Close();

                conn.Close();
                conn.Dispose();
            }
            eb.AddField($"Current warnings", $"{Warncount.ToString()} / {GlobalVars.GuildOptions.Single(go=>go.GuildID==Context.Guild.Id).PunishThreshold}");
            eb.AddField($"Augmentation requests completed", $"{AugsCompleted}");

            await Context.Channel.SendMessageAsync(null, false, eb.Build());
        }
    }
}
