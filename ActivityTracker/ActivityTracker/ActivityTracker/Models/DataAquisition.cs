using LiveChartsCore.Defaults;
using System;
using System.Collections.Generic;
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
        private SensorSpeed _speed = SensorSpeed.UI;
        private Vector3 _lastMagentometerData;
        private Vector3 _lastAccelometerData;

        public DataAquisition()
        {
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            Magnetometer.ReadingChanged += Magnetometer_ReadingChanged;
        }

        public void Start()
        {
            _src = new CancellationTokenSource();
            CancellationToken ct = _src.Token;
            _worker = Task.Run(async () =>
            {
                int _sendCounter = 0;
                Thread.Sleep(100);
                try
                {
                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();
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
                        //accelometer
                        if (!Accelerometer.IsMonitoring)
                        {
                            Accelerometer.Start(_speed);
                        }
                        if (!Magnetometer.IsMonitoring) { Magnetometer.Start(_speed); }
                        var _location = await Geolocation.GetLastKnownLocationAsync();
                        Console.WriteLine(_location);
                        Configuration.Instance.AddLog(_lastAccelometerData, _lastMagentometerData, _location);
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
            _lastAccelometerData = data.Acceleration;
            // Process Acceleration X, Y, and Z
        }
        void Magnetometer_ReadingChanged(object sender, MagnetometerChangedEventArgs e)
        {
            var data = e.Reading;
            // Process MagneticField X, Y, and Z
            Console.WriteLine($"Magnetometer: X: {data.MagneticField.X}, Y: {data.MagneticField.Y}, Z: {data.MagneticField.Z}");
            _lastMagentometerData = data.MagneticField;
        }
    }
}
