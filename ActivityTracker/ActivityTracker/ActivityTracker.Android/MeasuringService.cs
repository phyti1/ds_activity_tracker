using ActivityTracker.Droid.Helpers;
using Android.App;
using Android.Content;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ActivityTracker.Droid
{
    [Service(Label = "MeasuringService")]
    public class MeasuringService : Service
    {
        CancellationTokenSource _cts;
        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            _cts = new CancellationTokenSource();

            Notification notif = DependencyService.Get<INotification>().ReturnNotif();
            StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notif);

            Models.Configuration.Instance.DataAquisition.Start();
            //Task.Run(() => 
            //{
            //    while (true)
            //    {
            //        _cts.Token.ThrowIfCancellationRequested();
            //        Xamarin.Essentials.Vibration.Vibrate(100);
            //        Thread.Sleep(1000);
            //    }

            //}, _cts.Token);

            return StartCommandResult.Sticky;
        }
        public override void OnDestroy()
        {
            Models.Configuration.Instance.DataAquisition.Stop();
            //if (_cts != null)
            //{
            //    _cts.Token.ThrowIfCancellationRequested();
            //    _cts.Cancel();
            //}
            base.OnDestroy();
        }
    }
}
