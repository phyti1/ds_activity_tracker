using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;
using static ActivityTracker.Models.Configuration;

namespace ActivityTracker.ViewModels
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    public class ActivityTypeNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string activity = MainPageViewModel.ActivityNameMapping[(ActivityTypeE)value];
            return activity;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null) 
            { 
                //default
                return Models.Configuration.Instance.ActivityType; 
            }
            // get key from value
            foreach(var act_key in MainPageViewModel.ActivityNameMapping.Keys)
            {
                if(MainPageViewModel.ActivityNameMapping[act_key] == (string)value)
                {
                    return act_key;
                }
            }
            throw new InvalidOperationException();
        }
    }

}
