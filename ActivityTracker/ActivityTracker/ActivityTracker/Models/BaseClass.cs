using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace ActivityTracker.Models
{
    public class BaseClass : INotifyPropertyChanged
    {
        bool _serializeOnPropertyChanged;
        public BaseClass(bool serializeOnPropertyChanged)
        {
            _serializeOnPropertyChanged = serializeOnPropertyChanged;
        }
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
