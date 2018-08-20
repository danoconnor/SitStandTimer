using SitStandTimer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitStandTimer
{
    class Constants
    {
        public class ScheduleDefaults
        {
            public static ScheduleType ScheduleType
            {
                get { return ScheduleType.Indefinite; }
            }

            public static int NumTimesToLoop
            {
                get { return 5; }
            }

            public static HashSet<DayOfWeek> Days
            {
                get { return new HashSet<DayOfWeek>(); }
            }

            public static TimeSpan StartTime
            {
                get { return new TimeSpan(9, 0, 0); }
            }

            public static TimeSpan EndTime
            {
                get { return new TimeSpan(17, 0, 0); }
            }
        }
    }
}
