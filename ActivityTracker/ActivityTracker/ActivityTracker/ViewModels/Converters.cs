using ActivityTracker.ViewModels;
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
            string activity = TabbedPageViewModel.ActivityNameMapping[(ActivityTypeE)value];
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
            foreach(var act_key in TabbedPageViewModel.ActivityNameMapping.Keys)
            {
                if(TabbedPageViewModel.ActivityNameMapping[act_key] == (string)value)
                {
                    return act_key;
                }
            }
            throw new InvalidOperationException();
        }
    }
    public class EnumNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Models.Configuration.ModelTypeE)value;
        }
    }

    public class PredictionEmojiConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string prediction = (string)value;
            if (prediction == "Walking")
            {
                return "🚶";
            }
            else if(prediction == "jogging")
            {
                return "🏃";
            }
            else if (prediction == "Elevatoring")
            {
                return "🛗";
            }
            else if (prediction == "Sitting")
            {
                return "🪑";
            }
            else if (prediction == "Bicicling")
            {
                return "🚴‍";
            }
            else if(prediction == "Stairway")
            {
                return "𓊍";
            }
            else if(prediction == "Transport")
            {
                return "🚊🚗";
            }
            else
            {
                return prediction;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }

}
