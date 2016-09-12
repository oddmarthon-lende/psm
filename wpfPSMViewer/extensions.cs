/// <copyright file="extensions.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Extensions</summary>
/// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;

namespace PSMViewer
{

     

    /// <summary>
    /// Adds extensions to some object types.
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        /// Manually update all binding targets
        /// </summary>
        /// <param name="obj">The object to start recursive update from</param>
        /// <param name="properties">Dependency properties to update</param>
        public static void UpdateBindingTargets(this DependencyObject obj, params DependencyProperty[] properties)
        {
            foreach (DependencyProperty depProperty in properties)
            {
                //check whether the submitted object provides a bound property
                //that matches the property parameters
                BindingExpression be =
                  BindingOperations.GetBindingExpression(obj, depProperty);
                if (be != null) be.UpdateTarget();
            }

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                //process child items recursively
                DependencyObject childObject = VisualTreeHelper.GetChild(obj, i);
                UpdateBindingTargets(childObject, properties);
            }
        }

        /// <summary>
        /// Manually update all binding sources
        /// </summary>
        /// <param name="obj">The object to start recursive update from</param>
        /// <param name="properties">Dependency properties to update</param>
        public static void UpdateBindingSources(this DependencyObject obj, params DependencyProperty[] properties)
        {
            foreach (DependencyProperty depProperty in properties)
            {
                //check whether the submitted object provides a bound property
                //that matches the property parameters
                BindingExpression be =
                  BindingOperations.GetBindingExpression(obj, depProperty);
                if (be != null) be.UpdateSource();
            }

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                //process child items recursively
                DependencyObject childObject = VisualTreeHelper.GetChild(obj, i);
                UpdateBindingSources(childObject, properties);
            }
        }

        /// <summary>
        /// Contains the last error that occured
        /// </summary>
        public static Exception LastError { get; private set; }

        private static Dictionary<IReload, KeyValuePair<IReload, PropertyChangedEventHandler>> _forwarded = new Dictionary<IReload, KeyValuePair<IReload, PropertyChangedEventHandler>>();

        /// <summary>
        /// Forwards the <see cref="IReload.Status"/>
        /// </summary>
        /// <param name="source">The source object that contains the status to forward.</param>
        /// <param name="destination">The destination object that will get its <see cref="IReload.Status"/> synced with <paramref name="source"/></param>
        /// <returns>The <paramref name="destination"/></returns>
        public static IReload Forward(this IReload source, IReload destination)
        {
            KeyValuePair<IReload, PropertyChangedEventHandler> forwarded;
            PropertyChangedEventHandler handler = (sender, e) => { if (e.PropertyName == "Status") destination.Status = source.Status; };
            INotifyPropertyChanged src = (INotifyPropertyChanged)source;

            if (_forwarded.TryGetValue(source, out forwarded))
            {               

                src.PropertyChanged -= forwarded.Value;
                _forwarded.Remove(source);

            }

            forwarded = new KeyValuePair<IReload, PropertyChangedEventHandler>(destination, handler);

            _forwarded.Add(source, forwarded);

            src.PropertyChanged += forwarded.Value;

            return destination;
        }

        /// <summary>
        /// Serializes objects to a stream
        /// </summary>
        /// <param name="stream">The stream that the XAML is written to</param>
        public static void Export(this object obj, Stream stream)
        {

            XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Indent = true,
                ConformanceLevel = ConformanceLevel.Auto,
                OmitXmlDeclaration = true

            });

            XamlDesignerSerializationManager mgr = new XamlDesignerSerializationManager(writer)
            {
                XamlWriterMode = XamlWriterMode.Expression
            };

            XamlWriter.Save(obj, mgr);

        }

        /// <summary>
        /// Reloads objects that implements the IReload interface and displays a messagebox if any error occurs.
        /// </summary>
        /// <param name="obj">The object that implements the <see cref="IReload"/> interface</param>
        /// 
        public static void OnReload(this object control, IReload obj)
        {
            OnReload(control, obj, (error) =>
            {
                MessageBox.Show(error.GetBaseException().Message, error.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            });

        }

        /// <summary>
        /// Show a dialog box with the exception message
        /// </summary>
        public static MessageBoxResult Show(this Exception e, MessageBoxButton buttons = MessageBoxButton.OK)
        {
            return MessageBox.Show(e.GetBaseException().Message, e.Message, buttons, MessageBoxImage.Error);
        }

        /// <summary>
        /// Create a new instance of this type
        /// </summary>
        /// <param name="args"></param>
        /// <returns>The newly created instance</returns>
        public static object New(this Type t, params object[] args)
        {
            return Activator.CreateInstance(t, args);
        }

        /// <summary>
        /// Reloads objects that implements the IReload interface and forward any exception to the <paramref name="ErrorHandler"/>
        /// </summary>
        /// <param name="obj">The object that implements the <see cref="IReload"/> interface </param>
        /// <param name="ErrorHandler">The delegate that will handle any exceptions that occur.</param>
        public static void OnReload(this object control, IReload obj, Action<Exception> ErrorHandler)
        {

            obj.CancellationTokenSource.Cancel();
            obj.CancellationTokenSource = new System.Threading.CancellationTokenSource();

            obj.Dispatcher.InvokeAsync(delegate
            {
                obj.Status = ReloadStatus.Loading;
            });

            obj.Dispatcher.InvokeAsync(obj.Reload, DispatcherPriority.Background, obj.CancellationTokenSource.Token).Task.ContinueWith(task =>
            {

                switch (task.Status)
                {

                    case TaskStatus.Faulted:

                        obj.Dispatcher.InvokeAsync(delegate
                        {

                            obj.Status = ReloadStatus.Error;
                            LastError = task.Exception;

                            if(ErrorHandler != null)
                                ErrorHandler(LastError);

                        });

                        break;

                }

                obj.Dispatcher.Invoke(delegate
                {
                    obj.Status = ReloadStatus.Idle;
                });


            });

        }

        /// <summary>
        /// Set the property value using the objects dispatcher. Useful if the current thread is not the owner of the object, because some properties will be inaccessible.
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="name">The property name</param>
        /// <returns>The value</returns>
        public static void SetValue(this DispatcherObject obj, string name, object value)
        {

            obj.Dispatcher.Invoke(delegate
            {

                foreach (PropertyDescriptor p in TypeDescriptor.GetProperties(obj, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.Valid) }))
                {

                    if (p.Name == name)
                        p.SetValue(obj, value);

                }

            });

        }

        /// <summary>
        /// Get the property value using the objects dispatcher. Useful if the current thread is not the owner of the object, because some properties will be inaccessible.
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="name">The property name</param>
        /// <returns>The value</returns>
        public static object GetValue(this DispatcherObject obj, string name)
        {

            return obj.Dispatcher.Invoke<object>(delegate
            {

                foreach (PropertyDescriptor p in TypeDescriptor.GetProperties(obj, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.Valid) }))
                {

                    if (p.Name == name)
                        return p.GetValue(obj);

                }

                return null;

            }, DispatcherPriority.Normal);

        }

        /// <summary>
        /// Creates a thumbnail image of the elements contents
        /// </summary>
        /// <param name="v"></param>
        /// <param name="thumbWidth"></param>
        /// <param name="thumbHeight"></param>
        /// <returns>A thumbnail image bitmap source of the <see cref="ContentControl"/></returns>
        public static Stream GetThumbnailImageStream(this ContentControl v, int thumbWidth, int thumbHeight)
        {

            int width = (int)((FrameworkElement)v.Content).ActualWidth;
            int height = (int)((FrameworkElement)v.Content).ActualHeight;

            System.Windows.Media.PixelFormat pf = System.Windows.Media.PixelFormats.Pbgra32;
            int stride = (pf.BitsPerPixel / 8);
            Bitmap img = new Bitmap(width, height);

            if (!v.IsVisible || width == 0 || height == 0)
                return null;

            RenderTargetBitmap bmp = new RenderTargetBitmap(img.Width, img.Height, img.HorizontalResolution, img.VerticalResolution, pf);

            bmp.Render(v);

            Graphics g = Graphics.FromImage(img);

            g.FillRectangle(System.Drawing.Brushes.Black, new Rectangle(new System.Drawing.Point(0, 0), new System.Drawing.Size(img.Width, img.Height)));

            using (MemoryStream memorystream = new MemoryStream())
            {

                BitmapEncoder encoder = new BmpBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(memorystream);

                g.DrawImage(new Bitmap(memorystream), 0F, 0F);

            }

            g.Flush(System.Drawing.Drawing2D.FlushIntention.Flush);

            MemoryStream stream = new MemoryStream();

            img.GetThumbnailImage(thumbWidth, thumbHeight, null, IntPtr.Zero).Save(stream, ImageFormat.Bmp);
                       
            return stream;
                        
        }

        /// <summary>
        /// Gets a <see cref="BitmapSource"/> that can be used to create a thumbnail.
        /// </summary>
        /// <param name="width">The width of the generated thumbnail. Proportions will be kept.</param>
        /// <returns>The bitmap source</returns>
        public static BitmapSource GetThumbnail(this ContentControl control, int width = 320)
        {
            double ratio = control.ActualWidth / control.ActualHeight;
            Stream stream = control.GetThumbnailImageStream(width, (int)(width / ratio));
            return stream == null ? null : BitmapFrame.Create(stream);
        }

        /// <summary>
        /// Find all objects of type <typeparamref name="T"/> in the visual tree
        /// </summary>
        /// <typeparam name="T">The type of object to find</typeparam>
        /// <param name="obj">Object from where to start the search</param>
        /// <returns>Array with the results</returns>
        public static T[] Find<T>(this DependencyObject p, DependencyObject obj = null)
        {

            List<T> result = new List<T>();
            int count = VisualTreeHelper.GetChildrenCount(obj ?? p);

            for (int i = 0; i < count; i++)
            {

                DependencyObject d = VisualTreeHelper.GetChild(obj ?? p, i);

                if (d is T)
                {
                    result.Add((T)(object)d);
                }

                result.AddRange(Find<T>(p, d));
            }

            return result.ToArray();

        }
        
    }
}
