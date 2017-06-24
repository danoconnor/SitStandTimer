using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitStandTimer.ViewModels
{
    public class MainPageVM
    {
        public MainPageVM()
        {
            ModeText = "STAND";
            TimeLeftText = "59 minutes until you can sit.";
        }

        public string ModeText { get; private set; }
        public string TimeLeftText { get; private set; }
    }
}
