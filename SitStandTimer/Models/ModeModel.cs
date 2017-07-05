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
            // Format the time so we don't have a bunch of zeros
            string timeFormat = @"hh\:mm\:ss";
            if (TimeInMode < TimeSpan.FromMinutes(1))
            {
                timeFormat = @"ss";
            }
            else if (TimeInMode < TimeSpan.FromHours(1))
            {
                timeFormat = @"mm\:ss";
            }

            return string.Format("{0} for {1}", ModeName, TimeInMode.ToString(timeFormat));
        }
    }
}
