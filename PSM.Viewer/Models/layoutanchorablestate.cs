/// <copyright file="layoutanchorablestate.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary></summary>
/// 

using Xceed.Wpf.AvalonDock.Layout;

namespace PSM.Viewer.Models
{

    /// <summary>
    /// Used for serialization of Layout state
    /// </summary>
    public class LayoutAnchorableSavedState
    {
        public string ContentId { get; set; } = null;
        public bool IsFloating { get; set; } = false;
        public bool IsHidden { get; set; } = false;
        public bool IsAutoHidden { get; set; } = false;
        public double FloatingHeight { get; set; } = 0D;
        public double FloatingWidth { get; set; } = 0D;
        public double FloatingLeft { get; set; } = 0D;
        public double FloatingTop { get; set; } = 0D;
        public double AutoHideWidth { get; set; } = 200D;

        public LayoutAnchorableSavedState() { }

        public LayoutAnchorableSavedState(LayoutAnchorable layout)
        {
            AutoHideWidth = layout.AutoHideWidth;
            ContentId = layout.ContentId;
            IsFloating = layout.IsFloating;
            IsHidden = layout.IsHidden;
            IsAutoHidden = layout.IsAutoHidden;
            FloatingHeight = layout.FloatingHeight;
            FloatingWidth = layout.FloatingWidth;
            FloatingLeft = layout.FloatingLeft;
            FloatingTop = layout.FloatingTop;

        }

        public void Restore(LayoutAnchorable layout)
        {

            layout.AutoHideWidth  = AutoHideWidth;
            layout.FloatingHeight = FloatingHeight;
            layout.FloatingWidth  = FloatingWidth;
            layout.FloatingLeft   = FloatingLeft;
            layout.FloatingTop    = FloatingTop;
            
            if (IsFloating)
                layout.Float();
            else if (IsAutoHidden)
                layout.ToggleAutoHide();
            else if (IsHidden)
                layout.Hide();
            
        }
    }

}
