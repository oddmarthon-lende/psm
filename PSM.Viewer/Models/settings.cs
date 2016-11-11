using PSM.Stores;
using PSM.Viewer.Editors;
using PSM.Viewer.Models;
using PSM.Viewer.Visualizations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PSM.Viewer.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class StoreOption
    {
        /// <summary>
        /// 
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public StoreOption(string key, object value)
        {

            Key = key;
            Value = value;

        }

        /// <summary>
        /// Constructor
        /// </summary>
        public StoreOption() { }
    }

    public class StoreOptionsList : List<StoreOption> { }

    /// <summary>
    /// Mainwindow options
    /// </summary>
    [Serializable]
    public class Settings : INotifyPropertyChanged
    {
        /// <summary>
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private Window _window;
        /// <summary>
        /// References the main window.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Window Window {

            get { return _window; }
            set {

                _window = value;

                if (_startindex != null)
                    StartIndex = _startindex;

                if (_endindex != null)
                    EndIndex = _endindex;
            }
        }
                
        private string _indexfield;

        [Category("Controls")]
        [Editor(typeof(StoreEnumEditor), typeof(StoreEnumEditor))]
        [PropertyOrder(-1)]
        [Description("Gets/Sets the data index field")]
        public string IndexField
        {

            get {
                
                try {                
                    
                    Enum.Parse(_index, _indexfield);
                }
                catch(Exception)
                {
                    _indexfield = PSM.Store.Get(Dispatcher.CurrentDispatcher).Default.ToString();
                }
                                
                return _indexfield;

            }

            set {

                try {

                    Enum.Parse(_index, value);                                        
                }
                catch(Exception) {
                    value = PSM.Store.Get(Dispatcher.CurrentDispatcher).Default.ToString();
                }
                finally
                {
                    _indexfield = value;
                }

                OnPropertyChanged();

            }
        }      

        private IComparable _startindex;
        [Category("Controls")]
        [PropertyOrder(0)]
        [Description("Gets/Sets the start index")]
        public object StartIndex
        {
            get
            {
                try
                {
                    return ((MultiControl)Window.DataContext).Get((Enum)Enum.Parse(_index, IndexField)).Start;
                }
                catch (Exception)
                {
                    return null;
                }
                
            }

            set
            {
                try
                {

                    ((MultiControl)Window.DataContext).Get((Enum)Enum.Parse(_index, IndexField)).Start = (IComparable)value;
                    OnPropertyChanged();
                }
                catch (Exception)
                {
                    _startindex = (IComparable)value;
                }
            }
        }

        private IComparable _endindex;

        [Category("Controls")]
        [PropertyOrder(1)]
        [Description("Gets/Sets the end index")]
        public object EndIndex
        {
            get
            {
                try
                {
                    return ((MultiControl)Window.DataContext).Get((Enum)Enum.Parse(_index, IndexField)).End;
                }
                catch (Exception)
                {
                    return null;
                }
                
            }

            set
            {
                try {
                    ((MultiControl)Window.DataContext).Get((Enum)Enum.Parse(_index, IndexField)).End = (IComparable)value;
                    OnPropertyChanged();
                }
                catch(Exception) {
                    _endindex = (IComparable)value;
                }

                
            }
        }

        [Browsable(false)]
        public StoreOptionsList StoreOptions
        {

            get
            {

                StoreOptionsList list = new StoreOptionsList();
                IOptions options = Store;
                Stores.Properties props = options.Get();
                
                foreach(var p in props) {

                    list.Add(new StoreOption(p.Key.DisplayName, options.Get<object>(p.Key.DisplayName)));

                }

                return list;

            }

            set
            {

                foreach(StoreOption opt in value)
                {

                    if (opt.Key == "Store") {
                        PSM.Store.Get(Dispatcher.CurrentDispatcher, (Type)opt.Value);
                        break;
                    }
                    

                }

                IOptions options = Store;

                foreach (StoreOption opt in value)
                {

                    options.GetType().GetProperty(opt.Key).SetValue(options, opt.Value);

                }

                OnPropertyChanged("Store");

            }
        }

        [Category("Store")]
        [System.ComponentModel.DisplayName("Options")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IOptions Store
        {
            get
            {
                return PSM.Store.Get(Dispatcher.CurrentDispatcher).Options;
            }
            
        }

        /// <summary>
        /// Gets the store index type
        /// </summary>
        private Type _index
        {
            get
            {
                return PSM.Store.Get(Dispatcher.CurrentDispatcher).Index;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Settings()
        {
            IndexField = PSM.Store.Get(Dispatcher.CurrentDispatcher).Default.ToString();
        }

        /// <summary>
        /// Changes the editor for properties that has value range data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void settings_propertyGrid_SelectedObjectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

            PropertyGrid grid = (PropertyGrid)sender;
            Dictionary<PropertyDescriptor, PropertyItem> items = new Dictionary<PropertyDescriptor, PropertyItem>();
            IOptions settings = Store;

            PropertyGrid[] grids = grid.Parent.Find<PropertyGrid>(grid.Parent);

            foreach (PropertyItem item in grid.Properties)
            {
                items.Add(item.PropertyDescriptor, item);
            }

            foreach (var p in settings.Get())
            {

                PropertyDescriptor descriptor = p.Key;

                PropertyItem item = items[descriptor];

                descriptor.RemoveValueChanged(grid, settings_propertyGrid_PropertyDescriptorValueChanged);

                if (descriptor.Name == "Store")
                {
                    descriptor.AddValueChanged(settings, (EventHandler)delegate {


                        Type t = settings.Get<Type>("Store");
                        global::PSM.Stores.IStore store = PSM.Store.Get(System.Windows.Threading.Dispatcher.CurrentDispatcher, t);

                        store.Options.GetType().GetProperty("Store").SetValue((object)store.Options, t);

                        grid.SelectedObject = store.Options;

                        if(Window is MainWindow)
                        {
                            grids[0].SelectedObject = null;
                            grids[0].SelectedObject = this;
                        }


                    });
                }
                else
                    descriptor.AddValueChanged(grid, settings_propertyGrid_PropertyDescriptorValueChanged);

                if (p.Value.Count > 0)
                {
                    StoreOptionEditor editor = new StoreOptionEditor(descriptor);
                    item.Editor = editor.ResolveEditor(item);
                }


            }

            grid.Update();
        }

        /// <summary>
        /// Refreshes the  editors.
        /// </summary>
        public void settings_propertyGrid_PropertyDescriptorValueChanged(object sender, EventArgs e)
        {
            PropertyGrid grid = (PropertyGrid)sender;
            PropertyGrid[] grids = grid.Parent.Find<PropertyGrid>(grid.Parent);

            settings_propertyGrid_SelectedObjectChanged(grids[1], null);
        }

        public void settings_propertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {

            PropertyItem item = (PropertyItem)e.OriginalSource;
            PropertyGrid grid = (PropertyGrid)sender;

            if (item.DisplayName == "IndexField")
            {
                foreach (PropertyItem p in grid.Properties)
                {
                    if (p.DisplayName == "StartIndex" || p.DisplayName == "EndIndex")
                    {
                        if (p.Value is DateTime)
                        {
                            p.Editor = new Xceed.Wpf.Toolkit.PropertyGrid.Editors.DateTimeUpDownEditor().ResolveEditor(p);
                        }
                        else
                        {
                            p.Editor = new Xceed.Wpf.Toolkit.PropertyGrid.Editors.LongUpDownEditor().ResolveEditor(p);
                        }
                    }
                }

                ((PropertyGrid)sender).Update();

            }
        }

    }
}
