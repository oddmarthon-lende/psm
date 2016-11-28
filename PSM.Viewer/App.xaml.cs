using PSM.Viewer.Models;
using System.ComponentModel;
using System.Windows;

namespace PSM.Viewer
{
    public partial class App : Application
    {

        public App()
        {
            TypeDescriptor.AddProvider(new KeyItem.KeyItemTypeDescriptionProvider(), typeof(KeyItem));
        }
        
    }
}
