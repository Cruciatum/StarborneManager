using System;
using System.Collections.Generic;
using System.Text;

namespace Starborne_Management_Bot.Classes.HelperObjects
{
    internal class GuildOption
    {
        internal ulong GuildID { get; set; }
        internal string GuildName { get; set; }
        internal ulong OwnerID { get; set; }
        internal string Prefix { get; set; }
        internal ushort PunishThreshold { get; set; }
        internal ushort MaxReserves { get; set; }
    }
}
