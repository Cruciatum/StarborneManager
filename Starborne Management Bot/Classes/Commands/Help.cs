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
        [Command("help"), Alias("commands","cmds","h")]
        public async Task ListCommands()
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = "Currently available commands";
            eb.WithColor(Color.Blue);

            var prefix = Context.Message.Content.Substring(0, 1);

            #region Add Commands to EmbedBuilder
            eb.AddField($"{prefix}reserve [coord1] [coord2]", $"Reserve a station.");
            eb.AddField($"{prefix}reserve check <coord1> <coord2>", $"Check which stations have been reserved by members of your alliance\nOptional parameters: coordinates.");
            eb.AddField($"{prefix}reserve remove [coord1] [coord2]", $"Reserve a location you have reserved in the past.");
            eb.AddField($"{prefix}nap list", $"View all active Non-Aggression Pacts for your Alliance.");
            eb.AddField($"{prefix}nap add [AllianceName] <AllianceTag>", $"Add a new active Non-Aggression Pact.");
            eb.AddField($"{prefix}nap remove [AllianceName]", $"Remove an inactive Non-Aggression Pact.");
            eb.AddField($"{prefix}warn max [amount]", $"Set the maximum amount of warnings a user can receive before facing consequences.\n*(Administrator permission required)*");
            eb.AddField($"{prefix}warn [@User]", $"Add a warning to [@User]'s record, also displays current amount of warnings.\n*(Administrator permission required)*");
            #endregion

            await Context.Channel.SendMessageAsync(null, false, eb.Build());
        }
    }
}
