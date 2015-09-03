/// <copyright file="extensions.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Extensions</summary>
/// 
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PSMViewer
{

    public static class Extensions
    {

        /// <summary>
        /// Reloads objects that implements the IReload interface and displays a messagebox if any error occurs.
        /// </summary>
        /// <param name="obj">The object that implements the <see cref="IReload"/> interface</param>
        /// 
        public static void OnReload(this Control control, IReload obj)
        {
            OnReload(control, obj, (error) =>
            {
                MessageBox.Show(error.GetBaseException().Message, error.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            });

        }

        /// <summary>
        /// Reloads objects that implements the IReload interface and forward any exception to the <paramref name="ErrorHandler"/>
        /// </summary>
        /// <param name="obj">The object that implements the <see cref="IReload"/> interface </param>
        /// <param name="ErrorHandler">The delegate that will handle any exceptions that occur.</param>
        public static void OnReload(this Control control, IReload obj, Action<Exception> ErrorHandler)
        {

            obj.Dispatcher.Invoke(delegate
            {
                obj.Status = ReloadStatus.Loading;
            });

            obj.Dispatcher.InvokeAsync(obj.Reload, DispatcherPriority.Background, obj.Cancel.Token).Task.ContinueWith(task =>
            {

                switch (task.Status)
                {

                    case TaskStatus.Faulted:

                        obj.Dispatcher.InvokeAsync(delegate
                        {

                            obj.Status = ReloadStatus.Error;
                            ErrorHandler(task.Exception);

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

            });

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
    }
}
