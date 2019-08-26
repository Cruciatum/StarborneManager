using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Starborne_Management_Bot.Classes.Commands
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        [Command("help"), Alias("commands", "cmds", "h")]
        public async Task ListCommands([Remainder]string arg = "")
        {
            var prefix = Context.Message.Content.Substring(0, 1);

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithColor(Color.Blue);
            if (arg == "")
            {
                eb.Title = "Currently available command categories";
                eb.AddField("Reserve", $"For more commands: {prefix}help reserve");
                eb.AddField("NAP", $"For more commands: {prefix}help nap");
                eb.AddField("Aug", $"For more commands: {prefix}help aug");
                eb.AddField("Misc", $"For more commands: {prefix}help misc");
            }
            else
            {
                #region Add Commands to EmbedBuilder
                switch (arg.ToLower())
                {
                    case "reserve":
                        #region Reserve commands
                        eb.AddField($"{prefix}reserve [coord1] [coord2]", $"Reserve a station.");
                        eb.AddField($"{prefix}reserve list <@User>", $"List all reserved locations by yourself or by [@User]");
                        eb.AddField($"{prefix}reserve check <coord1> <coord2>", $"Check which stations have been reserved by members of your alliance\nOptional parameters: coordinates.");
                        eb.AddField($"{prefix}reserve remove [coord1] [coord2]", $"Remove a location you have reserved in the past.");
                        #endregion
                        break;
                    case "nap":
                        #region NAP commands
                        eb.AddField($"{prefix}nap list", $"View all active Non-Aggression Pacts for your Alliance.");
                        eb.AddField($"{prefix}nap add [AllianceName] <AllianceTag>", $"Add a new active Non-Aggression Pact.\n**NOTE:** Make sure you replaces any whitespace in the Alliance's name with an underscore (_)");
                        eb.AddField($"{prefix}nap remove [AllianceName]", $"Remove an inactive Non-Aggression Pact.");
                        #endregion
                        break;
                    case "aug":
                        #region Aug commands
                        eb.AddField($"{prefix}aug request [coord1] [coord2]", "Put in a request for an augmentation on your outposts.");
                        eb.AddField($"{prefix}aug search <amount>", "Get the [amount] oldest augmentation requests.");
                        eb.AddField($"{prefix}aug search [@User] <amount>", "Get the [amount] oldest augmentation requests by [@User]");
                        eb.AddField($"{prefix}aug complete [ID]", $"Mark an augmentation request as being completed. (ID can be found using {prefix}aug search)");
                        eb.AddField($"{prefix}aug complete [@User] [coord1] [coord2]", "Mark an augmentation request on location [coord1,coord2] by [@User] as completed.");
                        #endregion
                        break;
                    case "misc":
                        #region Misc commands
                        eb.AddField($"{prefix}userinfo [@User]", "Get information for a user in this alliance.");
                        eb.AddField($"{prefix}reserve max [amount]", $"Set a maximum on howmany locations each user can reserve.\n*(Administrator permissions required)*");
                        eb.AddField($"{prefix}warn max [amount]", $"Set the maximum amount of warnings a user can receive before facing consequences.\n*(Administrator permission required)*");
                        eb.AddField($"{prefix}warn [@User]", $"Add a warning to [@User]'s record, also displays current amount of warnings.\n*(Administrator permission required)*");
                        #endregion
                        break;
                    default:
                        var m = await Context.Channel.SendMessageAsync($"Unknown option for Help: \"{arg}\"");
                        GlobalVars.AddRandomTracker(m);
                        return;
                }
                #endregion
            }
            await Context.Channel.SendMessageAsync(null, false, eb.Build());
        }
    }
}
