using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace Starborne_Management_Bot.Classes.HelperObjects
{
    internal static class Extensions
    {
        internal static Timer StartTimer(this Timer t, ElapsedEventHandler handler, ulong interval)
        {
            t.Interval = interval;
            t.Elapsed += handler;
            t.Enabled = true;

            return t;
        }
    }
}
