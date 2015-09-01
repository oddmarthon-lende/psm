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
            OnReload(control, obj, (error) => {
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
        public static BitmapSource GetThumbnailImage(this ContentControl v, int thumbWidth, int thumbHeight)
        {

            int width = (int)((FrameworkElement)v.Content).ActualWidth;
            int height = (int)((FrameworkElement)v.Content).ActualHeight;

            if (!v.IsVisible || width == 0 || height == 0)
                return null;

            System.Windows.Media.PixelFormat pf = System.Windows.Media.PixelFormats.Pbgra32;
            int stride = (pf.BitsPerPixel / 8);
            Bitmap img = new Bitmap(width, height);
            RenderTargetBitmap bmp = new RenderTargetBitmap(img.Width, img.Height, img.HorizontalResolution, img.VerticalResolution, pf);

            bmp.Render(v);
                        
            Graphics g = Graphics.FromImage(img);

            g.FillRectangle(System.Drawing.Brushes.Black, new Rectangle(new System.Drawing.Point(0, 0), new System.Drawing.Size(img.Width, img.Height)));
            
            using (MemoryStream stream = new MemoryStream())
            {

                BitmapEncoder encoder = new BmpBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(stream);
                
               g.DrawImage(new Bitmap(stream), 0F, 0F);
                
            }
            
            g.Flush(System.Drawing.Drawing2D.FlushIntention.Flush);
            
            using (MemoryStream stream = new MemoryStream())
            {
                
                img.GetThumbnailImage(thumbWidth, thumbHeight, null, IntPtr.Zero).Save(stream, ImageFormat.Bmp);

                pf = PixelFormats.Bgr32;
                stride = pf.BitsPerPixel / 8;

                int size = thumbWidth * thumbHeight * stride;

                byte[] dst = new byte[size];
                byte[] src = stream.ToArray();
                
                int w = thumbWidth * stride;
                int j = 0;
                for (int i = size - w - 1; i >= 54; i -= w)
                {
                    Array.ConstrainedCopy(src, i, dst, j, w);
                    j += w;
                }

                return BitmapSource.Create(thumbWidth, thumbHeight, img.HorizontalResolution, img.VerticalResolution, pf, null, dst, w);
            }
        }
    }
}
