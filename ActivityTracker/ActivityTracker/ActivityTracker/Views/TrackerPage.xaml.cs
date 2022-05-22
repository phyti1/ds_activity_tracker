using ActivityTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ActivityTracker.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TrackerPage : ContentPage
    {
        public TrackerPage()
        {
            InitializeComponent();
            BindingContext = new TabbedPageViewModel();
        }
    }
}