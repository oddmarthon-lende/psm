using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PSM.Viewer.Models
{
    /// <summary>
    /// A multicontrol can handle the different <typeparamref name="ControlType"/> in one object.
    /// Each key gets a <typeparamref name="MultiControl"/> when added to the <typeparamref name="VisualizationControl"/>
    /// </summary>
    public class MultiControl : IDisposable, IReload, INotifyPropertyChanged
    {

        private KeyItem _key = null;

        public KeyItem Key {

            get
            {
                return _key;
            }

            set
            {

                _key = value;

                foreach (var pair in Controls)
                {
                    pair.Value.Selected = _key;
                }

            }
        }

        public ObservableCollection<EntryItem> Entries { get; set; }

        public Dispatcher Dispatcher { get; protected set; } = Dispatcher.CurrentDispatcher;

        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        private ReloadStatus _status = ReloadStatus.Idle;
        public ReloadStatus Status {

            get { return _status; }
            set { SetField(ref _status, value); }

        }

        public event DataChangedEventHandler DataChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        private Dictionary<Enum, Controls> Controls = new Dictionary<Enum, Models.Controls>();

        private Enum _indexIdentifier;

        /// <summary>
        /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<TField>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);

            return true;
        }
        
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="key">The <typeparamref name="KeyItem"/> this <typeparamref name="MultiControl"/> is for</param>
        /// <param name="Entries">An alternate collection for the data. Used to share a collection between different controls.</param>
        public MultiControl(KeyItem key = null, LoadHandler load = null, ObservableCollection<EntryItem> Entries = null)
        {
                       
            this.Entries = Entries ?? new ObservableCollection<EntryItem>();

            _indexIdentifier = PSM.Store.Get(Dispatcher).Default;

            Controls.Add(PSM.Stores.DB.IndexType.Index, new Controls<long, long>(this.Entries, 1));
            Controls.Add(PSM.Stores.DB.IndexType.Id, new Controls<long, long>(this.Entries, 1));
            Controls.Add(PSM.Stores.DB.IndexType.Value, new Controls<long, long>(this.Entries, 1));
            Controls.Add(PSM.Stores.DB.IndexType.Timestamp, new Controls<DateTime, TimeSpan>(this.Entries, new TimeSpan(1, 0, 0)));
            Controls.Add(PSM.Stores.Advantage.IndexType.Time, new Controls<DateTime, TimeSpan>(this.Entries, new TimeSpan()));
            Controls.Add(PSM.Stores.Advantage.IndexType.Depth, new Controls<long, long>(this.Entries, 100));
            Controls.Add(PSM.Stores.Advantage.IndexType.Index, new Controls<long, long>(this.Entries, 1));
            Controls.Add(PSM.Stores.Dummy.IndexType.Dummy, new Controls<long, long>(this.Entries, 1));
            
            foreach (var pair in Controls)
            {
                
                if (load != null)
                    pair.Value.Load += load;

                pair.Value.DataChanged += Value_DataChanged;
                pair.Value.PropertyChanged += Value_PropertyChanged;
            }

            this.Key = key;

        }

        private void Value_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Status")
                Status = ((Models.Controls)sender).Status;
        }

        /// <summary>
        /// Called when the data has changed for a control type
        /// </summary>
        /// <param name="sender"></param>
        private void Value_DataChanged(object sender)
        {
            if (DataChanged != null)
                DataChanged(sender);
        }

        /// <summary>
        /// Gets the controls
        /// </summary>
        /// <param name="indexIdentifier"></param>
        /// <param name="Start"></param>
        /// <param name="Count"></param>
        /// <returns>The controls</returns>
        public Controls Get(Enum indexIdentifier = null, object Count = null)
        {

            indexIdentifier = indexIdentifier ?? _indexIdentifier;

            Controls c = Controls[indexIdentifier];

            if (c != null)
            {

                c.Activate(this);

                if (Count != null)
                    c.Count = Count;

                _indexIdentifier = indexIdentifier;

                if (Key != null)
                    Key.IndexIdentifier = _indexIdentifier;

            }

            return c;
        }

        /// <summary>
        /// Cleans up and releases any resource used by this object.
        /// </summary>
        public void Dispose()
        {

            foreach (var pair in Controls)
            {
                pair.Value.DataChanged -= Value_DataChanged;
                pair.Value.PropertyChanged -= Value_PropertyChanged;
            }

            foreach (var pair in Controls)
            {
                pair.Value.Dispose();
            }
        }
               

        public void Reload()
        {
            Controls ctrl = Get();

            if(ctrl != null)
                ctrl.Reload();
        }

        public bool Next()
        {
            Controls ctrl = Get();
            if(ctrl != null)
                return ctrl.Next();

            return false;
        }

        public bool Previous()
        {
            Controls ctrl = Get();
            if (ctrl != null)
                return ctrl.Previous();
            return false;
        }

        public void Reset()
        {
            foreach (var ctrl in Controls)
                ctrl.Value.Reset();
        }
    }
}
