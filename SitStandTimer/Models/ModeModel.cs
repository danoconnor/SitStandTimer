using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitStandTimer.Models
{
    public class ModeModel
    {
        public string ModeName { get; set; }
        public TimeSpan TimeInMode { get; set; }

        public override string ToString()
        {
            string timeString = "";
            if (TimeInMode.Hours != 0)
            {
                timeString += TimeInMode.Hours + " hours ";
            }
            
            if (TimeInMode.Minutes != 0)
            {
                timeString += TimeInMode.Minutes + " minutes ";
            }

            if (TimeInMode.Seconds != 0)
            {
                timeString += TimeInMode.Seconds + " seconds";
            }

            return string.Format("{0} for {1}", ModeName, timeString.Trim());
        }
    }
}
