using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ActivityTracker.Models
{
    public class DataAquisition
    {
        private Task _worker;
        private Task _geoWorker;
        private Location _location = Geolocation.GetLastKnownLocationAsync().Result;
        public bool AreWorkersRunning()
        {
            if(!_worker.IsCanceled && !_worker.IsCompleted && _worker.Status != TaskStatus.WaitingForActivation &&
               !_geoWorker.IsCanceled && !_geoWorker.IsCompleted && _geoWorker.Status != TaskStatus.WaitingForActivation)
            {
                return true;
            }
            return false;
        }
        private CancellationTokenSource _src;
        private SensorSpeed _speed = SensorSpeed.Fastest;
        private List<Vector3> _magentometerData = new List<Vector3>();
        private List<Vector3> _accelometerData = new List<Vector3>();
        private List<Vector3> _gyroscopeData = new List<Vector3>();
        private List<Quaternion> _orientationData = new List<Quaternion>();

        public DataAquisition()
        {
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            Magnetometer.ReadingChanged += Magnetometer_ReadingChanged;
            OrientationSensor.ReadingChanged += OrientationSensor_ReadingChanged;
            Gyroscope.ReadingChanged += Gyroscope_ReadingChanged;
        }
        ~DataAquisition()
        {
            // This is very essential, the phone sensors can hang up if the tracking is not stopped on closing.
            // Only a complete restart will restart the sensors.

            if (Accelerometer.IsMonitoring) { Accelerometer.Stop(); }
            if (Magnetometer.IsMonitoring) { Magnetometer.Stop(); }
            if (OrientationSensor.IsMonitoring) { OrientationSensor.Stop(); }
            if (Gyroscope.IsMonitoring) { Gyroscope.Stop(); }
        }

        public void Start()
        {
            Vibration.Vibrate(TimeSpan.FromSeconds(1));
            Configuration.Instance.MeasGuid = Guid.NewGuid().ToString().Substring(0, 8);
            Configuration.Instance.MeasIndex = 0;

            _src = new CancellationTokenSource();
            CancellationToken ct = _src.Token;
            _worker = Task.Run(async () =>
            {
                //start delay
                //Thread.Sleep(10000);
                Device.BeginInvokeOnMainThread(() =>
                {
                    Configuration.Instance.Log += "Measurement starting";
                });
                int _msCounter = 0;
                try
                {
                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();
                        // log measurement every 10 ms
                        Thread.Sleep(50);
                        if (!Accelerometer.IsMonitoring) { Accelerometer.Start(_speed); }
                        if (!Magnetometer.IsMonitoring) { Magnetometer.Start(_speed); }
                        if (!OrientationSensor.IsMonitoring) { OrientationSensor.Start(_speed); }
                        if (!Gyroscope.IsMonitoring) { Gyroscope.Start(_speed); }

                        ct.ThrowIfCancellationRequested();
                        if(_accelometerData.Count() > 0 && _magentometerData.Count() > 0 && _gyroscopeData.Count() > 0 && _orientationData.Count() > 0)
                        {
                            Configuration.Instance.AddLog(GetMedian(_accelometerData), GetMedian(_magentometerData), GetMedian(_gyroscopeData), GetMedian(_orientationData), _location);
                            Console.WriteLine($"{_accelometerData.Count()},{_magentometerData.Count()},{_gyroscopeData.Count()},{_orientationData.Count()}");
                            _accelometerData.Clear();
                            _magentometerData.Clear();
                            _gyroscopeData.Clear();
                            _orientationData.Clear();
                        }
                        else
                        {
                            ct.ThrowIfCancellationRequested();
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                if (!ct.IsCancellationRequested)
                                {
                                    Configuration.Instance.Log += "no data";
                                }
                            });
                        }
                        _msCounter += 50;

                        //faster time interval for prediction than tracking
                        if(_msCounter % 2000 == 0 && Configuration.Instance.IsPredicting)
                        {
                            Task.Run(async () =>
                            {
                                await Configuration.Instance.SendResetLog(RunTypeE.Predicting);
                            }).GetAwaiter();
                        }
                        if (_msCounter % 10000 == 0 && Configuration.Instance.IsTracking)
                        {
                            Task.Run(async () =>
                            {
                                await Configuration.Instance.SendResetLog(RunTypeE.Tracking);
                            }).GetAwaiter();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    // temporary
                    Configuration.Instance.Log += e;
                }
                if (Accelerometer.IsMonitoring) { Accelerometer.Stop(); }
                if (Magnetometer.IsMonitoring) { Magnetometer.Stop(); }
                if (OrientationSensor.IsMonitoring) { OrientationSensor.Stop(); }
                if (Gyroscope.IsMonitoring) { Gyroscope.Stop(); }

                //send log after cancellation
                if (Configuration.Instance.IsTracking)
                {
                    await Configuration.Instance.SendResetLog(RunTypeE.Tracking);
                }

                Device.BeginInvokeOnMainThread(() =>
                {
                    Configuration.Instance.IsTracking = false;
                });
            });
            _geoWorker = Task.Run(async () =>
            {
                bool _permissionGranted = false;
                Device.BeginInvokeOnMainThread(async () =>
                {
                    while (await Permissions.CheckStatusAsync<Permissions.LocationAlways>() != PermissionStatus.Granted)
                    {
                        await Permissions.RequestAsync<Permissions.LocationAlways>();
                    }
                    _permissionGranted = true;
                });
                // wait until permission is granted
                while (!_permissionGranted)
                {
                    Thread.Sleep(1);
                }
                while (true)
                {
                    try
                    {
                        //var _oldTime = DateTime.Now;
                        _location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best));
                        //var _diff = DateTime.Now - _oldTime;
                        ct.ThrowIfCancellationRequested();
                        if (_location == null)
                        {
                            Configuration.Instance.Log += "Location returned null";
                        }
                        Console.WriteLine(_location);
                    }
                    catch (Exception)
                    {

                    }
                }
            });
        }
        public void Stop()
        {
            _src.Cancel();
            // wait for cancellation to complete
            Task.Run(() =>
            {
                while (AreWorkersRunning())
                {
                    Thread.Sleep(10);
                }
            }).Wait();
        }

        void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            Console.WriteLine($"Accelerometer: X: {data.Acceleration.X}, Y: {data.Acceleration.Y}, Z: {data.Acceleration.Z}");
            _accelometerData.Add(data.Acceleration);
            // Process Acceleration X, Y, and Z
        }
        //DateTime _last = DateTime.Now;
        void Magnetometer_ReadingChanged(object sender, MagnetometerChangedEventArgs e)
        {
            //Configuration.Instance.Log += (DateTime.Now - _last).ToString();
            var data = e.Reading;
            // Process MagneticField X, Y, and Z
            Console.WriteLine($"Magnetometer: X: {data.MagneticField.X}, Y: {data.MagneticField.Y}, Z: {data.MagneticField.Z}");
            _magentometerData.Add(data.MagneticField);
            //_last = DateTime.Now;
        }
        void OrientationSensor_ReadingChanged(object sender, OrientationSensorChangedEventArgs e)
        {
            var data = e.Reading;
            Console.WriteLine($"Accelerometer: X: {data.Orientation.X}, Y: {data.Orientation.Y}, Z: {data.Orientation.Z}");
            _orientationData.Add(data.Orientation);
            // Process Acceleration X, Y, and Z
        }
        void Gyroscope_ReadingChanged(object sender, GyroscopeChangedEventArgs e)
        {
            var data = e.Reading;
            Console.WriteLine($"Accelerometer: X: {data.AngularVelocity.X}, Y: {data.AngularVelocity.Y}, Z: {data.AngularVelocity.Z}");
            _gyroscopeData.Add(data.AngularVelocity);
            // Process Acceleration X, Y, and Z
        }

        static Quaternion GetMedian(IEnumerable<Quaternion> source)
        {
            Quaternion _result;
            _result.X = (float)GetMedian(source.Select(v => (double)v.X));
            _result.Y = (float)GetMedian(source.Select(v => (double)v.Y));
            _result.Z = (float)GetMedian(source.Select(v => (double)v.Z));
            _result.W = (float)GetMedian(source.Select(v => (double)v.W));
            return _result;
        }

        static Vector3 GetMedian(IEnumerable<Vector3> source)
        {
            Vector3 _result;
            _result.X = (float)GetMedian(source.Select(v => (double)v.X));
            _result.Y = (float)GetMedian(source.Select(v => (double)v.Y));
            _result.Z = (float)GetMedian(source.Select(v => (double)v.Z));
            return _result;
        }
        static double GetMedian(IEnumerable<double> source)
        {
            // Create a copy of the input, and sort the copy
            double[] temp = source.ToArray();
            Array.Sort(temp);

            int count = temp.Length;
            if (count == 0)
            {
                temp = new double[] { 0 };
                count = 1;
                //throw new InvalidOperationException("Empty collection");
            }
            if (count % 2 == 0)
            {
                // count is even, average two middle elements
                double a = temp[count / 2 - 1];
                double b = temp[count / 2];
                return (a + b) / 2;
            }
            else
            {
                // count is odd, return the middle element
                return temp[count / 2];
            }
        }
    }
}
