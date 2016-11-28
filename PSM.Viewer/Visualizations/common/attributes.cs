using System;
using System.Collections;
using System.Collections.Generic;

namespace PSM.Viewer.Visualizations
{
    /// <summary>
    /// Used to specify the name that will displayed in the UI.
    /// </summary>
    class DisplayNameAttribute : Attribute
    {
        private string _name;

        public DisplayNameAttribute(string name)
        {
            _name = name;
        }

        public static explicit operator string(DisplayNameAttribute d)
        {
            return d == null ? "" : d._name;
        }

        public override bool IsDefaultAttribute()
        {
            return false;
        }

    }

    /// <summary>
    /// Used to specify the icon that will displayed for the class
    /// </summary>
    class IconAttribute : Attribute
    {
        private string _path;

        public IconAttribute(string path)
        {
            _path = path;
        }

        public static explicit operator string(IconAttribute i)
        {
            return i == null ? "" : i._path;
        }

        public override bool IsDefaultAttribute()
        {
            return false;
        }
    }

    /// <summary>
    /// Mark classes with this attribute to hide in the UI
    /// </summary>
    class VisibleAttribute : Attribute
    {
        private bool _visible = false;

        public VisibleAttribute(bool Visible)
        {
            _visible = Visible;
        }

        public static explicit operator bool(VisibleAttribute v)
        {
            return v == null ? true : v._visible;
        }

        public override bool IsDefaultAttribute()
        {
            return false;
        }

    }

    /// <summary>
    /// Used to attach a widget to a category or subcategories in the menu
    /// </summary>
    class SubCategoryAttribute : Attribute, IEnumerable<string>
    {

        private List<string> _categories;

        public SubCategoryAttribute(params string[] categories)
        {
            _categories = new List<string>(categories);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _categories.GetEnumerator();
        }

        public override bool IsDefaultAttribute()
        {
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _categories.GetEnumerator();
        }
    }
}
