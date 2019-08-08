using System;
using System.Collections.Generic;
using System.Text;

namespace Starborne_Management_Bot.Classes.HelperObjects
{
    internal class Options
    {
        internal bool LogEmbeds { get; set; }
        internal bool LogAttachments { get; set; }
        internal bool Option3 { get; set; }
        internal bool Option4 { get; set; }

        internal ulong LogChannelID { get; set; }
    }
}
