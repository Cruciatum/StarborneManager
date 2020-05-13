using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Threading.Tasks;
using Starborne_Management_Bot.Classes.HelperObjects;
using System.Linq;
using Discord.Rest;

namespace Starborne_Management_Bot
{
    internal static class GlobalVars
    {
        internal static DiscordSocketClient Client { get; set; }
        internal static DBSettings dbSettings { get; set; }
        internal static BotSettings bSettings { get; set; }

        internal static List<GuildOption> GuildOptions { get; set; } = new List<GuildOption>();

        internal static List<TimeoutTracker> UserTimeouts { get; set; } = new List<TimeoutTracker>(); //Keep track of user timeouts after command usage
        internal static List<TimeoutTimer> UserTimeoutTimers { get; set; } = new List<TimeoutTimer>(); //Keep track of timers so proper one can be found
        internal static List<TrackedMessage> RandomMessages { get; set; } = new List<TrackedMessage>(); //For Random other stuff including error messages

        internal static void AddRandomTracker(RestUserMessage msg)
        {
            var tMsg = new TrackedMessage(msg, 0);
            Timer t = new Timer();
            async void handler(object sender, ElapsedEventArgs e)
            {
                t.Stop();
                await UntrackMessage(tMsg);
            }
            t.StartTimer(handler, 15000);
            RandomMessages.Add(tMsg);
        }

        internal static void AddUserTimeout(SocketUser usr, ulong guildID)
        {
            var track = new TimeoutTracker(usr, guildID);
            TimeoutTimer tTimer = null;
            Timer t = new Timer();
            void handler(object sender, ElapsedEventArgs e)
            {
                t.Stop();
                if (UserTimeouts.Contains(track))
                {
                    UserTimeouts.Remove(track);
                    UserTimeoutTimers.Remove(tTimer);
                }
            }
            t.StartTimer(handler, (int)(Constants._CMDTIMEOUT_ * 1000));
            tTimer = new TimeoutTimer(track);
            UserTimeouts.Add(track);
            UserTimeoutTimers.Add(tTimer);
        }
        internal static async Task<bool> CheckUserTimeout(SocketUser usr, ulong guildID, IMessageChannel channel)
        {
            var Tracker = UserTimeouts.SingleOrDefault(ut => ut.TrackedUser == usr && ut.GuildID == guildID);
            if (Tracker != null)
            {
                TimeoutTimer t = UserTimeoutTimers.SingleOrDefault(p => p.Tracker == Tracker);
                var msg = await channel.SendMessageAsync($"Slow down {usr.Username}! Try again in {TimeSpan.FromSeconds((int)Constants._CMDTIMEOUT_ - (DateTime.Now - t.StartTime).TotalSeconds).Seconds}.{(TimeSpan.FromSeconds(5 - (DateTime.Now - t.StartTime).TotalSeconds).Milliseconds) / 100} seconds.");
                AddRandomTracker((RestUserMessage)msg);
                return false;
            }
            return true;
        }

        internal static async Task UntrackMessage(TrackedMessage msg)
        {
            if (RandomMessages.Contains(msg)) RandomMessages.Remove(msg);


            if (!msg.IsDeleted)
            {
                try
                {
                    await msg.SourceMessage.DeleteAsync();
                }
                catch { }
                msg.IsDeleted = true;
            }
        }
    }

    internal class TrackedMessage
    {
        internal RestUserMessage SourceMessage { get; set; }
        internal ulong TriggerById { get; set; }
        internal bool IsDeleted { get; set; }

        public TrackedMessage(RestUserMessage source, ulong triggerID)
        {
            SourceMessage = source;
            TriggerById = triggerID;
        }
    }
    internal class TimeoutTracker
    {
        internal SocketUser TrackedUser { get; set; }
        internal ulong GuildID { get; set; }

        public TimeoutTracker(SocketUser usr, ulong id)
        {
            TrackedUser = usr;
            GuildID = id;
        }
    }
    internal class TimeoutTimer
    {
        internal TimeoutTracker Tracker { get; set; }
        internal DateTime StartTime { get; }

        internal TimeoutTimer(TimeoutTracker timeout)
        {
            Tracker = timeout;
            StartTime = DateTime.Now;
        }
    }
}

