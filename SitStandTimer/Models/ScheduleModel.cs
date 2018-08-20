using System;
using System.Collections.Generic;

namespace SitStandTimer.Models
{
    /// <summary>
    /// Indicates how the user wants the schedule loop to be repeated
    /// </summary>
    public enum ScheduleType
    {
        Indefinite, // Loop forever
        NumTimes, // Loop a set number of times before stopping
        Scheduled // Run the loop on a schedule (i.e. 9am - 5pm M-F)
    }

    public class ScheduleModel
    {
        public ScheduleModel()
        {
            // Initialize defaults that will get overwritten with any values we find during JSON deserialization
            ScheduleType = Constants.ScheduleDefaults.ScheduleType;
            NumTimesToLoop = Constants.ScheduleDefaults.NumTimesToLoop;
            Days = Constants.ScheduleDefaults.Days;
            StartTime = Constants.ScheduleDefaults.StartTime;
            EndTime = Constants.ScheduleDefaults.EndTime;
        }

        public ScheduleType ScheduleType { get; set; }

        /// <summary>
        /// This value will only be set if ScheduleType equals ScheduleType.NumTimes
        /// </summary>
        public int NumTimesToLoop { get; set; }

        /// <summary>
        /// Which days to run the scheduled tasks. Will only be set if ScheduleType equals ScheduleType.Scheduled.
        /// </summary>
        public HashSet<DayOfWeek> Days { get; set; }

        /// <summary>
        /// The time to begin scheduled tasks if it a valid day of the week. Only the Hour and Minute properties should be set on StartTime.
        /// 
        /// Will only be set if ScheduleType equals ScheduleType.Scheduled.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// The time to end scheduled tasks if it a valid day of the week. Only the Hour and Minute properties should be set on EndTime.
        /// 
        /// Will only be set if ScheduleType equals ScheduleType.Scheduled.
        /// </summary>
        public TimeSpan EndTime { get; set; }
    }
}
