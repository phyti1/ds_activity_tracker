using ActivityTracker.Helpers;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using XamarinForms.LocationService.Messages;

namespace ActivityTracker.Models
{
    public class Configuration : BaseClass
    {
        public enum ActivityTypeE
        {
            Sitting,
            Walking,
            Jogging,
            Running,
            Bicycling,
            Elevatoring,
            Stairway,
            Transport,
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
        internal int MeasIndex = 0;
        internal string MeasGuid { get; private set; }
        public DataAquisition DataAquisition { get; private set; }
        private Configuration() : base(true)
        {
            ResetGraphs();
            DataAquisition = new DataAquisition();
        }
        public void ResetGraphs()
        {
            VisLeft = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Values = _accelometer,
                    //Fill = 
                }
            };
            VisRight = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Values = _magnetometer,
                }
            };
            _accelometer.Clear();
            _magnetometer.Clear();
        }

        private ActivityTypeE? _activityType = null;
        public ActivityTypeE ActivityType
        {
            get
            {
                if(_activityType == null)
                {
                    var _result = SecureStorage.GetAsync(nameof(ActivityType)).Result;
                    if(_result != null)
                    {
                        _activityType = (ActivityTypeE)Convert.ToInt32(_result);
                    }
                    else
                    {
                        _activityType = ActivityTypeE.Sitting;
                    }
                }
                //saving does not work as ui saves default 
                return (ActivityTypeE)_activityType;
            }
            set
            {
                _activityType = value;
                SecureStorage.SetAsync(nameof(ActivityType), Convert.ToString((int)_activityType.Value));
                OnPropertyChanged();
                MeasGuid = Guid.NewGuid().ToString().Substring(0, 8);
                MeasIndex = 0;
            }
        }
        private string _name = null;
        public string Name
        {
            get
            {
                if(_name == null)
                {
                    var _result = SecureStorage.GetAsync(nameof(Name)).Result;
                    if (_result != null)
                    {
                        _name = _result;
                    }
                }
                return _name;
            }
            set
            {
                _name = value;
                SecureStorage.SetAsync(nameof(Name), _name);
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
                        Log += "Name cannot be empty";
                        OnPropertyChanged();
                    }
                    else
                    {
                        if (value)
                        {
                            MessagingCenter.Send(new StartServiceMessage(), "ServiceStarted");
                            //_dataAquisition.Start();
                            Vibration.Vibrate(TimeSpan.FromSeconds(1));
                            MeasGuid = Guid.NewGuid().ToString().Substring(0, 8);
                            MeasIndex = 0;
                        }
                        else
                        {
                            MessagingCenter.Send(new StopServiceMessage(), "ServiceStopped");
                            //_dataAquisition.Stop();
                            // reset visualization
                            while (Instance.DataAquisition.AreWorkersRunning())
                            {
                                Thread.Sleep(10);
                            }
                            ResetGraphs();
                            Vibration.Vibrate(TimeSpan.FromSeconds(1));
                            //wait for 1st vibration to end and add a pause
                            Thread.Sleep(1100);
                            Vibration.Vibrate(TimeSpan.FromSeconds(1));
                        }
                        _isEnabled = value;
                        OnPropertyChanged();
                        Log += IsEnabled ? "Tracker started" : "Tracker stopped";
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
                _log = value + "\r\n";
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

        private ObservableCollection<ISeries> _visLeft;
        public ObservableCollection<ISeries> VisLeft 
        {
            get => _visLeft;
            set
            {
                _visLeft = value;
                OnPropertyChanged();
            }
        }
        private ObservableCollection<ISeries> _visRight;
        public ObservableCollection<ISeries> VisRight 
        {
            get => _visRight;
            set
            {
                _visRight = value;
                OnPropertyChanged();
            }
        }
        private string _csvLog;
        private double _lastAcc = 0;
        public void AddLog(Vector3 accelometer, Vector3 magnetometer, Vector3 _gyroscopeData, Quaternion _orientationData, Location location)
        {
            _csvLog += $"{DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss.fff")},{Name},{ActivityType},{accelometer.X},{accelometer.Y},{accelometer.Z},{magnetometer.X},{magnetometer.Y},{magnetometer.Z},{_gyroscopeData.X},{_gyroscopeData.Y},{_gyroscopeData.Z},{_orientationData.X},{_orientationData.Y},{_orientationData.Z},{_orientationData.W},{location?.Latitude},{location?.Longitude}\r\n";
            //add ui series
            Device.BeginInvokeOnMainThread(() =>
            {
                var _newAcc = Math.Abs(accelometer.X) + Math.Abs(accelometer.Y) + Math.Abs(accelometer.Z);
                _accelometer.Add(new ObservableValue(_newAcc - _lastAcc));
                _lastAcc = _newAcc;
                _magnetometer.Add(new ObservableValue(Math.Abs(magnetometer.X) + Math.Abs(magnetometer.Y) + Math.Abs(magnetometer.Z)));
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
                    Log += "Data sending failure, retrying next time";
                });
            }
            else
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Log += $"Data sent ({MeasIndex})";
                });
            }
            //short feedback
            Vibration.Vibrate(TimeSpan.FromMilliseconds(100));
        }
    }

}
