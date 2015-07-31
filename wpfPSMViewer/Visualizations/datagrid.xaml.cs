using PSMonitor;
using PSMViewer.Models;
using PSMViewer.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xceed.Wpf.DataGrid;

namespace PSMViewer.Visualizations
{

    public sealed partial class DataGrid : VisualizationControl
    {
        public static string DisplayName { get { return typeof(DataGrid).Name; } }
        public static string Icon { get; private set; } = null;

        public class RowItem : EntryItem
        {

            public string Path { get; private set; }
            public string Key { get; private set; }

            public RowItem (KeyItem key, EntryItem item) : base(item)
            {
                string p = key.Path;

                Key  = key.Name;
                Path = key.Parent == null ? p.Substring(0, p.LastIndexOf('.')) : key.Parent.Path;
            }
        }

        public IEnumerable<RowItem> Entries {

            get {
                return Enumerable.SelectMany<MultiControl, RowItem>(Controls, m => {
                    return m.Entries.Select(e => {
                        return new RowItem(m.Key, e);
                    });
                });
            }
        }

        public DataGrid()
        {
            InitializeComponent();
            
        }

        public override void Refresh()
        {
            OnPropertyChanged("Entries");
            base.Refresh();
        }
    }
}
