using ActivityTracker.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ActivityTracker
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var _navigationPage = new NavigationPage(new RootTabbedPage());
            MainPage = _navigationPage;
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
