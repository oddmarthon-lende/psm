/// <copyright file="keyitemvariable.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using PSM.Stores;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;

namespace PSM.Viewer.Models
{
    /// <summary>
    /// 
    /// </summary>
    public partial class KeyItem : Key, IReload, INotifyPropertyChanged, IKeyItem
    {

        /// <summary>
        /// Key path variable
        /// </summary>
        public class Variable : INotifyPropertyChanged, IReload, IDisposable
        {

            /// <summary>
            /// The name of the variable
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// The parent <see cref="KeyItem"/>
            /// </summary>
            public KeyItem Parent { get; private set; }

            /// <summary>
            /// The variable position in its path
            /// </summary>
            public int Position { get; private set; }

            /// <summary>
            /// The index of the value to get from the <see cref="Keys"/> 
            /// </summary>
            private int _index = -1;

            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
            /// </summary>
            /// <param name="propertyName"></param>
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }

            /// <summary>
            /// Conveniance method
            /// </summary>
            protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = "")
            {
                if (EqualityComparer<TField>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(propertyName);

                return true;
            }

            /// <summary>
            /// The selected index in the <see cref="Keys"/>
            /// </summary>
            public int SelectedIndex
            {

                get
                {
                    return _index;
                }

                set
                {

                    if(SetField(ref _index, Math.Max(Math.Min(value, Keys.Count - 1), 0)))
                    {
                        OnPropertyChanged("Value");
                    }

                    foreach (KeyValuePair<object, ObservableCollection<Variable>> p in _variables_global)
                    {
                        ObservableCollection<Variable> vars = p.Value;

                        if (vars.Contains(this))
                        {

                            foreach (Variable v in vars)
                            {

                                if (v.Position > this.Position)
                                {
                                    v.Reload();
                                }

                            }

                            break;
                        }
                    }

                }

            }

            /// <summary>
            /// The selected key
            /// </summary>
            public string Value
            {

                get
                {

                    int i = 0;

                    foreach (Key k in Keys)
                    {
                        if (i++ == SelectedIndex)
                            return k.Name;
                    }

                    if (SelectedIndex > 0)
                    {
                        SelectedIndex = 0;
                        return Value;
                    }

                    return null;
                }

                set
                {

                    int i = 0;

                    foreach (Key k in Keys)
                    {
                        if (k.Name == value)
                        {

                            SelectedIndex = i;
                            break;
                        }

                        i++;
                    }
                }

            }

            /// <summary>
            /// The possible values the variable can hold;
            /// </summary>
            public ObservableCollection<Key> Keys { get; private set; } = new ObservableCollection<Key>();

            public Dispatcher Dispatcher
            {
                get
                {
                    return Dispatcher.CurrentDispatcher;
                }
            }

            public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

            public ReloadStatus Status { get; set; } = ReloadStatus.Idle;

            /// <summary>
            /// Constructor
            /// </summary>
            public Variable() { }

            /// <summary>
            /// <see cref="object.ToString"/>
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return Name;
            }

            /// <summary>
            /// Parse and create variables from a path
            /// </summary>
            /// <param name="path">The path to parse</param>
            /// <returns>The variables found in the path</returns>
            public static IEnumerable<Variable> Parse(string path)
            {

                List<Variable> variables = new List<Variable>();
                List<string> components = new List<string>(path.Split('.'));

                Variable variable;

                foreach (Match match in Regex.Matches(path, @"\$.+?\b"))
                {

                    string parent = match.Index > 0 ? path.Substring(0, match.Index - 1) : null;
                    string name = match.Value.TrimStart('$').TrimEnd('.');

                    variable = new Variable();

                    foreach (Match m1 in Regex.Matches(match.Value, String.Format(@"(?<=:)\d+", name)))
                    {
                        if (m1.Success)
                        {
                            variable.SelectedIndex = System.Convert.ToInt32(m1.Value);

                            foreach (Match m2 in Regex.Matches(name, String.Format(@"(.+?)\:\d+", name)))
                            {
                                if (m2.Success)
                                {
                                    name = m2.Value.Split(':')[0];
                                }
                            }
                        }
                    }

                    variable.Name = name;
                    variable.Position = components.IndexOf(match.Value.TrimEnd('.'));
                    variable.Parent = parent == null ? null : KeyItem.Create(parent);

                    variables.Add(variable);
                }

                return variables;
            }

            public void Reload()
            {

                
                string parent = Parent == null ? "" : Parent.Path;
                int i = 0;
                int count = Keys.Count;
                bool changed = false;

                foreach (Key k in (Cache.ContainsKey(parent) ? Cache[parent] : Store.Get(Dispatcher).Keys(parent)))
                {

                    if(i < count)
                    {

                        if (Keys[i] != k)
                        {
                            Keys[i] = k;
                            changed = i == SelectedIndex;
                        }
                    }
                    else
                    {
                        Keys.Add(k);
                    }

                    i++;

                }

                count = Keys.Count;

                changed = count > i;

                for (; count > i; count--)
                {
                    Keys.RemoveAt(count - 1);
                }

                if (changed)
                {
                    OnPropertyChanged("Value");
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

            public void Dispose()
            {
                if(PropertyChanged != null)
                    foreach(var e in PropertyChanged.GetInvocationList())
                    {
                        PropertyChanged -= (PropertyChangedEventHandler)e;
                    }

                if(Parent != null)
                    Parent.Dispose();
            }
        }

    }
        
}
