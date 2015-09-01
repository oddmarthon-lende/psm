
/// <copyright file="iundo.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Undo stuff</summary>
/// 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace PSMViewer
{

    public interface IUndo
    {
        void PopState();
        void PushState();
    }


    public static class UndoExtension
    {

        private static Dictionary<FrameworkElement, Stack<MemoryStream>> States = new Dictionary<FrameworkElement, Stack<MemoryStream>>();

        private static Stack<IUndo> UndoStack = new Stack<IUndo>();

        public static int Count
        {
            get
            {
                return UndoStack.Count;
            }
        }

        public static Control PopState(this Control context, Func<DependencyProperty, bool> ShouldDeSerializeProperty = null, bool restore = true)
        {

            MemoryStream stream = null;
            Control    snapshot = null;

            try {

                stream = States[context].Pop();

                if (UndoStack.Peek() == context)
                    UndoStack.Pop();
            }
            catch(Exception) { }

            if (stream != null)
            {

                stream.Seek(0, SeekOrigin.Begin);

                snapshot = (Control)System.Windows.Markup.XamlReader.Load(stream);

                if (!restore)
                    return snapshot;

                LocalValueEnumerator enumerator = context.GetLocalValueEnumerator();

                while (enumerator.MoveNext())
                {

                    LocalValueEntry e = enumerator.Current;
                    PropertyInfo p = context.GetType().GetProperty(e.Property.Name);

                    if (p != null)
                    {

                        DesignerSerializationVisibilityAttribute d = p.GetCustomAttribute<DesignerSerializationVisibilityAttribute>();
                        
                        if (!e.Property.ReadOnly && (d == null || d.Visibility == DesignerSerializationVisibility.Visible) && ShouldDeSerializeProperty(e.Property))
                        {

                            object value = snapshot.GetValue(e.Property);
                            context.SetValue(e.Property, value);

                        }

                    }

                }

                foreach (PropertyDescriptor p in TypeDescriptor.GetProperties(context, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.Valid) }))
                {
                    DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(p);
                    
                    if(dpd != null)
                    { }
                    else if (!p.IsReadOnly && p.SerializationVisibility != DesignerSerializationVisibility.Hidden)
                    {
                        
                        object value = p.GetValue(snapshot);
                        p.SetValue(context, p.GetValue(snapshot));

                    }
                }
                                
            }

            return snapshot;
        }

        public static void PushState(this FrameworkElement context)
        {

            MemoryStream stream = new MemoryStream();

            MainWindow.Export(context, stream);

            if(!States.ContainsKey(context))
                States.Add(context, new Stack<MemoryStream>());
            
            States[context].Push(stream);

            try {
                UndoStack.Push((IUndo)context);
            }
            catch(InvalidCastException) { }

        }

        public static bool Undo()
        {

            try {

                UndoStack.Pop().PopState();

            }
            catch(Exception)
            {
                return false;
            }

            return true;
        }
    }
}
