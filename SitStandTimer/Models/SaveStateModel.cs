using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitStandTimer.Models
{
    /// <summary>
    /// A model for storing data on disk in between app sessions
    /// </summary>
    public class SaveStateModel
    {
        public long CurrentModeStartTime { get; set; }
        public Mode CurrentMode { get; set; }
    }
}
