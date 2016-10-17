using System;
using System.Collections.Generic;
using System.Linq;
using PSMViewer.Models;
using PSMViewer.ViewModels;
using System.Windows.Data;
using System.Collections.Specialized;
using PSMonitor;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows;

namespace PSMViewer.Visualizations
{

    public class ColorList : List<SolidColorBrush> {}

    [Visible(true)]
    [DisplayName("Dots")]
    [Icon("")]
    public sealed partial class Dots : VisualizationControl
    {

        public double Scale { get; set; } = 0.8;

        public ColorList Colors = new ColorList();

        public double Max { get; set; } = 1.0;

        public double Min { get; set; } = -1.0;

        public Dots()
        {
            
            InitializeComponent();

            Colors.Add(Brushes.Red);
            Colors.Add(Brushes.Orange);
            Colors.Add(Brushes.Green);
        }

        class ColorInterpolator
        {
            delegate byte ComponentSelector(Color color);

            static ComponentSelector _redSelector = color => color.R;
            static ComponentSelector _greenSelector = color => color.G;
            static ComponentSelector _blueSelector = color => color.B;

            public static Color InterpolateBetween(
                Color endPoint1,
                Color endPoint2,
                double lambda)
            {
                if (lambda < 0 || lambda > 1)
                {
                    throw new ArgumentOutOfRangeException("lambda");
                }

                Color color = Color.FromRgb(
                    InterpolateComponent(endPoint1, endPoint2, lambda, _redSelector),
                    InterpolateComponent(endPoint1, endPoint2, lambda, _greenSelector),
                    InterpolateComponent(endPoint1, endPoint2, lambda, _blueSelector)
                );

                return color;
            }

            static byte InterpolateComponent(
                Color endPoint1,
                Color endPoint2,
                double lambda,
                ComponentSelector selector)
            {
                return (byte)(selector(endPoint1)
                    + (selector(endPoint2) - selector(endPoint1)) * lambda);
            }
        }

        public override MultiControl Add(KeyItem key, ObservableCollection<EntryItem> collection = null)
        {

            MultiControl c = base.Add(key, collection);

            if (c != null)
            {
                
                Ellipse ellipse = new Ellipse() {};
                Canvas canvas = new Canvas() { DataContext = key };
                TextBlock title = new TextBlock() {};

                canvas.Children.Add(ellipse);
                
                c.DataChanged += delegate
                {
                    if (c.Entries.Count > 0)
                    {

                        double value = c.Entries.Average((e) => { return key.Convert<double>((Entry)e); });

                        value = Math.Max(0.0, Math.Min(1.0, (value - Min) / (Max - Min)));

                        ellipse.Fill = new SolidColorBrush(
                            ColorInterpolator.InterpolateBetween(
                                Colors[(int)Math.Max(0, Math.Min(Colors.Count - 1, Math.Floor(value * Colors.Count)))].Color, 
                                Colors[(int)Math.Max(0, Math.Min(Colors.Count - 1, Math.Ceiling(value * Colors.Count)))].Color, value ));
                    }
                        
                };
                
                canvas.Children.Add(title);

                grid.Children.Add(canvas);
            }
                

            return c;
        }

        public override void Remove(KeyItem key)
        {
            UIElement[] elements = new UIElement[grid.Children.Count];
            grid.Children.CopyTo(elements, 0);

            foreach(var canvas in elements)
            {
                if (((KeyItem)((Canvas)canvas).DataContext) == key)
                    grid.Children.Remove((UIElement)canvas);
            }

            base.Remove(key);
        }

        public override void Refresh()
        {
            base.Refresh();

            if (grid.Children.Count > 0)
            {

                for (int i = 0; i < grid.Children.Count; i++)
                { 

                    Canvas canvas = (Canvas)grid.Children[i];
                    Ellipse ellipse = ((Ellipse)canvas.Children[0]);
                    TextBlock title = ((TextBlock)canvas.Children[1]);

                    double d = Math.Min(canvas.ActualHeight, canvas.ActualWidth) * Scale;

                    ellipse.Width = ellipse.Height = d;

                    ellipse.SetValue(Canvas.TopProperty, (canvas.ActualHeight - d) / 2.0);
                    ellipse.SetValue(Canvas.LeftProperty, (canvas.ActualWidth - d) / 2.0);

                    title.SetValue(Canvas.TopProperty, canvas.ActualHeight - title.ActualHeight);
                    title.SetValue(Canvas.LeftProperty, (canvas.ActualWidth / 2) - (title.ActualWidth / 2));
                    title.Text = ((KeyItem)canvas.DataContext).Title.ToString();

                }

            }
                        
        }


    }
}
