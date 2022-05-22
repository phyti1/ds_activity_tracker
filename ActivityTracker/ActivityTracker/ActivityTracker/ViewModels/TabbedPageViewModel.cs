using ActivityTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ActivityTracker.Models.Configuration;

namespace ActivityTracker.ViewModels
{
    class TabbedPageViewModel
    {
        public List<string> ModelNames
        {
            get
            {
                var _list = Enum.GetNames(typeof(Configuration.ModelTypeE)).ToList();
                return _list;
            }
        }
        internal static Dictionary<ActivityTypeE, string> ActivityNameMapping = new Dictionary<ActivityTypeE, string>()
        {
            {ActivityTypeE.Sitting, "Stehen/Sitzen" },
            {ActivityTypeE.Walking, "Spazieren" },
            {ActivityTypeE.Jogging, "Joggen" },
            {ActivityTypeE.Bicycling, "Fahhrad fahren" },
            {ActivityTypeE.Elevatoring, "Lift fahren" },
            {ActivityTypeE.Stairway, "Treppen steigen" },
            {ActivityTypeE.Transport, "Fahrzeug fahren" },
        };

        public List<string> ActivityNames
        {
            get
            {
                return new List<string>(ActivityNameMapping.Values);
            }
        }

        internal TabbedPageViewModel()
        {

        }
    }
}
