using PSMViewer.Models;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PSMViewer.Converters
{
    /// <summary>
    /// Convert icons for the tree 
    /// </summary>
    public class TreeIconConverter : IValueConverter
    {
        private ImageSourceConverter _imageSourceConverter = new ImageSourceConverter();

        private Assembly _assembly = Assembly.GetExecutingAssembly();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is KeyItem)
            {
                string fn = "link";
                KeyItem key = (KeyItem)value;

                if (key.Status == ReloadStatus.Error)
                    fn = key.Type == null ? "folder_error" : "link_error";
                else if (key.Type == null)
                    fn = key.Children.Count == 0 ? "folder" : "folder_key";

                using (Stream str = _assembly.GetManifestResourceStream(String.Format("PSMViewer.Icons.{0}.png", fn)))
                {
                    return BitmapFrame.Create(str);
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
