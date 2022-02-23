using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ActivityTracker.Models
{
    class Configuration : BaseClass
    {
        public enum ActivityTypeE
        {
            Idle,
            Walking,
            Jogging,
            Running,
            Bicicling,
            Elevatoring,

        }

        static Configuration _instance = null;
        public static Configuration Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new Configuration();
                }
                return _instance;
            }
        }

        private Configuration() : base(true)
        {
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Values = _observableValues,
                    //Fill = null
                }
            };
        }

        private ActivityTypeE _activityType = ActivityTypeE.Idle;
        public ActivityTypeE ActivityType
        {
            get => _activityType;
            set
            {
                _activityType = value;
                OnPropertyChanged();
            }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
                Log += IsEnabled ? "Tracker started\r\n" : "Tracker stopped\r\n";
            }
        }

        private string _log;
        public string Log
        {
            get => _log;
            set
            {
                _log = value;
                OnPropertyChanged();
            }
        }


        // LiveCharts already provides the LiveChartsCore.Defaults.ObservableValue class.
        private readonly ObservableCollection<ObservableValue> _observableValues = new ObservableCollection<ObservableValue>
        {
            new ObservableValue(2),
            new ObservableValue(5),
            new ObservableValue(4),
            new ObservableValue(5),
            new ObservableValue(2),
            new ObservableValue(6),
            new ObservableValue(6),
            new ObservableValue(6),
            new ObservableValue(4),
            new ObservableValue(2),
            new ObservableValue(3),
            new ObservableValue(4),
            new ObservableValue(3)
        };

        private ObservableCollection<ISeries> _series;
        public ObservableCollection<ISeries> Series 
        {
            get => _series;
            set
            {
                _series = value;
                OnPropertyChanged();
            }
        }
    }

}
