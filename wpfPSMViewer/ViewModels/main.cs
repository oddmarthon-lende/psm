using System;
using System.Collections.Generic;
using PSMonitor;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Globalization;
using System.Collections;
using System.Linq;
using PSMViewer.Models;

namespace PSMViewer.ViewModels
{
    
    public class Main : Controls, INotifyPropertyChanged
    {
        
        private ObservableCollection<EntryItem> _entries = new ObservableCollection<EntryItem>();        
        public ObservableCollection<EntryItem> Entries
        {
            get { return _entries; }
        }
        
        public object this[string key]
        {
            get { return Properties.Settings.Default[key]; }
            set { Properties.Settings.Default[key] = value; }
        }

        public Main()
        {

            Timebased = new Controls<DateTime, TimeSpan>(Entries, DateTime.Now, new TimeSpan(1, 0, 0));
            Indexbased = new Controls<long, long>(Entries, 0L, 100L);

            this.PropertyChanged += Changed;

            Timebased.PropertyChanged += Changed;
            Indexbased.PropertyChanged += Changed;

            Timebased.ActivationRequested += Activate;
            Indexbased.ActivationRequested += Activate;

            Control = Indexbased;

        }

        private void Activate(Controls sender)
        {
            Control = sender;
        }

        private void Changed(object sender, PropertyChangedEventArgs e)
        {
            
            if (e.PropertyName == "Selected")
            {
                if(sender == this)
                    Timebased.Selected = Indexbased.Selected = this.Selected;
            }
            else if (typeof(Controls).IsInstanceOfType(sender))
            {
                ((Controls)sender).Activate();
            }
        }

        private Controls Control = null;

        public Controls<DateTime, TimeSpan> Timebased { get; private set; }
        public Controls<long, long> Indexbased { get; private set; }

        public override IEnumerable<EntryItem> Reload(KeyItem key)
        {
            return Control.Reload(key);
        }

        public override void Reload()
        {
            if (Control != null)
                Control.Reload();
        }

        public override bool Next()
        {

            if (Control != null)
                return Control.Next();

            return false;
        }

        public override bool Previous()
        {
            if (Control != null)
                return Control.Previous();

            return false;
        }
    }
    
    public interface IReload
    {
        void Reload();
        bool Next();
        bool Previous();
    }

}
