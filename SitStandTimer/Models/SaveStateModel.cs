﻿using System;
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

        // Adding this field for debug purposes so we can see when the background task is running and what it is doing
        public DebugInfo LastRunDebugInfo { get; set; }
    }

    public class DebugInfo
    {
        public string LastRunTime { get; set; }
        public string[] ScheduledNotificationsRemoved { get; set; }
        public string[] NotificationsScheduled { get; set; }
    }
}
