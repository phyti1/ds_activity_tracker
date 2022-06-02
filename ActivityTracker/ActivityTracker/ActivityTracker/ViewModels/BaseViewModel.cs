using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ActivityTracker.Models;

namespace ActivityTracker.Models
{
    class BaseViewModel : Models.BaseClass
    {
        public BaseViewModel() : base(false) { }
        public bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }
        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        //can be overwritten, does not have to
        protected virtual void ResetScrolling() { }
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
