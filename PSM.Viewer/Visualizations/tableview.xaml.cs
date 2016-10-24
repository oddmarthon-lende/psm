/// <copyright file="tableview.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System.Collections.Generic;
using PSM.Viewer.Models;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using PSM.Viewer.Dialogs;
using System;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid;
using PSM.Viewer.Commands;

namespace PSM.Viewer.Visualizations
{
    /// <summary>
    /// 
    /// </summary>
    public class Column
    {

        public Dictionary<KeyItem, Row> Rows { get; private set; }

        private IList<Cell> _cells;

        public KeyItem Key { get; private set; }
        
        public string Header
        {
            get
            {
                return Key == null ? null : Key.Title.Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Cell> Cells
        {
            get
            {
                if (Rows == null)
                    return null;

                return Rows.Select((k, i) =>
                {
                    foreach(Cell c in _cells)
                    {
                        if (c.Column == this && c.Row == k.Value)
                            return c;
                    }

                    return new Cell(null, null, null);
                });

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="rows"></param>
        /// <param name="cells"></param>
        public Column(KeyItem key, Dictionary<KeyItem, Row> rows, IList<Cell> cells)
        {
            Key = key;
            Rows = rows;
            _cells = cells;

        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Row
    {
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<KeyItem, Column> Columns { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        private IList<Cell> _cells;

        /// <summary>
        /// 
        /// </summary>
        public KeyItem Key { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public string Header
        {
            get
            {
                return Key == null ? null : Key.Title.Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Cell> Cells
        {
            get
            {

                return Columns.Select((k, i) =>
                {

                    foreach(Cell c in _cells)
                    {
                        if (c.Row == this && c.Column == k.Value)
                            return c;
                    }

                    return new Cell(null, null, null);

                });

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="columns"></param>
        /// <param name="cells"></param>
        public Row(KeyItem key, Dictionary<KeyItem, Column> columns, IList<Cell> cells)
        {
            Key = key;
            Columns = columns;
            _cells = cells;

        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Cell : INotifyPropertyChanged
    {
        /// <summary>
        /// 
        /// </summary>
        public MultiControl Control { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 
        /// </summary>
        public Column Column { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Row Row { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public EntryItem Entry
        {
            get
            {
                return Control == null || Control.Entries.Count == 0 ? null : Control.Entries[0];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private object _value;

        /// <summary>
        /// 
        /// </summary>
        public object Value
        {
            get
            {
                return _value ?? (Entry == null ? null : Entry.Value is DateTime ? ((DateTime)Entry.Value).ToString("o") : Control.Key.Convert<string>((Entry)Entry));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public SolidColorBrush Brush
        {
            get
            {
                return Control == null ? Row == null ? Brushes.White : Row.Key.Brush : Control.Key.Brush;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="col"></param>
        /// <param name="row"></param>
        public Cell(MultiControl control, Column col, Row row)
        {

            Control = control;
            Column = col;
            Row = row;

            if(Control != null)
                Control.DataChanged += _control_DataChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        private void _control_DataChanged(object sender)
        {
            OnPropertyChanged("Value");
            OnPropertyChanged("Entry");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(object value)
        {
            _value = value;
        }
    }

    [Visible(true)]
    [DisplayName("Table View")]
    [Icon("../icons/table.png")]
    [SubCategory("Built-In")]
    public sealed partial class TableView : TableBase
    {

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<KeyItem, Column> _columns = new Dictionary<KeyItem, Column>();

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<KeyItem, Row> _rows = new Dictionary<KeyItem, Row>();

        /// <summary>
        /// 
        /// </summary>
        private List<Cell> _cells = new List<Cell>();

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Column> Columns
        {
            get
            {
                List<Cell> row_header_cells = new List<Cell>();
                List<Column> list = _columns.Select((k, i) =>
                {
                    return k.Value;
                }).ToList();

                Column column = new Column(null, _rows, row_header_cells);

                foreach (KeyValuePair<KeyItem, Row> k in _rows)
                {
                    Cell cell = new Cell(null, column, k.Value);
                    cell.SetValue(k.Value.Header);
                    row_header_cells.Add(cell);

                }

                list.Insert(0, column);

                return list;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Row> Rows
        {
            get
            {
                return _rows.Select((k, i) =>
                {
                    return k.Value;
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private List<KeyItem> _rowKeyCache = new List<KeyItem>();

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, KeyItem> _columnKeyCache = new Dictionary<string, KeyItem>();

        /// <summary>
        /// 
        /// </summary>
        public KeyItemPathList RowPaths
        {
            get
            {
                KeyItemPathList list = new KeyItemPathList();

                foreach(Row r in Rows)
                {
                    list.Add(new KeyItemPath(r.Key));
                }

                return list;
            }

            set
            {

                _rowKeyCache.Clear();

                foreach (KeyItemPath p in value)
                {

                    KeyItem k = p.ToKeyItem();
                    _rowKeyCache.Add(k);
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public KeyItemPathList ColumnPaths
        {
            get
            {
                KeyItemPathList list = new KeyItemPathList();

                foreach (Column c in Columns.Where((c) => { return c.Key != null; }))
                {
                    list.Add(new KeyItemPath(c.Key));
                }

                return list;
            }

            set
            {

                _columnKeyCache.Clear();

                foreach (KeyItemPath p in value)
                {

                    KeyItem k = p.ToKeyItem();
                    _columnKeyCache.Add(p.Path, k);
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int SplitPosition
        {
            get { return (int)GetValue(SplitPositionProperty); }
            set { SetValue(SplitPositionProperty, value); }
        }
        public static readonly DependencyProperty SplitPositionProperty =
            DependencyProperty.Register("SplitPosition", typeof(int), typeof(TableView), new PropertyMetadata(1), (value) => {

                if((int)value >= 1)
                    return true;

                return false;
            });

        /// <summary>
        /// Constructor
        /// </summary>
        public TableView()
        {
            InitializeComponent();

            RegisterUserCommand();
            RegisterUserCommand("Row Headers", new RelayCommand(ExecuteCommand, canExecute, CommandType.EDIT_ROWS));
            RegisterUserCommand("Column Headers", new RelayCommand(ExecuteCommand, canExecute, CommandType.EDIT_COL));

            Properties.Add(new PropertyDefinition()
            {
                Category = "Table View",
                TargetProperties = new List<object>(new string[] { "SplitPosition" })
            });

            Foreground = Brushes.White;

        }

        /// <summary>
        /// 
        /// </summary>
        private new enum CommandType
        {
            EDIT_ROWS,
            EDIT_COL
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="parameter"></param>
        protected override void ExecuteCommand(object sender, object parameter)
        {

            RelayCommand cmd = (RelayCommand)sender;
            KeyEditor window = null;

            switch ((CommandType)cmd.Arguments[0].Value)
            {
                case CommandType.EDIT_COL:

                    
                    window = new KeyEditor(Columns.Where((c) => { return c.Key != null; }).Select((c, i) =>
                    {
                        return new KeyEditor.KeyEditorItem(c.Key);
                    }).ToArray());

                    window.Title = String.Format("Edit Column Headers [{0}]", Title);

                    break;

                case CommandType.EDIT_ROWS:
                    
                    window = new KeyEditor(Rows.Select((r, i) =>
                    {
                        return new KeyEditor.KeyEditorItem(r.Key);
                    }).ToArray());

                    window.Title = String.Format("Edit Row Headers [{0}]", Title);

                    break;
            }

            switch ((CommandType)cmd.Arguments[0].Value)
            {

                case CommandType.EDIT_COL:
                case CommandType.EDIT_ROWS:
                                        
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Owner = this.Owner;
                    window.Width = Math.Sqrt(this.Owner.Width * this.Owner.Height);
                    window.Height = window.Width;
                    window.CanAdd = false;
                    window.CanDelete = false;
                    window.TreeVisibility = Visibility.Collapsed;

                    window.ShowDialog();

                    this.OnReload(this);

                    break;
            }

            base.ExecuteCommand(sender, parameter);
        }

        /// <summary>
        /// Refreshed the control
        /// </summary>
        public override void Refresh()
        {
            
            _rows.Clear();
            _columns.Clear();
            _cells.Clear();

            foreach (MultiControl control in Controls)
            {

                Path p = Path.Extract(control.Key.Path);

                int count = p.Count();
                int pos = Math.Min(Math.Max(0, count), SplitPosition);

                KeyItem r = KeyItem.Create(String.Join(".", p.Components.ToArray(), 0, pos));
                KeyItem c = KeyItem.Create(String.Join(".", p.Components.ToArray(), pos, count - pos));

                r.Color = Colors.LightGray;
                c.Color = Colors.LightGray;

                string s = c.Path;
                                

                if (_rowKeyCache.Contains(r))
                    r = _rowKeyCache.Find((k) => { return k == r; });
                else
                    _rowKeyCache.Add(r);

                if (!_rows.ContainsKey(r))
                {
                    _rows.Add(r, new Row(r, _columns, _cells));
                }

                if (_columnKeyCache.ContainsKey(c.Path))
                    c = _columnKeyCache[c.Path];
                else
                    _columnKeyCache.Add(c.Path, c);

                if (!_columns.ContainsKey(c))
                {
                    _columns.Add(c, new Column(c, _rows, _cells));
                }

                _cells.Add(new Cell(control, _columns[c], _rows[r]));                

            }
            
            OnPropertyChanged("Columns");
            OnPropertyChanged("Rows");

            base.Refresh();

        }
    }
}
