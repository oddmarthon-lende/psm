/// <copyright file="tree_icon_converter.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>

using PSMViewer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PSMViewer.Converters
{
    /// <summary>
    /// Convert icons for the tree 
    /// </summary>
    public class TreeIconConverter : IMultiValueConverter
    {
        private ImageSourceConverter _imageSourceConverter = new ImageSourceConverter();

        private Assembly _assembly = Assembly.GetExecutingAssembly();

        private class ImageRefresher
        {
            
            public ImageRefresher(TreeViewItem item, Image img)
            {

            }

            void Refresh(object sender, RoutedEventArgs e)
            {

            }

        }

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {

            if(value[0] is TreeViewItem)
            {

                string path = "PSMViewer.Icons.{0}.png";
                string fn = "bullet_key";

                TreeViewItem item = (TreeViewItem)value[0];
                Image img = (Image)value[1];
                KeyItem key = (KeyItem)item.DataContext;

                if (key.Type == null)
                {

                    fn = !item.IsExpanded ? "folder" : "folder_key";

                    RoutedEventHandler handler = new RoutedEventHandler((sender, e) => {

                        fn = !item.IsExpanded ? "folder" : "folder_key";

                        using (Stream str = _assembly.GetManifestResourceStream(String.Format(path, fn)))
                        {
                            img.Source = BitmapFrame.Create(str);
                        }

                    });

                    item.AddHandler(TreeViewItem.ExpandedEvent, handler);
                    item.AddHandler(TreeViewItem.CollapsedEvent, handler);
                    
                }

                using (Stream str = _assembly.GetManifestResourceStream(String.Format(path, fn)))
                {
                    return BitmapFrame.Create(str);
                }
            }
            return null;
        }
        
        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
