using ActivityTracker.Helpers;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

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
        private DataAquisition _dataAquisition;
        private Configuration() : base(true)
        {
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Values = _accelometer,
                    //Fill = null
                }
            };
            Series.Add(new LineSeries<ObservableValue>
            {
                Values = _magnetometer,
                //Fill = null
            });
            _dataAquisition = new DataAquisition();
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
        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if(value != _isEnabled)
                {
                    if (string.IsNullOrWhiteSpace(Name))
                    {
                        Log += "Name cannot be empty\r\n";
                        OnPropertyChanged();
                    }
                    else
                    {
                        if (value)
                        {
                            _dataAquisition.Start();
                        }
                        else
                        {
                            _dataAquisition.Stop();
                        }
                        _isEnabled = value;
                        OnPropertyChanged();
                        Log += IsEnabled ? "Tracker started\r\n" : "Tracker stopped\r\n";
                    }
                }
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

        private ObservableCollection<ObservableValue> _accelometer = new ObservableCollection<ObservableValue>();
        private ObservableCollection<ObservableValue> _magnetometer = new ObservableCollection<ObservableValue>();

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
        private string _csvLog; //= "datetime,activity,acc_x,acc_y,acc_z,mag_x,mag_y,mag_z,lat,long\r\n";
        public void AddLog(Vector3 accelometer, Vector3 magnetometer, Vector3 _gyroscopeData, Quaternion _orientationData, Location location)
        {
            _csvLog += $"{DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss.fff")},{Name},{ActivityType},{accelometer.X},{accelometer.Y},{accelometer.Z},{magnetometer.X},{magnetometer.Y},{magnetometer.Z},{_gyroscopeData.X},{_gyroscopeData.Y},{_gyroscopeData.Z},{_orientationData.X},{_orientationData.Y},{_orientationData.Z},{_orientationData.W},{location?.Latitude},{location?.Longitude}\r\n";
            //add ui series
            Device.BeginInvokeOnMainThread(() =>
            {
                _accelometer.Add(new ObservableValue(Math.Abs(accelometer.X) + Math.Abs(accelometer.Y) + Math.Abs(accelometer.Z) / 10000) );
                _magnetometer.Add(new ObservableValue(Math.Abs(magnetometer.X) + Math.Abs(magnetometer.Y) * magnetometer.Z));
                if (_accelometer.Count > 100)
                {
                    _accelometer.RemoveAt(0);
                }
                if (_magnetometer.Count > 100)
                {
                    _magnetometer.RemoveAt(0);
                }
            });
        }
        public async Task SendResetLog()
        {
            var _localLog = _csvLog;
            _csvLog = "";
            var _success = await Database.SendData(_localLog);
            if (!_success)
            {
                _csvLog = _localLog + _csvLog;
                Device.BeginInvokeOnMainThread(() =>
                {
                    Log += "Data sending failure, retrying next time\r\n";
                });
            }
            else
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Log += "Data sent\r\n";
                });
            }
        }
    }

}
