using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSMonitor.Stores;
using System.Collections.ObjectModel;
using PSMonitor;
using PSMViewer.ViewModels;

namespace PSMViewer.Models
{
    
    public class KeyItem : Key, IReload
    {
        
        private KeyItem _Parent = null;
        public KeyItem Parent { get { return _Parent; } }

        private ObservableCollection<KeyItem> _Children = new ObservableCollection<KeyItem>();
        public ObservableCollection<KeyItem> Children
        {

            get
            {
                return _Children;
            }

        }
        
        private string _path = null;
        public string Path
        {
            get
            {

                if (_path == null)
                {

                    KeyItem parent = this.Parent;
                    List<string> names = new List<string>();

                    names.Add(this.Name);

                    while (parent != null && parent.Name != null)
                    {
                        names.Add(parent.Name);
                        parent = parent.Parent;
                    }

                    names.Reverse();

                    _path = string.Join<string>(".", names);

                }

                return _path ?? this.Name;
            }
        }

        public KeyItem() : base(null, null) { }
        public KeyItem(Key key) : base(key.Name, key.Type) { }
        public KeyItem(string key, Type type) : base(key, type) { }

        public void Reload()
        {

            _Children.Clear();

            foreach (Key k in PSM.Store.GetKeys(Path))
            {
                _Children.Add(new KeyItem(k) { _Parent = this });
            }

        }

        public bool Next()
        {
            throw new NotImplementedException();
        }

        public bool Previous()
        {
            throw new NotImplementedException();
        }
    }
}
