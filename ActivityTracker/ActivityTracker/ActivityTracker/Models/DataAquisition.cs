using LiveChartsCore.Defaults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ActivityTracker.Models
{
    class DataAquisition
    {
        private static Task _worker;
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

        public void Start()
        {
            _src = new CancellationTokenSource();
            CancellationToken ct = _src.Token;
            _worker = Task.Run(async () =>
            {
                int _sendCounter = 0;
                try
                {
                    while (await Permissions.CheckStatusAsync<Permissions.LocationAlways>() != PermissionStatus.Granted)
                    {
                        bool _success = false;
                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            await Permissions.RequestAsync<Permissions.LocationAlways>();
                            _success = true;
                        });
                        while (!_success)
                        {
                            Thread.Sleep(10);
                        }
                    }
                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();
                        Thread.Sleep(10);
                        if (!Accelerometer.IsMonitoring){ Accelerometer.Start(_speed); }
                        if (!Magnetometer.IsMonitoring) { Magnetometer.Start(_speed); }
                        if (!OrientationSensor.IsMonitoring) { OrientationSensor.Start(_speed); }
                        if (!Gyroscope.IsMonitoring) { Gyroscope.Start(_speed); }

                        var _location = await Geolocation.GetLastKnownLocationAsync();
                        if(_location == null)
                        {
                            Configuration.Instance.Log += "Location returned null\r\n";
                        }
                        Console.WriteLine(_location);
                        Configuration.Instance.AddLog(GetMedian(_accelometerData), GetMedian(_magentometerData), GetMedian(_gyroscopeData), GetMedian(_orientationData), _location);
                        _sendCounter += 1;
                        if (_sendCounter > 100)
                        {
                            Task.Run(async () =>
                            {
                                await Configuration.Instance.SendResetLog();
                            }).GetAwaiter();
                            _sendCounter = 0;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    // temporary
                    Configuration.Instance.Log += e + "\r\n";
                }
                if (Accelerometer.IsMonitoring) { Accelerometer.Stop(); }
                if (Magnetometer.IsMonitoring) { Magnetometer.Stop(); }
                if (OrientationSensor.IsMonitoring) { OrientationSensor.Stop(); }
                if (Gyroscope.IsMonitoring) { Gyroscope.Stop(); }

                Device.BeginInvokeOnMainThread(() =>
                {
                    Configuration.Instance.IsEnabled = false;
                });
            });
        }
        public void Stop()
        {
            _src.Cancel();
            // wait for cancellation to complete
            Task.Run(() =>
            {
                while (!_worker.IsCanceled && !_worker.IsCompleted)
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
            //Configuration.Instance.Log += (DateTime.Now - _last).ToString() + "\r\n";
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
