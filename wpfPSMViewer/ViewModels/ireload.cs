using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSMViewer.ViewModels
{
    public interface IReload
    {
        void Reload();
        bool Next();
        bool Previous();
    }

}
