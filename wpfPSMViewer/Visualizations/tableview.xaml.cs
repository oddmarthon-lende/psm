using System.Collections.Generic;
using PSMViewer.ViewModels;
using PSMViewer.Models;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using PSMViewer.Dialogs;
using System;
using System.Windows.Media;

namespace PSMViewer.Visualizations
{
   
    public class Column
    {

        public Dictionary<KeyItem, Row> Rows { get; private set; }

        private IList<Cell> _cells;

        public KeyItem Key
        {
            get
            {
                return _header.Key;
            }
        }

        private KeyItemTitle _header;
        public string Header
        {
            get
            {
                return _header != null ? _header.Value : null;
            }
        }

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

        public Column(KeyItemTitle header, Dictionary<KeyItem, Row> rows, IList<Cell> cells)
        {
            _header = header;
            Rows = rows;
            _cells = cells;

        }
    }

    public class Row
    {

        public Dictionary<KeyItemTitle, Column> Columns { get; private set; }

        private IList<Cell> _cells;

        public KeyItem Key
        {
            get
            {
                return _header;
            }
        }

        private KeyItem _header;
        public string Header
        {
            get
            {
                return _header.Title.Value;
            }
        }

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

        public Row(KeyItem header, Dictionary<KeyItemTitle, Column> columns, IList<Cell> cells)
        {
            _header = header;
            Columns = columns;
            _cells = cells;

        }
    }

    public class Cell : INotifyPropertyChanged
    {

        public MultiControl Control { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public Column Column { get; private set; }

        public Row Row { get; private set; }

        private object _value;
        public object Value
        {
            get
            {
                return _value ?? (Control == null || Control.Entries.Count == 0 ? null : Control.Entries[0].Value is DateTime ? ((DateTime)Control.Entries[0].Value).ToString("o") : Control.Key.Convert<string>((PSMonitor.Entry)Control.Entries[0]));
            }
        }

        public SolidColorBrush Brush
        {
            get
            {
                return Control == null ? Row == null ? Brushes.White : Row.Key.Brush : Control.Key.Brush;
            }
        }

        public Cell(MultiControl control, Column col, Row row)
        {

            Control = control;
            Column = col;
            Row = row;

            if(Control != null)
                Control.DataChanged += _control_DataChanged;
        }

        private void _control_DataChanged(object sender)
        {
            OnPropertyChanged("Value");
        }

        public void SetValue(object value)
        {
            _value = value;
        }
    }

    [Visible(true)]
    [DisplayName("Table View")]
    [Icon("../icons/table.png")]
    [SubCategory("Built-In")]
    public sealed partial class TableView : VisualizationControl
    {

        private Dictionary<KeyItemTitle, Column> _columns = new Dictionary<KeyItemTitle, Column>();

        private Dictionary<KeyItem, Row> _rows = new Dictionary<KeyItem, Row>();

        private List<Cell> _cells = new List<Cell>();

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

        private List<KeyItem> _rowKeysCache = new List<KeyItem>();

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

                _rowKeysCache.Clear();

                foreach (KeyItemPath p in value)
                {

                    KeyItem k = KeyItemPath.ToKeyItem(p);
                    _rowKeysCache.Add(k);
                }

            }
        }

        public TableView()
        {
            InitializeComponent();

            RegisterUserCommand();
            RegisterUserCommand("Edit Rows", new RelayCommand(ExecuteCommand, canExecute, CommandType.EDIT_ROWS));

        }

        private new enum CommandType
        {
            EDIT_ROWS
        }

        protected override void ExecuteCommand(object sender, object parameter)
        {

            RelayCommand cmd = (RelayCommand)sender;

            switch ((CommandType)cmd.Arguments[0].Value)
            {
                case CommandType.EDIT_ROWS:

                    {

                        KeyEditor window;

                        window = new KeyEditor(Rows.Select((r, i) =>
                        {
                            return new KeyEditor.Item(r.Key);
                        }).ToArray());

                        window.Title = String.Format("Edit Rows [{0}]", Title);
                        window.WindowStyle = WindowStyle.ToolWindow;
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        window.Owner = this.Owner;
                        window.Width = Math.Sqrt(this.Owner.Width * this.Owner.Height);
                        window.Height = window.Width;
                        window.CanAdd = false;
                        window.CanDelete = false;
                        window.TreeVisibility = Visibility.Collapsed;

                        window.Closed += (a, b) => Refresh();
                        window.ShowDialog();
                    }

                    break;
            }

            base.ExecuteCommand(sender, parameter);
        }

        public override void Refresh()
        {
            
            _rows.Clear();
            _columns.Clear();
            _cells.Clear();

            foreach (MultiControl control in Controls)
            {

                KeyItem parent = control.Key.Parent != null ? control.Key.Parent : null;

                if (_rowKeysCache.Contains(parent))
                    parent = _rowKeysCache.Find((k) => { return k == parent; });

                if (!_rows.ContainsKey(parent))
                {
                    _rows.Add(parent, new Row(parent, _columns, _cells));
                }

                if (!_columns.ContainsKey(control.Key.Title))
                {
                    _columns.Add(control.Key.Title, new Column(control.Key.Title, _rows, _cells));
                }

                _cells.Add(new Cell(control, _columns[control.Key.Title], _rows[parent]));                

            }
            
            OnPropertyChanged("Columns");
            OnPropertyChanged("Rows");

            base.Refresh();

        }
    }
}
