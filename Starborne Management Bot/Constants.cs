using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Starborne_Management_Bot
{
    internal static partial class Constants
    {
        internal const double _CMDTIMEOUT_ = 5d;

        internal static readonly string _WORKDIR_ = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        internal static readonly ulong[] _IGNOREDGUILDS_ = new ulong[] { 264445053596991498 };

        internal static string TranslateForOS(string s)
        {
            if (_WORKDIR_.Contains(@"/"))
            {
                s = s.Replace(@"\", @"/");
            }
            else
                s = s.Replace(@"/", @"\");
            return s;
        }
    }
}
