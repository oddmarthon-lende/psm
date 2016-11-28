/// <copyright file="logview.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
namespace PSM.Viewer.Visualizations
{

    [Visible(true)]
    [DisplayName("Log View")]
    [Icon("../icons/table.png")]
    [SubCategory("Built-In")]
    public sealed partial class LogView : TableBase
    {
                
        public LogView()
        {
            InitializeComponent();
        }
        
    }
}
