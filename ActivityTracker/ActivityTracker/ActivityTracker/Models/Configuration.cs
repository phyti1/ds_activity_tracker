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
    public enum RunTypeE
    {
        None,
        Predicting,
        Tracking
    }
    public enum ModelTypeE
    {
        None,
        SPR_RandomForest,
        SPR_CNN1,
        SPR_CNN2,
        RF_cnn,
        RF_Cluster
    }


    public class StringClass : BaseClass
    {
        string _value = "";
        public string Value
        {
            get => _value;
            set
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    _value = value;
                    OnPropertyChanged();
                });
            }
        }
    }
    public class ModelTypeClass : BaseClass
    {
        ModelTypeE _value = ModelTypeE.None;
        public ModelTypeE Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }
    }
    public class Configuration : BaseClass
    {
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
        public int MeasIndex = 0;
        public string MeasGuid { get; internal set; }
        public DataAquisition DataAquisition { get; private set; }
        private Configuration()
        {
            ResetGraphs();
            DataAquisition = new DataAquisition();
            SelectedModels.Add(new ModelTypeClass() { Value = ModelTypeE.None });
            SelectedModels.Add(new ModelTypeClass() { Value = ModelTypeE.None });
            Predictions.Add(new StringClass());
            Predictions.Add(new StringClass());
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
        private List<ModelTypeClass> _selectedModels = new List<ModelTypeClass>();
        public List<ModelTypeClass> SelectedModels
        {
            get => _selectedModels;
            set
            {
                _selectedModels = value;
                OnPropertyChanged();
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
        private bool _isTracking;
        public bool IsTracking
        {
            get => _isTracking;
            set
            {
                if(value != _isTracking)
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
                            Log = "";
                            MessagingCenter.Send(new StartServiceMessage(), "ServiceStarted");
                            //_dataAquisition.Start();
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
                        _isTracking = value;
                        OnPropertyChanged();
                        Log += IsTracking ? "Tracker started" : "Tracker stopped";
                    }
                }
            }
        }
        private bool _isPredicting = false;
        public bool IsPredicting
        {
            get => _isPredicting;
            set
            {
                if(value != _isPredicting)
                {
                    // do not run in background, start directly
                    if (value)
                    {
                        Log = "";
                        DataAquisition.Start();
                        for(int i = 0; i < SelectedModels.Count(); i++)
                        {
                            if (SelectedModels[i].Value != ModelTypeE.None)
                            {
                                Predictions[i].Value = "loading...";
                            }
                        }
                    }
                    else
                    {
                        DataAquisition.Stop();
                        Predictions.ForEach(p => p.Value = "");
                    }
                    _isPredicting = value;
                    OnPropertyChanged();
                }
            }
        }

        private List<StringClass> _predictions = new List<StringClass>();
        public List<StringClass> Predictions
        {
            get => _predictions;
            set
            {
                _predictions = value;
                OnPropertyChanged();
            }
        }

        private int n_MaxLogChars = 10000;
        private string _log;
        public string Log
        {
            get => _log;
            set
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    _log = value + "\r\n";
                    //limit to n last characters so the app does not hang up on errors
                    if (_log.Length > n_MaxLogChars)
                    {
                        _log = "reduced output due to performance optimization...\r\n\r\n" + _log.Substring(_log.Length - n_MaxLogChars, n_MaxLogChars);
                    }
                    OnPropertyChanged();
                });
            }
        }


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
        private string _csvLogHistory = "";
        public async Task SendResetLog(RunTypeE runtype)
        {
            if(runtype == RunTypeE.None)
            {
                throw new Exception("either prediction or tracking mode must be specified");
            }
            var _localLog = _csvLog;
            _csvLog = "";
            bool _success = false;
            if (runtype == RunTypeE.Predicting)
            {
                //allow for faster sending intervals
                _csvLogHistory += _localLog;
                //reduce text to 180 last records
                _csvLogHistory = string.Join("\n", _csvLogHistory.Split(new char[] { '\n' }).Reverse().Take(180).Reverse());
                for(int i = 0; i < SelectedModels.Count(); i++)
                {
                    var _selectedModel = SelectedModels[i].Value;
                    if(_selectedModel != ModelTypeE.None)
                    {
                        var _oldTime = DateTime.Now;
                        string _prediction = await Database.PostPrediction(_csvLogHistory, _selectedModel);
                        Predictions[i].Value = _prediction;
                        var _timeDiff = DateTime.Now - _oldTime;
                        Log += $"{Configuration.Instance.SelectedModels[i].Value}: <{Predictions[i].Value}> req. time: {Math.Round(_timeDiff.TotalMilliseconds)}ms";
                    }
                }
                _success = true;
            }
            if(runtype == RunTypeE.Tracking)
            {
                _success = await Database.SendTrackingData(_localLog);
            }
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
                    if (runtype == RunTypeE.Tracking)
                    {
                        Log += $"Data sent ({MeasIndex})";
                    }
                });
            }
            //short feedback for data logging
            if (runtype == RunTypeE.Tracking)
            {
                Vibration.Vibrate(TimeSpan.FromMilliseconds(100));
            }
        }
    }

}
