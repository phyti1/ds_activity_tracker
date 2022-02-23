using System;
using System.Collections.Generic;
using System.Text;
using static ActivityTracker.Models.Configuration;

namespace ActivityTracker.ViewModels
{
    class MainPageViewModel
    {
        internal static Dictionary<ActivityTypeE, string> ActivityNameMapping = new Dictionary<ActivityTypeE, string>()
        {
            {ActivityTypeE.Idle, "Idle" },
            {ActivityTypeE.Walking, "Spazieren" },
            {ActivityTypeE.Jogging, "Joggen" },
            {ActivityTypeE.Running, "Rennen" },
            {ActivityTypeE.Bicicling, "Fahhrad fahren" },
            {ActivityTypeE.Elevatoring, "Lift fahren" }
        };

        public List<string> ActivityNames
        {
            get
            {
                return new List<string>(ActivityNameMapping.Values);
            }
        }

        internal MainPageViewModel()
        {

        }
    }
}
