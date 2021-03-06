﻿/// <copyright file="tree.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the KeyItem\Key tree</summary>
/// 

using PSM.Viewer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PSM.Viewer
{
    /// <summary>
    /// Treeview of the PSM Store keys
    /// </summary>
    public partial class Tree : TreeView, IReload, INotifyPropertyChanged
    {
        public delegate void KeyEvent(Tree sender, KeyItem key, MouseEventArgs args);
        public event KeyEvent KeyRightClick;

        /// <summary>
        /// The currently selected key
        /// </summary>
        public KeyItem Key
        {
            get { return (KeyItem)GetValue(KeyProperty); }
            private set { SetValue(KeyProperty, value); }
        }
        /// <summary>
        /// Identifiees the Tree.Key property
        /// </summary>
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register("Key", typeof(KeyItem), typeof(Tree), new PropertyMetadata(null));


        private Window _window = null;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<TField>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// Gets a window containing the treeview
        /// </summary>
        public Window Window
        {
            get
            {
                if (_window == null)
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

        private ReloadStatus _status = ReloadStatus.Idle;
        /// <summary>
        /// IReload.Status
        /// </summary>
        public ReloadStatus Status
        {
            get {
                return _status;
            }
            set {
                SetField(ref _status, value);
            }
        }

        
        /// <summary>
        /// IReload.Cancel
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// The default contructor
        /// </summary>
        public Tree()
        {
            
            InitializeComponent();

            AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(Reload));

            AddHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler(Reload));

            AddHandler(TreeViewItem.PreviewMouseMoveEvent, new RoutedEventHandler((sender, e) =>
            {

                MouseEventArgs m = (MouseEventArgs)e;

                if (Key == null) return;

                switch(m.LeftButton)
                {

                    case MouseButtonState.Pressed:
                                                
                        DragDrop.DoDragDrop((DependencyObject)m.Source, Key.Path, DragDropEffects.Link);

                        break;
                }
                

            }));

            AddHandler(TreeViewItem.MouseRightButtonUpEvent, new RoutedEventHandler((sender, e) =>
            {

                MouseEventArgs m = (MouseEventArgs)e;

                KeyItem key = null;

                try
                {
                    key = (KeyItem)((TextBlock)m.OriginalSource).DataContext;
                }
                catch(Exception)
                {

                }

                if(key != null)
                {
                    if (KeyRightClick != null)
                        KeyRightClick(this, key, m);
                }
                
            }));

            SelectedItemChanged += Tree_SelectedItemChanged;

        }
        
        /// <summary>
        /// IReload.Next
        /// </summary>
        /// <returns><c>false</c></returns>
        public bool Next()
        {
            return false;
        }

        /// <summary>
        /// IReload.Previous (required by the interface)
        /// </summary>
        /// <returns><c>false</c></returns>
        public bool Previous()
        {
            return false;
        }        

        /// <summary>
        /// IReload.Reload
        /// Reloads the root item
        /// </summary>
        public void Reload()
        {
            rootItem.IsExpanded = false;
            ((KeyItem)rootItem.DataContext).Reload();
            rootItem.IsExpanded = true;
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

        #region EventHandlers

        private void Reload(object sender, RoutedEventArgs e)
        {
            
            KeyItem key = GetDataContext(e);

            key.Forward(this);
            this.OnReload(key);
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
            Key = null;
            Window.Close();
        }

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

            if (SelectedValue is KeyItem)
                Key = (KeyItem)SelectedValue;

        }

        #endregion

    }
}
