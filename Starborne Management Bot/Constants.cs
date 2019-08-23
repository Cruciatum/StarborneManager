using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Starborne_Management_Bot
{
    internal static class Constants
    {
        internal const double _CMDTIMEOUT_ = 5d;
        internal const string _DBTOKEN_ = "YIJGegaMbG0AAAAAAAAA5w8fnpfyUn5OTr7IJ2lvRkSpmWgYhv7algWnR342UasH";

        internal static readonly string _WORKDIR_ = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

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
