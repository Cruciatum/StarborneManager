using Discord;
using Discord.Commands;
using Starborne_Management_Bot.Classes.HelperObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starborne_Management_Bot.Classes.Commands
{
    public class ModCmds : ModuleBase<SocketCommandContext>
    {
        [Command("setprefix"), Alias("prefix", "newprefix"), Summary("Set a new prefix for this server"), RequireUserPermission(GuildPermission.Administrator, ErrorMessage = "You require Administrator permissions to do this")]
        public async Task SetPrefix(string newPrefix)
        {
            if (newPrefix != null && newPrefix != "")
            {
                GlobalVars.GuildOptions.Single(x => x.GuildID == Context.Guild.Id).Prefix = newPrefix;
                DBControl.UpdateDB($"UPDATE SBGuilds SET Prefix = '{newPrefix}' WHERE GuildID = {Context.Guild.Id};");

                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I have updated your server's prefix to {newPrefix}");
            }
        }

        [Command("goodbye"), Summary("Leave a server")]
        public async Task LeaveGuild()
        {
            if (Context.User.Id != Context.Guild.Owner.Id)
            {
                var uMsg = await Context.Channel.SendMessageAsync($"{Context.User.Mention}, NO, screw you! Only {Context.Guild.Owner.Mention} can make me leave!");
                var owner = Context.Guild.Owner;
                var channel = await owner.GetOrCreateDMChannelAsync();
                var msgToOwner = await channel.SendMessageAsync($"Hi {Context.Guild.Owner.Username}, user {Context.User.Mention} has tried to make me leave {Context.Guild.Name} in channel: #{Context.Channel.Name}");

                GlobalVars.AddRandomTracker(uMsg);
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Goodbye {Context.Guild.Owner.Mention}, apparantly {Context.User.Mention} wants me gone :sob:");
                await Context.Guild.LeaveAsync();
            }
        }
    }
}
