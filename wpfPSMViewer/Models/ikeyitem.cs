using PSMonitor.Stores;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace PSMViewer.Models
{
    public interface IKeyItem : IReload, INotifyPropertyChanged
    {
        /// <summary>
        /// 
        /// </summary>
        KeyValueConversion Conversion { get; }

        /// <summary>
        /// 
        /// </summary>
        KeyItemTitle Title { get; }

        /// <summary>
        /// 
        /// </summary>
        string Path { get; }

        /// <summary>
        /// 
        /// </summary>
        string StaticPath { get; }

        /// <summary>
        /// 
        /// </summary>
        Color Color { get; set; }

        /// <summary>
        /// 
        /// </summary>
        SolidColorBrush Brush { get; }
        
        /// <summary>
        /// 
        /// </summary>
        KeyItemW W { get; set; }

        /// <summary>
        /// 
        /// </summary>
        ObservableCollection<IKeyItem> Children { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        void CopyTo(IKeyItem other);
    }
}
