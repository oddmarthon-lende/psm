/// <copyright file="tree.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the KeyItem\Key tree</summary>
/// 

using PSMViewer.Models;
using PSMViewer.ViewModels;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PSMViewer
{

    public partial class Tree : TreeView, IReload
    {
        
        private CancellationTokenSource _c = new CancellationTokenSource();
        public CancellationTokenSource Cancel
        {
            get
            {
                return _c;
            }
        }

        public Tree()
        {
            InitializeComponent();

            AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(Reload));

            AddHandler(TreeViewItem.PreviewMouseMoveEvent, new RoutedEventHandler((sender, e) =>
            {

                MouseEventArgs m = (MouseEventArgs)e;

                switch(m.LeftButton)
                {

                    case MouseButtonState.Pressed:
                                                
                        DragDrop.DoDragDrop((DependencyObject)m.Source, _key.Path, DragDropEffects.Link);

                        break;
                }
                

            }));

            SelectedItemChanged += Tree_SelectedItemChanged;

        }

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

            if (SelectedValue is KeyItem)
                _key = (KeyItem)SelectedValue;
                        
        }

        private KeyItem _key = null;
        public KeyItem Key
        {
            get
            {
                return _key;
            }
        }

        private Window _window = null;
        public Window Window
        {
            get
            {
                if(_window == null)
                {

                    _window = new Window()
                    {
                        Template = (ControlTemplate)FindResource("WindowTemplate"),
                        WindowStyle = WindowStyle.None,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = this,
                        ShowInTaskbar = false
                    };

                    _window.Closing += Window_Closing;

                }
                return _window;
            }            
        }

        public bool Next()
        {
            return false;
        }

        public bool Previous()
        {
            return false;
        }

        public void Reload()
        {            
            ((KeyItem)rootItem.DataContext).Reload();
        }

        private KeyItem GetDataContext(RoutedEventArgs e)
        {

            FrameworkElement item = null;
            KeyItem key = null;

            try
            {
                item = (FrameworkElement)e.OriginalSource;
            }
            catch (Exception) { }

            if (item == null) return null;

            try
            {
                key = (KeyItem)item.DataContext;
            }
            catch (Exception) { }

            if (key == null) return null;

            return key;
        }

        private void Reload(object sender, RoutedEventArgs e)
        {
            GetDataContext(e).Reload();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            ((Window)sender).Hide();
        }

        private void Ok_Button_Click(object sender, RoutedEventArgs e)
        {
            Window.Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            _key = null;
            Window.Close();
        }
        
    }
}
