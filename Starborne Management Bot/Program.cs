using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Newtonsoft.Json;
using System.Timers;
using System.Net.Http;
using Starborne_Management_Bot.Classes.Data;
using Starborne_Management_Bot.Classes.HelperObjects;

using IBM.Data.DB2.Core;
using Dropbox.Api;
using Starborne_Management_Bot.Classes.Commands;

namespace Starborne_Management_Bot
{
    class Program
    {
        private DiscordSocketClient Client;
        private CommandService Commands;
        private IServiceProvider Provider;
        private static readonly HttpClient httpClient = new HttpClient();

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            using (var dbClient = new DropboxClient(Constants._DBTOKEN_))
            {
                if (File.Exists(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\BotSettings.json")))
                {
                    GlobalVars.bSettings = new BotSettings(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\BotSettings.json"));
                }
                else
                {
                    if (!Directory.Exists(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data")))
                    {
                        Directory.CreateDirectory(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data"));
                    }
                    using (var response = await dbClient.Files.DownloadAsync("/Data/BotSettings.json"))
                    {
                        var f = File.Create(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\BotSettings.json"));
                        using (var rw = new StreamWriter(f))
                        {
                            rw.Write(await response.GetContentAsStringAsync());
                        }
                    }
                    GlobalVars.bSettings = new BotSettings(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\BotSettings.json"));
                }

                if (File.Exists(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\DBSettings.json")))
                {
                    GlobalVars.dbSettings = new DBSettings(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\DBSettings.json"));
                }
                else
                {
                    if (!Directory.Exists(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data")))
                    {
                        Directory.CreateDirectory(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data"));
                    }
                    using (var response = await dbClient.Files.DownloadAsync("/Data/DBSettings.json"))
                    {
                        var f = File.Create(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\DBSettings.json"));
                        using (var rw = new StreamWriter(f))
                        {
                            rw.Write(await response.GetContentAsStringAsync());
                        }
                    }
                    GlobalVars.dbSettings = new DBSettings(Constants._WORKDIR_ + Constants.TranslateForOS(@"\Data\DBSettings.json"));
                }
            }

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug
            });

            Commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            });

            Provider = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(Commands)
                .BuildServiceProvider();

            Client.MessageReceived += Client_MessageReceived;
            await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            Client.Ready += Client_Ready;
            Client.Log += Client_Log;
            Client.JoinedGuild += Client_JoinedGuild;
            Client.LeftGuild += Client_LeftGuild;
            Client.UserJoined += Client_UserJoined;
            Client.UserLeft += Client_UserLeft;

            if (!Directory.Exists(LogWriter.LogFileLoc.Replace(Constants.TranslateForOS(@"Logs\Log"), Constants.TranslateForOS(@"Logs\"))))
            {
                Directory.CreateDirectory(LogWriter.LogFileLoc.Replace(Constants.TranslateForOS(@"Logs\Log"), Constants.TranslateForOS(@"Logs\")));
            }

            DBControl.dbSettings = GlobalVars.dbSettings;

            GetSQLData();

            await Client.LoginAsync(TokenType.Bot, GlobalVars.bSettings.token);
            await Client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task Client_UserLeft(SocketGuildUser arg)
        {
            string sql = $"DELETE FROM SBUsers WHERE UserID = {arg.Id};";
            DBControl.UpdateDB(sql);
            await Client_Log(new LogMessage(LogSeverity.Info, "Client_UserLeft", $"User {arg.Id} left guild {arg.Guild.Id}"));
        }

        private async Task Client_UserJoined(SocketGuildUser arg)
        {
            string sql = $"INSERT INTO SBUsers(UserID, GuildID, WarnCount, AugmentsComplete) VALUES ({arg.Id}, {arg.Guild.Id}, 0, 0);";
            DBControl.UpdateDB(sql);
            await Client_Log(new LogMessage(LogSeverity.Info, "Client_UserJoined", $"User {arg.Id} joined guild {arg.Guild.Id}"));
        }

        private async Task CheckGuildsStartup()
        {
            foreach (SocketGuild g in Client.Guilds)
            {
                if (GlobalVars.GuildOptions.Where(x => x.GuildID == g.Id).Count() <= 0)
                {
                    await Client_JoinedGuild(g);
                    GetUsers(g);
                }
            }
        }

        private async Task Client_LeftGuild(SocketGuild arg)
        {
            Console.WriteLine($"{DateTime.Now} -> Left guild: {arg.Id}");

            GlobalVars.GuildOptions.Remove(GlobalVars.GuildOptions.Single(x => x.GuildID == arg.Id));

            DBControl.UpdateDB($"DELETE FROM SBGuilds WHERE GuildID = {arg.Id};");

            await UpdateActivity();
            await Task.Delay(100);

        }

        private async Task Client_JoinedGuild(SocketGuild arg)
        {
            Console.WriteLine($"{DateTime.Now} -> Joined guild: {arg.Id}");

            GuildOption go = new GuildOption();

            go.GuildID = arg.Id;
            go.GuildName = arg.Name;
            go.OwnerID = arg.Owner.Id;
            go.Prefix = "]";
            go.PunishThreshold = 0;
            go.MaxReserves = 1;
            GlobalVars.GuildOptions.Add(go);

            DBControl.UpdateDB($"INSERT INTO SBGuilds VALUES ({go.GuildID.ToString()}, '{go.GuildName.Replace(@"'", "_")}',{go.OwnerID.ToString()},'{go.Prefix}', {go.PunishThreshold}, {go.MaxReserves});");

            await UpdateActivity();
            await Task.Delay(100);
        }

        private async Task Client_Log(LogMessage arg)
        {
            if (arg.Severity <= LogSeverity.Info)
            {
                if (arg.Exception != null)
                {
                    Console.WriteLine($"EXCEPTION [{arg.Severity.ToString()}]: {DateTime.Now} at {arg.Exception.Source} -> {arg.Exception.Message}");
                }
                Console.WriteLine($"[{arg.Severity.ToString().ToUpper()}]: {DateTime.Now} at {arg.Source} -> {arg.Message}");
            }
            if (arg.Exception != null)
            {
                await LogWriter.WriteLogFile($"EXCEPTION [{arg.Severity.ToString()}]: {DateTime.Now} {arg.Exception.Source} -> {arg.Exception.Message}");
            }
            await LogWriter.WriteLogFile($"[{arg.Severity.ToString().ToUpper()}]: {DateTime.Now} at {arg.Source} -> {arg.Message}");
        }

        private async Task Client_Ready()
        {
            await UpdateActivity();
            await CheckGuildsStartup();
            await Client_Log(new LogMessage(LogSeverity.Info, "Client_Ready", "Bot ready!"));
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg.Content.Length <= 1 && msg.Embeds.Count == 0 && msg.Attachments.Count == 0) return;

            var context = new SocketCommandContext(Client, msg);
            var guildOptions = GlobalVars.GuildOptions.Single(x => x.GuildID == context.Guild.Id);

            if (context.Message == null && context.Message.Content == "") return;
            if (context.User.IsBot) return;

            int argPos = 0;

            if (!(msg.HasStringPrefix(guildOptions.Prefix, ref argPos)) && !(msg.HasMentionPrefix(Client.CurrentUser, ref argPos))) return;

            var r = Reservations.ResponseWaiters.SingleOrDefault(rw => rw.User == context.User && context.Message.Content.Contains($"reserve {rw.X} {rw.Y}"));
            if (r == null && !(await GlobalVars.CheckUserTimeout(context.Message.Author, context.Guild.Id, context.Channel))) return; 

            IResult Result = null;
            try
            {
                Result = await Commands.ExecuteAsync(context, argPos, Provider);
                if (Result.Error == CommandError.UnmetPrecondition)
                {
                    var errorMsg = await context.Channel.SendMessageAsync(Result.ErrorReason);
                    GlobalVars.AddRandomTracker(errorMsg);
                }
                else if (!Result.IsSuccess)
                {
                    if (Result.ErrorReason.ToLower().Contains("unknown command"))
                    {
                        await Client_Log(new LogMessage(LogSeverity.Error, "Client_MessageReceived", $"Unknown command sent by {context.Message.Author.ToString()} in guild: {context.Guild.Id} - Command text: {context.Message.Content}"));
                        var errorMsg = await context.Channel.SendMessageAsync($"Sorry, I don't know what I'm supposed to do with that...");
                        GlobalVars.AddRandomTracker(errorMsg);
                    }
                    else if (Result.ErrorReason.ToLower().Contains("too many param"))
                    {
                        await Client_Log(new LogMessage(LogSeverity.Warning, "Client_MessageReceived", $"Invalid parameters sent by {context.Message.Author.ToString()} in guild: {context.Guild.Id} - Command text: {context.Message.Content}"));
                        var errorMsg = await context.Channel.SendMessageAsync($"Pretty sure you goofed on the parameters you've supplied there {context.Message.Author.Mention}!");
                        GlobalVars.AddRandomTracker(errorMsg);
                    }
                    else
                        await Client_Log(new LogMessage(LogSeverity.Error, "Client_MessageReceived", $"Command text: {context.Message.Content} | Error: {Result.ErrorReason}"));
                }
                GlobalVars.AddUserTimeout(context.Message.Author, context.Guild.Id);
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Critical, context.Message.Content, Result.ErrorReason, ex));
            }
        }

        private async Task UpdateActivity()
        {
            await Client.SetGameAsync($"{GlobalVars.bSettings.activity.Replace("{count}", Client.Guilds.Count.ToString())} {GlobalVars.bSettings.version}");
        }

        private void GetSQLData()
        {
            //Load prefix & options from DB
            DB2ConnectionStringBuilder sBuilder = new DB2ConnectionStringBuilder();
            sBuilder.Database = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.Server = GlobalVars.dbSettings.host + ":" + GlobalVars.dbSettings.port;
            DB2Connection conn = new DB2Connection();
            conn.ConnectionString = sBuilder.ConnectionString;


            using (conn)
            {
                conn.Open();

                #region Get Guilds
                DB2Command cmd = new DB2Command($"SELECT * FROM SBGuilds", conn);
                DB2DataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    GuildOption go = new GuildOption();

                    go.GuildID = Convert.ToUInt64(dr.GetValue(0));
                    go.GuildName = Convert.ToString(dr.GetValue(1));
                    go.OwnerID = Convert.ToUInt64(dr.GetValue(2));
                    go.Prefix = Convert.ToString(dr.GetValue(3));
                    go.PunishThreshold = Convert.ToUInt16(dr.GetValue(4));
                    go.MaxReserves = Convert.ToUInt16(dr.GetValue(5));

                    GlobalVars.GuildOptions.Add(go);
                }
                #endregion

                conn.Close();
                conn.Dispose();
            }
        }

        private void GetUsers(SocketGuild guild)
        {
            DB2ConnectionStringBuilder sBuilder = new DB2ConnectionStringBuilder();
            sBuilder.Database = GlobalVars.dbSettings.db;
            sBuilder.UserID = GlobalVars.dbSettings.username;
            sBuilder.Password = GlobalVars.dbSettings.password;
            sBuilder.Server = GlobalVars.dbSettings.host + ":" + GlobalVars.dbSettings.port;
            DB2Connection conn = new DB2Connection();
            conn.ConnectionString = sBuilder.ConnectionString;

            using (conn)
            {
                conn.Open();

                DBControl.UpdateDB($"CREATE TABLE tmp{guild.Id} (UserID BIGINT, GuildID BIGINT, WarnCount SMALLINT, AugmentsComplete INT);");

                string sql = $"INSERT INTO tmp{guild.Id} VALUES";

                foreach (SocketUser user in guild.Users)
                {
                    if (!user.IsBot)
                        sql += $" ({user.Id}, {guild.Id}, 0, 0),";
                }
                sql = sql.TrimEnd(',');
                sql += ";";

                DBControl.UpdateDB(sql);

                sql = $"INSERT INTO SBUsers (UserID, GuildID, WarnCount, AugmentsComplete) SELECT UserID, GuildID, WarnCount, AugmentsComplete FROM tmp{guild.Id} AS NU WHERE NOT EXISTS ( SELECT 1 FROM SBUsers AS U WHERE U.UserID = NU.UserID AND U.GuildID = NU.GuildID AND U.WarnCount = NU.WarnCount AND U.AugmentsComplete = NU.AugmentsComplete);";
                DBControl.UpdateDB(sql);

                DBControl.UpdateDB($"DROP TABLE tmp{guild.Id};");

                conn.Close();
                conn.Dispose();
            }
        }
    }
}
